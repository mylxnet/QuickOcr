namespace QuickOcr.Settings;

/// <summary>
/// 应用配置，JSON 持久化于 %AppData%\QuickOcr\settings.json。
/// </summary>
public sealed class AppSettings
{
    /// <summary>快捷键序列化格式 "Modifier|Modifier|Key"，默认 Control|Shift|O。</summary>
    public string Hotkey { get; set; } = "Control|Shift|O";

    /// <summary>OCR 引擎：Tesseract 或 HttpApi。</summary>
    public string Engine { get; set; } = "Tesseract";

    /// <summary>自定义 HTTP API 地址。</summary>
    public string ApiUrl { get; set; } = "";

    /// <summary>自定义 API 鉴权头名称（可选）。</summary>
    public string ApiHeaderName { get; set; } = "";

    /// <summary>自定义 API 鉴权头值（可选）。</summary>
    public string ApiHeaderValue { get; set; } = "";

    /// <summary>Tesseract 识别语言，例如 chi_sim+eng。</summary>
    public string Language { get; set; } = "chi_sim+eng";

    /// <summary>开机自启。</summary>
    public bool RunAtStartup { get; set; } = false;

    /// <summary>识别后复制到剪贴板。</summary>
    public bool CopyToClipboard { get; set; } = true;

    /// <summary>识别后用记事本打开。</summary>
    public bool OpenInNotepad { get; set; } = true;
}
