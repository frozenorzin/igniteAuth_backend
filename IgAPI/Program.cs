using System.Diagnostics;
using IgAPI.Logging;
using IgniteAuth.Interfaces;
using IgniteAuth.Processors;
using IgniteAuth.Processors.Validation;
using IgniteAuth.Results;
using IgniteAuth.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IgniteAuth.Data;
using IgAPI.Services;
using IgAPI.Services.Interfaces;
using IgAPI.Analytics;

EnvLoader.LoadFromEnvFile();
var builder = WebApplication.CreateBuilder(args);


// This Program.cs must do thing thing

var intentSecret = Environment.GetEnvironmentVariable("IGNITE_AUTH_SECRET")
    ?? throw new InvalidOperationException(
        "IGNITE_AUTH_SECRET is required. Add it to the solution .env file or environment.");
var intentSecretKey = Encoding.UTF8.GetBytes(intentSecret);
if (intentSecretKey.Length == 0)
    throw new InvalidOperationException("IGNITE_AUTH_SECRET cannot be empty.");

var policyDataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");

builder.Services.AddScoped<ControlPlaneDecision>();
builder.Services.AddScoped<IControlPlaneProcessor, ControlPlaneProcessor>();
builder.Services.AddSingleton<ICpcIdGenerator, CpcIdGenerator>();
builder.Services.AddSingleton<IDecisionAnalyticsStore>(serviceProvider =>
    new CsvDecisionAnalyticsStore(
        builder.Environment.ContentRootPath,
        serviceProvider.GetRequiredService<ILogger<CsvDecisionAnalyticsStore>>()));

builder.Services.AddScoped<IIntentValidator>(serviceProvider => new IntentValidator(
    serviceProvider.GetRequiredService<IntentHashes>(), intentSecretKey));
builder.Services.AddScoped<ICommandValidator, CommandValidator>();
builder.Services.AddScoped<IIntentCommandMapper>(serviceProvider => new IntentCommandMapper(
    serviceProvider.GetRequiredService<IntentCommandMapping>(), intentSecretKey));

// Load and validate eagerly: stale hashes or inconsistent policy data must not
// turn a valid request into a mysterious denial at runtime.
var displayIntents = IntentHashes.LoadFromJson(Path.Combine(policyDataDirectory, "IntentHashes.json"));
var enforcedIntents = IntentHashes.LoadFromJson(Path.Combine(policyDataDirectory, "IntentHashes.hashed.json"));
var commands = CommandData.LoadFromJson(Path.Combine(policyDataDirectory, "CommandData.json"));
var displayMappings = IntentCommandMapping.LoadFromJson(Path.Combine(policyDataDirectory, "IntentCommandMapping.json"));
var enforcedMappings = IntentCommandMapping.LoadFromJson(Path.Combine(policyDataDirectory, "IntentCommandMapping.hashed.json"));
PolicyCatalogValidator.Validate(displayIntents, enforcedIntents, commands, displayMappings, enforcedMappings, intentSecretKey);
builder.Services.AddSingleton(enforcedIntents);
builder.Services.AddSingleton(commands);
builder.Services.AddSingleton(enforcedMappings);
builder.Services.AddSingleton<IIgniteAuthReferenceDataService>(
    new IgniteAuthReferenceDataService(displayIntents, commands, displayMappings));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
if (jwtKey == "ThisIsADevOnlyJwtKey_ChangeInProduction_1234567890")
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Configure a unique Jwt:Key outside development.");
    // Never sign tokens with the public sample key. Development tokens expire
    // on restart unless a private Jwt:Key is configured.
    jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    builder.Configuration["Jwt:Key"] = jwtKey;
}

builder.Services.AddControllers();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "IgAPI",

            ValidateAudience = true,
            ValidAudience = "IgAPI.Client",

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                return context.Response.WriteAsJsonAsync(new
                {
                    message = "Unauthorized User"
                });
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin", "User")
        .Build();
});

var app = builder.Build();
var responseLog = new ResponseLogWriter(app.Environment.ContentRootPath, app.Logger);

app.Use(async (context, next) =>
{
    var startedAt = Stopwatch.GetTimestamp();
    string? exceptionType = null;
    try
    {
        await next(context);
    }
    catch (Exception exception)
    {
        exceptionType = exception.GetType().Name;
        throw;
    }
    finally
    {
        var statusCode = exceptionType is not null && !context.Response.HasStarted
            ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;
        await responseLog.WriteAsync(context, startedAt, statusCode, exceptionType);
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
