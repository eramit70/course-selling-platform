using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TradingCourse.Application;

#nullable disable

namespace TradingCourse.Application.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260722235000_AddHomepageBannerTextColors")]
    public partial class AddHomepageBannerTextColors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "ButtonColor", table: "HomepageBanners", type: "nvarchar(7)", maxLength: 7, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HeadingColor", table: "HomepageBanners", type: "nvarchar(7)", maxLength: 7, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SubheadingColor", table: "HomepageBanners", type: "nvarchar(7)", maxLength: 7, nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ButtonColor", table: "HomepageBanners");
            migrationBuilder.DropColumn(name: "HeadingColor", table: "HomepageBanners");
            migrationBuilder.DropColumn(name: "SubheadingColor", table: "HomepageBanners");
        }
    }
}
