using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface
{
    public interface ISpxExpressService
    {
        Task<BaseResponse<object>> GetItemByOrderIdAsync(string orderId);
    }
}
