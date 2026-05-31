using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace AH.TestU4IDS.NET10.ParentAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = "UserTrailPolicy")]
public class WeatherForecastController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetAsync(CancellationToken cancellationToken)
    {
        var accessToken = HttpContext.Request.Headers.Authorization
            .ToString()
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        var client = httpClientFactory.CreateClient("WeatherApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var forecasts = await client.GetFromJsonAsync<IEnumerable<WeatherForecast>>(
            "/WeatherForecast", cancellationToken);

        return Ok(forecasts);
    }
}
