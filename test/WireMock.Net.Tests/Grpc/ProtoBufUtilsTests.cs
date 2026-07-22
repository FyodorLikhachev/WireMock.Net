// Copyright © WireMock.Net

using WireMock.Util;

namespace WireMock.Net.Tests.Grpc;

public class ProtoBufUtilsTests
{
    private static readonly ProtoBufUtils _sut = new();

    [Fact]
    public async Task GetProtoBufMessageWithHeader_MultipleProtoFiles()
    {
        // Arrange
        var greet = ReadProtoFile("greet1.proto");
        var request = ReadProtoFile("request.proto");

        // Act
        var responseBytes = await _sut.GetProtoBufMessageWithHeaderAsync(
            [greet, request],
            "greet.HelloRequest", new
            {
                name = "hello"
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        Convert.ToBase64String(responseBytes).Should().Be("AAAAAAcKBWhlbGxv");
    }

    [Fact]
    public async Task GetProtoBufMessageWithHeader_MultipleValues()
    {
        // Arrange
        var greet = ReadProtoFile("greet1.proto");
        var request = ReadProtoFile("request.proto");

        // Act
        var responseBytes = await _sut.GetProtoBufMessageWithHeaderAsync(
            [greet, request],
            "greet.HelloRequest", new object[]
            {
                new { name = "hello" },
                new { name = "hello" }
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert : each value is encoded as a separate length-prefixed message
        Convert.ToBase64String(responseBytes).Should().Be("AAAAAAcKBWhlbGxvAAAAAAcKBWhlbGxv");
    }

    private static string ReadProtoFile(string filename)
    {
        return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Grpc", filename));
    }
}