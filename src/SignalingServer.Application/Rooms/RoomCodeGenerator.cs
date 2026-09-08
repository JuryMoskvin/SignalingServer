using System.Security.Cryptography;

namespace SignalingServer.Application.Rooms;

/// <summary>
/// Generates short, human-shareable room codes using an unambiguous alphabet
/// (no 0/O, 1/I/L) so codes are easy to read aloud or retype.
/// </summary>
public sealed class RoomCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int MaxAttempts = 10;

    private readonly IRoomRepository _repository;
    private readonly RoomOptions _options;

    public RoomCodeGenerator(IRoomRepository repository, RoomOptions options)
    {
        _repository = repository;
        _options = options;
    }

    public string GenerateUnique()
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = Generate(_options.CodeLength);
            if (_repository.Get(code) is null)
            {
                return code;
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate a unique room code after {MaxAttempts} attempts.");
    }

    private static string Generate(int length)
    {
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(buffer);
    }
}
