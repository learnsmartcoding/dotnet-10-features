using ChatApp.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI.Chat;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

namespace ChatApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IConfiguration _config;

    public ChatController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("stream")]
    public IResult Stream([FromBody] ChatRequest request)
    {
        var apiKey = _config["OpenAI:ApiKey"]!;
        var model = _config["OpenAI:Model"] ?? "gpt-4o";
        var client = new ChatClient(model, apiKey);

        var messages = request.Messages
            .Select<Models.ChatMessage, OpenAI.Chat.ChatMessage>(m =>
                m.Role == "user"
                    ? new UserChatMessage(m.Content)
                    : new AssistantChatMessage(m.Content))
            .ToList();

        // We return the Stream directly via TypedResults
        // This is the "First-Class" SSE support in .NET 10
        return TypedResults.ServerSentEvents(GetChatStream(client, messages));
    }

    // This helper method creates the IAsyncEnumerable that SSE needs
    private async IAsyncEnumerable<SseItem<string>> GetChatStream(
        ChatClient client,
        List<OpenAI.Chat.ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var updates = client.CompleteChatStreamingAsync(messages, cancellationToken: ct);

        await foreach (var update in updates)
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return new SseItem<string>(part.Text, eventType: "delta");
                }
            }
        }

        // Terminal event
        yield return new SseItem<string>("[DONE]", eventType: "done");
    }


    [HttpPost("stream-ollama")]
    public IResult StreamOllama([FromBody] ChatRequest request)
    {
        // Default Ollama local URL
        var uri = new Uri(_config["Ollama:Uri"] ?? "http://localhost:11434");
        var model = _config["Ollama:Model"] ?? "llama3.1"; // Or "phi3", etc.

        // OllamaApiClient implements IChatClient from Microsoft.Extensions.AI
        IChatClient client = new OllamaApiClient(uri, model);

        // Convert your DTOs to the standard Microsoft.Extensions.AI.ChatMessage
        var chatMessages = request.Messages.Select(m => new Microsoft.Extensions.AI.ChatMessage(
            m.Role.ToLower() == "user" ? ChatRole.User : ChatRole.Assistant,
            m.Content
        )).ToList();

        return TypedResults.ServerSentEvents(GetOllamaStream(client, chatMessages));
    }

    private async IAsyncEnumerable<SseItem<string>> GetOllamaStream(
        IChatClient client,
        List<Microsoft.Extensions.AI.ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // GetStreamingResponseAsync is the unified way to stream in .NET
        var stream = client.GetStreamingResponseAsync(messages, cancellationToken: ct);

        await foreach (var update in stream)
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return new SseItem<string>(update.Text, eventType: "delta");
            }
        }

        yield return new SseItem<string>("[DONE]", eventType: "done");
    }
}