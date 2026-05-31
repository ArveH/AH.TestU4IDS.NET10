using AH.TestU4IDS.NET10.Web;
using AH.TestU4IDS.NET10.Web.Components;
using AH.TestU4IDS.NET10.Web.Middleware;
using AH.TestU4IDS.NET10.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

var oidcSettings = builder.Configuration
    .GetSection(OpenIdConnectSettings.SectionName)
    .Get<OpenIdConnectSettings>() ?? new OpenIdConnectSettings();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = oidcSettings.Authority;
        options.ClientId = oidcSettings.ClientId;
        options.ClientSecret = oidcSettings.ClientSecret;
        options.ResponseType = "code";

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Clear();
        foreach (var scope in oidcSettings.Scopes)
        {
            options.Scope.Add(scope);
        }

        // Map the name claim so User.Identity.Name resolves from the id token.
        options.TokenValidationParameters.NameClaimType = "name";

        options.Events.OnRedirectToIdentityProvider = context =>
        {
            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
            {
               context.ProtocolMessage.AcrValues = $"tenant:{oidcSettings.Tenant}";
                if (!string.IsNullOrWhiteSpace(oidcSettings.IdpName))
                {
                    context.ProtocolMessage.AcrValues += $" loginidp:{oidcSettings.IdpName}";
                }
            }
            return Task.CompletedTask;
        };

        options.Events.OnAuthenticationFailed = context =>
        {
            var errorMessage = context.Exception?.Message ?? "An error occurred during authentication.";

            context.HandleResponse();
            // Carry the error in the query string so it survives the redirect into a new
            // request/DI scope and renders under static server-side rendering.
            context.Response.Redirect($"/?authError={Uri.EscapeDataString($"Login failed: {errorMessage}")}");
            return Task.CompletedTask;
        };

        options.Events.OnRemoteFailure = context =>
        {
            var errorMessage = context.Failure?.Message
                ?? context.Request.Query["error_description"].ToString()
                ?? context.Request.Query["error"].ToString();
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                errorMessage = "An error occurred during authentication.";
            }

            context.HandleResponse();
            // Carry the error in the query string so it survives the redirect into a new
            // request/DI scope and renders under static server-side rendering.
            context.Response.Redirect($"/?authError={Uri.EscapeDataString($"Login failed: {errorMessage}")}");
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Add HTTP context accessor for token retrieval in services
builder.Services.AddHttpContextAccessor();

// Add error service for global error handling
builder.Services.AddScoped<ErrorService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        client.BaseAddress = new("https://parentapi");
    });

var app = builder.Build();

// Add custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Endpoints used by the UI to trigger the OIDC login/logout challenges.
app.MapGet("/authentication/login", (string? returnUrl) =>
    Results.Challenge(new AuthenticationProperties
    {
        RedirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
    }));

app.MapPost("/authentication/logout", () =>
    Results.SignOut(new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .RequireAuthorization();

app.MapDefaultEndpoints();

await app.RunAsync();
