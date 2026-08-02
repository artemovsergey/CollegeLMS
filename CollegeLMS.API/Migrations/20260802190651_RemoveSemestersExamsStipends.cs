using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSemestersExamsStipends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "retakes");

            migrationBuilder.DropTable(name: "stipend_list_items");

            migrationBuilder.DropTable(name: "exams");

            migrationBuilder.DropTable(name: "stipend_lists");

            migrationBuilder.DropTable(name: "semesters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "semesters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    end_date = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    name = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    start_date = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    type = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
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
                    table.PrimaryKey("pk_semesters", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "exams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    semester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    exam_date = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    subject = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    type = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
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
                    table.PrimaryKey("pk_exams", x => x.id);
                    table.ForeignKey(
                        name: "fk_exams_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_exams_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_exams_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "stipend_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    semester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    name = table.Column<string>(
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
                    table.PrimaryKey("pk_stipend_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_stipend_lists_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "retakes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    reason = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: false
                    ),
                    retake_date = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
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
                    table.PrimaryKey("pk_retakes", x => x.id);
                    table.ForeignKey(
                        name: "fk_retakes_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_retakes_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "stipend_list_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stipend_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    average_score = table.Column<double>(type: "double precision", nullable: false),
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
                    table.PrimaryKey("pk_stipend_list_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_stipend_list_items_stipend_lists_stipend_list_id",
                        column: x => x.stipend_list_id,
                        principalTable: "stipend_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_stipend_list_items_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_exams_group_id",
                table: "exams",
                column: "group_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_exams_semester_id",
                table: "exams",
                column: "semester_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_exams_teacher_id",
                table: "exams",
                column: "teacher_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_retakes_exam_id",
                table: "retakes",
                column: "exam_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_retakes_student_id",
                table: "retakes",
                column: "student_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_semesters_name",
                table: "semesters",
                column: "name"
            );

            migrationBuilder.CreateIndex(
                name: "ix_stipend_list_items_stipend_list_id",
                table: "stipend_list_items",
                column: "stipend_list_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_stipend_list_items_student_id",
                table: "stipend_list_items",
                column: "student_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_stipend_lists_semester_id",
                table: "stipend_lists",
                column: "semester_id"
            );
        }
    }
}
