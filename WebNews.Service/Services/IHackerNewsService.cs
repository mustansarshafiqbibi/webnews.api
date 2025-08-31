using WebNews.Service.Models;

namespace WebNews.Service.Services
{
    public interface IHackerNewsService
    {
        Task<List<NewsStory>> GetNewestStoriesAsync(int page, int pageSize, string? search = null);
    }
}
