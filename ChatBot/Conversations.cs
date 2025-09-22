using System.Diagnostics;
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
            api.MapPost("/{conversationId}/chat", Chat);

            return app;
        }
    }

    public async static Task<IResult> Chat(IConversationRepository context,
        ActivitySource source, int conversationId,
        NewMessageRequest request, CancellationToken cancellationToken)
    {
        using (var span = source.StartActivity("Add message to conversation"))
        {
            try
            {
                var userMessage = ResponseItem.CreateUserMessageItem(request.Message);
                await context.AddResponseToConversation(conversationId, userMessage);
            }
            catch (ConversionNotFoundException)
            {
                span?.SetStatus(ActivityStatusCode.Error, "Conversation not found");
                return Results.NotFound();
            }
        }

        return Results.Ok();
    }

    public async static Task<Created<NewConversationResponse>> AddConversation(ApplicationDataContext context)
    {
        var conversation = new Conversation();
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return TypedResults.Created(null as string, new NewConversationResponse(conversation.Id));
    }

    public record NewConversationResponse(int ConversationId);

    public record NewMessageRequest(string Message);
    public record NewMessageResponse(int MessageId);

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