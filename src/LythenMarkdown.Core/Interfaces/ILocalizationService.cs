using System;
using System.Collections.Generic;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 本地化服务接口
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// 设置语言
    /// </summary>
    void SetLanguage(string languageCode);

    /// <summary>
    /// 获取当前语言
    /// </summary>
    string GetCurrentLanguage();

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// 获取本地化字符串（带参数）
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// 获取可用语言列表
    /// </summary>
    IReadOnlyList<LanguageInfo> GetAvailableLanguages();

    /// <summary>
    /// 语言变更时事件
    /// </summary>
    event EventHandler<string>? LanguageChanged;
}
