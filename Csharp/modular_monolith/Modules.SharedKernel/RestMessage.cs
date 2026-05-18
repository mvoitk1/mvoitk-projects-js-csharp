namespace Modules.SharedKernel;

/// <summary>
/// Generic error / status payload used by REST controllers across modules.
/// </summary>
public class RestMessage
{
    public RestMessage()
    {
    }

    public RestMessage(params string[] messages)
    {
        Messages = messages;
    }

    public ICollection<string> Messages { get; set; } = new List<string>();
}
