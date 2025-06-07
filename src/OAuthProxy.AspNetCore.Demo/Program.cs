using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OAuthProxy.AspNetCore.Apis;
using OAuthProxy.AspNetCore.Demo.Services;
using OAuthProxy.AspNetCore.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThirdPartyOAuthProxy(builder.Configuration)
    .WithTokenStorageDatabase(options =>
    {
        var sqlLiteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection");
        if (!string.IsNullOrEmpty(sqlLiteConnectionString))
        {
            options.UseSqlite(sqlLiteConnectionString);
        }
        else
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        }
    })
    .WithOAuthServiceClient<ThirdPartyClientA>("ServiceA")
    .WithDefaultJwtUserIdProvider();


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
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder("Basic").RequireAuthenticatedUser().Build();
});

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

app.MapOAuthEndpoints();
app.MapProxyEndpoints();

app.Run();

