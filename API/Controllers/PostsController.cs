using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Application.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FounditAi.Controllers;

[ApiController]
[Route("api/posts")]
public sealed class PostsController(IPostService postService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<PostSearchResult>>> SearchPosts(
        [FromQuery] string q,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length > 2000)
            return BadRequest(new { message = "Provide a search query between 1 and 2000 characters." });

        try
        {
            return Ok(await postService.SearchPostsAsync(q.Trim(), limit, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "The embedding service is unavailable." });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("reindex-embeddings")]
    public async Task<IActionResult> ReindexEmbeddings(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(new { indexed = await postService.ReindexPostEmbeddingsAsync(cancellationToken) });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PostDto>>> GetPosts(CancellationToken cancellationToken) =>
        Ok(await postService.GetPostsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDto>> GetPost(int id, CancellationToken cancellationToken)
    {
        var post = await postService.GetPostAsync(id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<PostDto>> CreatePost([FromForm] PostFormRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await using var photoStream = request.Photo?.OpenReadStream();
        try
        {
            var post = await postService.CreatePostAsync(
                new CreatePostRequest(request.Title, request.Description, request.LocationId, request.Type),
                userId, photoStream, request.Photo?.FileName, request.Photo?.ContentType, cancellationToken);
            return post is null ? BadRequest(new { message = "Choose a valid post type and existing location." }) : CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<PostDto>> UpdatePost(int id, [FromForm] PostUpdateFormRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await using var photoStream = request.Photo?.OpenReadStream();
        try
        {
            var post = await postService.UpdatePostAsync(
                id, new UpdatePostRequest(request.Title, request.Description, request.LocationId, request.Type, request.Status),
                userId, photoStream, request.Photo?.FileName, request.Photo?.ContentType, cancellationToken);
            return post is null ? NotFound() : Ok(post);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePost(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await postService.DeletePostAsync(id, userId, cancellationToken) ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out int userId) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

public class PostFormRequest
{
    [Required, StringLength(200, MinimumLength = 1)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(5000, MinimumLength = 1)] public string Description { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int LocationId { get; set; }
    [Required] public string Type { get; set; } = string.Empty;
    public IFormFile? Photo { get; set; }
}

public sealed class PostUpdateFormRequest : PostFormRequest
{
    [Required] public string Status { get; set; } = string.Empty;
}
