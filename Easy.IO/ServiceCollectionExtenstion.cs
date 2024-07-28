using Easy.IO.Preferences;
using Microsoft.Extensions.DependencyInjection;

namespace Easy.IO
{
    public static class ServiceCollectionExtenstion
    {
        public static IServiceCollection AddEasyIO(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IPreferences), new Easy.IO.Preferences.SharePreference());
            return services;
        }

    }
}
