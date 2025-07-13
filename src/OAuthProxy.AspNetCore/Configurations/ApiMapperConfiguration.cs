
namespace OAuthProxy.AspNetCore.Configurations
{
    public class ApiMapperConfiguration
    {
        /// <summary>
        /// Gets or sets the prefix used for proxy URLs.
        /// The prefix will be followed by {serviceProviderName} to form the complete URL.
        /// </summary>
        public string ProxyUrlPrefix { get; set; } = "/api/proxy";

        /// <summary>
        /// Gets or sets the name of the query parameter used to specify the final redirect URL after the authorization flow.
        /// </summary>
        public string AuthorizeRedirectUrlParameterName { get; set; } = "local_redirect_uri";
        /// <summary>
        /// Gets or sets the list of URLs that are allowed for redirection.
        /// </summary>
        public IList<string> WhiteListedRedirectUrls { get; set; } = [];
        /// <summary>
        /// Gets or sets a value indicating whether the generic API should be mapped which allows proxying any endpoint
        /// </summary>
        public bool MapGenericApi { get; set; } = true;
    }
}