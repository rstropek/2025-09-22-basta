var builder = DistributedApplication.CreateBuilder(args);

var chatbot = builder.AddProject<Projects.ChatBot>("chatbot");

builder.Build().Run();
