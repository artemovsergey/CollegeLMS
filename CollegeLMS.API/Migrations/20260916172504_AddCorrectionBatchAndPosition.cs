using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrectionBatchAndPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "correction_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correction_date = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    week = table.Column<int>(type: "integer", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at = table.Column<DateTime>(
                        type: "timestamp without time zone",
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
                    table.PrimaryKey("pk_correction_batches", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "correction_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    change_type = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    week = table.Column<int>(type: "integer", nullable: false),
                    number_pair = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    teacher_name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    removed_subject = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    removed_teacher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    removed_teacher_name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    removed_number_pair = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    history_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_correction_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_correction_positions_correction_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "correction_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_correction_batches_correction_date",
                table: "correction_batches",
                column: "correction_date"
            );

            migrationBuilder.CreateIndex(
                name: "ix_correction_batches_status",
                table: "correction_batches",
                column: "status"
            );

            migrationBuilder.CreateIndex(
                name: "ix_correction_positions_batch_id",
                table: "correction_positions",
                column: "batch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_correction_positions_status",
                table: "correction_positions",
                column: "status"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "correction_positions");

            migrationBuilder.DropTable(name: "correction_batches");
        }
    }
}
