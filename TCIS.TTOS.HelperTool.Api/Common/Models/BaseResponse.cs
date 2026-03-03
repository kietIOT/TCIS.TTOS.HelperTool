namespace TCIS.TTOS.HelperTool.API.Common.Models;

public class BaseResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}
