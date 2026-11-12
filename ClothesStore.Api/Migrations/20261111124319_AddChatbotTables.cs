using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothesStore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatbotTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reply = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PredictedIntent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    ProductCount = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    IsHelpful = table.Column<bool>(type: "bit", nullable: true),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    CorrectIntent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChatModelTrainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BaseSamples = table.Column<int>(type: "int", nullable: false),
                    AdminSamples = table.Column<int>(type: "int", nullable: false),
                    MicroAccuracy = table.Column<double>(type: "float", nullable: true),
                    MacroAccuracy = table.Column<double>(type: "float", nullable: true),
                    HandTestAccuracy = table.Column<double>(type: "float", nullable: true),
                    HandTestCount = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatModelTrainings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatTrainingSamples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChatLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatTrainingSamples", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatLogs_CreatedAt",
                table: "ChatLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatLogs_CustomerId",
                table: "ChatLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatLogs_Intent",
                table: "ChatLogs",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_ChatTrainingSamples_Label",
                table: "ChatTrainingSamples",
                column: "Label");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatLogs");

            migrationBuilder.DropTable(
                name: "ChatModelTrainings");

            migrationBuilder.DropTable(
                name: "ChatTrainingSamples");
        }
    }
}
