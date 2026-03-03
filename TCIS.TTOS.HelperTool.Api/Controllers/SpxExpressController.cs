using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface;

namespace TCIS.TTOS.HelperTool.API.Controllers
{
    [ApiController]
    [Route("api/spx-express")]
    public class SpxExpressController(ISpxExpressService spxExpressService) : ControllerBase
    {
        private readonly ISpxExpressService _spxExpressService = spxExpressService;

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetJobs(string orderId)
        {
            return Ok(await _spxExpressService.GetItemByOrderIdAsync(orderId));
        }

    }
}
