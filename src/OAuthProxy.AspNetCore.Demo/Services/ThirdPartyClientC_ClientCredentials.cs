namespace OAuthProxy.AspNetCore.Demo.Services
{
    public class ThirdPartyClientC_ClientCredentials
    {
        private readonly HttpClient _httpClient;

        public ThirdPartyClientC_ClientCredentials([FromKeyedServices("ThirdPartyClientC_ClientCredentials")] HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string CallSomeMethod()
        {
            // Simulate a call to API
            return "ThirdPartyClientC_ClientCredentials API called successfully!";
        }
    }
}
