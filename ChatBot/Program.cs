using ChatBot;
using ChatBotDb;
using Microsoft.EntityFrameworkCore.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqliteDbContext<ApplicationDataContext>("chatbot-db");
builder.Services.AddScoped<MigrationsManager>();

var app = builder.Build();

await app.Services.ApplyMigrations();

app.MapConversationsEndpoints();

app.Run();
