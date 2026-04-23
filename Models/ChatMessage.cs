// -----------------------------------------------------------------------------
// File: Models/ChatMessage.cs
//
// Purpose:
//   Defines the <see cref="HeatherDemoApp.Models.ChatMessage"/> data-transfer
//   object (DTO) used throughout the HeatherDemoApp to represent a single
//   message in a chat conversation between a user and the "Heather" AI agent.
//
// Usage:
//   * The Razor page model <c>ChatModel</c> (Pages/Chat.cshtml.cs) stores a
//     <c>List&lt;ChatMessage&gt;</c> in the ASP.NET Core session to preserve
//     conversation history across HTTP requests.
//   * The agent backend (<c>AgentService</c>) consumes a list of these messages
//     when replaying prior turns to the Azure AI Agents service so that the
//     agent has the full conversational context.
//   * Instances are JSON-serialized (via System.Text.Json) when persisted to
//     session state, so property names must remain stable.
// -----------------------------------------------------------------------------

namespace HeatherDemoApp.Models;

/// <summary>
/// Represents a single message exchanged in a chat conversation.
/// Each message carries a <see cref="Role"/> identifying the speaker
/// and the textual <see cref="Content"/> of the message.
/// </summary>
/// <remarks>
/// This class is a simple POCO (Plain Old CLR Object) used both as a UI
/// binding model (for rendering messages on the Razor chat page) and as a
/// persistence/transfer payload (for session storage and for forwarding
/// history to the Azure AI Agent).
/// </remarks>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the role of the speaker for this message.
    /// </summary>
    /// <value>
    /// One of the following well-known string values (case-insensitive):
    /// <list type="bullet">
    ///   <item><description><c>"user"</c>      – message authored by the end user.</description></item>
    ///   <item><description><c>"assistant"</c> – message authored by the AI agent (Heather).</description></item>
    ///   <item><description><c>"system"</c>    – system/grounding instruction (reserved for future use).</description></item>
    /// </list>
    /// Defaults to <c>"user"</c>.
    /// </value>
    public string Role { get; set; } = "user"; // user | assistant | system

    /// <summary>
    /// Gets or sets the textual content of the message.
    /// </summary>
    /// <remarks>
    /// For assistant messages this may contain Markdown, which the Chat view
    /// renders to HTML at the browser via the marked.js library.
    /// Defaults to <see cref="string.Empty"/> so the property is never null.
    /// </remarks>
    public string Content { get; set; } = string.Empty;
}
