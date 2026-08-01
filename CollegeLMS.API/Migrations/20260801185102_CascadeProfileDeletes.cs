using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class CascadeProfileDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assignment_submissions_students_student_id",
                table: "assignment_submissions"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_retakes_students_student_id",
                table: "retakes"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_stipend_list_items_students_student_id",
                table: "stipend_list_items"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_test_attempts_students_student_id",
                table: "test_attempts"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_transfer_records_students_student_id",
                table: "transfer_records"
            );

            migrationBuilder.AddForeignKey(
                name: "fk_assignment_submissions_students_student_id",
                table: "assignment_submissions",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "fk_retakes_students_student_id",
                table: "retakes",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "fk_stipend_list_items_students_student_id",
                table: "stipend_list_items",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "fk_test_attempts_students_student_id",
                table: "test_attempts",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "fk_transfer_records_students_student_id",
                table: "transfer_records",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assignment_submissions_students_student_id",
                table: "assignment_submissions"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_retakes_students_student_id",
                table: "retakes"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_stipend_list_items_students_student_id",
                table: "stipend_list_items"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_test_attempts_students_student_id",
                table: "test_attempts"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_transfer_records_students_student_id",
                table: "transfer_records"
            );

            migrationBuilder.AddForeignKey(
                name: "fk_assignment_submissions_students_student_id",
                table: "assignment_submissions",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "fk_retakes_students_student_id",
                table: "retakes",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "fk_stipend_list_items_students_student_id",
                table: "stipend_list_items",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "fk_test_attempts_students_student_id",
                table: "test_attempts",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "fk_transfer_records_students_student_id",
                table: "transfer_records",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
        }
    }
}
