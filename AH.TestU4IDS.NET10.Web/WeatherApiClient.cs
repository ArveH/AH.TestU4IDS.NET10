namespace AH.TestU4IDS.NET10.Web;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

public class WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            List<WeatherForecast>? forecasts = null;

            await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
            {
                if (forecasts?.Count >= maxItems)
                {
                    break;
                }
                if (forecast is not null)
                {
                    forecasts ??= [];
                    forecasts.Add(forecast);
                }
            }

            return forecasts?.ToArray() ?? [];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled; let cancellation propagate unchanged.
            throw;
        }
        catch (TaskCanceledException ex)
        {
            // No cancellation requested means the HttpClient request timed out.
            logger.LogError(ex, "The weather API request timed out.");
            throw new WeatherApiException(
                "The weather service did not respond in time. Please try again later.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Unable to reach the weather API.");
            throw new WeatherApiException(
                "Unable to load weather data because the weather service is unavailable.", ex);
        }
    }
}

/// <summary>
/// Represents a failure that occurred while accessing the weather API.
/// The <see cref="Exception.Message"/> is safe to display to end users.
/// </summary>
public class WeatherApiException(string message, Exception innerException)
    : Exception(message, innerException);

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
