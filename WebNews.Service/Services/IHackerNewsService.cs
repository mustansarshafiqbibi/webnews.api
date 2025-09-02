using WebNews.Service.Models;

namespace WebNews.Service.Services
{
    public interface IHackerNewsService
    {
        Task<NewsResponse> GetNewestStoriesAsync(int page, int pageSize, string? search = null);
    }
}
