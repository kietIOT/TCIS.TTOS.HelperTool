using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HelperTool.API.Features.SpxExpress;

namespace TCIS.TTOS.HelperTool.API.Controllers;

[ApiController]
[Route("api/spx-express")]
public class SpxExpressController(ISpxExpressService spxExpressService) : ControllerBase
{
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetByOrderId(string orderId)
    {
        return Ok(await spxExpressService.GetItemByOrderIdAsync(orderId));
    }
}
