// Copyright © WireMock.Net

using System.Collections;
using JsonConverter.Abstractions;
using Newtonsoft.Json.Linq;
using ProtoBufJsonConverter;
using ProtoBufJsonConverter.Models;
using WireMock.ResponseBuilders;

namespace WireMock.Util;

internal class ProtoBufUtils : IProtoBufUtils
{
    public async Task<byte[]> GetProtoBufMessageWithHeaderAsync(
        IReadOnlyList<string>? protoDefinitions,
        string? messageType,
        object? value,
        IJsonConverter? jsonConverter = null,
        CancellationToken cancellationToken = default
    )
    {
        if (protoDefinitions == null || string.IsNullOrWhiteSpace(messageType) || value is null)
        {
            return [];
        }

        if (TryGetMessageValues(value, out var values))
        {
            // The value is a collection : encode each item as a separate length-prefixed message (gRPC server streaming).
            using var memoryStream = new MemoryStream();
            foreach (var messageValue in values)
            {
                var message = await ConvertToProtoBufWithHeaderAsync(protoDefinitions, messageType!, messageValue!, cancellationToken).ConfigureAwait(false);
                memoryStream.Write(message, 0, message.Length);
            }

            return memoryStream.ToArray();
        }

        return await ConvertToProtoBufWithHeaderAsync(protoDefinitions, messageType!, value, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]> ConvertToProtoBufWithHeaderAsync(
        IReadOnlyList<string> protoDefinitions,
        string messageType,
        object value,
        CancellationToken cancellationToken
    )
    {
        var resolver = new WireMockProtoFileResolver(protoDefinitions);
        var request = new ConvertToProtoBufRequest(protoDefinitions[0], messageType, value, true)
            .WithProtoFileResolver(resolver);

        return await SingletonFactory<Converter>
            .GetInstance()
            .ConvertAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// A ProtoBuf message is always represented as a JSON object, so a collection (JSON array) unambiguously means multiple messages.
    /// </summary>
    private static bool TryGetMessageValues(object value, out IReadOnlyList<object?> values)
    {
        switch (value)
        {
            case JArray jArray:
                values = jArray.Cast<object?>().ToArray();
                return true;

            case string or byte[] or JToken or IDictionary:
                values = [];
                return false;

            case IEnumerable enumerable:
                values = enumerable.Cast<object?>().ToArray();
                return true;

            default:
                values = [];
                return false;
        }
    }

    public IResponseBuilder UpdateResponseBuilder(IResponseBuilder responseBuilder, string protoBufMessageType, object bodyAsJson, params string[] protoDefinitions)
    {
        if (protoDefinitions.Length > 0)
        {
            return responseBuilder.WithBodyAsProtoBuf(protoDefinitions, protoBufMessageType, bodyAsJson);
        }

        // ProtoDefinition(s) is/are defined at Mapping/Server level
        return responseBuilder.WithBodyAsProtoBuf(protoBufMessageType, bodyAsJson);
    }
}