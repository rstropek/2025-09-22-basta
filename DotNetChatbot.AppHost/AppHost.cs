var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("openai-api-key", secret: true);
var model = builder.AddParameter("openai-model");

var chatbot = builder.AddProject<Projects.ChatBot>("chatbot")
    .WithEnvironment("OPENAI_API_KEY", apiKey)
    .WithEnvironment("OPENAI_MODEL", model);

builder.Build().Run();
