using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemDependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    DependentSystemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    DependencySystemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    DependencyType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemDependencies_LogisticsSystems_DependencySystemId",
                        column: x => x.DependencySystemId,
                        principalTable: "LogisticsSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemDependencies_LogisticsSystems_DependentSystemId",
                        column: x => x.DependentSystemId,
                        principalTable: "LogisticsSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemDependencies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SystemDependencies_DependencySystemId",
                table: "SystemDependencies",
                column: "DependencySystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemDependencies_DependentSystemId",
                table: "SystemDependencies",
                column: "DependentSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemDependencies_TenantId",
                table: "SystemDependencies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemDependencies_TenantId_DependencySystemId",
                table: "SystemDependencies",
                columns: new[] { "TenantId", "DependencySystemId" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemDependencies_TenantId_DependentSystemId",
                table: "SystemDependencies",
                columns: new[] { "TenantId", "DependentSystemId" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemDependencies_TenantId_DependentSystemId_DependencySyst~",
                table: "SystemDependencies",
                columns: new[] { "TenantId", "DependentSystemId", "DependencySystemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemDependencies");
        }
    }
}
