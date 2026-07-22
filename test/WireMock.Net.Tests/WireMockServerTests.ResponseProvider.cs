// Copyright © WireMock.Net

#if NET8_0_OR_GREATER
using Google.Protobuf;
using Greet;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using WireMock.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseProviders;
using WireMock.Server;
using WireMock.Settings;

// ReSharper disable once CheckNamespace
namespace WireMock.Net.Tests;

public partial class WireMockServerTests
{
    /// <summary>
    /// A custom <see cref="IResponseProvider"/> which handles a gRPC server streaming call :
    /// each reply read from the queue is pushed to the response stream as a separate length-prefixed message,
    /// until the queue is closed.
    /// </summary>
    private class ServerStreamingResponseProvider(IBlockingQueue<HelloReply> replies) : IResponseProvider
    {
        private const int GrpcHeaderSize = 5;

        public async Task<(IResponseMessage Message, IMapping? Mapping)> ProvideResponseAsync(IMapping mapping, HttpContext context, IRequestMessage requestMessage, WireMockServerSettings settings)
        {
            var response = context.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "application/grpc";

            while (replies.TryRead(out var reply))
            {
                await WriteGrpcMessageAsync(response.Body, reply.ToByteArray());
                await response.Body.FlushAsync();
            }

            response.AppendTrailer("grpc-status", "0");

            return (new HandledResponse(DateTime.UtcNow), null);
        }

        private static async Task WriteGrpcMessageAsync(Stream stream, byte[] payload)
        {
            var frame = new byte[GrpcHeaderSize + payload.Length];
            frame[1] = (byte)(payload.Length >> 24);
            frame[2] = (byte)(payload.Length >> 16);
            frame[3] = (byte)(payload.Length >> 8);
            frame[4] = (byte)payload.Length;
            payload.CopyTo(frame, GrpcHeaderSize);

            await stream.WriteAsync(frame, 0, frame.Length);
        }
    }

    [Fact]
    public async Task WireMockServer_WithCustomResponseProvider_GrpcServerStreaming_ShouldPushMessagesToClient()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var replies = new BlockingQueue<HelloReply>();
        using var server = WireMockServer.Start(useHttp2: true);

        server
            .Given(Request.Create()
                .UsingPost()
                .WithPath("/greet.Greeter/SayHelloServerStreaming")
            )
            .RespondWith(new ServerStreamingResponseProvider(replies));

        var channel = GrpcChannel.ForAddress(server.Url!);
        var client = new Greeter.GreeterClient(channel);

        using var call = client.SayHelloServerStreaming(new HelloRequest { Name = "stef" }, cancellationToken: cancellationToken);

        // Act and Assert : feed the replies one by one and assert each is received by the client before the next one is supplied
        replies.Write(new HelloReply { Message = "hello stef 1" });
        (await call.ResponseStream.MoveNext(cancellationToken)).Should().BeTrue();
        call.ResponseStream.Current.Message.Should().Be("hello stef 1");

        replies.Write(new HelloReply { Message = "hello stef 2" });
        (await call.ResponseStream.MoveNext(cancellationToken)).Should().BeTrue();
        call.ResponseStream.Current.Message.Should().Be("hello stef 2");

        replies.Write(new HelloReply { Message = "hello stef 3" });
        (await call.ResponseStream.MoveNext(cancellationToken)).Should().BeTrue();
        call.ResponseStream.Current.Message.Should().Be("hello stef 3");

        // Closing the queue ends the stream
        replies.Close();
        (await call.ResponseStream.MoveNext(cancellationToken)).Should().BeFalse();
    }
}
#endif
