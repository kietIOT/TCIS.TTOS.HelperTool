using Microsoft.EntityFrameworkCore;
using TCIS.TTOS.ToolHelper.Dal.Entities;

namespace TCIS.TTOS.ToolHelper.DAL
{
    public class ToolHelperDbContext(DbContextOptions<ToolHelperDbContext> options) : DbContext(options)
    {
        public DbSet<TrackingShipment> TrackingShipments { get; } = null!;
        public DbSet<TrackingSubscription> TrackingSubscriptions { get; } = null!;
        public DbSet<TrackingEvent> TrackingEvents { get; } = null!;

        public DbSet<NotificationOutbox> NotificationOutbox { get; } = null!;
        public DbSet<ShipmentPollLock> ShipmentPollLocks { get; } = null!;

        public DbSet<MonitoredHost> MonitoredHosts { get; } = null!;
        public DbSet<MonitoredService> MonitoredServices { get; } = null!;
        public DbSet<DeploymentHistory> DeploymentHistories { get; } = null!;

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<TrackingShipment>(e =>
            {
                e.ToTable("tracking_shipments");
                e.HasKey(x => x.SpxTn);

                e.HasIndex(x => new { x.IsTerminal, x.NextPollAt })
                 .HasDatabaseName("ix_tracking_shipments_due_poll");

                e.HasIndex(x => x.ClientOrderId)
                 .HasDatabaseName("ix_tracking_shipments_client_order_id");

                // jsonb (nếu dùng string vẫn map được)
                e.Property(x => x.LastRawJson).HasColumnType("jsonb");

                e.HasMany(x => x.Subscriptions)
                 .WithOne(x => x.Shipment)
                 .HasForeignKey(x => x.SpxTn)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.Events)
                 .WithOne(x => x.Shipment)
                 .HasForeignKey(x => x.SpxTn)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.OutboxMessages)
                 .WithOne(x => x.Shipment)
                 .HasForeignKey(x => x.SpxTn)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.PollLock)
                 .WithOne(x => x.Shipment)
                 .HasForeignKey<ShipmentPollLock>(x => x.SpxTn)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ---- tracking_subscriptions
            b.Entity<TrackingSubscription>(e =>
            {
                e.ToTable("tracking_subscriptions");
                e.HasKey(x => x.Id);

                e.HasIndex(x => new { x.UserId, x.SpxTn })
                 .IsUnique();

                e.HasIndex(x => new { x.SpxTn, x.IsActive })
                 .HasDatabaseName("ix_tracking_subscriptions_active");
            });

            // ---- tracking_events
            b.Entity<TrackingEvent>(e =>
            {
                e.ToTable("tracking_events");
                e.HasKey(x => x.Id);

                e.HasIndex(x => new { x.SpxTn, x.EventTime })
                 .HasDatabaseName("ix_tracking_events_tn_time");

                e.HasIndex(x => new { x.SpxTn, x.EventTime, x.TrackingCode })
                 .IsUnique();

                e.Property(x => x.CurrentLocation).HasColumnType("jsonb");
                e.Property(x => x.NextLocation).HasColumnType("jsonb");
                e.Property(x => x.RawRecord).HasColumnType("jsonb");
            });

            // ---- notification_outbox
            b.Entity<NotificationOutbox>(e =>
            {
                e.ToTable("notification_outbox");
                e.HasKey(x => x.Id);

                e.HasIndex(x => new { x.Status, x.NextRetryAt })
                 .HasDatabaseName("ix_outbox_pending");

                e.HasIndex(x => new { x.SpxTn, x.CreatedAt })
                 .HasDatabaseName("ix_outbox_spx");

                e.HasIndex(x => new { x.EventKey, x.UserId, x.Channel })
                 .IsUnique();

                e.Property(x => x.Payload).HasColumnType("jsonb");
            });

            // ---- shipment_poll_locks
            b.Entity<ShipmentPollLock>(e =>
            {
                e.ToTable("shipment_poll_locks");
                e.HasKey(x => x.SpxTn);

                e.HasIndex(x => x.LockedUntil)
                 .HasDatabaseName("ix_poll_locks_until");
            });

            // ---- monitored_hosts
            b.Entity<MonitoredHost>(e =>
            {
                e.ToTable("monitored_hosts");
                e.HasKey(x => x.Id);

                e.HasIndex(x => x.IpAddress)
                 .IsUnique()
                 .HasDatabaseName("ix_monitored_hosts_ip");

                e.HasIndex(x => new { x.IsActive, x.Status })
                 .HasDatabaseName("ix_monitored_hosts_active_status");

                e.HasMany(x => x.Services)
                 .WithOne(x => x.Host)
                 .HasForeignKey(x => x.HostId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ---- monitored_services
            b.Entity<MonitoredService>(e =>
            {
                e.ToTable("monitored_services");
                e.HasKey(x => x.Id);

                e.HasIndex(x => new { x.HostId, x.Name })
                  .IsUnique()
                  .HasDatabaseName("ix_monitored_services_host_name");

                e.HasIndex(x => new { x.HostId, x.IsActive })
                  .HasDatabaseName("ix_monitored_services_host_active");

                e.HasIndex(x => x.Type)
                  .HasDatabaseName("ix_monitored_services_type");

                e.HasIndex(x => x.Name)
                  .HasDatabaseName("ix_monitored_services_name");

                e.HasMany(x => x.DeploymentHistories)
                  .WithOne(x => x.Service)
                  .HasForeignKey(x => x.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            // ---- deployment_histories
            b.Entity<DeploymentHistory>(e =>
            {
                e.ToTable("deployment_histories");
                e.HasKey(x => x.Id);

                e.HasIndex(x => new { x.ServiceId, x.StartedAt })
                  .HasDatabaseName("ix_deployment_histories_service_started");

                e.HasIndex(x => x.Status)
                  .HasDatabaseName("ix_deployment_histories_status");
            });
        }
    }
}
