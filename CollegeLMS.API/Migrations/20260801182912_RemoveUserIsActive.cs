using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_students_users_user_id", table: "students");

            migrationBuilder.DropForeignKey(name: "fk_teachers_users_user_id", table: "teachers");

            migrationBuilder.DropColumn(name: "is_active", table: "users");

            migrationBuilder.AddForeignKey(
                name: "fk_students_users_user_id",
                table: "students",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "fk_teachers_users_user_id",
                table: "teachers",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_students_users_user_id", table: "students");

            migrationBuilder.DropForeignKey(name: "fk_teachers_users_user_id", table: "teachers");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddForeignKey(
                name: "fk_students_users_user_id",
                table: "students",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "fk_teachers_users_user_id",
                table: "teachers",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
        }
    }
}
