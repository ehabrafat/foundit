using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Infrastructure.Data;

namespace Infrastructure.Migrations;

[DbContext(typeof(ApiDbContext))]
[Migration("20261119090000_AddPostEmbeddings")]
public sealed class AddPostEmbeddings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
        migrationBuilder.AddColumn<Pgvector.Vector>(
            name: "Embedding",
            table: "Posts",
            type: "vector(1536)",
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_Posts_Embedding",
            table: "Posts",
            column: "Embedding")
            .Annotation("Npgsql:IndexMethod", "hnsw")
            .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Posts_Embedding", table: "Posts");
        migrationBuilder.DropColumn(name: "Embedding", table: "Posts");
    }
}
