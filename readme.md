# OAuthProxy.AspNetCore

[![.NET](https://img.shields.io/badge/.NET-9.0%2B-blue)](https://dotnet.microsoft.com/)
[![Build, Test and Publish](https://github.com/lacc/OAuthProxy/actions/workflows/cicd_publish_nuget.yaml/badge.svg)](https://github.com/lacc/OAuthProxy/actions/workflows/cicd_publish_nuget.yaml)
[![CodeQL](https://github.com/lacc/OAuthProxy/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lacc/OAuthProxy/actions/workflows/github-code-scanning/codeql)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](#license)

**OAuthProxy.AspNetCore** is a modular, extensible ASP.NET Core library that simplifies OAuth flows for third-party services by acting as a secure proxy. It handles authorization, token storage, and state management, making it ideal for backend APIs that need to interact with multiple external OAuth providers without exposing sensitive credentials in client applications. It uses Entity Framework Core for token storage and supports any EF Core-compatible database provider (e.g., SQLite, SQL Server, PostgreSQL, InMemory).

## Key Benefits

- **Secure Token Management:** Stores access and refresh tokens securely using Entity Framework Core.
- **CSRF Protection:** Built-in state management for secure authorization flows using the dotnet DataProtection API.
- **Pluggable Identity:** Supports JWT, custom user ID providers, or other identity mechanisms.
- **Extensible Architecture:** Easily add new OAuth providers with minimal configuration.
- **Server-Side Secrets:** Keeps OAuth client secrets safe on the server, away from client applications.
- **Demo & Tests Included:** Comes with a demo project and comprehensive unit tests.

> **Note:** Supports the **Authorization Code flow** and the **Client Credentials flow**. Future flows may be added.

---

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage](#usage)
- [Extending the Library](#extending-the-library)
- [Database Migrations](#database-migrations)
- [Testing](#testing)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgements](#acknowledgements)

---

## Features

- **OAuth Proxy API:** Proxies authorization flows, keeping secrets server-side.
- **Secure Token Storage:** Uses EF Core for token refresh and storage.
- **CSRF-Safe State Management:** Prevents CSRF attacks with state validation.
- **Pluggable User Identity:** Integrates with JWT or custom identity providers.
- **Minimal Boilerplate:** Add new OAuth providers with ease.
- **Demo Project:** Includes a working ASP.NET Core app with OpenAPI support.
- **Comprehensive Tests:** Ensures reliability with unit tests.
- **Authorization Code Flow:** Supports user authentication with third-party services.
- **Client Credentials Flow:** Supports machine-to-machine authentication for backend services.

---

## Prerequisites

- **.NET SDK 9.0 or later**
- **Entity Framework Core** (matching your project’s version)
- **Relational Database** (e.g., SQLite, SQL Server, PostgreSQL)
- **OAuth Provider Account** (e.g., GitHub, Google) with client credentials
- **ASP.NET Core Knowledge** (minimal APIs or MVC)
- **HTTPS Endpoint** for OAuth callbacks

---

## Quick Start

Follow these steps to set up and run the demo project:

1. **Clone the Repository:**
   ```sh
   git clone https://github.com/lacc/OAuthProxy.git
   cd OAuthProxy/src
   dotnet build
   ```

2. **Configure Secrets:**
   - Set up your client ID and secret using `dotnet user-secrets` or environment variables.
   - Update `appsettings.json` with your provider’s details, e.g.:
     ```json
     "ThirdPartyServices": {
       "ServiceA": {
         "ClientId": "your-client-id",
         "ClientSecret": "your-client-secret",
         "TokenEndpoint": "https://provider.com/oauth/token",
         "AuthorizeEndpoint": "https://provider.com/oauth/authorize",
         "ApiBaseUrl": "https://api.provider.com",
         "Scopes": "read write"
       }
     }
     ```

3. **Run the Demo:**
   ```sh
   cd OAuthProxy.AspNetCore.Demo
   dotnet run
   ```  
   > **Note:** The default configuration uses SQLite and db file with auto migration on startup.

4. **Authenticate:**
   - Start the authentication process by calling:
     ```
     https://localhost:7135/api/proxy/ServiceA/authorization
     ```

5. **Use the Proxy:**
   - After successful authentication, call any URL from your provider via the demo proxy, e.g.:
     ```
     https://localhost:7135/api/proxy/ServiceA/{some_provider_endpoint}
     ```
   > **Note:** The name `ServiceA` can be customized in the `Program.cs` file.

### Important Notes
- **Generic Proxy Behavior:** The library registers a mapper for all endpoints under the client config name (e.g., `ServiceA`). This means every request to `/api/proxy/ServiceA/*` is proxied directly to the third-party provider without validation. While this is useful for testing, it’s not ideal for production due to limited control and security risks.
- **Recommendation:** Create your own endpoints instead of relying on the generic proxy. Use the configured `HttpClient` (requested via `FromKeyedServices`) to call third-party endpoints and process the results according to your project’s needs.
- **Using `HttpClient`:** When creating custom endpoints, request the `HttpClient` with `FromKeyedServices("ServiceA")` to ensure it’s pre-configured with authentication tokens.
- **EF Core and migrations**
  - The library includes a DbContext with pre-built migrations, which are applied automatically if AutoMigration is enabled.
  - No design-time tools (e.g., Microsoft.EntityFrameworkCore.Design) are required in consuming projects.
  - The library is provider-agnostic. You must include the appropriate EF Core provider package (e.g., Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.SqlServer) in your project based on your database choice.
---

## Client Credentials Flow

The **Client Credentials flow** is supported for machine-to-machine scenarios where no user interaction is required. This is ideal for backend services or daemons that need to authenticate directly with third-party APIs.
The expiration of the Access Token is 1 year if not specified otherwise by the provider. Once the token expores there is no automated token refresh happening.

### How to Configure

1. **Update `appsettings.json`**  
   Add your service configuration under `ThirdPartyServices`:
   ```json
   {
     "ThirdPartyServices": { 
        "ServiceC": { 
          "ClientId": "your-client-id", 
          "ClientSecret": "your-client-secret", 
          "TokenEndpoint": "https://provider.com/oauth/token", 
          "ApiBaseUrl": "https://api.provider.com", 
          "Scopes": "read write" 
        } 
   }
   ```
2. **Register the Client in `Program.cs`**          
   Use `.WithClientCredentialsFlow()` when adding the service client:
   ```csharp
   builder.Services.AddThirdPartyOAuthProxy(builder.Configuration, proxyBuilder => proxyBuilder 
      .AddOAuthServiceClient<ThirdPartyClientC_ClientCredentials>("ServiceC", clientBuilder => clientBuilder
         .WithClientCredentialsFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceC")) ) ); 
   ```
3. **Inject and Use the Client**  
   Example client class:
   
### Notes

- The client credentials flow does not require user interaction or redirection.
- Tokens are securely stored and managed by the library.
- You can customize the token exchanger by using `.ConfigureTokenExchanger<T>()` on the client builder.

## Configuration

### 1. Get OAuth Credentials
- Register your app with an OAuth provider to obtain `ClientId`, `ClientSecret`, etc.
- Set the callback URL to `/api/proxy/{Name}/callback` (e.g., `/api/proxy/ServiceA/callback`).

### 2. Update `appsettings.json`
See the example in [Quick Start](#quick-start) and [Client Credentials Flow](#client-credentials-flow).

### 3. Add to Your Project
In `Program.cs`, configure OAuthProxy:

```csharp
builder.Services.AddThirdPartyOAuthProxy(builder.Configuration, proxyBuilder => proxyBuilder
  .WithTokenStorageOptions(options => 
  {
    options.AutoMigration = true;
    options.DatabaseOptions = dbOptions => dbOptions.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
  })
  .ConfigureDataProtector(builder =>
  {
      builder.SetApplicationName("OAuthProxy")
          .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")))
          .SetDefaultKeyLifetime(TimeSpan.FromDays(60));
  })
  //Optional
  .ConfigureApiMapper(config =>
    {
        config.ProxyUrlPrefix = "/api/oauth";
        config.AuthorizeRedirectUrlParameterName = "local_redirect_uri";
        config.WhitelistedRedirectUrls =
        [
            "https://localhost:5001/",
            "https://localhost:5001/someRedirectPage"
        ];
        config.MapGenericApi = true;
    })
  .AddOAuthServiceClient<GitHubClient>("ServiceA", clientBuilder => clientBuilder
    .WithAuthorizationCodeFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceA")))
  .AddOAuthServiceClient<GitHubClient>("ServiceC", clientBuilder => clientBuilder
    .WithClientCredentialsFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceC")))

);
```

---

## Usage

### 1. Create a Client Class
Define a class to use the pre-configured `HttpClient`:

```csharp
public class GitHubClient
{
    private readonly HttpClient _httpClient;

    public GitHubClient([FromKeyedServices("ServiceA")] HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetUserProfileAsync()
    {
        var response = await _httpClient.GetAsync("user");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : string.Empty;
    }
}
```

### 2. Map Endpoints
In `Program.cs`:

```csharp
app.MapProxyClientEndpoints();
app.UseAuthentication();
```

### 3. Start Authorization
- Redirect users to `/api/proxy/{Name}/authorize` (e.g., `/api/proxy/ServiceA/authorize`).
- The callback handles token exchange.

### 4. Use the API
- Call methods like `GetUserProfileAsync()` to access the third-party API.

---

## Extending the Library

- **New Provider:** Add with `.AddOAuthServiceClient<TClient>("Name", ...)`.
- **Custom Identity:** Use `.WithUserIdProvider<T>()`.
- **Custom Tokens:** Implement `IOAuthAuthorizationTokenExchanger`.
- **HTTP Client Message Handler:** Use `AddHttpMessageHandler` to customize HTTP requests.

## Configure the Library
- Storage options
  ```csharp
    proxyBuilder
      .WithTokenStorageOptions(options => 
      {
        options.AutoMigration = true;
        options.DatabaseOptions = dbOptions => dbOptions.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
      })
  ```
    - Auto migration on startup: `options.AutoMigration` (default false)
    - EF database configuration: `options.DatabaseOptions`
      You must also add the EF Core provider package for your chosen database. For example:
      - SQLite: `dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.8`
      - SQL Server: `dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.8`
      - PostgreSQL: `dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.8`


- Configure dotnet [Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction)
  ```csharp
  proxyBuilder.ConfigureDataProtector(builder =>
  {
      builder
          .SetApplicationName("OAuthProxy")
          .PersistKeysToFileSystem(new DirectoryInfo(
              Path.Combine(AppContext.BaseDirectory, "keys")))
          .SetDefaultKeyLifetime(TimeSpan.FromDays(60));
  })
  ```
- Custom User ID Provider
  ```csharp
  proxyBuilder.WithUserIdProvider<CustomUserIdProvider>()
  ```
  > **Note:** The default user id provider uses claims to determine the user id (`sub` or `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`)

- Configure API Mapper
  ```csharp
  proxyBuilder.ConfigureApiMapper(config =>
  {
      config.ProxyUrlPrefix = "/api/oauth";
      config.AuthorizeRedirectUrlParameterName = "redirect_uri";
      config.WhitelistedRedirectUrls =
      [
          "https://localhost:5001/",
          "https://localhost:5001/someRedirectPage"
      ];
      config.MapGenericApi = false;
  });
  ```
  - `ProxyUrlPrefix`: Base path for proxy endpoints (default is `/api/proxy`)
  - `AuthorizeRedirectUrlParameterName`: Query parameter for redirect URL after authorization (default is `local_redirect_uri`)
  - `WhitelistedRedirectUrls`: List of allowed redirect URLs after the authorization flow to prevent open redirects
  - `MapGenericApi`: If true, maps all endpoints under `/api/proxy/{Name}/*` to the third-party service. This is useful for quick testing but not recommended for production due to security risks.


- Configure 3rd party service
  ```csharp
    proxyBuilder.AddOAuthServiceClient<ThirdPartyClientA>("ServiceA", proxyClientBuilder => proxyClientBuilder
      .WithAuthorizationCodeFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceA")))
    
    proxyBuilder.AddOAuthServiceClient<ThirdPartyClientB>("ServiceB", proxyClientBuilder => proxyClientBuilder
      .WithAuthorizationCodeFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceB"), builder =>
      {
          builder.ConfigureTokenExchanger<DummyCodeExchanger>();
      }))
  ```
  - Optionally use `ConfigureTokenExchanger` to replace the default token exchanger service

- Extend with custom HTTP Client Message Handler
  - Create a new class for the message handler:
 
    ```csharp
    public class DummyHttpMessageHandler : DelegatingHandler
    {

        protected override async Task<HttpResponseMessage> SendAsync(
                  HttpRequestMessage request,
                  CancellationToken cancellationToken)
        {
            request.Headers.Add("X-Api-Key", "TestKey");

            return await base.SendAsync(request, cancellationToken);

        }
    }
    ```
  - Register the handler in the proxy client builder:
  
    ```csharp
      proxyClientBuilder
            .AddHttpMessageHandler<DummyHttpMessageHandler>()
            .WithAuthorizationCodeFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceB"));
    ```

## Example: Custom Endpoint with Minimal APIs
Here’s how to create a custom endpoint using ASP.NET Core minimal APIs, which offers more control than the generic proxy:

```csharp
app.MapGet("/api/serviceA/custom-endpoint", async ([FromKeyedServices("ServiceA")] HttpClient httpClient) =>
{
    var response = await httpClient.GetAsync("some_real_github_url");
    return Results.Ok(await response.Content.ReadAsStringAsync());
});
```

**Why This Is Better:**
- Validates and sanitizes inputs before proxying requests.
- Enables custom error handling and response processing.
- Improves security by limiting exposed endpoints.
- Allows for additional logic like caching or logging.

By using custom endpoints, you gain greater flexibility and security compared to the generic `/api/proxy/ServiceA/*` endpoint.

---

## Database Migrations

Update the database schema:

```sh
cd src\OAuthProxy.AspNetCore
dotnet ef database update -c TokenDbContext -s ..\OAuthProxy.AspNetCore.Demo
```

Adding new migrations:
```sh
cd src\OAuthProxy.AspNetCore
dotnet ef migrations add initial -s ..\OAuthProxy.AspNetCore.Demo
```
---

## Testing

Run the tests:

```sh
dotnet test
```

---

### Enable not secure HTTP redirects
HTTP redirects instead of HTTPS can be enabled by proxy client by setting the `AllowHttpRedirects` to true.

> **Note:** Use this with caution only for testing purposes, as it can expose sensitive data over insecure connections.
```csharp
  .AddOAuthServiceClient<ThirdPartyClientB>("ServiceB", proxyClientBuilder => 
  {
        proxyClientBuilder.AllowHttpRedirects = true;
        proxyClientBuilder
            .AddHttpMessageHandler<DummyHttpMessageHandler>()
            .WithAuthorizationCodeFlow(builder.Configuration.GetSection("ThirdPartyServices:ServiceB"));
  }
```

## Contributing

We’d love your help! 

---

## License

MIT License

---

## Acknowledgements

Powered by ASP.NET Core, EF Core, and Moq.
