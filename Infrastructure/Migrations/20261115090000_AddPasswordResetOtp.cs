using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(ApiDbContext))]
[Migration("20261115090000_AddPasswordResetOtp")]
public partial class AddPasswordResetOtp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PasswordResetOtpHash", table: "Users", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "PasswordResetOtpExpiresAtUtc", table: "Users", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PasswordResetOtpHash", table: "Users");
        migrationBuilder.DropColumn(name: "PasswordResetOtpExpiresAtUtc", table: "Users");
    }
}
