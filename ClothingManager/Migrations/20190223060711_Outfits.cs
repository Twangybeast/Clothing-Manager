using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClothingManager.Migrations
{
    public partial class Outfits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OutfitID",
                table: "Clothings",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Outfits",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outfits", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ClothingOutfits",
                columns: table => new
                {
                    Order = table.Column<int>(nullable: false),
                    ClothingId = table.Column<int>(nullable: false),
                    OutfitId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClothingOutfits", x => new { x.ClothingId, x.OutfitId });
                    table.ForeignKey(
                        name: "FK_ClothingOutfits_Clothings_ClothingId",
                        column: x => x.ClothingId,
                        principalTable: "Clothings",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClothingOutfits_Outfits_OutfitId",
                        column: x => x.OutfitId,
                        principalTable: "Outfits",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clothings_OutfitID",
                table: "Clothings",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingOutfits_OutfitId",
                table: "ClothingOutfits",
                column: "OutfitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clothings_Outfits_OutfitID",
                table: "Clothings",
                column: "OutfitID",
                principalTable: "Outfits",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clothings_Outfits_OutfitID",
                table: "Clothings");

            migrationBuilder.DropTable(
                name: "ClothingOutfits");

            migrationBuilder.DropTable(
                name: "Outfits");

            migrationBuilder.DropIndex(
                name: "IX_Clothings_OutfitID",
                table: "Clothings");

            migrationBuilder.DropColumn(
                name: "OutfitID",
                table: "Clothings");
        }
    }
}
