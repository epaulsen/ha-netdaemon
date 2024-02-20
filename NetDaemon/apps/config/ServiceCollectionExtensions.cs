using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MyNetDaemon.apps.config
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigService<T, TData>(this IServiceCollection services, string filename)
            where T : YamlConfigurationBase<TData> where TData : class, new()
        {
            services.AddSingleton<T>(f =>
            {
                var logger = f.GetRequiredService<ILogger<T>>();
                var ctor = typeof(T).GetConstructors().FirstOrDefault();
                if (ctor == null)
                {
                    throw new ArgumentException($"No constructor found for {typeof(T).Name}");
                }

                var config = (T)ctor.Invoke(new object[] { logger, Path.Combine(Directory.GetCurrentDirectory(), "apps", "config", filename) });
                return config;
            });

            return services;
        }
    }
}
