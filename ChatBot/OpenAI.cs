using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatBotDb;
using OpenAI.Responses;

namespace ChatBot;

public class OpenAIManager(OpenAIResponseClient client,
    IConversationRepository context, IConfiguration config,
    ILoggerFactory loggerFactory)
{
    private string? developerMessage = null;

    public async IAsyncEnumerable<AssistantResponseMessage> GetAssistantStreaming(
        int conversationId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Get conversation history from DB
        var conversation = await context.GetConversation(conversationId);

        bool requiresAction;
        do
        {
            requiresAction = false;

            var options = await GetResponseCreationOptions();
            var response = client.CreateResponseStreamingAsync(conversation, options, cancellationToken);

            await foreach (var chunk in response)
            {
                if (cancellationToken.IsCancellationRequested) { break; }

                if (chunk is StreamingResponseOutputTextDeltaUpdate textDelta)
                {
                    yield return new AssistantResponseMessage(textDelta.Delta);
                }

                if (chunk is StreamingResponseOutputItemDoneUpdate doneUpdate
                    && doneUpdate.Item is not ReasoningResponseItem)
                {
                    await context.AddResponseToConversation(conversationId, doneUpdate.Item);
                    conversation.Add(doneUpdate.Item);

                    if (doneUpdate.Item is FunctionCallResponseItem functionCall)
                    {
                        requiresAction = true;
                        FunctionCallOutputResponseItem functionResult;

                        switch (functionCall.FunctionName)
                        {
                            case nameof(ProductsTools.GetAvailableColorsForFlower):
                                var argument = JsonSerializer.Deserialize<ProductsTools.GetAvailableColorsForFlowerRequest>(functionCall.FunctionArguments)!;
                                var availableColors = ProductsTools.GetAvailableColorsForFlower(argument);
                                var availableColorsJson = JsonSerializer.Serialize(availableColors);
                                functionResult = new FunctionCallOutputResponseItem(functionCall.CallId, availableColorsJson);
                                break;
                            default:
                                functionResult = new FunctionCallOutputResponseItem(
                                    functionCall.CallId,
                                    "Function not recognized."
                                );
                                break;
                        }

                        await context.AddResponseToConversation(conversationId, functionResult);
                        conversation.Add(functionResult);
                    }

                }
            }
        }
        while (requiresAction);
    }

    private async Task<ResponseCreationOptions> GetResponseCreationOptions()
    {
        developerMessage ??= await GetDeveloperMessage();
        var options = new ResponseCreationOptions
        {
            Instructions = developerMessage,
            ReasoningOptions = new()
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Low
            },
            MaxOutputTokenCount = 2500,
            StoredOutputEnabled = false
        };
        options.Tools.Add(ProductsTools.GetAvailableColorsForFlowerTool);

        return options;
    }

    private async static Task<string> GetDeveloperMessage()
    {
        const string resourceName = "ChatBot.developer-message.md";

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Resource {resourceName} not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public record AssistantResponseMessage(string DeltaText);
}