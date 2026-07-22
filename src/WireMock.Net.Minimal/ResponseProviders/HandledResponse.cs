// Copyright © WireMock.Net

namespace WireMock.ResponseProviders;

/// <summary>
/// Special response marker to indicate that the response has already been written to the HttpContext by the <see cref="IResponseProvider"/>,
/// so it should not be written again.
/// Return this from a custom <see cref="IResponseProvider"/> which writes the response (e.g. a streaming response) to the HttpContext itself.
/// </summary>
public class HandledResponse : ResponseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HandledResponse"/> class.
    /// </summary>
    /// <param name="dateTime">The DateTime on which the response was handled.</param>
    public HandledResponse(DateTime dateTime)
    {
        DateTime = dateTime;
    }
}
