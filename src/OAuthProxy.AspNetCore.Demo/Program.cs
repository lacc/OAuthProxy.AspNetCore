using OAuthProxy.AspNetCore.Apis;
using OAuthProxy.AspNetCore.Demo.Services;
using OAuthProxy.AspNetCore.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThirdPartyOAuthProxy(builder.Configuration);
builder.Services.AddThirdPartyServiceClient<ThirdPartyClientA>("ServiceA");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

await app.EnsureOAuthProxyDb();
app.MapOAuthEndpoints();
app.MapProxyEndpoints();

app.Run();

