using System.Diagnostics;
using ChatBot;
using ChatBotDb;
using OpenAI.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqliteDbContext<ApplicationDataContext>("chatbot-db");
builder.Services.AddScoped<MigrationsManager>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<OpenAIManager>();

builder.Services.AddSingleton(new ActivitySource(
    builder.Configuration["OTEL_SERVICE_NAME"] ?? throw new InvalidOperationException("OTEL_SERVICE_NAME not set")));

builder.Services.AddSingleton(_ => new OpenAIResponseClient(
    builder.Configuration["OPENAI_MODEL"],
    builder.Configuration["OPENAI_API_KEY"]));

var app = builder.Build();

await app.Services.ApplyMigrations();

app.MapConversationsEndpoints();

app.Run();
