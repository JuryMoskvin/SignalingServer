using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SignalingServer.Api.Contracts;

namespace SignalingServer.Tests.Signaling;

public class SignalingRelayIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task TwoPeers_ExchangeOfferThroughRelay_PayloadForwardedVerbatim()
    {
        using var httpClient = factory.CreateClient();
        var createResponse = await httpClient.PostAsync("/ws/api/rooms", null);
        createResponse.EnsureSuccessStatusCode();
        var room = await createResponse.Content.ReadFromJsonAsync<CreateRoomResponse>();
        Assert.NotNull(room);

        var wsClient = factory.Server.CreateWebSocketClient();
        var baseUri = factory.Server.BaseAddress;

        using var socketA = await wsClient.ConnectAsync(
            new Uri(baseUri, $"/ws/signaling?roomCode={room!.RoomCode}&deviceId=device-a"), CancellationToken.None);

        var peerJoinedTask = ReceiveOneMessageAsync(socketA);

        using var socketB = await wsClient.ConnectAsync(
            new Uri(baseUri, $"/ws/signaling?roomCode={room.RoomCode}&deviceId=device-b"), CancellationToken.None);

        var peerJoinedRaw = await peerJoinedTask;
        var peerJoinedMessage = JsonSerializer.Deserialize<SignalingMessage>(peerJoinedRaw);
        Assert.Equal(SignalingMessageTypes.PeerJoined, peerJoinedMessage!.Type);

        var offerJson = "{\"type\":\"offer\",\"roomCode\":\"" + room.RoomCode +
            "\",\"senderId\":\"device-a\",\"payload\":{\"sdp\":\"v=0...\"}}";
        var receiveOnB = ReceiveOneMessageAsync(socketB);
        await socketA.SendAsync(Encoding.UTF8.GetBytes(offerJson), WebSocketMessageType.Text, true, CancellationToken.None);

        var receivedOnB = await receiveOnB;
        Assert.Equal(offerJson, receivedOnB);

        await socketA.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await socketB.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Fact]
    public async Task ThirdDevice_JoiningFullRoom_IsRejected()
    {
        using var httpClient = factory.CreateClient();
        var createResponse = await httpClient.PostAsync("/ws/api/rooms", null);
        var room = await createResponse.Content.ReadFromJsonAsync<CreateRoomResponse>();
        Assert.NotNull(room);

        var wsClient = factory.Server.CreateWebSocketClient();
        var baseUri = factory.Server.BaseAddress;

        using var socketA = await wsClient.ConnectAsync(
            new Uri(baseUri, $"/ws/signaling?roomCode={room!.RoomCode}&deviceId=device-a"), CancellationToken.None);
        using var socketB = await wsClient.ConnectAsync(
            new Uri(baseUri, $"/ws/signaling?roomCode={room.RoomCode}&deviceId=device-b"), CancellationToken.None);

        using var socketC = await wsClient.ConnectAsync(
            new Uri(baseUri, $"/ws/signaling?roomCode={room.RoomCode}&deviceId=device-c"), CancellationToken.None);

        var buffer = new byte[1024];
        var result = await socketC.ReceiveAsync(buffer, CancellationToken.None);

        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        Assert.Equal(WebSocketCloseStatus.PolicyViolation, result.CloseStatus);
    }

    private static async Task<string> ReceiveOneMessageAsync(WebSocket socket)
    {
        var buffer = new byte[8 * 1024];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}
