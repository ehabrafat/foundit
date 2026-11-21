using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(ApiDbContext))]
[Migration("20261030130000_AddBaseEntityColumns")]
public partial class AddBaseEntityColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: DateTime.UnixEpoch);

        migrationBuilder.AddColumn<DateTime?>(
            name: "DeletedAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: DateTime.UnixEpoch);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CreatedAt", table: "Users");
        migrationBuilder.DropColumn(name: "DeletedAt", table: "Users");
        migrationBuilder.DropColumn(name: "IsDeleted", table: "Users");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "Users");
    }
}
