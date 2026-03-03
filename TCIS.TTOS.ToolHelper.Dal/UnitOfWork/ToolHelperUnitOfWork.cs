using Microsoft.EntityFrameworkCore;
using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.ToolHelper.DAL.Repositories;

namespace TCIS.TTOS.ToolHelper.DAL.UnitOfWork
{
    public interface IToolHelperUnitOfWork : IGenericUnitOfWork
    {
        IToolHelperRepository<TrackingShipment> TrackingShipmentRepository { get; }
        IToolHelperRepository<TrackingSubscription> TrackingSubscriptionRepository { get; }
        IToolHelperRepository<TrackingEvent> TrackingEventGroupRepository { get; }
        IToolHelperRepository<NotificationOutbox> NotificationOutboxRepository { get; }
        IToolHelperRepository<ShipmentPollLock> ShipmentPollLockRepository { get; }
    }

    public class ToolHelperUnitOfWork(IDbContextFactory<ToolHelperDbContext> dbContextFactory) :
        GenericUnitOfWork<ToolHelperDbContext>(dbContextFactory), IToolHelperUnitOfWork
    {
        private IToolHelperRepository<TrackingShipment>? _trackingShipmentRepository;
        private IToolHelperRepository<TrackingSubscription>? _trackingSubscriptionRepository;
        private IToolHelperRepository<TrackingEvent>? _trackingEventGroupRepository;

        private IToolHelperRepository<NotificationOutbox>? _notificationOutboxRepository;
        private IToolHelperRepository<ShipmentPollLock>? _shipmentPollLockRepository;

        public IToolHelperRepository<TrackingShipment> TrackingShipmentRepository => _trackingShipmentRepository ??= new ToolHelperRepository<TrackingShipment>(_dbContext);
        public IToolHelperRepository<NotificationOutbox> NotificationOutboxRepository => _notificationOutboxRepository ??= new ToolHelperRepository<NotificationOutbox>(_dbContext);
        public IToolHelperRepository<ShipmentPollLock> ShipmentPollLockRepository => _shipmentPollLockRepository ??= new ToolHelperRepository<ShipmentPollLock>(_dbContext);

        public IToolHelperRepository<TrackingSubscription> TrackingSubscriptionRepository => _trackingSubscriptionRepository ??= new ToolHelperRepository<TrackingSubscription>(_dbContext);
        public IToolHelperRepository<TrackingEvent> TrackingEventGroupRepository
                => _trackingEventGroupRepository ??= new ToolHelperRepository<TrackingEvent>(_dbContext);

    }
}
