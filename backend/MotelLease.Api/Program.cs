using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;
using MotelLease.Api.Errors;
using MotelLease.Api.Extensions;
using MotelLease.Api.Jobs;
using MotelLease.Api.Notifications;
using MotelLease.Api.RateLimiting;
using MotelLease.Api.Validation;
using MotelLease.Application;
using MotelLease.Application.Common.Security;
using MotelLease.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddApiAuthentication();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddRealtimeNotifications();

const string FrontendCorsPolicy = "FrontendCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Background work is registered here and only here (docs/domain-rules.md §8).
builder.Services.AddHostedService<AppointmentExpiryJob>();
builder.Services.AddHostedService<DepositExpiryJob>();
builder.Services.AddHostedService<BillReminderJob>();
builder.Services.AddHostedService<LeaseExpiryJob>();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    // Enums travel as names in both directions. They are also stored as text
    // (CLAUDE.md, Database rules), so a new value never shifts an existing one's meaning.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Model binding failures (a malformed Guid, a non-numeric enum) are reported by MVC itself.
// Suppressing the built-in factory would lose those, so only the shape is aligned: everything
// this API returns on failure is problem+json.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.ClientErrorMapping[StatusCodes.Status400BadRequest].Link = null);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token from /api/v1/auth/login."
    });

    // Applied document-wide so every [Authorize] action in Swagger UI sends the token;
    // anonymous actions ignore it.
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// Vietnamese is the default; English is the alternative. Server-side messages (validation,
// emails, notifications) follow the request language, not just the frontend.
builder.Services.AddRequestLocalization(options =>
{
    options.SetDefaultCulture(SupportedLanguages.Default)
        .AddSupportedCultures(SupportedLanguages.All)
        .AddSupportedUICultures(SupportedLanguages.All);
});

var app = builder.Build();

if (args.Contains("seed") || args.Contains("--seed"))
{
    await MotelLease.Infrastructure.Persistence.DbSeeder.SeedAsync(app.Services);
    return;
}

app.UseRequestLocalization();

// First in the pipeline, so it also covers anything the later middleware throws.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>(NotificationRealtimeSetup.HubPath);

// Liveness probe. Real readiness (including a database round-trip) comes with the health
// check work in step 3.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
