using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace AH.TestU4IDS.NET10.ParentAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = "UserTrailPolicy")]
public class WeatherForecastController(
    IHttpClientFactory httpClientFactory,
    DiscoveryCache discoveryCache,
    IOptions<AgentDelegationOptions> agentDelegationOptions,
    ILogger<WeatherForecastController> logger) : ControllerBase
{
    private readonly AgentDelegationOptions _agentDelegation = agentDelegationOptions.Value;

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetAsync(CancellationToken cancellationToken)
    {
        var userAccessToken = HttpContext.Request.Headers.Authorization
            .ToString()
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        var (agentAccessToken, error) = await ExchangeTokenAsync(userAccessToken, cancellationToken);
        if (agentAccessToken is null)
        {
            return Problem(
                title: "Token exchange failed.",
                detail: error,
                statusCode: StatusCodes.Status502BadGateway);
        }

        var client = httpClientFactory.CreateClient("WeatherApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", agentAccessToken);

        try
        {
            var forecasts = await client.GetFromJsonAsync<IEnumerable<WeatherForecast>>(
                "/WeatherForecast", cancellationToken);

            return Ok(forecasts);
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or System.Text.Json.JsonException)
        {
            logger.LogError(ex, "Failed to retrieve weather forecasts from the downstream API.");
            return Problem(
                title: "Failed to retrieve weather forecasts.",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Exchanges the incoming user access token for a new access token using the
    /// custom "agent_delegation" extension grant. The token endpoint is resolved
    /// from the authority's discovery document (.well-known/openid-configuration).
    /// Returns the access token on success, or a descriptive error message on failure;
    /// it never throws so the request can fail gracefully.
    /// </summary>
    private async Task<(string? AccessToken, string? Error)> ExchangeTokenAsync(
        string userAccessToken, CancellationToken cancellationToken)
    {
        var disco = await discoveryCache.GetAsync();
        if (disco.IsError)
        {
            logger.LogError("Failed to load discovery document: {Error}", disco.Error);
            return (null, $"Failed to load discovery document: {disco.Error}");
        }

        var tokenClient = httpClientFactory.CreateClient("Authority");
        var tokenRequest = new TokenRequest
        {
            Address = disco.TokenEndpoint,
            GrantType = "agent_delegation",
            ClientId = _agentDelegation.ClientId,
            ClientSecret = _agentDelegation.ClientSecret,
            Parameters =
            {
                { "token", userAccessToken },
                { "scope", _agentDelegation.Scope },
                { "resource", _agentDelegation.Resource }
            }
        };

        TokenResponse response;
        try
        {
            response = await tokenClient.RequestTokenAsync(tokenRequest, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller actually cancelled (e.g. the client disconnected) — let it propagate.
            throw;
        }
        catch (Exception ex)
        {
            // RequestTokenAsync normally surfaces failures via response.IsError, but
            // some failures can still throw. This also covers resilience timeouts
            // (Polly TimeoutRejectedException / TaskCanceledException that the caller
            // did not request) and unreachable/slow token endpoints.
            logger.LogError(ex, "An unexpected error occurred while requesting the agent token.");
            return (null, $"Token exchange failed: {ex.Message}");
        }

        if (response.IsError)
        {
            // Covers protocol errors as well as exceptions captured by IdentityModel
            // (response.ErrorType == ResponseErrorType.Exception, response.Exception).
            logger.LogError(
                response.Exception,
                "Token exchange failed. ErrorType: {ErrorType}, Error: {Error}",
                response.ErrorType,
                response.Error);
            return (null, $"Token exchange failed: {response.Error}");
        }

        return string.IsNullOrEmpty(response.AccessToken)
            ? (null, "The token endpoint response did not contain an access token.")
            : (response.AccessToken, null);
    }
}
