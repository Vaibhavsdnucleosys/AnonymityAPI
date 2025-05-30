using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonymityAPI.Migrations
{
    /// <inheritdoc />
    public partial class updategoogleid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tb_Users_GoogleId",
                table: "Tb_Users");

            migrationBuilder.AlterColumn<string>(
                name: "GoogleId",
                table: "Tb_Users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Tb_Users_GoogleId",
                table: "Tb_Users",
                column: "GoogleId",
                unique: true,
                filter: "[GoogleId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tb_Users_GoogleId",
                table: "Tb_Users");

            migrationBuilder.AlterColumn<string>(
                name: "GoogleId",
                table: "Tb_Users",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tb_Users_GoogleId",
                table: "Tb_Users",
                column: "GoogleId",
                unique: true);
        }
    }
}
