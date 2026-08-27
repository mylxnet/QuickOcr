using QuickOcr.Settings;

namespace QuickOcr.Ocr;

/// <summary>
/// 按 AppSettings.Engine 创建对应 OCR 引擎并缓存：设置不变时复用，变化时重建。
/// </summary>
public static class OcrEngineFactory
{
    private static IOcrEngine? _cached;
    private static string? _cacheKey;

    public static IOcrEngine Create(AppSettings settings)
    {
        var key = $"{settings.Engine}|{settings.Language}|{settings.ApiUrl}";
        if (_cached != null && _cacheKey == key) return _cached;

        _cached?.Dispose();
        _cached = Build(settings);
        _cacheKey = key;
        return _cached;
    }

    private static IOcrEngine Build(AppSettings s)
    {
        if (s.Engine == "HttpApi" && !string.IsNullOrWhiteSpace(s.ApiUrl))
        {
            return new HttpApiEngine(s.ApiUrl, s.ApiHeaderName, s.ApiHeaderValue);
        }

        // 默认 Tesseract：确保语言包就绪后创建引擎
        TessdataManager.EnsureLanguages(s.Language);
        return new TesseractEngine(TessdataManager.GetTessdataDir(), s.Language);
    }
}
