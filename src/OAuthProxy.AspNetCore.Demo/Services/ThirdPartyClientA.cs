namespace OAuthProxy.AspNetCore.Demo.Services
{
    public class ThirdPartyClientA
    {
        private readonly HttpClient _httpClient;

        public ThirdPartyClientA([FromKeyedServices("ServiceA")] HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CallSomeMethod()
        {
            var endpoint = "relative_path_to_base_url";
            var response = await _httpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();
            else
                return string.Empty;
        }
    }
}
