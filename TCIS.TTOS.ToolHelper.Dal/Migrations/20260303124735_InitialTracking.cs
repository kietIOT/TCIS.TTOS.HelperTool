using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TCIS.TTOS.ToolHelper.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tracking_shipments",
                columns: table => new
                {
                    SpxTn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Carrier = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClientOrderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeliverType = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastEventTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEventCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    LastMessage = table.Column<string>(type: "text", nullable: true),
                    LastMilestoneName = table.Column<string>(type: "text", nullable: true),
                    Fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CurrentLocationName = table.Column<string>(type: "text", nullable: true),
                    CurrentFullAddress = table.Column<string>(type: "text", nullable: true),
                    CurrentLat = table.Column<double>(type: "double precision", nullable: true),
                    CurrentLng = table.Column<double>(type: "double precision", nullable: true),
                    NextLocationName = table.Column<string>(type: "text", nullable: true),
                    NextFullAddress = table.Column<string>(type: "text", nullable: true),
                    NextLat = table.Column<double>(type: "double precision", nullable: true),
                    NextLng = table.Column<double>(type: "double precision", nullable: true),
                    LastRawJson = table.Column<string>(type: "jsonb", nullable: true),
                    RawVersion = table.Column<int>(type: "integer", nullable: false),
                    NextPollAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PollIntervalSec = table.Column<int>(type: "integer", nullable: false),
                    PollFailCount = table.Column<int>(type: "integer", nullable: false),
                    LastPolledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_shipments", x => x.SpxTn);
                });

            migrationBuilder.CreateTable(
                name: "notification_outbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpxTn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Pref = table.Column<int>(type: "integer", nullable: false),
                    EventKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_outbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_outbox_tracking_shipments_SpxTn",
                        column: x => x.SpxTn,
                        principalTable: "tracking_shipments",
                        principalColumn: "SpxTn",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shipment_poll_locks",
                columns: table => new
                {
                    SpxTn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_poll_locks", x => x.SpxTn);
                    table.ForeignKey(
                        name: "FK_shipment_poll_locks_tracking_shipments_SpxTn",
                        column: x => x.SpxTn,
                        principalTable: "tracking_shipments",
                        principalColumn: "SpxTn",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracking_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpxTn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TrackingCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MilestoneCode = table.Column<int>(type: "integer", nullable: true),
                    MilestoneName = table.Column<string>(type: "text", nullable: true),
                    BuyerMessage = table.Column<string>(type: "text", nullable: true),
                    SellerMessage = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CurrentLocation = table.Column<string>(type: "jsonb", nullable: true),
                    NextLocation = table.Column<string>(type: "jsonb", nullable: true),
                    RawRecord = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tracking_events_tracking_shipments_SpxTn",
                        column: x => x.SpxTn,
                        principalTable: "tracking_shipments",
                        principalColumn: "SpxTn",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracking_subscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpxTn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Pref = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tracking_subscriptions_tracking_shipments_SpxTn",
                        column: x => x.SpxTn,
                        principalTable: "tracking_shipments",
                        principalColumn: "SpxTn",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_EventKey_UserId_Channel",
                table: "notification_outbox",
                columns: new[] { "EventKey", "UserId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending",
                table: "notification_outbox",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_spx",
                table: "notification_outbox",
                columns: new[] { "SpxTn", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_poll_locks_until",
                table: "shipment_poll_locks",
                column: "LockedUntil");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_SpxTn_EventTime_TrackingCode",
                table: "tracking_events",
                columns: new[] { "SpxTn", "EventTime", "TrackingCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tracking_events_tn_time",
                table: "tracking_events",
                columns: new[] { "SpxTn", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "ix_tracking_shipments_client_order_id",
                table: "tracking_shipments",
                column: "ClientOrderId");

            migrationBuilder.CreateIndex(
                name: "ix_tracking_shipments_due_poll",
                table: "tracking_shipments",
                columns: new[] { "IsTerminal", "NextPollAt" });

            migrationBuilder.CreateIndex(
                name: "ix_tracking_subscriptions_active",
                table: "tracking_subscriptions",
                columns: new[] { "SpxTn", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_subscriptions_UserId_SpxTn",
                table: "tracking_subscriptions",
                columns: new[] { "UserId", "SpxTn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_outbox");

            migrationBuilder.DropTable(
                name: "shipment_poll_locks");

            migrationBuilder.DropTable(
                name: "tracking_events");

            migrationBuilder.DropTable(
                name: "tracking_subscriptions");

            migrationBuilder.DropTable(
                name: "tracking_shipments");
        }
    }
}
