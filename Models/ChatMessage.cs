namespace HeatherDemoApp.Models;

public class ChatMessage
{
    public string Role { get; set; } = "user"; // user | assistant | system
    public string Content { get; set; } = string.Empty;
}
