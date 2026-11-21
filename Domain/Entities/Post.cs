namespace Domain.Entities;

public enum PostType { Lost, Found }
public enum PostStatus { Open, Resolved }

public sealed class Post : BaseEntity
{
    private Post() { }

    public Post(string title, string description, string? photoPath, int locationId, PostType type, int userId)
    {
        Title = title;
        Description = description;
        PhotoPath = photoPath;
        LocationId = locationId;
        Type = type;
        UserId = userId;
        Status = PostStatus.Open;
    }

    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? PhotoPath { get; private set; }
    public int LocationId { get; private set; }
    public Location Location { get; private set; } = default!;
    public PostType Type { get; private set; }
    public PostStatus Status { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; } = default!;

    public void Update(string title, string description, int locationId, PostType type, PostStatus status, string? photoPath)
    {
        Title = title;
        Description = description;
        LocationId = locationId;
        Type = type;
        Status = status;
        if (photoPath is not null) PhotoPath = photoPath;
    }

}
