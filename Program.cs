using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

string dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
string dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
string dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "user";
string dbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? "password";
string dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "db";
string connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<Program>();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    logger.LogInformation("Checking database connection...");

    try
    {
        dbContext.Database.EnsureCreated();
        logger.LogInformation("Database ensured to be created.");

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        logger.LogInformation("Database connection opened.");

        var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'entries'
            );";
        var exists = (bool)await command.ExecuteScalarAsync();

        if (!exists)
        {
            command.CommandText = "CREATE TABLE public.\"entries\" (\"Id\" SERIAL PRIMARY KEY, \"Data\" TEXT NOT NULL);";
            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Table 'entries' created.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database.");
    }
}

app.MapGet("/", async (AppDbContext dbContext, ILogger<Program> logger) =>
{
    logger.LogInformation("Handling request for '/' endpoint.");

    try
    {
        var randomData = Guid.NewGuid().ToString();
        dbContext.Entries.Add(new Entry { Data = randomData });
        await dbContext.SaveChangesAsync();

        var count = await dbContext.Entries.CountAsync();

        return Results.Ok(new { Message = "Entry added successfully with random data.", Data = randomData, Count = count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while handling the request.");
        return Results.Problem("An error occurred while processing your request.");
    }
});

app.MapGet("/status", (ILogger<Program> logger) =>
{
    return Results.Ok(new { status = "UP" });
});

app.Run();
