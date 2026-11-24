using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessOperationAvailabilityRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailabilityRule",
                table: "BusinessOperations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOperations_TenantId_AvailabilityRule",
                table: "BusinessOperations",
                columns: new[] { "TenantId", "AvailabilityRule" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessOperations_TenantId_AvailabilityRule",
                table: "BusinessOperations");

            migrationBuilder.DropColumn(
                name: "AvailabilityRule",
                table: "BusinessOperations");
        }
    }
}
