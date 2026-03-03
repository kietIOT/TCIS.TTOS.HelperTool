using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TCIS.TTOS.HelperTool.API.Common.Models;
using TCIS.TTOS.HelperTool.API.Features.SpxExpress;
using TCIS.TTOS.HelperTool.API.Features.SpxExpress.Models;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Implement
{
    public class SpxExpressService(IOptions<SpxOptions> options, IHttpClientFactory httpClientFactory) : ISpxExpressService
    {
        private readonly SpxOptions _options = options.Value;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        public async Task<BaseResponse<object>> GetItemByOrderIdAsync(string orderId)
        {
            try
            {
                var url = GetBaseUrl() + $"shipment/order/open/order/get_order_info?spx_tn={orderId}&language_code=vi";

                using var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                using var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                var responseObject = JsonConvert.DeserializeObject<SpxRecordResponse>(responseBody)
                      ?? throw new InvalidOperationException("Spx response deserialize failed");

                return new BaseResponse<object>
                {
                    Data = responseObject.Data,
                    Message = responseObject.Message,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                if(ex is not OutOfMemoryException)
                {
                    return new BaseResponse<object>
                    {
                        Message = ex.Message,
                        IsSuccess = false
                    };
                }
                throw;
            }
        }
        private string GetBaseUrl()
        {
            return _options.BaseUrl;
        }
    }
}
