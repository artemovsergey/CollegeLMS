using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleHistoryRemovedTeacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_schedule_history_removed_teacher_id",
                table: "schedule_history",
                column: "removed_teacher_id"
            );

            migrationBuilder.AddForeignKey(
                name: "fk_schedule_history_teachers_removed_teacher_id",
                table: "schedule_history",
                column: "removed_teacher_id",
                principalTable: "teachers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_schedule_history_teachers_removed_teacher_id",
                table: "schedule_history"
            );

            migrationBuilder.DropIndex(
                name: "ix_schedule_history_removed_teacher_id",
                table: "schedule_history"
            );
        }
    }
}
