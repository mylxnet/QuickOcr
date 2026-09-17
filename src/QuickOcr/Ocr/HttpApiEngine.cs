using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickOcr.Ocr;

/// <summary>
/// 自定义 HTTP API 引擎。
/// 约定：POST {url}，multipart 字段 image，返回 JSON {"text":"..."}。
/// 非 JSON 返回则直接把响应体当文本返回。
/// </summary>
public sealed class HttpApiEngine : IOcrEngine
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly string _url;
    private readonly string _headerName;
    private readonly string _headerValue;

    public HttpApiEngine(string url, string headerName, string headerValue)
    {
        _url = url;
        _headerName = headerName;
        _headerValue = headerValue;
    }

    public async Task<string> RecognizeAsync(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        using var form = new MultipartFormDataContent();
        using var imageContent = new StreamContent(ms);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(imageContent, "image", "capture.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, _url) { Content = form };
        if (!string.IsNullOrEmpty(_headerName) && !string.IsNullOrEmpty(_headerValue))
        {
            request.Headers.TryAddWithoutValidation(_headerName, _headerValue);
        }

        using var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("text").GetString() ?? "";
        }
        catch
        {
            // 响应非约定 JSON，按原文返回供用户判断
            return json;
        }
    }

    public void Dispose() { }
}
