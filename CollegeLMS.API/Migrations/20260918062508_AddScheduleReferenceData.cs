using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bell_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number_pair = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
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
                    table.PrimaryKey("pk_bell_slots", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "big_breaks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    after_pair = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
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
                    table.PrimaryKey("pk_big_breaks", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "non_working_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_from = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    date_to = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    title = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
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
                    table.PrimaryKey("pk_non_working_days", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "practices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(
                        type: "character varying(10)",
                        maxLength: 10,
                        nullable: false
                    ),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_from = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    date_to = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    organization = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    note = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
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
                    table.PrimaryKey("pk_practices", x => x.id);
                    table.ForeignKey(
                        name: "fk_practices_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_practices_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "schedule_inserts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    day_of_week = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    course = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_schedule_inserts", x => x.id);
                }
            );

            migrationBuilder.InsertData(
                table: "bell_slots",
                columns: new[]
                {
                    "id",
                    "created_at",
                    "end_time",
                    "number_pair",
                    "start_time",
                    "updated_at",
                },
                values: new object[,]
                {
                    {
                        new Guid("b1000000-0000-0000-0000-000000000001"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 9, 50, 0, 0),
                        1,
                        new TimeSpan(0, 8, 30, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0000-000000000002"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 11, 20, 0, 0),
                        2,
                        new TimeSpan(0, 10, 0, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0000-000000000003"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 12, 50, 0, 0),
                        3,
                        new TimeSpan(0, 11, 30, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0000-000000000004"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 14, 20, 0, 0),
                        4,
                        new TimeSpan(0, 13, 0, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0000-000000000005"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 16, 25, 0, 0),
                        5,
                        new TimeSpan(0, 15, 5, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0000-000000000006"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 17, 55, 0, 0),
                        6,
                        new TimeSpan(0, 16, 35, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0000-000000000007"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 19, 25, 0, 0),
                        7,
                        new TimeSpan(0, 18, 5, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0000-000000000008"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 20, 55, 0, 0),
                        8,
                        new TimeSpan(0, 19, 35, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "big_breaks",
                columns: new[]
                {
                    "id",
                    "after_pair",
                    "created_at",
                    "end_time",
                    "start_time",
                    "updated_at",
                },
                values: new object[]
                {
                    new Guid("b2000000-0000-0000-0000-000000000001"),
                    4,
                    new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    new TimeSpan(0, 15, 5, 0, 0),
                    new TimeSpan(0, 14, 20, 0, 0),
                    new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                }
            );

            migrationBuilder.InsertData(
                table: "schedule_inserts",
                columns: new[]
                {
                    "id",
                    "course",
                    "created_at",
                    "day_of_week",
                    "end_time",
                    "is_active",
                    "start_time",
                    "title",
                    "updated_at",
                },
                values: new object[,]
                {
                    {
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        null,
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        "Monday",
                        new TimeSpan(0, 9, 0, 0, 0),
                        true,
                        new TimeSpan(0, 8, 30, 0, 0),
                        "Разговор о важном",
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b3000000-0000-0000-0000-000000000002"),
                        null,
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        "Thursday",
                        new TimeSpan(0, 13, 0, 0, 0),
                        true,
                        new TimeSpan(0, 12, 10, 0, 0),
                        "Классный час",
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_bell_slots_number_pair",
                table: "bell_slots",
                column: "number_pair",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_non_working_days_date_from",
                table: "non_working_days",
                column: "date_from"
            );

            migrationBuilder.CreateIndex(
                name: "ix_non_working_days_date_to",
                table: "non_working_days",
                column: "date_to"
            );

            migrationBuilder.CreateIndex(
                name: "ix_practices_date_from",
                table: "practices",
                column: "date_from"
            );

            migrationBuilder.CreateIndex(
                name: "ix_practices_group_id",
                table: "practices",
                column: "group_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_practices_teacher_id",
                table: "practices",
                column: "teacher_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_schedule_inserts_day_of_week",
                table: "schedule_inserts",
                column: "day_of_week"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bell_slots");

            migrationBuilder.DropTable(name: "big_breaks");

            migrationBuilder.DropTable(name: "non_working_days");

            migrationBuilder.DropTable(name: "practices");

            migrationBuilder.DropTable(name: "schedule_inserts");
        }
    }
}
