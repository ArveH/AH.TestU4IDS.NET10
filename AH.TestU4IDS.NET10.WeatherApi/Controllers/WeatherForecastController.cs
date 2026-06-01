using AH.TestU4IDS.NET10.WeatherApi.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AH.TestU4IDS.NET10.WeatherApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = "UserTrailPolicy")]
public class WeatherForecastController(ILogger<WeatherForecastController> logger) : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> GetAsync()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogInformation("Access token claims: {ClaimsJson}", accessToken.PrettifyToken());
        }

        return [.. Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })];
    }
}
