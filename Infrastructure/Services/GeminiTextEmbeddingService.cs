using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Posts;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public sealed class GeminiTextEmbeddingService(HttpClient httpClient, IConfiguration configuration) : ITextEmbeddingService
{
    private const int EmbeddingDimensions = 1536;
    private readonly string? _apiKey = configuration["Gemini:ApiKey"];
    private readonly string _model = configuration["Gemini:EmbeddingModel"] ?? "gemini-embedding-001";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public Task<float[]> EmbedDocumentAsync(string title, string description, CancellationToken cancellationToken = default) =>
        EmbedAsync(description, "RETRIEVAL_DOCUMENT", title, cancellationToken);

    public Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken = default) =>
        EmbedAsync(query, "RETRIEVAL_QUERY", null, cancellationToken);

    private async Task<float[]> EmbedAsync(string text, string taskType, string? title, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Gemini API key is not configured. Set Gemini__ApiKey.");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_model)}:embedContent");
        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = JsonContent.Create(new EmbeddingRequest(
            new Content([new Part(text)]),
            taskType,
            title,
            EmbeddingDimensions));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Gemini returned an empty embedding response.");
        var embedding = result.Embedding?.Values
            ?? throw new InvalidOperationException("Gemini returned no embedding vector.");
        if (embedding.Length != EmbeddingDimensions)
            throw new InvalidOperationException($"Expected a {EmbeddingDimensions}-dimensional embedding, got {embedding.Length}.");
        return embedding;
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("content")] Content Content,
        [property: JsonPropertyName("taskType")] string TaskType,
        [property: JsonPropertyName("title"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Title,
        [property: JsonPropertyName("outputDimensionality")] int OutputDimensionality);

    private sealed record Content([property: JsonPropertyName("parts")] Part[] Parts);
    private sealed record Part([property: JsonPropertyName("text")] string Text);
    private sealed record EmbeddingResponse([property: JsonPropertyName("embedding")] EmbeddingData? Embedding);
    private sealed record EmbeddingData([property: JsonPropertyName("values")] float[]? Values);
}
