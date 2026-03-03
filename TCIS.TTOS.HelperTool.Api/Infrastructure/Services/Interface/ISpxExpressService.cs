using TCIS.TTOS.HelperTool.API.Common.Models;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface
{
    public interface ISpxExpressService
    {
        Task<BaseResponse<object>> GetItemByOrderIdAsync(string orderId);
    }
}
