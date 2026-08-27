using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Tesseract;

namespace QuickOcr.Ocr;

/// <summary>
/// 基于 Tesseract 的本地 OCR 引擎。语言包由 TessdataManager 负责就绪。
/// 引擎实例线程不安全，单次识别在 Task.Run 内串行使用。
/// </summary>
public sealed class TesseractEngine : IOcrEngine
{
    private readonly Tesseract.TesseractEngine _engine;

    public TesseractEngine(string tessdataDir, string language)
    {
        _engine = new Tesseract.TesseractEngine(tessdataDir, language);
        _engine.SetVariable("preserve_interword_spaces", "1");
    }

    public Task<string> RecognizeAsync(Bitmap bitmap)
    {
        return Task.Run(() =>
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var bytes = ms.ToArray();

            using var pix = Pix.LoadFromMemory(bytes);
            using var page = _engine.Process(pix);
            return page.GetText() ?? "";
        });
    }

    public void Dispose()
    {
        _engine.Dispose();
    }
}
