using ChatBotDb;
using Microsoft.AspNetCore.Http.HttpResults;
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
            api.MapPost("/", AddConversation);

            return app;
        }
    }

    public async static Task<Created<NewConversationResponse>> AddConversation(ApplicationDataContext context)
    {
        var conversation = new Conversation();
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return TypedResults.Created(null as string, new NewConversationResponse(conversation.Id));
    }

    public record NewConversationResponse(int ConversationId);

    // public static async Task<IResult> TryOpenAI(IConfiguration config)
    // {
    //     var client = new OpenAIResponseClient(config["OPENAI_MODEL"], config["OPENAI_API_KEY"]);

    //     var options = new ResponseCreationOptions
    //     {
    //         Instructions = "Du bist der Papagei eines Piraten. Antworte so, mit vielen Emojis",
    //         ReasoningOptions = new()
    //         {
    //             ReasoningEffortLevel = ResponseReasoningEffortLevel.Low
    //         },
    //         StoredOutputEnabled = false
    //     };

    //     var response = await client.CreateResponseAsync("Sind Delphine Fische?", options);

    //     return Results.Ok(response.Value.GetOutputText());
    // }
}