using System.Diagnostics;
using ChatBot;
using ChatBotDb;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqliteDbContext<ApplicationDataContext>("chatbot-db");
builder.Services.AddScoped<MigrationsManager>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();

builder.Services.AddSingleton(new ActivitySource(
    builder.Configuration["OTEL_SERVICE_NAME"] ?? throw new InvalidOperationException("OTEL_SERVICE_NAME not set")));

var app = builder.Build();

await app.Services.ApplyMigrations();

app.MapConversationsEndpoints();

app.Run();
