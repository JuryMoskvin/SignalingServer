using SignalingServer.Api.Contracts;

namespace SignalingServer.Api.Options;

public sealed class IceServersOptions
{
    public const string SectionName = "IceServers";

    /// <summary>
    /// STUN servers today; TURN entries (with Username/Credential) can be appended here
    /// later without any code change on client or server — same DTO shape either way.
    /// </summary>
    public List<IceServerDto> Servers { get; set; } = [];
}
