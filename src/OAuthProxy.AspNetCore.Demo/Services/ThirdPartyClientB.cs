namespace OAuthProxy.AspNetCore.Demo.Services
{
    public class ThirdPartyClientB
    {
        public const string ServiceKey = "ServiceB";
        private readonly HttpClient _httpClient;

        public ThirdPartyClientB([FromKeyedServices("ServiceB")] HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string CallSomeMethod()
        {
            // Simulate a call to API
            return "ServiceB API called successfully!";
        }
    }
}
