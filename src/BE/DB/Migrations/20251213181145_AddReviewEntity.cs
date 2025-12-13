using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DB.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exchange_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    created = table.Column<DateTime>(type: "timestamp", nullable: false),
                    modified = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_Reviews_Exchanges_exchange_id",
                        column: x => x.exchange_id,
                        principalTable: "Exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_reviewed_user_id",
                        column: x => x.reviewed_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_reviewer_id",
                        column: x => x.reviewer_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_exchange_id_reviewer_id",
                table: "Reviews",
                columns: new[] { "exchange_id", "reviewer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_reviewed_user_id",
                table: "Reviews",
                column: "reviewed_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_reviewer_id",
                table: "Reviews",
                column: "reviewer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");
        }
    }
}
