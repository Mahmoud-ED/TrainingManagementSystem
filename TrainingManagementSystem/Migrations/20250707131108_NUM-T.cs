using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class NUMT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NUM",
                table: "Trainees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NUM",
                table: "Trainees");
        }
    }
}
