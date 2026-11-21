using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

[DbContext(typeof(ApiDbContext))]
[Migration("20261119101500_ResetPostEmbeddingsForGemini")]
public sealed class ResetPostEmbeddingsForGemini : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("UPDATE \"Posts\" SET \"Embedding\" = NULL;");

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Embeddings cannot be restored after they have been cleared.
    }
}
