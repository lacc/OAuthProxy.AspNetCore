using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OAuthProxy.AspNetCore.Apis;
using OAuthProxy.AspNetCore.Demo;
using OAuthProxy.AspNetCore.Demo.Apis;
using OAuthProxy.AspNetCore.Demo.Services;
using OAuthProxy.AspNetCore.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThirdPartyOAuthProxy(builder.Configuration, proxyBuilder => proxyBuilder
    .WithTokenStorageOptions(options =>
    {
        options.AutoMigration = true; // Enable automatic migration for the database
        options.DatabaseOptions = dbOptions =>
        {
            var sqlLiteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection");
            if (!string.IsNullOrEmpty(sqlLiteConnectionString))
            {
                dbOptions.UseSqlite(sqlLiteConnectionString, 
                    b => b.MigrationsAssembly("OAuthProxy.AspNetCore"));
            }
            else
            {
                dbOptions.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            }
        };
    })
    .AddOAuthServiceClient<ThirdPartyClientA>("ServiceA", proxyClientBuilder => proxyClientBuilder
        .WithAuthorizationCodeFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceA")))
    .AddOAuthServiceClient<ThirdPartyClientA>("ServiceB", proxyClientBuilder => proxyClientBuilder
        .WithAuthorizationCodeFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceB"), builder =>
        {
            builder.ConfigureTokenExchanger<DummyCodeExchanger>();
        }))
);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes.Add("BasicAuth", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "basic",
            Description = "Basic Authentication for Scalar API"
        });
        
        return Task.CompletedTask;
    });
});

//Add basic dummy authentication handler
builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);
builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder("Basic").RequireAuthenticatedUser().Build());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes("BasicAuth")
        .AddHttpAuthentication("BasicAuth", auth =>
        {
            auth.Username = "test";
            auth.Password = "test";
        })
        .WithPersistentAuthentication()
    );
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapProxyClientEndpoints()
   .MapServiceAClientEndpoints();

app.Run();

