using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drydock.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SlimProductSingleHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageApi",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ImageWeb",
                table: "products");

            migrationBuilder.DropColumn(
                name: "RepoApi",
                table: "products");

            migrationBuilder.DropColumn(
                name: "RepoWeb",
                table: "products");

            migrationBuilder.AddColumn<string>(
                name: "Repo",
                table: "products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Repo",
                table: "products");

            migrationBuilder.AddColumn<string>(
                name: "ImageApi",
                table: "products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageWeb",
                table: "products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepoApi",
                table: "products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepoWeb",
                table: "products",
                type: "TEXT",
                nullable: true);
        }
    }
}
