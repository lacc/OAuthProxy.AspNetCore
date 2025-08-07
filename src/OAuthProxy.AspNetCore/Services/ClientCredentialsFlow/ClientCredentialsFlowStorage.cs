using Microsoft.EntityFrameworkCore;
using OAuthProxy.AspNetCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuthProxy.AspNetCore.Services.ClientCredentialsFlow
{
    internal class ClientCredentialsFlowStorage
    {
        private readonly TokenDbContext _dbContext;

        public ClientCredentialsFlowStorage(TokenDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CreateOrReCreate(string serviceProviderName, string userId, string clientId, string clientSecret, string scope)
        {
            var existingFlow = await _dbContext.ClientCredentialsConfigs
                .FirstOrDefaultAsync(f => f.ThirdPartyServiceProvider == serviceProviderName && f.UserId == userId);
            if (existingFlow != null)
            {
                // Update existing flow
                existingFlow.ClientId = clientId;
                existingFlow.ClientSecret = clientSecret;
                existingFlow.Scope = scope;
            }
            else
            {
                // Create new flow
                var newFlow = new ClientCredentialsConfigEntity
                {
                    ThirdPartyServiceProvider = serviceProviderName,
                    UserId = userId,
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = scope
                };
                _dbContext.ClientCredentialsConfigs.Add(newFlow);
            }
            return await _dbContext.SaveChangesAsync() <= 0; // Return true if save failed
        }
    }
}
