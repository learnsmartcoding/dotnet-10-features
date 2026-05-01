//using ChatApp.API.Models;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.AI;
//using OllamaSharp;
//using System.Net.ServerSentEvents;
//using System.Runtime.CompilerServices;

//namespace LSC.SSE.Api.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class ChatOllamaController : ControllerBase
//    {
//        private readonly IConfiguration _config;

//        public ChatOllamaController(IConfiguration config)
//        {
//            _config = config;
//        }

//        [HttpPost("stream-ollama")]
//        public IResult StreamOllama([FromBody] ChatRequest request)
//        {
//            // Default Ollama local URL
//            var uri = new Uri(_config["Ollama:Uri"] ?? "http://localhost:11434");
//            var model = _config["Ollama:Model"] ?? "llama3.2"; // Or "phi3", etc.

//            // OllamaApiClient implements IChatClient from Microsoft.Extensions.AI
//            IChatClient client = new OllamaApiClient(uri, model);

//            // Convert your DTOs to the standard Microsoft.Extensions.AI.ChatMessage
//            var chatMessages = request.Messages.Select(m => new Microsoft.Extensions.AI.ChatMessage(
//                m.Role.ToLower() == "user" ? ChatRole.User : ChatRole.Assistant,
//                m.Content
//            )).ToList();

//            return TypedResults.ServerSentEvents(GetOllamaStream(client, chatMessages));
//        }

//        private async IAsyncEnumerable<SseItem<string>> GetOllamaStream(
//            IChatClient client,
//            List<Microsoft.Extensions.AI.ChatMessage> messages,
//            [EnumeratorCancellation] CancellationToken ct = default)
//        {
//            // GetStreamingResponseAsync is the unified way to stream in .NET
//            var stream = client.GetStreamingResponseAsync(messages, cancellationToken: ct);

//            await foreach (var update in stream)
//            {
//                if (!string.IsNullOrEmpty(update.Text))
//                {
//                    yield return new SseItem<string>(update.Text, eventType: "delta");
//                }
//            }

//            yield return new SseItem<string>("[DONE]", eventType: "done");
//        }
//    }
//}
