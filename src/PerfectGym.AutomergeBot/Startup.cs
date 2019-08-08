using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PerfectGym.AutomergeBot.Features.CodeReviewInspector;
using PerfectGym.AutomergeBot.Features.MergingBranches;
using PerfectGym.AutomergeBot.Features.PullRequestsManualMergingGovernor;
using PerfectGym.AutomergeBot.Notifications.UserNotifications;
using PerfectGym.AutomergeBot.RepositoryConnection;
using Serilog;
using ContainerRegistrations = PerfectGym.AutomergeBot.Notifications.SlackClient.ContainerRegistrations;


namespace PerfectGym.AutomergeBot
{
    public class Startup : StartupBase
    {
        private readonly IConfiguration _configuration;

        public Startup(IHostingEnvironment hostingEnvironment, IConfiguration configuration)
        {
            _configuration = BuildConfiguration(hostingEnvironment);
            Logging.EnsureLoggingInitialized(hostingEnvironment.ContentRootPath);
        }

        private IConfiguration BuildConfiguration(IHostingEnvironment hostingEnvironment)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("slackUserMappings.json", true, true);

            return builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => loggingBuilder
                .ClearProviders()
                .SetMinimumLevel(LogLevel.Trace)
                .AddSerilog(dispose: true));
            services.Configure<AutomergeBotConfiguration>(_configuration);
            new ContainerRegistrations(_configuration).DoRegistrations(services);


            services.AddTransient<IRepositoryConnectionProvider, RepositoryConnectionProvider>();
            services.AddTransient<IGitHubEventHttpRequestHandler, GitHubEventHttpRequestHandler>();
            new Notifications.SlackNotifications.ContainerRegistrations().DoRegistrations(services);
            services.AddTransient<IUserNotifier, UserNotifier>();

            new Features.MergingBranches.ContainerRegistrations().DoRegistrations(services);
            new Features.PullRequestsManualMergingGovernor.ContainerRegistrations().DoRegistrations(services);
            new Features.TempBranchesRemoving.ContainerRegistrations().DoRegistrations(services);
            new Features.AdditionalCodeReview.ContainerRegistrations(_configuration).DoRegistrations(services);
            new Features.CodeReviewInspector.ContainerRegistrations(_configuration).DoRegistrations(services);

        }

        public override void Configure(IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Starting...");

            var env = app.ApplicationServices.GetService<IHostingEnvironment>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            InitiallyLoadMergeDirectionsFromConfig(app);

            RegisterConfigurationChangedHandler(app);
            LogConfigurationUsed(app.ApplicationServices, logger);

            StartPullRequestsGovernor(app);
            StartCodeReviewInspector(app);
            app.Run(HandleRequest);
            logger.LogInformation("Started");
        }

        private void StartCodeReviewInspector(IApplicationBuilder app)
        {
            var codeReviewInspector = app.ApplicationServices.GetRequiredService<CodeReviewInspector>();
            codeReviewInspector.StartWorker();
        }

        private void InitiallyLoadMergeDirectionsFromConfig(IApplicationBuilder app)
        {
            UpdateMergeDirectionsProviderConfiguration(app.ApplicationServices);
        }

        private static void StartPullRequestsGovernor(IApplicationBuilder app)
        {
            var pullRequestGovernor = app.ApplicationServices.GetRequiredService<PullRequestsGovernor>();
            pullRequestGovernor.StartWorker();
        }

        private static void UpdateMergeDirectionsProviderConfiguration(IServiceProvider serviceProvider)
        {
            var cfg = serviceProvider.GetRequiredService<IOptionsMonitor<AutomergeBotConfiguration>>().CurrentValue;
            var mergeDirectionsProviderConfigurator = serviceProvider.GetRequiredService<IMergeDirectionsProviderConfigurator>();

            mergeDirectionsProviderConfigurator.UpdateMergeDirections(cfg.MergeDirectionsParsed);
        }

        private static void RegisterConfigurationChangedHandler(IApplicationBuilder app)
        {
            var cfg = app.ApplicationServices.GetRequiredService<IOptionsMonitor<AutomergeBotConfiguration>>();
            cfg.OnChange((a, b) => { OnConfigurationChanged(app.ApplicationServices); });
        }

        private static void OnConfigurationChanged(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Configuration has been changed");
            UpdateMergeDirectionsProviderConfiguration(serviceProvider);
            LogConfigurationUsed(serviceProvider, logger);
        }

        private static async Task HandleRequest(HttpContext context)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Startup>>();
            using (logger.BeginScope("{TraceIdentifier}", context.TraceIdentifier))
            {
                var request = context.Request;
                logger.LogInformation("Received HTTP request. Method: {method}, Path: {path}, QueryString: {queryString}",
                    request.Method,
                    request.Path,
                    request.QueryString);

                try
                {
                    var requestHandler = context.RequestServices.GetRequiredService<IGitHubEventHttpRequestHandler>();
                    await requestHandler.ProcessRequest(context);
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "Unhandled exception occured during processing request");
                }
            }
        }

        private static void LogConfigurationUsed(IServiceProvider serviceProvider, ILogger<Startup> logger)
        {
            var cfg = serviceProvider.GetRequiredService<IOptionsMonitor<AutomergeBotConfiguration>>().CurrentValue;
            var mergeDirectionsProvider = serviceProvider.GetRequiredService<IMergeDirectionsProvider>();


            logger.LogInformation("Working with repository: {repositoryOwner}/{repositoryName}", cfg.RepositoryOwner, cfg.RepositoryName);
            logger.LogInformation("Using merge directions configuration: {mergeDirections}", mergeDirectionsProvider.Get().GetMergingConfigurationInfo());
            if ((cfg.AutomergeOnlyForAuthors ?? new List<string>()).Any())
            {
                logger.LogWarning("Automerging only pushes from authors: {automergeOnlyForAuthors}", cfg.AutomergeOnlyForAuthors);
            }
        }
    }
}