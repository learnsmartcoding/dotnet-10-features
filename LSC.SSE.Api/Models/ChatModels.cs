namespace ChatApp.API.Models;

public record ChatMessage(string Role, string Content);
public record ChatRequest(List<ChatMessage> Messages);