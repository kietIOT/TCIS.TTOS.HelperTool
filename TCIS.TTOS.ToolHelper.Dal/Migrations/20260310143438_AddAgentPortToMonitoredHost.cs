using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCIS.TTOS.ToolHelper.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentPortToMonitoredHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgentPort",
                table: "monitored_hosts",
                type: "integer",
                nullable: false,
                defaultValue: 5155);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentPort",
                table: "monitored_hosts");
        }
    }
}
