using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeNameTeachersDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_practices_teachers_teacher_id",
                table: "practices"
            );

            migrationBuilder.DropIndex(name: "ix_practices_teacher_id", table: "practices");

            migrationBuilder.DropColumn(name: "organization", table: "practices");

            migrationBuilder.DropColumn(name: "teacher_id", table: "practices");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "practices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: ""
            );

            // Защитный backfill названия для существующих строк (миграция не должна падать).
            migrationBuilder.Sql(
                "UPDATE practices SET name = CASE kind WHEN 'Up' THEN 'УП' ELSE 'ПП' END WHERE name = '';"
            );

            migrationBuilder.CreateTable(
                name: "practice_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    practice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    pair_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_practice_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_practice_days_practices_practice_id",
                        column: x => x.practice_id,
                        principalTable: "practices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "practice_teachers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    practice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_practice_teachers", x => x.id);
                    table.ForeignKey(
                        name: "fk_practice_teachers_practices_practice_id",
                        column: x => x.practice_id,
                        principalTable: "practices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_practice_teachers_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_practice_days_practice_date",
                table: "practice_days",
                columns: new[] { "practice_id", "date" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_practice_days_practice_id",
                table: "practice_days",
                column: "practice_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_practice_teachers_practice_id",
                table: "practice_teachers",
                column: "practice_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_practice_teachers_practice_teacher",
                table: "practice_teachers",
                columns: new[] { "practice_id", "teacher_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_practice_teachers_teacher_id",
                table: "practice_teachers",
                column: "teacher_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "practice_days");

            migrationBuilder.DropTable(name: "practice_teachers");

            migrationBuilder.DropColumn(name: "name", table: "practices");

            migrationBuilder.AddColumn<string>(
                name: "organization",
                table: "practices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "teacher_id",
                table: "practices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.CreateIndex(
                name: "ix_practices_teacher_id",
                table: "practices",
                column: "teacher_id"
            );

            migrationBuilder.AddForeignKey(
                name: "fk_practices_teachers_teacher_id",
                table: "practices",
                column: "teacher_id",
                principalTable: "teachers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
