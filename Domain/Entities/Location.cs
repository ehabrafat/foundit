namespace Domain.Entities;

public sealed class Location : BaseEntity
{
    private Location() { }

    public Location(string name) => Name = name;

    public void Update(string name) => Name = name;

    public string Name { get; private set; } = default!;
}
