namespace Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void MarkDeleted(DateTime deletedAt)
    {
        IsDeleted = true;
        DeletedAt = deletedAt;
    }

    public void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;

    public void SetUpdatedAt(DateTime updatedAt) => UpdatedAt = updatedAt;
}
