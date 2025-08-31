using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using WebNews.Service.Models;
using WebNews.Service.Services;
using Xunit;

namespace WebNews.Test
{
    public class HackerNewsServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly Mock<ILogger<HackerNewsService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly HackerNewsService _service;

        public HackerNewsServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _memoryCacheMock = new Mock<IMemoryCache>();
            _loggerMock = new Mock<ILogger<HackerNewsService>>();
            _configurationMock = new Mock<IConfiguration>();

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    if (request.RequestUri!.ToString().Contains("newstories"))
                    {
                        var ids = JsonConvert.SerializeObject(new List<int> { 1, 2 });
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(ids)
                        };
                    }
                    else if (request.RequestUri!.ToString().Contains("item"))
                    {
                        // Extract the story ID from the URL
                        var idStr = request.RequestUri!.Segments.Last().Replace(".json", "");
                        int id = int.TryParse(idStr, out var parsedId) ? parsedId : 1;
                        var story = JsonConvert.SerializeObject(new NewsStory { Id = id, Title = "Test Story", Url = "http://test" });
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(story)
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.NotFound
                        };
                    }
                });

            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Setup cache to always miss and set
            object cacheValue = null;
            _memoryCacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
            
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

            var newStoriesSectionMock = new Mock<IConfigurationSection>();
            newStoriesSectionMock.Setup(s => s.Value).Returns("https://hacker-news.firebaseio.com/v0/newstories.json");
            _configurationMock.Setup(c => c.GetSection("HackerNews:NewStoriesUrl")).Returns(newStoriesSectionMock.Object);

            var singleStorySectionMock = new Mock<IConfigurationSection>();
            singleStorySectionMock.Setup(s => s.Value).Returns("https://hacker-news.firebaseio.com/v0/item/{0}.json");
            _configurationMock.Setup(c => c.GetSection("HackerNews:SingleStoryUrl")).Returns(singleStorySectionMock.Object);

            _service = new HackerNewsService(_httpClientFactoryMock.Object, _memoryCacheMock.Object, _loggerMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task GetNewestStoriesAsync_ReturnsStories()
        {
            var result = await _service.GetNewestStoriesAsync(1, 2);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Equal("Test Story", s.Title));
        }

        [Fact]
        public async Task GetNewestStoriesAsync_FiltersBySearch()
        {
            var result = await _service.GetNewestStoriesAsync(1, 2, "Test");

            Assert.All(result, s => Assert.Contains("Test", s.Title));
        }

        [Fact]
        public async Task GetNewestStoriesAsync_ReturnsEmptyOnException()
        {
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Throws(new Exception("Http error"));

            var result = await _service.GetNewestStoriesAsync(1, 2);

            Assert.Empty(result);
            _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        }
    }
}