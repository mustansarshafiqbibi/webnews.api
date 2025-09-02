using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebNews.Service.Controllers;
using WebNews.Service.Models;
using WebNews.Service.Services;
using Xunit;

namespace WebNews.Test
{
    public class WebNewsControllerTests
    {
        private readonly Mock<ILogger<WebNewsController>> _loggerMock;
        private readonly Mock<IHackerNewsService> _hackerNewsServiceMock;
        private readonly WebNewsController _controller;

        public WebNewsControllerTests()
        {
            _loggerMock = new Mock<ILogger<WebNewsController>>();
            _hackerNewsServiceMock = new Mock<IHackerNewsService>();
            _controller = new WebNewsController(_loggerMock.Object, _hackerNewsServiceMock.Object);
        }

        [Fact]
        public async Task Get_ReturnsStories_WhenServiceReturnsStories()
        {
            // Arrange
            var stories = new List<NewsStory>
            {
                new NewsStory { Id = 1, Title = "Story 1", Url = "http://story1" },
                new NewsStory { Id = 2, Title = "Story 2", Url = "http://story2" }
            };
            var newsResponse = new NewsResponse
            {
                TotalStories = 500,
                NewsStories = stories
            };
            _hackerNewsServiceMock
                .Setup(s => s.GetNewestStoriesAsync(1, 20, null))
                .ReturnsAsync(newsResponse);

            // Act
            var result = await _controller.Get(1, 20, null);

            // Assert
            var okResult = Assert.IsType<ActionResult<NewsResponse>>(result);
            Assert.Equal(newsResponse, okResult.Value);
        }

        [Fact]
        public async Task Get_ReturnsInternalServerError_WhenServiceThrowsException()
        {
            // Arrange
            _hackerNewsServiceMock
                .Setup(s => s.GetNewestStoriesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new System.Exception("Service error"));

            // Act
            var result = await _controller.Get(1, 20, null);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal("An error occurred while retrieving news stories.", objectResult.Value);
        }
    }
}