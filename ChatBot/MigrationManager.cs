using ChatBotDb;
using Microsoft.EntityFrameworkCore;

namespace ChatBot;

public class MigrationsManager(ApplicationDataContext context, ILogger<MigrationsManager> logger)
{
    public async Task ApplyMigrations()
    {
        logger.LogInformation("Applying migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Migrations applied.");
    }
}

public static class MigrationsManagerExtensions
{
    extension(IServiceProvider services)
    {
        public async Task ApplyMigrations()
        {
            using var scope = services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<MigrationsManager>();
            await manager.ApplyMigrations();
        }
    }
}