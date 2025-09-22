using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatBotDb;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Responses;

namespace ChatBot;

public class OpenAIManager(OpenAIResponseClient client,
    IConversationRepository context, IConfiguration config,
    ILoggerFactory loggerFactory)
{
    private string? developerMessage = null;

    private FunctionTool[]? mcpTools = null;

    public async IAsyncEnumerable<AssistantResponseMessage> GetAssistantStreaming(
        int conversationId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Get conversation history from DB
        var conversation = await context.GetConversation(conversationId);

        // Get tools provided by MCP server
        IMcpClient? mcpClient = null;
        if (mcpTools == null)
        {
            mcpClient = await GetMcpClient();
            mcpTools = await mcpClient.ListFunctionTools();
            if (mcpTools.Length == 0)
            {
                throw new InvalidOperationException("MCP server offers no tools, this should never happen");
            }
        }

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
                                var mcpTool = mcpTools!.FirstOrDefault(t => t.FunctionName == functionCall.FunctionName);
                                if (mcpTool != null)
                                {
                                    mcpClient ??= await GetMcpClient();
                                    var functionArguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(functionCall.FunctionArguments)!;
                                    var callResult = await mcpClient.CallToolAsync(functionCall.FunctionName, functionArguments);
                                    functionResult = new FunctionCallOutputResponseItem(functionCall.CallId, (callResult.Content[0] as TextContentBlock)?.Text ?? "");
                                }
                                else
                                {
                                    functionResult = new FunctionCallOutputResponseItem(functionCall.CallId, "Function not found");
                                }

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
        foreach (var tool in mcpTools!)
        {
            options.Tools.Add(tool);
        }

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

    private async Task<IMcpClient> GetMcpClient()
    {
        // Note that the names of these classes will change in the near future.
        // They have already been change in the GitHub repo, but not yet in the NuGet package.
        var transport = new SseClientTransport(new()
        {
            Endpoint = new Uri(config["Services:cart-mcp:http:0"]!),
            Name = "Shopping Cart MCP"
        }, loggerFactory);
        return await McpClientFactory.CreateAsync(transport, loggerFactory: loggerFactory);
    }

    public record AssistantResponseMessage(string DeltaText);
}


public static class McpClientToolExtensions
{
    extension(McpClientTool t)
    {
        public FunctionTool ToFunctionTool() => ResponseTool.CreateFunctionTool(
            functionName: t.Name,
            functionDescription: t.Description,
            functionParameters: BinaryData.FromString(
                t.JsonSchema.GetRawText()),
            strictModeEnabled: false);
    }

    extension(IMcpClient mcpClient)
    {
        public async Task<FunctionTool[]> ListFunctionTools() =>
            [.. (await mcpClient.ListToolsAsync()).Select(t => t.ToFunctionTool())];
    }
}