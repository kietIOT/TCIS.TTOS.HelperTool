using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCIS.TTOS.ToolHelper.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddRedisInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "redis_instances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Host = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Password = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Database = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redis_instances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_redis_instances_monitored_hosts_HostId",
                        column: x => x.HostId,
                        principalTable: "monitored_hosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_redis_instances_host_active",
                table: "redis_instances",
                columns: new[] { "HostId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_redis_instances_host_name",
                table: "redis_instances",
                columns: new[] { "HostId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redis_instances");
        }
    }
}
