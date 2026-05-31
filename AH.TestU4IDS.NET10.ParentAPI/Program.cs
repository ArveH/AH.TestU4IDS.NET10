using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

// Add authentication
var audience = builder.Configuration["JwtSettings:Audience"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["JwtSettings:Authority"];
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = audience,
        };
    });

// Add authorization policy with scope and user claim checks
var requiredScope = builder.Configuration["JwtSettings:RequiredScope"];
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("UserTrailPolicy", policy =>
        policy.RequireAssertion(context =>
        {
            var serviceProvider = context.Resource switch
            {
                HttpContext httpContext => httpContext.RequestServices,
                AuthorizationFilterContext filterContext => filterContext.HttpContext.RequestServices,
                _ => null
            };
            var logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger("Authorization.UserTrailPolicy");

            if (string.IsNullOrWhiteSpace(requiredScope))
            {
                logger?.LogWarning("UserTrailPolicy denied: required scope is not configured.");
                return false;
            }

            var hasScope = context.User.FindAll("scope")
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains(requiredScope, StringComparer.Ordinal);

            var unit4Id = context.User.FindFirst("unit4_id")?.Value;
            var sub = context.User.FindFirst("sub")?.Value;

            logger?.LogInformation(
                "UserTrailPolicy audit trail: unit4_id={Unit4Id}, sub={Sub}, hasScope={HasScope}",
                unit4Id,
                sub,
                hasScope);

            return hasScope
                && !string.IsNullOrWhiteSpace(unit4Id)
                && !string.IsNullOrWhiteSpace(sub);
        }));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("WeatherApi", client =>
{
    client.BaseAddress = new("https://weatherapi");
});

var app = builder.Build();

app.MapHealthChecks("/health");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseHsts();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
