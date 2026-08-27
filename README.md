# QuickOcr

> Windows 屏幕文字识别工具 · by Mr Lin

快捷键框选屏幕区域 → Tesseract OCR 识别 → 系统记事本打开结果。轻量托盘常驻，开箱即用。

## 功能特性

- 全局快捷键触发（默认 `Ctrl+Shift+O`，可自定义）
- 鼠标框选 + 释放后微调（8 个手柄调整、选区拖动、外部重选）
- 选区内透明高亮、选区外暗化遮罩，跨多显示器
- Tesseract 本地引擎（中英文），首次自动下载语言包
- 支持自定义 HTTP OCR API（可切换引擎）
- 识别结果用系统记事本打开，进程复用，不堆叠窗口
- 容错：应用单实例、识别串行锁、相同内容跳过
- 托盘常驻 + 设置窗口 + 帮助手册 + 关于
- 多尺寸矢量图标（16~256，无失真）
- 文件日志、PerMonitorV2 DPI 感知

## 下载与使用

1. 从 Releases 下载 `publish` 压缩包
2. 解压到任意目录
3. 运行 `QuickOcr.exe`，任务栏出现托盘图标
4. 按 `Ctrl+Shift+O`，框选屏幕上的文字
5. 识别完成，自动用记事本打开识别结果

### 快捷键

| 快捷键 | 作用 |
|---|---|
| `Ctrl+Shift+O` | 触发框选识别（可在设置中更改） |
| `ESC` | 取消框选 |
| `Enter` / 选区内单击 / 双击 | 确认执行识别 |
| 拖选区内部 | 整体移动选区 |
| 拖四角/四边手柄 | 调整选区大小 |
| 拖选区外部 | 重新框选 |

## 设置

右键托盘图标 → 设置：

- 快捷键录入（按下组合键捕获）
- 引擎选择：Tesseract 本地 / 自定义 HTTP API
- 识别语言（如 `chi_sim+eng`）
- HTTP API 的 URL 与鉴权头
- 识别后复制到剪贴板
- 开机自启动

### 自定义 HTTP API 约定

- 方法：`POST {url}`
- Body：`multipart/form-data`，字段名 `image`（图片文件）
- 返回：`{"text": "识别到的文字"}`
- 可选：鉴权头（如 `Authorization: Bearer xxx`）

## 开发

### 环境要求

- .NET 8 SDK
- Windows 10/11 x64

### 构建运行

```powershell
cd src\QuickOcr
dotnet build -c Debug -p:Platform=x64
dotnet run
```

### 发布打包

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

`publish` 文件夹即可分发（self-contained，无需安装 .NET 运行时）。

> 说明：Patagames Tesseract 依赖的 InteropDotNet 用 `Assembly.Location` 定位 native 库目录，在 `PublishSingleFile` 下 Location 为空会导致加载失败，因此采用「exe + 依赖 dll + `x64\` native 目录」的非单文件结构。

## 项目结构

```
src\QuickOcr\
├── App.xaml / App.xaml.cs     启动入口、托盘与热键装配
├── SingleInstance\            Mutex + 命名管道单实例守卫
├── Tray\                      托盘图标与右键菜单
├── Hotkey\                    全局热键注册、热键录入控件
├── Capture\                   全屏框选遮罩窗口（拖拽+微调+多屏）
├── Ocr\                       引擎接口 + Tesseract + HttpApi + 工厂 + 语言包管理
├── Output\                    记事本复用输出
├── Settings\                  设置窗口、配置持久化、开机自启
├── Help\ About\               帮助手册、关于
├── Utils\                     Logger、Win32 互操作
└── Assets\                    应用图标、图标生成脚本
```

## 技术栈

- .NET 8 + WPF
- Tesseract 5（Patagames.Tesseract 封装）
- System.Drawing 截图
- Win32 API（RegisterHotKey / NotifyIcon / SetForegroundWindow）

## 许可

- 本项目代码：MIT
- Tesseract：Apache 2.0
- 语言数据：tesseract-ocr/tessdata_fast

## 作者

© 2026 by Mr Lin
