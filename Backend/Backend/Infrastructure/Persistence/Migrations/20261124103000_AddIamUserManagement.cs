using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIamUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            // Generated column: RootTenantId equals TenantId when AccountType is Root (1), NULL otherwise.
            // In MySQL, multiple NULL values are treated as distinct by unique indexes.
            // This guarantees that each Tenant can have at most ONE Root account at the database engine level.
            migrationBuilder.Sql("ALTER TABLE Users ADD COLUMN RootTenantId char(36) GENERATED ALWAYS AS (CASE WHEN AccountType = 1 THEN TenantId ELSE NULL END) STORED;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX UX_Users_RootTenantId ON Users (RootTenantId);");

            // Precision account migration using inspected Primary Keys and expected TenantIds
            // User 1: Aditya (Owner of NorthStar Logistics tenant) -> Root
            migrationBuilder.Sql("UPDATE Users SET AccountType = 1, SecurityStamp = UUID(), UpdatedAt = UTC_TIMESTAMP(6) WHERE Id = 1 AND TenantId = '1ca3b8d4-1fa7-47bc-8b68-11c702086a4f';");

            // User 3: ResilienceAdmin (Owner of ABC Logistics tenant) -> Root
            migrationBuilder.Sql("UPDATE Users SET AccountType = 1, SecurityStamp = UUID(), UpdatedAt = UTC_TIMESTAMP(6) WHERE Id = 3 AND TenantId = '418646de-f135-4ab5-bf7b-99b36e5d506f';");

            // User 2: Zuu (Pre-existing unassigned legacy account) -> IAM (TenantId remains NULL)
            migrationBuilder.Sql("UPDATE Users SET AccountType = 2, SecurityStamp = UUID(), UpdatedAt = UTC_TIMESTAMP(6) WHERE Id = 2 AND TenantId IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX UX_Users_RootTenantId ON Users;");
            migrationBuilder.DropColumn(name: "RootTenantId", table: "Users");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Users");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Users");
            migrationBuilder.DropColumn(name: "SecurityStamp", table: "Users");
            migrationBuilder.DropColumn(name: "Status", table: "Users");
            migrationBuilder.DropColumn(name: "AccountType", table: "Users");
        }
    }
}
