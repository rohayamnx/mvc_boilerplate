using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Logging;
using mvc_boilerplate.Models;
using mvc_boilerplate.Models.UI;

namespace mvc_boilerplate.Services
{
    public interface IADAuthenticationService
    {
        Task<bool> ValidateCredentialsAsync(string username, string password);
        Task<string?> GetAdUserEmailAsync(string username);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ADAuthenticationService : IADAuthenticationService
    {
        private readonly ADSettings _adSettings;
        private readonly ILogger<ADAuthenticationService> _logger;

        public ADAuthenticationService(IConfiguration configuration, ILogger<ADAuthenticationService> logger)
        {
            _adSettings = configuration.GetSection("ActiveDirectory").Get<ADSettings>()
                ?? throw new ArgumentException("ActiveDirectory settings are not configured");
            _logger = logger;
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var context = new PrincipalContext(ContextType.Domain, _adSettings.Domain);
                    return context.ValidateCredentials(username, password);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating AD credentials for user: {Username}", username);
                return false;
            }
        }

        public async Task<string?> GetAdUserEmailAsync(string username)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var context = new PrincipalContext(ContextType.Domain, _adSettings.Domain);
                    var adUser = UserPrincipal.FindByIdentity(context, username);
                    return adUser?.EmailAddress;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving AD email for user: {Username}", username);
                return null;
            }
        }
    }
}