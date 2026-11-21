using Application.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FounditAi.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController(IPostService postService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetLocations(CancellationToken cancellationToken) =>
        Ok(await postService.GetLocationsAsync(cancellationToken));

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<LocationDto>> CreateLocation(CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var location = await postService.CreateLocationAsync(request, cancellationToken);
        return location is null ? Conflict(new { message = "A location with that name already exists." }) : CreatedAtAction(nameof(GetLocations), location);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LocationDto>> UpdateLocation(int id, UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var location = await postService.UpdateLocationAsync(id, request, cancellationToken);
        return location is null
            ? NotFoundOrConflict(id, request.Name)
            : Ok(location);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLocation(int id, CancellationToken cancellationToken) =>
        await postService.DeleteLocationAsync(id, cancellationToken) ? NoContent() : NotFound();

    private ActionResult<LocationDto> NotFoundOrConflict(int id, string name) =>
        Conflict(new { message = $"A location with the name '{name.Trim()}' may already exist, or location {id} was not found." });
}
