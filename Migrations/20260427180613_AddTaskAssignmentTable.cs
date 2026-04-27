using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkProcesses.Migrations
{
    public partial class AddTaskAssignmentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskItemId = table.Column<int>(type: "int", nullable: false),
                    AppUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsInWork = table.Column<bool>(type: "bit", nullable: false),
                    WorkSeconds = table.Column<int>(type: "int", nullable: false),
                    WorkStartTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAssignment_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskAssignment_Tasks_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignment_AppUserId",
                table: "TaskAssignment",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignment_TaskItemId_AppUserId",
                table: "TaskAssignment",
                columns: new[] { "TaskItemId", "AppUserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskAssignment");
        }
    }
}
