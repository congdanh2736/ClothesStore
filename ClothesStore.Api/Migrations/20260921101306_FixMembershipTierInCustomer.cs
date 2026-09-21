using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothesStore.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixMembershipTierInCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_MembershipTiers_Id",
                table: "Customers");

            migrationBuilder.AlterColumn<int>(
                name: "MembershipTierId",
                table: "Customers",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_MembershipTierId",
                table: "Customers",
                column: "MembershipTierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_MembershipTiers_MembershipTierId",
                table: "Customers",
                column: "MembershipTierId",
                principalTable: "MembershipTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_MembershipTiers_MembershipTierId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_MembershipTierId",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "MembershipTierId",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_MembershipTiers_Id",
                table: "Customers",
                column: "Id",
                principalTable: "MembershipTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
