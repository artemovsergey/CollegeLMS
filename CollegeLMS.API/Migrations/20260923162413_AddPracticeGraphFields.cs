using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeGraphFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "room",
                table: "practices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "subgroup",
                table: "practices",
                type: "integer",
                nullable: true
            );

            migrationBuilder.AddColumn<int[]>(
                name: "pair_numbers",
                table: "practice_days",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]
            );

            // Перенос данных: из старого счётчика пар получаем последовательность 1..pair_count.
            migrationBuilder.Sql(
                """
                UPDATE practice_days
                SET pair_numbers = ARRAY(SELECT generate_series(1, pair_count))
                WHERE pair_count >= 1;
                """
            );

            migrationBuilder.DropColumn(name: "pair_count", table: "practice_days");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pair_count",
                table: "practice_days",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            // Обратный перенос: счётчик равен длине массива номеров пар.
            migrationBuilder.Sql(
                """
                UPDATE practice_days
                SET pair_count = COALESCE(array_length(pair_numbers, 1), 0);
                """
            );

            migrationBuilder.DropColumn(name: "pair_numbers", table: "practice_days");

            migrationBuilder.DropColumn(name: "room", table: "practices");

            migrationBuilder.DropColumn(name: "subgroup", table: "practices");
        }
    }
}
