namespace LMSupplyDepots.LLamaEngine.Chat;

public record ChatMessage(string Role, string Content);

public class ChatHistory
{
    private readonly List<ChatMessage> _messages = new();
    private readonly string _systemPrompt;
    private readonly int _maxHistoryLength;

    public ChatHistory(string systemPrompt, int maxHistoryLength = 10)
    {
        _systemPrompt = systemPrompt;
        _maxHistoryLength = maxHistoryLength;
        _messages.Add(new ChatMessage("system", systemPrompt));
    }

    public void AddMessage(string role, string content)
    {
        _messages.Add(new ChatMessage(role, content));

        // Keep only the system prompt and last N messages if we exceed the limit
        if (_messages.Count > _maxHistoryLength + 1)  // +1 for system prompt
        {
            var systemPrompt = _messages[0];
            _messages.RemoveRange(1, _messages.Count - _maxHistoryLength - 1);
            _messages.Insert(0, systemPrompt);
        }
    }

    public string GetFormattedPrompt()
    {
        var builder = new System.Text.StringBuilder();

        foreach (var message in _messages)
        {
            builder.AppendLine($"{message.Role}: {message.Content}");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    public void Clear()
    {
        _messages.Clear();
        _messages.Add(new ChatMessage("system", _systemPrompt));
    }

    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();
}