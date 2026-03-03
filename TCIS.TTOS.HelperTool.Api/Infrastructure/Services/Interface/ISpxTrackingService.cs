using TCIS.TTOS.HelperTool.API.Common.Models;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface
{
    public interface ISpxTrackingService
    {
        Task<BaseResponse<object>> SubscribeAsync(string spxTn, CancellationToken ct = default);
        Task<BaseResponse<object>> UnsubscribeAsync(string spxTn, CancellationToken ct = default);
        Task<BaseResponse<object>> GetShipmentStatusAsync(string spxTn, CancellationToken ct = default);
        Task<BaseResponse<object>> GetAllActiveShipmentsAsync(CancellationToken ct = default);
        Task PollAndUpdateAsync(CancellationToken ct = default);
    }
}
