using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkProcesses.Migrations
{
    public partial class AddReferenceBooks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskAssignment_AspNetUsers_AppUserId",
                table: "TaskAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskAssignment_Tasks_TaskItemId",
                table: "TaskAssignment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskAssignment",
                table: "TaskAssignment");

            migrationBuilder.RenameTable(
                name: "TaskAssignment",
                newName: "TaskAssignments");

            migrationBuilder.RenameIndex(
                name: "IX_TaskAssignment_TaskItemId_AppUserId",
                table: "TaskAssignments",
                newName: "IX_TaskAssignments_TaskItemId_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskAssignment_AppUserId",
                table: "TaskAssignments",
                newName: "IX_TaskAssignments_AppUserId");

            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportMonthDay",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportPeriodicity",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportTime",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportWeekDay",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResourceId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkBasisComment",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkBasisId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkTypeId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DispatchCenterId",
                table: "Divisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskAssignments",
                table: "TaskAssignments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "DispatchCenters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Priorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Priorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludeInMonthlyPlan = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInWeeklyPlan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkBases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkBases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResourceTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceDepartments_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_PriorityId",
                table: "Tasks",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ResourceId",
                table: "Tasks",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_WorkBasisId",
                table: "Tasks",
                column: "WorkBasisId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_WorkTypeId",
                table: "Tasks",
                column: "WorkTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_DispatchCenterId",
                table: "Divisions",
                column: "DispatchCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceDepartments_DepartmentId",
                table: "ResourceDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceDepartments_ResourceId",
                table: "ResourceDepartments",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ResourceTypeId",
                table: "Resources",
                column: "ResourceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Divisions_DispatchCenters_DispatchCenterId",
                table: "Divisions",
                column: "DispatchCenterId",
                principalTable: "DispatchCenters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAssignments_AspNetUsers_AppUserId",
                table: "TaskAssignments",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAssignments_Tasks_TaskItemId",
                table: "TaskAssignments",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Priorities_PriorityId",
                table: "Tasks",
                column: "PriorityId",
                principalTable: "Priorities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Resources_ResourceId",
                table: "Tasks",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_WorkBases_WorkBasisId",
                table: "Tasks",
                column: "WorkBasisId",
                principalTable: "WorkBases",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_WorkTypes_WorkTypeId",
                table: "Tasks",
                column: "WorkTypeId",
                principalTable: "WorkTypes",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Divisions_DispatchCenters_DispatchCenterId",
                table: "Divisions");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskAssignments_AspNetUsers_AppUserId",
                table: "TaskAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskAssignments_Tasks_TaskItemId",
                table: "TaskAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Priorities_PriorityId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Resources_ResourceId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_WorkBases_WorkBasisId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_WorkTypes_WorkTypeId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "DispatchCenters");

            migrationBuilder.DropTable(
                name: "Priorities");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "ResourceDepartments");

            migrationBuilder.DropTable(
                name: "WorkBases");

            migrationBuilder.DropTable(
                name: "WorkTypes");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "ResourceTypes");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_PriorityId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ResourceId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_WorkBasisId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_WorkTypeId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Divisions_DispatchCenterId",
                table: "Divisions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskAssignments",
                table: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ReportMonthDay",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ReportPeriodicity",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ReportTime",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ReportWeekDay",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ResourceId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "WorkBasisComment",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "WorkBasisId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "WorkTypeId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DispatchCenterId",
                table: "Divisions");

            migrationBuilder.RenameTable(
                name: "TaskAssignments",
                newName: "TaskAssignment");

            migrationBuilder.RenameIndex(
                name: "IX_TaskAssignments_TaskItemId_AppUserId",
                table: "TaskAssignment",
                newName: "IX_TaskAssignment_TaskItemId_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskAssignments_AppUserId",
                table: "TaskAssignment",
                newName: "IX_TaskAssignment_AppUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskAssignment",
                table: "TaskAssignment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAssignment_AspNetUsers_AppUserId",
                table: "TaskAssignment",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAssignment_Tasks_TaskItemId",
                table: "TaskAssignment",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
