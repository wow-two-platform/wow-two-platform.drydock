using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Drydock.IntegrationTests.Support;

/// <summary>
/// JSON conventions for the E2E tests — camelCase + string enums, matching the host's
/// <c>AddJsonOptions(JsonStringEnumConverter)</c> on a default (camelCase) ASP.NET serializer.
/// </summary>
public static class TestJson
{
    /// <summary>Shared options for serializing request bodies and reading responses.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}

/// <summary>
/// The success envelope every Drydock 2xx body carries — the typed payload sits under <c>data</c>
/// (see <c>ApiResponse&lt;T&gt;.Success</c>).
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public sealed record ApiEnvelope<T>
{
    /// <summary>The response payload (serialized as <c>data</c>).</summary>
    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

/// <summary>Compact JSON request/response helpers for the E2E tests.</summary>
public static class HttpExtensions
{
    /// <summary>Serializes <paramref name="body"/> with the API JSON conventions (camelCase + string enums).</summary>
    public static StringContent AsJson(this object body)
    {
        var json = JsonSerializer.Serialize(body, TestJson.Options);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>POST a JSON body.</summary>
    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object body)
        => client.PostAsync(url, body.AsJson());

    /// <summary>PUT a JSON body.</summary>
    public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string url, object body)
        => client.PutAsync(url, body.AsJson());

    /// <summary>Reads the response body as an <see cref="ApiEnvelope{T}"/> and returns its <c>data</c> payload.</summary>
    public static async Task<T> ReadEnvelopeAsync<T>(this HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(TestJson.Options);
        return envelope is { Data: { } data }
            ? data
            : throw new InvalidOperationException($"Response had no `data` payload of type {typeof(T).Name}.");
    }
}
