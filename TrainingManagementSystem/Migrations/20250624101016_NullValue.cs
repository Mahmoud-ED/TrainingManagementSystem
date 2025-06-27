using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class NullValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanCours_Courses_CourseId",
                table: "PlanCours");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanCours_Locations_LocationId",
                table: "PlanCours");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanCours_Courses_CourseId",
                table: "PlanCours",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanCours_Locations_LocationId",
                table: "PlanCours",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanCours_Courses_CourseId",
                table: "PlanCours");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanCours_Locations_LocationId",
                table: "PlanCours");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanCours_Courses_CourseId",
                table: "PlanCours",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanCours_Locations_LocationId",
                table: "PlanCours",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");
        }
    }
}
