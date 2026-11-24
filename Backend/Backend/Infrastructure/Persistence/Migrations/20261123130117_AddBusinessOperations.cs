using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    LifecycleStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessOperations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SystemBusinessOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    LogisticsSystemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    BusinessOperationId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemBusinessOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemBusinessOperations_BusinessOperations_BusinessOperatio~",
                        column: x => x.BusinessOperationId,
                        principalTable: "BusinessOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemBusinessOperations_LogisticsSystems_LogisticsSystemId",
                        column: x => x.LogisticsSystemId,
                        principalTable: "LogisticsSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemBusinessOperations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOperations_TenantId_LifecycleStatus",
                table: "BusinessOperations",
                columns: new[] { "TenantId", "LifecycleStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOperations_TenantId_Name",
                table: "BusinessOperations",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemBusinessOperations_BusinessOperationId",
                table: "SystemBusinessOperations",
                column: "BusinessOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemBusinessOperations_LogisticsSystemId",
                table: "SystemBusinessOperations",
                column: "LogisticsSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemBusinessOperations_TenantId",
                table: "SystemBusinessOperations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemBusinessOperations_TenantId_LogisticsSystemId_Business~",
                table: "SystemBusinessOperations",
                columns: new[] { "TenantId", "LogisticsSystemId", "BusinessOperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemBusinessOperations");

            migrationBuilder.DropTable(
                name: "BusinessOperations");
        }
    }
}
