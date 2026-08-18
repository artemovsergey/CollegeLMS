using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarAndTeacherCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar_path",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "teachers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: ""
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "avatar_path", table: "users");

            migrationBuilder.DropColumn(name: "category", table: "teachers");
        }
    }
}
