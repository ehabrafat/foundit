using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(ApiDbContext))]
[Migration("20261115100000_AddPasswordResetToken")]
public partial class AddPasswordResetToken : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PasswordResetTokenHash", table: "Users", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "PasswordResetTokenExpiresAtUtc", table: "Users", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PasswordResetTokenHash", table: "Users");
        migrationBuilder.DropColumn(name: "PasswordResetTokenExpiresAtUtc", table: "Users");
    }
}
