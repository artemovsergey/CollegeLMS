using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGroupIdFromCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_courses_groups_group_id",
                table: "courses");

            migrationBuilder.DropIndex(
                name: "ix_courses_group_id",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "courses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_courses_group_id",
                table: "courses",
                column: "group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_courses_groups_group_id",
                table: "courses",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
