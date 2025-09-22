namespace ChatBot;

public static class ConversationsEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapConversationsEndpoints()
        {
            var api = app.MapGroup("/conversations");

            // Add routes here
            api.MapGet("/", () => "Hello from Conversations API!");

            return app;
        }
    }
}