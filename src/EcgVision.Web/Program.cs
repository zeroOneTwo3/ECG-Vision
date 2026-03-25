using EcgVision.Infrastructure;
using EcgVision.Infrastructure.Data;
using EcgVision.Web;
using EcgVision.Web.BackgroundWorkers;
using EcgVision.Web.Middleware;

using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{UserId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/ecgvision-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{UserId}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddAuthentication(builder.Configuration);
    builder.Services.AddEcgInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<EcgProcessorWorker>();
    builder.Services.AddHostedService<TempCleanupService>();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // 2. LOG ENRICHMENT FIRST
    app.UseMiddleware<LogEnrichmentMiddleware>();

    // 3. SECURE MIGRATIONS
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await SeedData.SeedAllAsync(services);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database Migration Failed!");
            throw;
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options => options.WithTitle("ECG-Vision API")
               .WithTheme(ScalarTheme.Saturn)
               .AddPreferredSecuritySchemes("Bearer")
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
    }
    else
    {
        // Only use redirection in Prod if you have Certs configured
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}