using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Pgvector;

namespace Infrastructure.Data;

public class ApiDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Location> Locations { get; set; }

    public override EntityEntry Remove(object entity)
    {
        MarkAsDeleted(entity);
        return Entry(entity);
    }

    public override void RemoveRange(IEnumerable<object> entities)
    {
        foreach (var entity in entities)
            MarkAsDeleted(entity);
    }

    public override void RemoveRange(params object[] entities)
    {
        foreach (var entity in entities)
            MarkAsDeleted(entity);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateEntityTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateEntityTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Location>().HasIndex(location => location.Name).IsUnique();
        modelBuilder.Entity<Location>().Property(location => location.Name).HasMaxLength(150);
        modelBuilder.Entity<Post>().Property(post => post.Title).HasMaxLength(200);
        modelBuilder.Entity<Post>().Property(post => post.Description).HasMaxLength(5000);
        modelBuilder.Entity<Post>().Property(post => post.Type).HasConversion<string>();
        modelBuilder.Entity<Post>().Property(post => post.Status).HasConversion<string>();
        modelBuilder.Entity<Post>().Property<Vector?>("Embedding").HasColumnType("vector(1536)");
        modelBuilder.Entity<Post>().HasIndex("Embedding").HasMethod("hnsw").HasOperators("vector_cosine_ops");
        modelBuilder.Entity<Post>().HasOne(post => post.Location).WithMany().HasForeignKey(post => post.LocationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Post>().HasOne(post => post.User).WithMany().HasForeignKey(post => post.UserId).OnDelete(DeleteBehavior.Restrict);
        base.OnModelCreating(modelBuilder);
    }

    private void MarkAsDeleted(object entity)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.MarkDeleted(DateTime.UtcNow);
            Entry(entity).State = EntityState.Modified;
            return;
        }

        base.Remove(entity);
    }

    private void UpdateEntityTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.Entity.MarkDeleted(now);
                entry.State = EntityState.Modified;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAt(now);
                entry.Entity.SetUpdatedAt(now);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdatedAt(now);
                entry.Property(entity => entity.CreatedAt).IsModified = false;
            }
        }
    }
}
