using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonsRemoveAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "assignment_submissions");

            migrationBuilder.DropTable(name: "assignments");

            migrationBuilder.DropColumn(name: "assignment_id", table: "course_materials");

            migrationBuilder.RenameColumn(
                name: "lecture_id",
                table: "course_materials",
                newName: "lesson_id"
            );

            migrationBuilder.RenameTable(name: "lectures", newName: "lessons");

            migrationBuilder.RenameColumn(name: "lecture_type", table: "lessons", newName: "kind");

            migrationBuilder.AddColumn<bool>(
                name: "is_current",
                table: "lessons",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.RenameIndex(
                name: "ix_lectures_course_id_order",
                table: "lessons",
                newName: "ix_lessons_course_id_order"
            );

            migrationBuilder.RenameIndex(
                name: "ix_lectures_test_id",
                table: "lessons",
                newName: "ix_lessons_test_id"
            );

            migrationBuilder.Sql("ALTER INDEX pk_lectures RENAME TO pk_lessons;");

            migrationBuilder.CreateTable(
                name: "course_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    file_path = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    content_type = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_course_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_course_documents_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_course_documents_course_id",
                table: "course_documents",
                column: "course_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "course_documents");

            migrationBuilder.DropIndex(name: "ix_lessons_course_id_order", table: "lessons");

            migrationBuilder.DropIndex(name: "ix_lessons_test_id", table: "lessons");

            migrationBuilder.DropColumn(name: "is_current", table: "lessons");

            migrationBuilder.RenameColumn(name: "kind", table: "lessons", newName: "lecture_type");

            migrationBuilder.RenameTable(name: "lessons", newName: "lectures");

            migrationBuilder.RenameColumn(
                name: "lesson_id",
                table: "course_materials",
                newName: "lecture_id"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "assignment_id",
                table: "course_materials",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_lectures_course_id_order",
                table: "lectures",
                columns: new[] { "course_id", "order" }
            );

            migrationBuilder.CreateIndex(
                name: "ix_lectures_test_id",
                table: "lectures",
                column: "test_id"
            );

            migrationBuilder.CreateTable(
                name: "assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    description = table.Column<string>(
                        type: "character varying(4000)",
                        maxLength: 4000,
                        nullable: false
                    ),
                    due_date = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: true
                    ),
                    max_score = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 100
                    ),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    title = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignments_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "assignment_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comment = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    file_path = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    score = table.Column<int>(type: "integer", nullable: true),
                    submitted_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignment_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignment_submissions_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_assignment_submissions_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_assignment_submissions_assignment_id_student_id",
                table: "assignment_submissions",
                columns: new[] { "assignment_id", "student_id" }
            );

            migrationBuilder.CreateIndex(
                name: "ix_assignment_submissions_student_id",
                table: "assignment_submissions",
                column: "student_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_assignments_course_id_order",
                table: "assignments",
                columns: new[] { "course_id", "order" }
            );
        }
    }
}
