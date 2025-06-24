using Microsoft.EntityFrameworkCore;
using OAuthProxy.AspNetCore.Abstractions;
using OAuthProxy.AspNetCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuthProxy.AspNetCore.Services
{
    internal class LocalRedirectUrlProvider : ILocalRedirectUrlProvider
    {
        private readonly TokenDbContext _dbContext;

        public LocalRedirectUrlProvider(TokenDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task PersistUriAsync(string authState, string uri)
        {
            if (string.IsNullOrEmpty(authState) || string.IsNullOrEmpty(uri))
            {
                throw new ArgumentException("AuthState and URI must not be null or empty.");
            }

            var stateEntity = await _dbContext.LocalRedirectUris
                .FirstOrDefaultAsync(s => s.AuthState == authState);

            if(stateEntity != null)
            {
                throw new InvalidOperationException($"Auth state '{authState}' already exists.");
            }

            var redirectUrl = new LocalRedirectUriEntity
            {
                AuthState = authState,
                LocalRedirectUrl = uri,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.LocalRedirectUris.Add(redirectUrl);
            await _dbContext.SaveChangesAsync();
        }
        public string GetPersistedUri(string authState, bool deleteAfterGet = true)
        {
            if (string.IsNullOrEmpty(authState))
            {
                throw new ArgumentException("AuthState must not be null or empty.");
            }
            var stateEntity = _dbContext.LocalRedirectUris
                .AsNoTracking()
                .FirstOrDefault(s => s.AuthState == authState);

            if (deleteAfterGet && stateEntity != null)
            {
                _dbContext.LocalRedirectUris.Remove(stateEntity);
                _dbContext.SaveChanges();
            }

            return stateEntity?.LocalRedirectUrl ?? string.Empty;
        }

        public async Task<string> GetPersistedUriAsync(string authState, bool deleteAfterGet = true)
        {
            if (string.IsNullOrEmpty(authState))
            {
                throw new ArgumentException("AuthState must not be null or empty.");
            }
            var stateEntity = await _dbContext.LocalRedirectUris
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.AuthState == authState);
            if (stateEntity == null)
            {
                throw new InvalidOperationException($"No redirect URI found for auth state '{authState}'.");
            }

            if (deleteAfterGet)
            {
                _dbContext.LocalRedirectUris.Remove(stateEntity);
                await _dbContext.SaveChangesAsync();
            }

            return stateEntity.LocalRedirectUrl;
        }
    }
}
