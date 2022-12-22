using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemesApi.Migrations
{
    public partial class ChangeMeta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalEstimates",
                table: "Metas");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDate",
                table: "Metas",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "Metas");

            migrationBuilder.AddColumn<int>(
                name: "TotalEstimates",
                table: "Metas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
