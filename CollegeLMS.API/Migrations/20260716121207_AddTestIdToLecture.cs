using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddTestIdToLecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "test_id",
                table: "lectures",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_lectures_test_id",
                table: "lectures",
                column: "test_id"
            );

            migrationBuilder.AddForeignKey(
                name: "fk_lectures_tests_test_id",
                table: "lectures",
                column: "test_id",
                principalTable: "tests",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_lectures_tests_test_id", table: "lectures");

            migrationBuilder.DropIndex(name: "ix_lectures_test_id", table: "lectures");

            migrationBuilder.DropColumn(name: "test_id", table: "lectures");
        }
    }
}
