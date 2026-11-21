using System.ComponentModel.DataAnnotations;

namespace Application.Posts;

public sealed record LocationDto(int Id, string Name);
public sealed record CreateLocationRequest([Required, StringLength(150, MinimumLength = 1)] string Name);
public sealed record UpdateLocationRequest([Required, StringLength(150, MinimumLength = 1)] string Name);
public sealed record PostDto(int Id, string Title, string Description, string? PhotoUrl, int LocationId, string LocationName, string Type, string Status, int UserId, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreatePostRequest([Required, StringLength(200, MinimumLength = 1)] string Title, [Required, StringLength(5000, MinimumLength = 1)] string Description, [Range(1, int.MaxValue)] int LocationId, [Required] string Type);
public sealed record UpdatePostRequest([Required, StringLength(200, MinimumLength = 1)] string Title, [Required, StringLength(5000, MinimumLength = 1)] string Description, [Range(1, int.MaxValue)] int LocationId, [Required] string Type, [Required] string Status);
public sealed record PostSearchResult(PostDto Post, double Distance);

public interface ITextEmbeddingService
{
    bool IsConfigured { get; }
    Task<float[]> EmbedDocumentAsync(string title, string description, CancellationToken cancellationToken = default);
    Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken = default);
}

public interface IPostService
{
    Task<IReadOnlyList<LocationDto>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<LocationDto?> CreateLocationAsync(CreateLocationRequest request, CancellationToken cancellationToken = default);
    Task<LocationDto?> UpdateLocationAsync(int id, UpdateLocationRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteLocationAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetPostsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostSearchResult>> SearchPostsAsync(string query, int limit = 20, CancellationToken cancellationToken = default);
    Task<int> ReindexPostEmbeddingsAsync(CancellationToken cancellationToken = default);
    Task<PostDto?> GetPostAsync(int id, CancellationToken cancellationToken = default);
    Task<PostDto?> CreatePostAsync(CreatePostRequest request, int userId, Stream? photo, string? fileName, string? contentType, CancellationToken cancellationToken = default);
    Task<PostDto?> UpdatePostAsync(int id, UpdatePostRequest request, int userId, Stream? photo, string? fileName, string? contentType, CancellationToken cancellationToken = default);
    Task<bool> DeletePostAsync(int id, int userId, CancellationToken cancellationToken = default);
}
