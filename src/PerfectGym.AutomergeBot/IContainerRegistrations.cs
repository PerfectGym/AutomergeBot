using Microsoft.Extensions.DependencyInjection;

namespace PerfectGym.AutomergeBot
{
    public interface IContainerRegistrations
    {
        void DoRegistrations(IServiceCollection services);
    }
}