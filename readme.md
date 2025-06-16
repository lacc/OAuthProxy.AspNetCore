# OAuthProxy
A modular, extensible ASP.NET Core solution for building secure OAuth proxy APIs with support for multiple third-party providers, token storage, and state validation.

# Features
- OAuth Proxy API: Easily proxy OAuth flows for multiple third-party services.
- Token Storage: Securely store and refresh access/refresh tokens using EF Core.
- State Management: Prevent CSRF attacks with robust state generation and validation.
- Pluggable User Identity: Use JWT or custom user ID providers.
- Extensible: Add new OAuth providers and flows with minimal code.
- Demo Project: Example ASP.NET Core app with basic authentication and OpenAPI integration.
- Comprehensive Tests: Unit tests for all core components.

# Getting Started
1. Clone the Repository
```sh 
git clone https://github.com/laccg/OAuthProxy.git 
cd OAuthProxy/src
```

2. Build the Solution
```sh
dotnet build 
```

3. Run the Demo Project
```sh 
cd OAuthProxy.AspNetCore.Demo dotnet run
```

The demo app will start on https://localhost:7135 (or as configured).

# Usage
## Add OAuth Proxy to Your ASP.NET Core App
In your Program.cs:

```csharp 
builder.Services.AddThirdPartyOAuthProxy(builder.Configuration, proxyBuilder => proxyBuilder 
  .WithTokenStorageOptions(options => 
  { 
    options.AutoMigration = true; 
    options.DatabaseOptions = dbOptions => dbOptions 
      .UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")); 
   }) 
  .AddOAuthServiceClient<YourClient>("ServiceA", clientBuilder => clientBuilder
    .WithAuthorizationCodeFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceA"));) 
);
```

## Map Proxy Endpoints
```csharp 
app.MapProxyClientEndpoints(); 
```

## Configure Authentication (Demo)
The demo uses basic authentication for demonstration:

```csharp 
builder.Services.AddAuthentication("Basic") .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);
```

## Database Migrations
EF Core is used for token and state storage. Migrations are in `OAuthProxy.AspNetCore.Data/Migrations`.

### To update the database:

```sh 
dotnet ef database update --project OAuthProxy.AspNetCore.Data
```

# Testing
Run all unit tests:

```sh 
dotnet test
```

# Extending
- Add appsettings configuration
- Add a new OAuth provider: Use `.AddOAuthServiceClient<TClient>("ProviderName", ...)` in your service configuration.
- Custom user ID provider: Use `.WithUserIdProvider<T>()` on the proxy builder.
- Custom token exchangers: Implement `IOAuthAuthorizationTokenExchanger` or `IOAuthAuthorizationRefreshTokenExchanger`.


# License
MIT License

# Contributing
Pull requests and issues are welcome! Please see CONTRIBUTING.md if available.

# Acknowledgements
Built with ASP.NET Core, EF Core, and Moq for testing.
