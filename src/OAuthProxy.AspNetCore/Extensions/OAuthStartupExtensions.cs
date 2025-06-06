using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Configurations;
using OAuthProxy.AspNetCore.Data;
using OAuthProxy.AspNetCore.Services;

namespace OAuthProxy.AspNetCore.Extensions;

public static class OAuthStartupExtensions
{
    public static IServiceCollection AddThirdPartyOAuthProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Dictionary<string, ThirdPartyServiceConfig>>(
            configuration.GetSection("ThirdPartyServices"));

        services.AddDbContext<TokenDbContext>(options =>
        {
            var sqlLiteConnectionString = configuration.GetConnectionString("SqliteConnection");
            if (!string.IsNullOrEmpty(sqlLiteConnectionString))
            {
                options.UseSqlite(sqlLiteConnectionString);
            }
            else
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }
        });

        services.AddHttpClient();
        services.AddScoped<TokenStorageService>();
        services.AddScoped<OAuthService>();
        //services.AddThirdPartyServiceClient<ServiceAClient>("ServiceA");

        services.AddSingleton<ThirdPartyServiceFactory>(serviceProvider =>
        {
            var configs = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Dictionary<string, ThirdPartyServiceConfig>>>().Value;
            return new ThirdPartyServiceFactory(serviceProvider, configs);
        });

        //services.AddControllersWithViews();
        //services.AddRazorPages();

        return services;
    }

    public static IServiceCollection AddThirdPartyServiceClient<TServiceClient>(this IServiceCollection services, string serviceProviderName)
        where TServiceClient : class, IThirdPartyService
    {
        services.AddKeyedScoped<IThirdPartyService, TServiceClient>(serviceProviderName, (serviceProvider, o) =>
        {
            var httpClient = serviceProvider.GetRequiredService<HttpClient>();

            var configs = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Dictionary<string, ThirdPartyServiceConfig>>>().Value;
            if (configs == null || configs.Count == 0)
            {
                throw new InvalidOperationException("Third-party service configurations are not available.");
            }

            var serviceConfig = configs.TryGetValue(serviceProviderName, out var config) ? config : null;
            if (serviceConfig == null)
            {
                throw new ArgumentException($"Configuration for service '{serviceProviderName}' not found.");
            }

            var logger = serviceProvider.GetRequiredService<ILogger<TServiceClient>>();
            return (TServiceClient)Activator.CreateInstance(typeof(TServiceClient), serviceProviderName, httpClient, serviceConfig, logger)!;
        });
        return services;

    }
    public static async Task EnsureOAuthProxyDb(this IApplicationBuilder app)
    {
        await using (var scope = app.ApplicationServices.CreateAsyncScope())
        await using (var dbContext = scope.ServiceProvider.GetRequiredService<TokenDbContext>())
        {
            //if (!await dbContext.Database.EnsureCreatedAsync())
            {
                await dbContext.Database.MigrateAsync();
            }
        }

    }
}
