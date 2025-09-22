using ChatBot;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapConversationsEndpoints();

app.Run();
