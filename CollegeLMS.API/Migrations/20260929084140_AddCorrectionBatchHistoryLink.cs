using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrectionBatchHistoryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Статус «Отменён» больше не используется: такие пакеты считаем черновыми.
            migrationBuilder.Sql(
                "UPDATE correction_batches SET status = 'Draft' WHERE status = 'Cancelled';"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "batch_id",
                table: "schedule_history",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_schedule_history_batch_id",
                table: "schedule_history",
                column: "batch_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_schedule_history_batch_id",
                table: "schedule_history"
            );

            migrationBuilder.DropColumn(name: "batch_id", table: "schedule_history");
        }
    }
}
