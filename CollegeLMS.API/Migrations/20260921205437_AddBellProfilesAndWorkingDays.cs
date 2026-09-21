using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddBellProfilesAndWorkingDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_bell_slots_number_pair", table: "bell_slots");

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000001")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000002")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000003")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000004")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000005")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000006")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000007")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000008")
            );

            migrationBuilder.AddColumn<Guid>(
                name: "profile_id",
                table: "big_breaks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<Guid>(
                name: "profile_id",
                table: "bell_slots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.CreateTable(
                name: "bell_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    days_of_week = table.Column<int[]>(type: "integer[]", nullable: false),
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
                    table.PrimaryKey("pk_bell_profiles", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "working_day_overrides",
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
                    substitute_day_of_week = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_working_day_overrides", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "bell_profile_dates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_from = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    date_to = table.Column<DateTime>(
                        type: "timestamp without time zone",
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
                    table.PrimaryKey("pk_bell_profile_dates", x => x.id);
                    table.ForeignKey(
                        name: "fk_bell_profile_dates_bell_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "bell_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.InsertData(
                table: "bell_profiles",
                columns: new[]
                {
                    "id",
                    "created_at",
                    "days_of_week",
                    "is_default",
                    "name",
                    "updated_at",
                },
                values: new object[,]
                {
                    {
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new int[0],
                        true,
                        "Обычный",
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b3000000-0000-0000-0000-000000000002"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new[] { 1 },
                        false,
                        "Понедельник",
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b3000000-0000-0000-0000-000000000003"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new[] { 4 },
                        false,
                        "Четверг",
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                }
            );

            // Строки, созданные вручную (не из seed), привязываем к базовому профилю.
            migrationBuilder.Sql(
                "UPDATE bell_slots SET profile_id = 'b3000000-0000-0000-0000-000000000001' WHERE profile_id = '00000000-0000-0000-0000-000000000000';"
            );
            migrationBuilder.Sql(
                "UPDATE big_breaks SET profile_id = 'b3000000-0000-0000-0000-000000000001' WHERE profile_id = '00000000-0000-0000-0000-000000000000';"
            );

            migrationBuilder.UpdateData(
                table: "big_breaks",
                keyColumn: "id",
                keyValue: new Guid("b2000000-0000-0000-0000-000000000001"),
                column: "profile_id",
                value: new Guid("b3000000-0000-0000-0000-000000000001")
            );

            migrationBuilder.InsertData(
                table: "bell_slots",
                columns: new[]
                {
                    "id",
                    "created_at",
                    "end_time",
                    "number_pair",
                    "profile_id",
                    "start_time",
                    "updated_at",
                },
                values: new object[,]
                {
                    {
                        new Guid("b1000000-0000-0000-0001-000000000001"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 9, 50, 0, 0),
                        1,
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new TimeSpan(0, 8, 30, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0001-000000000002"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 11, 20, 0, 0),
                        2,
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new TimeSpan(0, 10, 0, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0001-000000000003"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 12, 50, 0, 0),
                        3,
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new TimeSpan(0, 11, 30, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0001-000000000004"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 14, 20, 0, 0),
                        4,
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new TimeSpan(0, 13, 0, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0001-000000000005"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 16, 25, 0, 0),
                        5,
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new TimeSpan(0, 15, 5, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0001-000000000006"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 17, 55, 0, 0),
                        6,
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new TimeSpan(0, 16, 35, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0001-000000000007"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 19, 25, 0, 0),
                        7,
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new TimeSpan(0, 18, 5, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0001-000000000008"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 20, 55, 0, 0),
                        8,
                        new Guid("b3000000-0000-0000-0000-000000000001"),
                        new TimeSpan(0, 19, 35, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0002-000000000001"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 10, 40, 0, 0),
                        1,
                        new Guid("b3000000-0000-0000-0000-000000000002"),
                        new TimeSpan(0, 9, 10, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0002-000000000002"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 12, 20, 0, 0),
                        2,
                        new Guid("b3000000-0000-0000-0000-000000000002"),
                        new TimeSpan(0, 10, 50, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0002-000000000003"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 14, 20, 0, 0),
                        3,
                        new Guid("b3000000-0000-0000-0000-000000000002"),
                        new TimeSpan(0, 12, 50, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0002-000000000004"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 16, 0, 0, 0),
                        4,
                        new Guid("b3000000-0000-0000-0000-000000000002"),
                        new TimeSpan(0, 14, 30, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0002-000000000005"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 17, 40, 0, 0),
                        5,
                        new Guid("b3000000-0000-0000-0000-000000000002"),
                        new TimeSpan(0, 16, 10, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0002-000000000006"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 19, 20, 0, 0),
                        6,
                        new Guid("b3000000-0000-0000-0000-000000000002"),
                        new TimeSpan(0, 17, 50, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0003-000000000001"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 10, 0, 0, 0),
                        1,
                        new Guid("b3000000-0000-0000-0000-000000000003"),
                        new TimeSpan(0, 8, 30, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0003-000000000002"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 11, 40, 0, 0),
                        2,
                        new Guid("b3000000-0000-0000-0000-000000000003"),
                        new TimeSpan(0, 10, 10, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0003-000000000003"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 14, 30, 0, 0),
                        3,
                        new Guid("b3000000-0000-0000-0000-000000000003"),
                        new TimeSpan(0, 13, 0, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0003-000000000004"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 16, 10, 0, 0),
                        4,
                        new Guid("b3000000-0000-0000-0000-000000000003"),
                        new TimeSpan(0, 14, 40, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0003-000000000005"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 17, 50, 0, 0),
                        5,
                        new Guid("b3000000-0000-0000-0000-000000000003"),
                        new TimeSpan(0, 16, 20, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                    {
                        new Guid("b1000000-0000-0000-0003-000000000006"),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        new TimeSpan(0, 19, 30, 0, 0),
                        6,
                        new Guid("b3000000-0000-0000-0000-000000000003"),
                        new TimeSpan(0, 18, 0, 0, 0),
                        new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    },
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_big_breaks_profile_id",
                table: "big_breaks",
                column: "profile_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_bell_slots_profile_pair",
                table: "bell_slots",
                columns: new[] { "profile_id", "number_pair" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_bell_profile_dates_date_from",
                table: "bell_profile_dates",
                column: "date_from"
            );

            migrationBuilder.CreateIndex(
                name: "ix_bell_profile_dates_date_to",
                table: "bell_profile_dates",
                column: "date_to"
            );

            migrationBuilder.CreateIndex(
                name: "ix_bell_profile_dates_profile_id",
                table: "bell_profile_dates",
                column: "profile_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_bell_profiles_name",
                table: "bell_profiles",
                column: "name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_working_day_overrides_date_from",
                table: "working_day_overrides",
                column: "date_from"
            );

            migrationBuilder.CreateIndex(
                name: "ix_working_day_overrides_date_to",
                table: "working_day_overrides",
                column: "date_to"
            );

            migrationBuilder.AddForeignKey(
                name: "fk_bell_slots_bell_profiles_profile_id",
                table: "bell_slots",
                column: "profile_id",
                principalTable: "bell_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "fk_big_breaks_bell_profiles_profile_id",
                table: "big_breaks",
                column: "profile_id",
                principalTable: "bell_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bell_slots_bell_profiles_profile_id",
                table: "bell_slots"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_big_breaks_bell_profiles_profile_id",
                table: "big_breaks"
            );

            migrationBuilder.DropTable(name: "bell_profile_dates");

            migrationBuilder.DropTable(name: "working_day_overrides");

            migrationBuilder.DropTable(name: "bell_profiles");

            migrationBuilder.DropIndex(name: "ix_big_breaks_profile_id", table: "big_breaks");

            migrationBuilder.DropIndex(name: "ix_bell_slots_profile_pair", table: "bell_slots");

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000001")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000002")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000003")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000004")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000005")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000006")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000007")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000008")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000001")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000002")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000003")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000004")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000005")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000006")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0003-000000000001")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0003-000000000002")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0003-000000000003")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0003-000000000004")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0003-000000000005")
            );

            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0003-000000000006")
            );

            migrationBuilder.DropColumn(name: "profile_id", table: "big_breaks");

            migrationBuilder.DropColumn(name: "profile_id", table: "bell_slots");

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

            migrationBuilder.CreateIndex(
                name: "ix_bell_slots_number_pair",
                table: "bell_slots",
                column: "number_pair",
                unique: true
            );
        }
    }
}
