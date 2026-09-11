namespace SignalingServer.Api.Options;

/// <summary>
/// TURN relay credentials, kept out of appsettings.json (and therefore out of git) since
/// they're secrets — populate via environment variables (Turn__Host, Turn__Username,
/// Turn__Password) on the host running the container. STUN-only ICE fails whenever both
/// peers sit behind NATs that can't be traversed with reflexive candidates alone (common
/// across different ISPs/mobile networks); TURN is the fallback relay for that case.
/// </summary>
public sealed class TurnOptions
{
    public const string SectionName = "Turn";

    public string? Host { get; set; }
    public int Port { get; set; } = 3478;
    public string? Username { get; set; }
    public string? Password { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);
}
