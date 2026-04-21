using System.Collections.Generic;
using System.Threading.Tasks;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 安装服务接口
/// </summary>
public interface IInstallationService
{
    /// <summary>
    /// 检查是否为首次运行
    /// </summary>
    bool IsFirstRun();

    /// <summary>
    /// 获取安装路径
    /// </summary>
    string GetInstallationPath();

    /// <summary>
    /// 检查系统环境是否满足要求
    /// </summary>
    SystemRequirementResult CheckSystemRequirements();

    /// <summary>
    /// 显示首次运行欢迎窗口
    /// </summary>
    Task ShowWelcomeWindowAsync();

    /// <summary>
    /// 获取版本信息
    /// </summary>
    AppVersionInfo GetVersionInfo();

    /// <summary>
    /// 标记首次运行已完成
    /// </summary>
    void MarkFirstRunCompleted();
}

/// <summary>
/// 系统要求检查结果
/// </summary>
public class SystemRequirementResult
{
    public bool IsSupported { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 应用版本信息
/// </summary>
public record AppVersionInfo(
    string Version,
    string BuildNumber,
    string DotNetVersion,
    string AvaloniaVersion
);
