using System;
using System.IO;
using System.Net.Http;
using QuickOcr.Utils;

namespace QuickOcr.Ocr;

/// <summary>
/// tessdata 语言包管理：首次缺失时从 CDN 下载 traineddata 到 %AppData%\QuickOcr\tessdata。
/// 使用 tessdata_fast（小体积、识别质量可接受）；jsDelivr 优先，GitHub raw 回退。
/// </summary>
public static class TessdataManager
{
    private static readonly string TessdataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickOcr", "tessdata");

    private static readonly string[] CDNs =
    {
        "https://cdn.jsdelivr.net/gh/tesseract-ocr/tessdata_fast@main/{0}.traineddata",
        "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/{0}.traineddata"
    };

    public static string GetTessdataDir()
    {
        Directory.CreateDirectory(TessdataDir);
        return TessdataDir;
    }

    /// <summary>确保 langSpec（如 chi_sim+eng）所需语言包就绪。缺则下载。</summary>
    public static void EnsureLanguages(string langSpec)
    {
        if (string.IsNullOrWhiteSpace(langSpec)) return;

        foreach (var lang in langSpec.Split('+'))
        {
            var l = lang.Trim();
            if (string.IsNullOrEmpty(l)) continue;

            var file = Path.Combine(GetTessdataDir(), l + ".traineddata");
            if (!File.Exists(file))
            {
                Logger.Info($"语言包 {l} 缺失，开始下载");
                DownloadLanguage(l, file);
            }
        }
    }

    private static void DownloadLanguage(string lang, string dest)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        // jsDelivr/GitHub 对无 UA 请求可能拒绝，设置 UA 避免下载失败
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) QuickOcr/1.0");
        foreach (var tpl in CDNs)
        {
            var url = string.Format(tpl, lang);
            try
            {
                var bytes = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
                if (bytes.Length > 1024)
                {
                    File.WriteAllBytes(dest, bytes);
                    Logger.Info($"语言包 {lang} 下载成功：{bytes.Length} 字节，源 {url}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"语言包 {lang} 下载失败（{url}）：{ex.Message}");
                // 尝试备选 CDN
            }
        }
        Logger.Error($"语言包 {lang} 全部 CDN 下载失败");
        throw new InvalidOperationException(
            $"无法下载 Tesseract 语言包：{lang}。请检查网络，或手动放置 traineddata 到 {dest}");
    }
}
