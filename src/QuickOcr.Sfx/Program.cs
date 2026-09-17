using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;

// QuickOcr 自解压安装器：嵌入 publish.zip，解压到 LocalAppData 后启动主程序。
// 单文件 self-contained exe，目标机无需安装 .NET。

const string Version = "1.0.1";
var dest = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "QuickOcr", Version);
Directory.CreateDirectory(dest);

try
{
    // 1. 定位嵌入的 publish.zip 资源
    var asm = Assembly.GetExecutingAssembly();
    var resName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("publish.zip"))
        ?? throw new InvalidOperationException("未找到嵌入资源 publish.zip");

    // 2. 释放 zip 到临时文件
    var zipPath = Path.Combine(dest, "_publish.zip");
    using (var stream = asm.GetManifestResourceStream(resName)!)
    using (var fs = File.Create(zipPath))
        stream.CopyTo(fs);

    // 3. 解压（覆盖已有文件，保证升级时更新）
    ZipFile.ExtractToDirectory(zipPath, dest, overwriteFiles: true);
    File.Delete(zipPath);

    // 4. 启动主程序
    var exe = Path.Combine(dest, "QuickOcr.exe");
    if (File.Exists(exe))
    {
        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
    }
    else
    {
        System.Windows.MessageBox.Show($"未找到主程序：{exe}", "QuickOcr 安装",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }
}
catch (Exception ex)
{
    System.Windows.MessageBox.Show($"安装失败：{ex.Message}", "QuickOcr 安装",
        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
}
