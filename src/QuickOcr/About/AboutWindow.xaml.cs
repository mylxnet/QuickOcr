using System.Reflection;
using System.Windows;

namespace QuickOcr.About;

/// <summary>
/// 关于：显示版本号与技术栈信息。
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var ver = Assembly.GetEntryAssembly()?.GetName()?.Version;
        VersionText.Text = ver != null ? $"版本 {ver}" : "版本未知";
    }
}
