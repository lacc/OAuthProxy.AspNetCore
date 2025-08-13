
namespace OAuthProxy.AspNetCore.Demo.Services
{
    public class ThirdPartyClientC_ClientCredentials
    {
        public const string ServiceKey = "ServiceC";
        private readonly HttpClient _httpClient;

        public ThirdPartyClientC_ClientCredentials([FromKeyedServices(ServiceKey)] HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string CallSomeMethod()
        {
            // Simulate a call to API
            return "ThirdPartyClientC API called successfully!";
        }
    }
}
