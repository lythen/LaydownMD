using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace LythenMarkdown.UI;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try
            {
                var crashDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LythenMarkdown");
                Directory.CreateDirectory(crashDir);
                File.AppendAllText(Path.Combine(crashDir, "crash.log"), $"[{DateTime.Now:O}] {ex}\n\n");
            }
            catch
            {
                // 如果写入用户配置文件夹失败，则回退到临时目录，且不抛出以免掩盖原始异常
                try
                {
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "LythenMarkdown_crash.log"), $"[{DateTime.Now:O}] {ex}\n\n");
                }
                catch
                {
                    // 最后一重保护：吞掉所有异常，保留原始异常抛出
                }
            }

            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions())
            .With(new X11PlatformOptions())
            .With(new MacOSPlatformOptions { ShowInDock = true });
    }
}
