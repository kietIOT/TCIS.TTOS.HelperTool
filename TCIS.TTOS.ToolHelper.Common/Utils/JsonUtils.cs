using Newtonsoft.Json;

namespace TCIS.TTOS.HelperTool.Common.Utils;

public class JsonUtils<T> : BaseJsonUtils where T : class
{
    public static string ToJson(object obj) => JsonConvert.SerializeObject(obj);

    public static string Serialize(T? obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return JsonConvert.SerializeObject(obj, _defaultOptions);
    }

    public static T? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<T?>(json, _defaultOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public class BaseJsonUtils
{
    protected BaseJsonUtils() { }

    protected static readonly JsonSerializerSettings _defaultOptions = new()
    {
        Formatting = Formatting.None,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };
}