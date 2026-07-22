// Copyright © WireMock.Net

using System.Net;

namespace WireMock.ResponseProviders;

/// <summary>
/// Special response marker to indicate WebSocket has been handled
/// </summary>
internal class WebSocketHandledResponse : HandledResponse
{
    public WebSocketHandledResponse(DateTime dateTime) : base(dateTime)
    {
        // 101 Switching Protocols
        StatusCode = (int)HttpStatusCode.SwitchingProtocols;
    }
}