using System.Drawing;
using System.Threading.Tasks;

namespace QuickOcr.Ocr;

/// <summary>
/// OCR 引擎统一接口。识别结果返回纯文本。
/// </summary>
public interface IOcrEngine : IDisposable
{
    Task<string> RecognizeAsync(Bitmap bitmap);
}
