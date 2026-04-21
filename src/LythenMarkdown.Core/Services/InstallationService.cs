using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using LythenMarkdown.Core.Interfaces;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 安装服务实现
/// </summary>
public class InstallationService : IInstallationService
{
    private const string FirstRunMarkerFile = "first_run.marker";

    public bool IsFirstRun()
    {
        var markerPath = GetFirstRunMarkerPath();
        return !File.Exists(markerPath);
    }

    public string GetInstallationPath()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var location = assembly.Location;
        
        if (string.IsNullOrEmpty(location))
        {
            // 单文件发布时使用进程路径
            location = Environment.ProcessPath ?? "";
        }

        return Path.GetDirectoryName(location) ?? "";
    }

    public SystemRequirementResult CheckSystemRequirements()
    {
        var result = new SystemRequirementResult
        {
            IsSupported = true
        };

        // 检查 .NET 运行时版本
        var dotnetVersion = Environment.Version;
        if (dotnetVersion.Major < 8)
        {
            result.IsSupported = false;
            result.Errors.Add($"需要 .NET 8.0 或更高版本，当前版本: {dotnetVersion}");
        }

        // 检查操作系统
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            result.Warnings.Add("当前版本主要针对 Windows 平台优化");
        }

        return result;
    }

    public Task ShowWelcomeWindowAsync()
    {
        // TODO: 实现欢迎窗口
        return Task.CompletedTask;
    }

    public AppVersionInfo GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;

        return new AppVersionInfo(
            Version: version?.ToString(3) ?? "1.0.0",
            BuildNumber: version?.Build.ToString() ?? "0",
            DotNetVersion: Environment.Version.ToString(),
            AvaloniaVersion: "11.2.0"
        );
    }

    public void MarkFirstRunCompleted()
    {
        var markerPath = GetFirstRunMarkerPath();
        var directory = Path.GetDirectoryName(markerPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(markerPath, DateTime.Now.ToString("O"));
    }

    private string GetFirstRunMarkerPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "LythenMarkdown", FirstRunMarkerFile);
    }
}
