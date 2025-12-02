using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DB.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_Languages_language_id",
                table: "Books");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropIndex(
                name: "IX_Books_language_id",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "language_id",
                table: "Books");

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "Books",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "language",
                table: "Books");

            migrationBuilder.AddColumn<Guid>(
                name: "language_id",
                table: "Books",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    created = table.Column<DateTime>(type: "timestamp", nullable: false),
                    modified = table.Column<DateTime>(type: "timestamp", nullable: false),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_language_id",
                table: "Books",
                column: "language_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Languages_language_id",
                table: "Books",
                column: "language_id",
                principalTable: "Languages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
