using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using mvc_boilerplate.Models;
using mvc_boilerplate.Models.UI;

namespace mvc_boilerplate.Services
{
    public interface IStaffHubAuthInfoService
    {
        Task<StaffHubAuthInfo?> GetAuthInfoAsync();
    }

    public class StaffHubAuthInfoService : IStaffHubAuthInfoService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StaffHubAuthInfoService> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;

        public StaffHubAuthInfoService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<StaffHubAuthInfoService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _apiKey = configuration["StaffHubApi:ApiKey"] ?? "";

            var baseUrl = configuration["StaffHubApi:BaseUrl"] ?? "";
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("StaffHub API base URL is not configured");
            }

            _httpClient.BaseAddress = new Uri(baseUrl);

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            }
        }

        public async Task<StaffHubAuthInfo?> GetAuthInfoAsync()
        {
            var cacheKey = "StaffHub_AuthInfo";
            if (_cache.TryGetValue(cacheKey, out StaffHubAuthInfo? cached))
            {
                return cached;
            }

            try
            {
                var response = await _httpClient.GetAsync("/api/auth/auth-info");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var authInfo = JsonSerializer.Deserialize<StaffHubAuthInfo>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (authInfo != null)
                    {
                        _cache.Set(cacheKey, authInfo, TimeSpan.FromMinutes(10));
                    }

                    return authInfo;
                }

                _logger.LogWarning("Failed to get StaffHub auth info. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving StaffHub auth info");
                return null;
            }
        }
    }
}