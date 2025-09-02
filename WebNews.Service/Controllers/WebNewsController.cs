using Microsoft.AspNetCore.Mvc;
using WebNews.Service.Models;
using WebNews.Service.Services;

namespace WebNews.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class WebNewsController : ControllerBase
    {
        
        private readonly ILogger<WebNewsController> _logger;
        private readonly IHackerNewsService _hackerNewsService;

        public WebNewsController(ILogger<WebNewsController> logger, IHackerNewsService hackerNewsService)
        {
            _logger = logger;
            _hackerNewsService = hackerNewsService;
        }

        [HttpGet]
        public async Task<ActionResult<NewsResponse>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            try
            {
                var newStories = await _hackerNewsService.GetNewestStoriesAsync(page, pageSize, search);

                return newStories;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occurred in Get (page: {Page}, pageSize: {PageSize}, search: {Search})", page, pageSize, search);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving news stories.");
            }

        }
    }
}
