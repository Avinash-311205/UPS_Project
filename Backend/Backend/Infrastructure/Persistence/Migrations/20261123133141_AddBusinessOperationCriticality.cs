using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessOperationCriticality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Criticality",
                table: "BusinessOperations",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOperations_TenantId_Criticality",
                table: "BusinessOperations",
                columns: new[] { "TenantId", "Criticality" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessOperations_TenantId_Criticality",
                table: "BusinessOperations");

            migrationBuilder.DropColumn(
                name: "Criticality",
                table: "BusinessOperations");
        }
    }
}
