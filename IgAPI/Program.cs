using IgniteAuth.Interfaces;
using IgniteAuth.Processors;
using IgniteAuth.Processors.Validation;
using IgniteAuth.Results;
using IgniteAuth.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IgniteAuth.Data;

EnvLoader.LoadFromEnvFile();
var builder = WebApplication.CreateBuilder(args);

var intentSecret = Environment.GetEnvironmentVariable("IGNITE_AUTH_SECRET")
    ?? throw new InvalidOperationException(
        "IGNITE_AUTH_SECRET is required. Add it to the solution .env file or environment.");
var intentSecretKey = Encoding.UTF8.GetBytes(intentSecret);
if (intentSecretKey.Length == 0)
    throw new InvalidOperationException("IGNITE_AUTH_SECRET cannot be empty.");

var policyDataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");

builder.Services.AddScoped<ControlPlaneDecision>();
builder.Services.AddScoped<IControlPlaneProcessor, ControlPlaneProcessor>();

builder.Services.AddScoped<IIntentValidator>(serviceProvider => new IntentValidator(
    serviceProvider.GetRequiredService<IntentHashes>(), intentSecretKey));
builder.Services.AddScoped<ICommandValidator, CommandValidator>();
builder.Services.AddScoped<IIntentCommandMapper>(serviceProvider => new IntentCommandMapper(
    serviceProvider.GetRequiredService<IntentCommandMapping>(), intentSecretKey));

builder.Services.AddSingleton(_ => IntentHashes.LoadFromJson(
    Path.Combine(policyDataDirectory, "IntentHashes.hashed.json")));
builder.Services.AddSingleton(_ => CommandData.LoadFromJson(
    Path.Combine(policyDataDirectory, "CommandData.json")));
builder.Services.AddSingleton(_ => IntentCommandMapping.LoadFromJson(
    Path.Combine(policyDataDirectory, "IntentCommandMapping.hashed.json")));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");

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

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
