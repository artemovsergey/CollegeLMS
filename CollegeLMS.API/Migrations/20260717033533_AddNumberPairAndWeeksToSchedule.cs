using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberPairAndWeeksToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "number_pair",
                table: "schedule_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<int>>(
                name: "weeks",
                table: "schedule_entries",
                type: "integer[]",
                nullable: true);

            migrationBuilder.Sql("UPDATE schedule_entries SET weeks = ARRAY[1] WHERE weeks IS NULL");

            migrationBuilder.AlterColumn<List<int>>(
                name: "weeks",
                table: "schedule_entries",
                type: "integer[]",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "ix_schedule_entries_number_pair",
                table: "schedule_entries",
                column: "number_pair");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_schedule_entries_number_pair",
                table: "schedule_entries");

            migrationBuilder.DropColumn(
                name: "number_pair",
                table: "schedule_entries");

            migrationBuilder.DropColumn(
                name: "weeks",
                table: "schedule_entries");
        }
    }
}
