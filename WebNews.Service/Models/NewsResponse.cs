namespace WebNews.Service.Models
{
    public class NewsResponse
    {
        public int TotalStories { get; set; }

        public List<NewsStory> NewsStories { get; set; } = [];
    }
}
