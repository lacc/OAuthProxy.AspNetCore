using System.Collections.Generic;
using OAuthProxy.AspNetCore.Apis;
using Xunit;

namespace OAuthProxy.AspNetCore.Tests
{
    public class OAuthAuthorizationCodeFlowApiMapper_WhitelistTests
    {
        [Theory]
        [InlineData(new string[0], "https://any.url/allowed", true)] // Empty whitelist allows all
        [InlineData(new[] { "https://allowed.com/callback" }, "https://allowed.com/callback", true)]
        [InlineData(new[] { "https://allowed.com/callback" }, "https://notallowed.com/callback", false)]
        [InlineData(new[] { "https://allowed.com/*" }, "https://allowed.com/callback", true)]
        [InlineData(new[] { "https://allowed.com/*" }, "https://allowed.com/other", true)]
        [InlineData(new[] { "https://allowed.com/*" }, "https://allowed.com/", true)]
        [InlineData(new[] { "https://allowed.com/*" }, "https://notallowed.com/callback", false)]
        [InlineData(new[] { "https://allowed.com/callback", "https://other.com/*" }, "https://other.com/abc", true)]
        [InlineData(new[] { "https://allowed.com/callback", "https://other.com/*" }, "https://allowed.com/callback", true)]
        [InlineData(new[] { "https://allowed.com/callback", "https://other.com/*" }, "https://notallowed.com/", false)]
        [InlineData(new[] { "https://allowed.com/callback*" }, "https://allowed.com/callback", true)]
        [InlineData(new[] { "https://allowed.com/callback*" }, "https://allowed.com/callbackExtra", true)]
        [InlineData(new[] { "https://allowed.com/callback*" }, "https://allowed.com/call", false)]
        [InlineData(new[] { "https://allowed.com/callback" }, "https://allowed.com/callback/", false)]
        public void IsUrlWhitelisted_WorksAsExpected(string[] whitelist, string redirectUrl, bool expected)
        {
            var result = InvokeIsUrlWhitelisted(whitelist, redirectUrl);
            Assert.Equal(expected, result);
        }

        private static bool InvokeIsUrlWhitelisted(IEnumerable<string> whitelist, string redirectUrl)
        {
            // Use reflection to call the private static method
            var method = typeof(OAuthAuthorizationCodeFlowApiMapper)
                .GetMethod("IsUrlWhitelisted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var res = method?.Invoke(null, [whitelist, redirectUrl]);
            if (res is bool result)
            {
                return result;
            }

            return false;
        }
    }
}