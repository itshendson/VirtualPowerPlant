using System.Runtime.CompilerServices;

namespace Ingestion.Extensions
{
    public static class AutofacExtensions
    {
        public static IServiceCollection AddMediator(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                //cfg.LicenseKey = "";
                cfg.AddOpenBehavior(typeof(Behaviors.CommandResultLoggingBehavior<,>));
            });

            return services;
        }
    }
}
