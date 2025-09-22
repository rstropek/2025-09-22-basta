using OpenAI.Responses;

namespace ChatBot;

public static class ConversationsEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapConversationsEndpoints()
        {
            var api = app.MapGroup("/conversations");

            // Add routes here
            api.MapGet("/", TryOpenAI);

            return app;
        }
    }

    public static async Task<IResult> TryOpenAI(IConfiguration config)
    {
        var client = new OpenAIResponseClient(config["OPENAI_MODEL"], config["OPENAI_API_KEY"]);

        var options = new ResponseCreationOptions
        {
            Instructions = "Du bist der Papagei eines Piraten. Antworte so, mit vielen Emojis",
            ReasoningOptions = new()
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Low
            }
        };

        var response = await client.CreateResponseAsync("Sind Delphine Fische?", options);

        return Results.Ok(response.Value.GetOutputText());
    }
}