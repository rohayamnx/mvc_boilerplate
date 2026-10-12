using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using mvc_boilerplate.Models.Dto;

namespace mvc_boilerplate.Services
{
    public interface IStaffHubApiService
    {
        Task<StaffDto?> GetStaffByIdAsync(string staffId);
        Task<List<StaffDto>> SearchStaffAsync(string searchTerm);
        Task<List<StaffDto>> GetAllStaffAsync(bool includeInactive = false);
    }

    public class StaffHubApiService : IStaffHubApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StaffHubApiService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public StaffHubApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<StaffHubApiService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;

            var baseUrl = configuration["StaffHubApi:BaseUrl"] ?? "";
            var apiKey = configuration["StaffHubApi:ApiKey"] ?? "";

            var cacheMinutes = configuration.GetValue<int>("CacheSettings:StaffHubApi:DurationMinutes", 10);
            _cacheDuration = TimeSpan.FromMinutes(cacheMinutes);

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("StaffHub API base URL is not configured");
            }

            _httpClient.BaseAddress = new Uri(baseUrl);

            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            }

            var origin = configuration["App:BaseUrl"];
            if (!string.IsNullOrEmpty(origin))
            {
                _httpClient.DefaultRequestHeaders.Add("Origin", origin);
            }
        }

        public async Task<StaffDto?> GetStaffByIdAsync(string staffId)
        {
            var cacheKey = $"StaffHub_Staff_{staffId}";
            if (_cache.TryGetValue(cacheKey, out StaffDto? cachedStaff))
            {
                return cachedStaff;
            }

            try
            {
                var response = await _httpClient.GetAsync($"/api/staff/{staffId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var staff = JsonSerializer.Deserialize<StaffDto>(json, _jsonOptions);

                    if (staff != null)
                    {
                        _cache.Set(cacheKey, staff, _cacheDuration);
                    }

                    return staff;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Staff with ID {StaffId} not found in StaffHub", staffId);
                    return null;
                }
                else
                {
                    _logger.LogError("Failed to get staff {StaffId}. Status: {StatusCode}", staffId, response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff details for ID: {StaffId}", staffId);
                return null;
            }
        }

        public async Task<List<StaffDto>> SearchStaffAsync(string searchTerm)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/staff/search?searchTerm={Uri.EscapeDataString(searchTerm)}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<StaffDto>>(json, _jsonOptions);
                    return list ?? new List<StaffDto>();
                }

                _logger.LogError("Failed to search staff '{Term}'. Status: {StatusCode}", searchTerm, response.StatusCode);
                return new List<StaffDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching staff with term: {SearchTerm}", searchTerm);
                return new List<StaffDto>();
            }
        }

        public async Task<List<StaffDto>> GetAllStaffAsync(bool includeInactive = false)
        {
            var cacheKey = $"StaffHub_AllStaff_{includeInactive}";
            if (_cache.TryGetValue(cacheKey, out List<StaffDto>? cachedList))
            {
                return cachedList ?? new List<StaffDto>();
            }

            try
            {
                var response = await _httpClient.GetAsync($"/api/staff?includeInactive={includeInactive.ToString().ToLowerInvariant()}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<StaffDto>>(json, _jsonOptions) ?? new List<StaffDto>();
                    _cache.Set(cacheKey, list, _cacheDuration);
                    return list;
                }

                _logger.LogError("Failed to get all staff. Status: {StatusCode}", response.StatusCode);
                return new List<StaffDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all staff from StaffHub");
                return new List<StaffDto>();
            }
        }

    }
}