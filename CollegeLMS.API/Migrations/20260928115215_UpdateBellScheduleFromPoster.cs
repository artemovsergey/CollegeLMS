using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBellScheduleFromPoster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000008")
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000001"),
                column: "end_time",
                value: new TimeSpan(0, 10, 0, 0, 0)
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000002"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 11, 40, 0, 0),
                    new TimeSpan(0, 10, 10, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000003"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 13, 40, 0, 0),
                    new TimeSpan(0, 12, 10, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000004"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 15, 20, 0, 0),
                    new TimeSpan(0, 13, 50, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000005"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 15, 30, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000006"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 18, 40, 0, 0),
                    new TimeSpan(0, 17, 10, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000007"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 20, 20, 0, 0),
                    new TimeSpan(0, 18, 50, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000001"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 10, 55, 0, 0), new TimeSpan(0, 9, 25, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000002"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 12, 35, 0, 0), new TimeSpan(0, 11, 5, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000003"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 14, 35, 0, 0), new TimeSpan(0, 13, 5, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000004"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 16, 15, 0, 0),
                    new TimeSpan(0, 14, 45, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000005"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 17, 55, 0, 0),
                    new TimeSpan(0, 16, 25, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000006"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 19, 35, 0, 0), new TimeSpan(0, 18, 5, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "big_breaks",
                keyColumn: "id",
                keyValue: new Guid("b2000000-0000-0000-0000-000000000001"),
                columns: new[] { "after_pair", "end_time", "start_time" },
                values: new object[]
                {
                    2,
                    new TimeSpan(0, 12, 10, 0, 0),
                    new TimeSpan(0, 11, 40, 0, 0),
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
                    "profile_id",
                    "start_time",
                    "updated_at",
                },
                values: new object[]
                {
                    new Guid("b2000000-0000-0000-0000-000000000002"),
                    2,
                    new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    new TimeSpan(0, 13, 5, 0, 0),
                    new Guid("b3000000-0000-0000-0000-000000000002"),
                    new TimeSpan(0, 12, 35, 0, 0),
                    new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "big_breaks",
                keyColumn: "id",
                keyValue: new Guid("b2000000-0000-0000-0000-000000000002")
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000001"),
                column: "end_time",
                value: new TimeSpan(0, 9, 50, 0, 0)
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000002"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 11, 20, 0, 0), new TimeSpan(0, 10, 0, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000003"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 12, 50, 0, 0),
                    new TimeSpan(0, 11, 30, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000004"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 14, 20, 0, 0), new TimeSpan(0, 13, 0, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000005"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 16, 25, 0, 0), new TimeSpan(0, 15, 5, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000006"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 17, 55, 0, 0),
                    new TimeSpan(0, 16, 35, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0001-000000000007"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 19, 25, 0, 0), new TimeSpan(0, 18, 5, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000001"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 10, 40, 0, 0), new TimeSpan(0, 9, 10, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000002"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 12, 20, 0, 0),
                    new TimeSpan(0, 10, 50, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000003"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 14, 20, 0, 0),
                    new TimeSpan(0, 12, 50, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000004"),
                columns: new[] { "end_time", "start_time" },
                values: new object[] { new TimeSpan(0, 16, 0, 0, 0), new TimeSpan(0, 14, 30, 0, 0) }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000005"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 17, 40, 0, 0),
                    new TimeSpan(0, 16, 10, 0, 0),
                }
            );

            migrationBuilder.UpdateData(
                table: "bell_slots",
                keyColumn: "id",
                keyValue: new Guid("b1000000-0000-0000-0002-000000000006"),
                columns: new[] { "end_time", "start_time" },
                values: new object[]
                {
                    new TimeSpan(0, 19, 20, 0, 0),
                    new TimeSpan(0, 17, 50, 0, 0),
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
                    "profile_id",
                    "start_time",
                    "updated_at",
                },
                values: new object[]
                {
                    new Guid("b1000000-0000-0000-0001-000000000008"),
                    new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    new TimeSpan(0, 20, 55, 0, 0),
                    8,
                    new Guid("b3000000-0000-0000-0000-000000000001"),
                    new TimeSpan(0, 19, 35, 0, 0),
                    new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                }
            );

            migrationBuilder.UpdateData(
                table: "big_breaks",
                keyColumn: "id",
                keyValue: new Guid("b2000000-0000-0000-0000-000000000001"),
                columns: new[] { "after_pair", "end_time", "start_time" },
                values: new object[]
                {
                    4,
                    new TimeSpan(0, 15, 5, 0, 0),
                    new TimeSpan(0, 14, 20, 0, 0),
                }
            );
        }
    }
}
