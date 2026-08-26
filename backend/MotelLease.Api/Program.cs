using MotelLease.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Vietnamese is the default; English is the alternative. Server-side messages (validation,
// emails, notifications) follow the request language, not just the frontend.
builder.Services.AddRequestLocalization(options =>
{
    options.SetDefaultCulture("vi")
        .AddSupportedCultures("vi", "en")
        .AddSupportedUICultures("vi", "en");
});

var app = builder.Build();

app.UseRequestLocalization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Liveness probe. Real readiness (including a database round-trip) comes with the health
// check work in step 3.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
