using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebNews.Service.Models;

namespace WebNews.Service.Services
{
    public class HackerNewsService : IHackerNewsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<HackerNewsService> _logger;
        private readonly IConfiguration _configuration;
        private const string NewStoryCacheKey = "NewestStories";
        private const int CacheDurationMinutes = 5;
        private const string _newStoriesUrl = null;

        public HackerNewsService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<HackerNewsService> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
            _logger = logger;
            _configuration = configuration;
        }
        public async Task<List<NewsStory>> GetNewestStoriesAsync(int page, int pageSize, string? search = null)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                var newStoryIds = await GetNewStoryIdsAsync(httpClient);

                var pageIds = newStoryIds.Skip((page - 1) * pageSize).Take(pageSize);

                var stories = new List<NewsStory>();

                foreach (var storyId in pageIds)
                {
                    var story = await GetStoryAsync(httpClient, storyId);

                    if (story != null && (string.IsNullOrEmpty(search) || (story.Title?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)))
                    {
                        stories.Add(story);
                    }
                }

                return stories;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Error occurred in {nameof(GetNewestStoriesAsync)} (page: {page}, pageSize: {pageSize}, search: {search})");
                return [];
            }
        }

        private async Task<NewsStory?> GetStoryAsync(HttpClient client, int storyId)
        {

            try
            {
                var singleStoryUrl = _configuration.GetSection("HackerNews:SingleStoryUrl").Value;

                var url = string.Format(singleStoryUrl, storyId);

                var response = await client.GetStringAsync(url).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<NewsStory>(response);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetStoryAsync (storyId: {StoryId})", storyId);
                return null;
            }
        }

        private async Task<List<int>> GetNewStoryIdsAsync(HttpClient client)
        {

            try
            {
                if (!_memoryCache.TryGetValue(NewStoryCacheKey, out List<int> storyIds))
                {
                    var newStoriesUrl = _configuration.GetSection("HackerNews:NewStoriesUrl").Value;

                    var response = await client.GetStringAsync(newStoriesUrl).ConfigureAwait(false);
                    storyIds = JsonConvert.DeserializeObject<List<int>>(response);

                    _memoryCache.Set(NewStoryCacheKey, storyIds, TimeSpan.FromMinutes(CacheDurationMinutes));
                }

                return storyIds;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occurred in GetNewStoryIdsAsync");
                return [];
            }
        }
    }
}
