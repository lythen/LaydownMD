using System.Threading.Tasks;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 会话服务接口
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// 保存会话
    /// </summary>
    Task SaveSessionAsync(SessionData session);

    /// <summary>
    /// 加载会话
    /// </summary>
    Task<SessionData?> LoadSessionAsync();

    /// <summary>
    /// 清除会话
    /// </summary>
    Task ClearSessionAsync();

    /// <summary>
    /// 启动自动保存
    /// </summary>
    void StartAutoSave(int intervalSeconds);

    /// <summary>
    /// 停止自动保存
    /// </summary>
    void StopAutoSave();

    /// <summary>
    /// 设置会话数据提供程序（用于自动保存）
    /// </summary>
    void SetSessionDataProvider(Func<SessionData> provider);

    /// <summary>
    /// 获取会话文件路径
    /// </summary>
    string GetSessionFilePath();
    
    /// <summary>
    /// 同步保存会话（用于窗口关闭时）
    /// </summary>
    void SaveSessionSync(SessionData session);
}
