var builder = DistributedApplication.CreateBuilder(args);

var sqlite = builder.AddSqlite(
    "chatbot-db",
    builder.Configuration["Database:path"]!,
    builder.Configuration["Database:fileName"]!)
    .WithSqliteWeb();

var apiKey = builder.AddParameter("openai-api-key", secret: true);
var model = builder.AddParameter("openai-model");

var chatbot = builder.AddProject<Projects.ChatBot>("chatbot")
    .WithReference(sqlite)
    .WithEnvironment("OPENAI_API_KEY", apiKey)
    .WithEnvironment("OPENAI_MODEL", model);

builder.Build().Run();
