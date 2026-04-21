using System.Threading.Tasks;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 设置服务接口
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 加载设置
    /// </summary>
    Task<AppSettings> LoadSettingsAsync();

    /// <summary>
    /// 保存设置
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// 获取当前设置
    /// </summary>
    AppSettings CurrentSettings { get; }

    /// <summary>
    /// 添加到最近文件
    /// </summary>
    Task AddRecentFileAsync(string filePath);

    /// <summary>
    /// 获取设置文件路径
    /// </summary>
    string GetSettingsFilePath();
}
