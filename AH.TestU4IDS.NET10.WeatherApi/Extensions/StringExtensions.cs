using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace AH.TestU4IDS.NET10.WeatherApi.Extensions;

internal static class StringExtensions
{
    internal static string PrettifyToken(this string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claims = jwt.Claims
            .GroupBy(claim => claim.Type)
            .ToDictionary(
                group => group.Key,
                group => group.Count() == 1
                    ? ParseClaimValue(group.First().Value)
                    : group.Select(claim => ParseClaimValue(claim.Value)).ToArray());

        return JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object ParseClaimValue(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && (value.StartsWith('{') || value.StartsWith('[')))
        {
            try
            {
                return JsonDocument.Parse(value).RootElement.Clone();
            }
            catch (JsonException)
            {
                // Intentionally ignored: return the original value if parsing fails
            }
        }

        return value;
    }
}
