using TCIS.TTOS.HelperTool.API.Common.Models;

namespace TCIS.TTOS.HelperTool.API.Features.SpxExpress;

public interface ISpxExpressService
{
    Task<BaseResponse<object>> GetItemByOrderIdAsync(string orderId);
}
