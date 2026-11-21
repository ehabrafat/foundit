using System.Security.Cryptography;
using Application.Posts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class PostService(ApiDbContext dbContext, string webRootPath, ITextEmbeddingService embeddingService, ILogger<PostService> logger) : IPostService
{
    private const long MaxPhotoSize = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp"
    };

    public async Task<IReadOnlyList<LocationDto>> GetLocationsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Locations.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.Name)
            .Select(x => new LocationDto(x.Id, x.Name)).ToListAsync(cancellationToken);

    public async Task<LocationDto?> CreateLocationAsync(CreateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await dbContext.Locations.AnyAsync(x => !x.IsDeleted && x.Name.ToLower() == name.ToLower(), cancellationToken)) return null;
        var location = new Location(name);
        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new LocationDto(location.Id, location.Name);
    }

    public async Task<LocationDto?> UpdateLocationAsync(int id, UpdateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var location = await dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (location is null) return null;

        var name = request.Name.Trim();
        if (await dbContext.Locations.AnyAsync(x => !x.IsDeleted && x.Id != id && x.Name.ToLower() == name.ToLower(), cancellationToken))
            return null;

        location.Update(name);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new LocationDto(location.Id, location.Name);
    }

    public async Task<bool> DeleteLocationAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (location is null) return false;

        dbContext.Remove(location);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PostDto>> GetPostsAsync(CancellationToken cancellationToken = default)
    {
        var posts = await dbContext.Posts.AsNoTracking().Include(x => x.Location).Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return posts.Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<PostSearchResult>> SearchPostsAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        var embedding = new Vector(await embeddingService.EmbedQueryAsync(query, cancellationToken));
        var matches = await dbContext.Posts.AsNoTracking().Include(x => x.Location)
            .Where(x => !x.IsDeleted && EF.Property<Vector?>(x, "Embedding") != null)
            .Select(x => new { Post = x, Distance = EF.Property<Vector>(x, "Embedding").CosineDistance(embedding) })
            .OrderBy(x => x.Distance).Take(Math.Clamp(limit, 1, 50)).ToListAsync(cancellationToken);
        return matches.Select(x => new PostSearchResult(ToDto(x.Post), x.Distance)).ToArray();
    }

    public async Task<int> ReindexPostEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        if (!embeddingService.IsConfigured)
            throw new InvalidOperationException("Gemini API key is not configured. Set Gemini__ApiKey.");

        var posts = await dbContext.Posts.Where(x => !x.IsDeleted && EF.Property<Vector?>(x, "Embedding") == null)
            .OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var indexed = 0;
        foreach (var post in posts)
        {
            var vector = await TryEmbedPostAsync(post.Title, post.Description, cancellationToken);
            if (vector is null) continue;
            dbContext.Entry(post).Property<Vector?>("Embedding").CurrentValue = vector;
            await dbContext.SaveChangesAsync(cancellationToken);
            indexed++;
        }
        return indexed;
    }

    public async Task<PostDto?> GetPostAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await dbContext.Posts.AsNoTracking().Include(x => x.Location).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        return post is null ? null : ToDto(post);
    }

    public async Task<PostDto?> CreatePostAsync(CreatePostRequest request, int userId, Stream? photo, string? fileName, string? contentType, CancellationToken cancellationToken = default)
    {
        if (!TryParseType(request.Type, out var type) || !await LocationExistsAsync(request.LocationId, cancellationToken)) return null;
        var photoPath = photo is null ? null : await SavePhotoAsync(photo, fileName, contentType, cancellationToken);
        var post = new Post(request.Title.Trim(), request.Description.Trim(), photoPath, request.LocationId, type, userId);
        var embedding = await TryEmbedPostAsync(post.Title, post.Description, cancellationToken);
        dbContext.Posts.Add(post);
        dbContext.Entry(post).Property<Vector?>("Embedding").CurrentValue = embedding;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (photoPath is not null) DeletePhoto(photoPath);
            throw;
        }
        return await GetPostAsync(post.Id, cancellationToken);
    }

    public async Task<PostDto?> UpdatePostAsync(int id, UpdatePostRequest request, int userId, Stream? photo, string? fileName, string? contentType, CancellationToken cancellationToken = default)
    {
        var post = await dbContext.Posts.Include(x => x.Location).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.UserId == userId, cancellationToken);
        if (post is null || !TryParseType(request.Type, out var type) || !TryParseStatus(request.Status, out var status) || !await LocationExistsAsync(request.LocationId, cancellationToken)) return null;
        var oldPhoto = post.PhotoPath;
        var newPhoto = photo is null ? null : await SavePhotoAsync(photo, fileName, contentType, cancellationToken);
        post.Update(request.Title.Trim(), request.Description.Trim(), request.LocationId, type, status, newPhoto);
        var embedding = await TryEmbedPostAsync(post.Title, post.Description, cancellationToken);
        dbContext.Entry(post).Property<Vector?>("Embedding").CurrentValue = embedding;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (newPhoto is not null) DeletePhoto(newPhoto);
            throw;
        }
        if (newPhoto is not null && oldPhoto is not null) DeletePhoto(oldPhoto);
        return await GetPostAsync(post.Id, cancellationToken);
    }

    public async Task<bool> DeletePostAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var post = await dbContext.Posts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.UserId == userId, cancellationToken);
        if (post is null) return false;
        var photoPath = post.PhotoPath;
        dbContext.Remove(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (photoPath is not null) DeletePhoto(photoPath);
        return true;
    }

    private async Task<bool> LocationExistsAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Locations.AnyAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    private async Task<Vector?> TryEmbedPostAsync(string title, string description, CancellationToken cancellationToken)
    {
        if (!embeddingService.IsConfigured) return null;
        try
        {
            return new Vector(await embeddingService.EmbedDocumentAsync(title, description, cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Could not create a post embedding with Gemini. The post will be saved without vector search data.");
            return null;
        }
    }

    private async Task<string> SavePhotoAsync(Stream source, string? fileName, string? contentType, CancellationToken cancellationToken)
    {
        if (source.Length > MaxPhotoSize) throw new ArgumentException("Photo must be 5 MB or smaller.");
        if (contentType is null || !AllowedImageTypes.TryGetValue(contentType, out var extension))
            throw new ArgumentException("Photo must be a JPEG, PNG, or WebP image.");
        var directory = Path.Combine(webRootPath, "post-photos");
        Directory.CreateDirectory(directory);
        var storedName = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}{extension}";
        var physicalPath = Path.Combine(directory, storedName);
        await using var destination = File.Create(physicalPath);
        await source.CopyToAsync(destination, cancellationToken);
        return $"/post-photos/{storedName}";
    }

    private void DeletePhoto(string photoPath)
    {
        var name = Path.GetFileName(photoPath);
        var path = Path.Combine(webRootPath, "post-photos", name);
        if (File.Exists(path)) File.Delete(path);
    }

    private static bool TryParseType(string value, out PostType type) => Enum.TryParse(value, true, out type) && Enum.IsDefined(type);
    private static bool TryParseStatus(string value, out PostStatus status) => Enum.TryParse(value, true, out status) && Enum.IsDefined(status);
    private static PostDto ToDto(Post post) => new(post.Id, post.Title, post.Description, post.PhotoPath, post.LocationId, post.Location.Name, post.Type.ToString().ToLowerInvariant(), post.Status.ToString().ToLowerInvariant(), post.UserId, post.CreatedAt, post.UpdatedAt);
}
