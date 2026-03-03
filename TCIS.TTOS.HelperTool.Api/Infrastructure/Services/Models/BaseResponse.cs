namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models
{
    public class BaseResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}
