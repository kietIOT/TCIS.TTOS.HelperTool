using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace TCIS.TTOS.HelperTool.Common.Utils;

public interface IHttpUtils
{
    Task<T?> GetAsync<T>(string url, object content, string token, Dictionary<string, string> contentHeader);

    Task<T?> PostAsync<T>(string url, object content, string accessToken, Dictionary<string, string> contentHeader);
    Task<HttpResponseMessage?> PostAsync(string url, object content, string accessToken, Dictionary<string, string> contentHeader);
    Task<T?> PostAsync<T>(string url, object? content);

    Task<T?> PutAsync<T>(string url, object content);
    Task<T?> DeleteAsync<T>(string url, object content);
}

public class HttpUtils(IHttpClientFactory httpClientFactory) : IHttpUtils
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public async Task<T?> GetAsync<T>(string url, object content, string token, Dictionary<string, string> contentHeader)
    {
        using var client = _httpClientFactory.CreateClient();
        try
        {
            ConfigureClient(client, token, contentHeader);
            var response = await client.GetAsync(BuildQueryPath(url, content));

            return await HandleResponseAsync<T?>(response);
        }
        catch (Exception ex)
        {
            if (ex is not OutOfMemoryException)
                return default;

            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string url, object content, string accessToken, Dictionary<string, string> contentHeader)
    {
        using var client = _httpClientFactory.CreateClient();
        try
        {
            ConfigureClient(client, accessToken, contentHeader);
            var response = await client.PostAsync(url, CreateHttpContent(content));

            return await HandleResponseAsync<T?>(response);
        }
        catch (Exception ex)
        {
            if (ex is not OutOfMemoryException)
                return default;

            throw;
        }
    }

    public async Task<HttpResponseMessage?> PostAsync(string url, object content, string accessToken, Dictionary<string, string> contentHeader)
    {
        using var client = _httpClientFactory.CreateClient();
        try
        {
            ConfigureClient(client, accessToken, contentHeader);
            var response = await client.PostAsync(url, CreateHttpContent(content));
            return response;
        }
        catch (Exception ex)
        {
            if (ex is not OutOfMemoryException)
                return null;

            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string url, object? content)
    {
        var client = _httpClientFactory.CreateClient();
        HttpResponseMessage response;

        response = content == null ? await client.PostAsync(url, null)
            : await client.PostAsync(url, new StringContent(JsonUtils<object>.ToJson(content), Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(res, JsonSettings);
        }

        return default;
    }

    public async Task<T?> PutAsync<T>(string url, object content)
    {
        using var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await client.PutAsync(url, CreateHttpContent(content));

            return await HandleResponseAsync<T?>(response);
        }
        catch (Exception ex)
        {
            if (ex is not OutOfMemoryException)
                return default;

            throw;
        }
    }

    public async Task<T?> DeleteAsync<T>(string url, object content)
    {
        using var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await CreateAndSendDeleteRequestAsync(client, url, content);

            return await HandleResponseAsync<T?>(response);
        }
        catch (Exception ex)
        {
            if (ex is not OutOfMemoryException)
                return default;

            throw;
        }
    }

    private static async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T?>(responseData);
    }

    private static StringContent? CreateHttpContent(object content)
    {
        if (content == null)
            return null;

        var json = JsonConvert.SerializeObject(content);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static void ConfigureClient(HttpClient client, string token, Dictionary<string, string> contentHeader)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (contentHeader != null)
        {
            foreach (var header in contentHeader.Where(header => !client.DefaultRequestHeaders.Contains(header.Key)))
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value ?? string.Empty);
            }
        }
    }

    private static async Task<HttpResponseMessage> CreateAndSendDeleteRequestAsync(HttpClient client, string url, object content)
    {
        if (content == null)
        {
            return await client.DeleteAsync(url);
        }

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri(url),
            Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json")
        };
        return await client.SendAsync(request);
    }

    private static string? BuildQueryPath(string url, object obj)
    {
        try
        {
            if (obj == null)
            {
                return url;
            }

            StringBuilder builder = new();
            string condition = "?";
            builder.Append(url);
            Type type = obj.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());

            foreach (PropertyInfo prop in props)
            {
                var name = prop.Name;
                var value = prop.GetValue(obj, null)?.ToString();

                builder.Append($"{condition}{name}={value}");
                condition = "&";
            }

            return builder.ToString();

        }
        catch (Exception ex)
        {
            if (ex is not OutOfMemoryException)
                return null;

            throw;
        }
    }

    private readonly JsonSerializerSettings JsonSettings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
    };
}
