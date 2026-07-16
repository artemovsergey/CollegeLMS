using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Добавляем колонку как nullable (без default)
            migrationBuilder.AddColumn<string>(
                name: "login",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true
            );

            // 2. Заполняем login для существующих пользователей (часть email до @)
            migrationBuilder.Sql(
                "UPDATE users SET login = SPLIT_PART(email, '@', 1) WHERE login IS NULL"
            );

            // 3. Делаем колонку NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "login",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true
            );

            // 4. Создаём уникальный индекс
            migrationBuilder.CreateIndex(
                name: "ix_users_login",
                table: "users",
                column: "login",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_users_login", table: "users");

            migrationBuilder.DropColumn(name: "login", table: "users");
        }
    }
}
