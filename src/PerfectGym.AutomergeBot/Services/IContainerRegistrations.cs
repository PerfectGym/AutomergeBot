using Microsoft.Extensions.DependencyInjection;

namespace PerfectGym.AutomergeBot.Services
{
    public interface IContainerRegistrations
    {
        void DoRegistrations(IServiceCollection services);
    }
}