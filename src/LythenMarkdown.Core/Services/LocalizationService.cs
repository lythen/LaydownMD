using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 本地化服务实现
/// </summary>
public class LocalizationService : ILocalizationService
{
    private string _currentLanguage = "zh-CN";
    private readonly Dictionary<string, Dictionary<string, string>> _strings = new();

    public event EventHandler<string>? LanguageChanged;

    public LocalizationService()
    {
        InitializeStrings();
    }

    private void InitializeStrings()
    {
        // 简体中文
        _strings["zh-CN"] = new Dictionary<string, string>
        {
            ["App.Title"] = "Lythen-Markdown",
            ["Menu.File"] = "文件",
            ["Menu.Edit"] = "编辑",
            ["Menu.View"] = "视图",
            ["Menu.Format"] = "格式",
            ["Menu.Help"] = "帮助",
            ["File.New"] = "新建",
            ["File.Open"] = "打开...",
            ["File.Save"] = "保存",
            ["File.SaveAs"] = "另存为...",
            ["File.Exit"] = "退出",
            ["Edit.Undo"] = "撤销",
            ["Edit.Redo"] = "重做",
            ["Edit.Cut"] = "剪切",
            ["Edit.Copy"] = "复制",
            ["Edit.Paste"] = "粘贴",
            ["Edit.Find"] = "查找...",
            ["Edit.Replace"] = "替换...",
            ["View.EditMode"] = "编辑模式",
            ["View.SplitMode"] = "分栏模式",
            ["View.PreviewMode"] = "预览模式",
            ["View.PopupPreview"] = "弹窗预览...",
            ["Help.UserGuide"] = "使用说明",
            ["Help.Intro"] = "软件介绍",
            ["Help.About"] = "关于",
            ["Status.Modified"] = "已修改",
            ["Status.Line"] = "行",
            ["Status.Column"] = "列",
            ["Dialog.Save.Title"] = "保存确认",
            ["Dialog.Save.Message"] = "是否保存对 {0} 的更改？",
            ["Dialog.Save.Yes"] = "保存",
            ["Dialog.Save.No"] = "不保存",
            ["Dialog.Save.Cancel"] = "取消"
        };

        // English
        _strings["en-US"] = new Dictionary<string, string>
        {
            ["App.Title"] = "Lythen-Markdown",
            ["Menu.File"] = "File",
            ["Menu.Edit"] = "Edit",
            ["Menu.View"] = "View",
            ["Menu.Format"] = "Format",
            ["Menu.Help"] = "Help",
            ["File.New"] = "New",
            ["File.Open"] = "Open...",
            ["File.Save"] = "Save",
            ["File.SaveAs"] = "Save As...",
            ["File.Exit"] = "Exit",
            ["Edit.Undo"] = "Undo",
            ["Edit.Redo"] = "Redo",
            ["Edit.Cut"] = "Cut",
            ["Edit.Copy"] = "Copy",
            ["Edit.Paste"] = "Paste",
            ["Edit.Find"] = "Find...",
            ["Edit.Replace"] = "Replace...",
            ["View.EditMode"] = "Edit Mode",
            ["View.SplitMode"] = "Split Mode",
            ["View.PreviewMode"] = "Preview Mode",
            ["View.PopupPreview"] = "Popup Preview...",
            ["Help.UserGuide"] = "User Guide",
            ["Help.Intro"] = "Introduction",
            ["Help.About"] = "About",
            ["Status.Modified"] = "Modified",
            ["Status.Line"] = "Line",
            ["Status.Column"] = "Column",
            ["Dialog.Save.Title"] = "Save Confirmation",
            ["Dialog.Save.Message"] = "Do you want to save changes to {0}?",
            ["Dialog.Save.Yes"] = "Save",
            ["Dialog.Save.No"] = "Don't Save",
            ["Dialog.Save.Cancel"] = "Cancel"
        };
    }

    public void SetLanguage(string languageCode)
    {
        if (_strings.ContainsKey(languageCode))
        {
            _currentLanguage = languageCode;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageCode);
            LanguageChanged?.Invoke(this, languageCode);
        }
    }

    public string GetCurrentLanguage()
    {
        return _currentLanguage;
    }

    public string GetString(string key)
    {
        if (_strings.TryGetValue(_currentLanguage, out var langStrings))
        {
            if (langStrings.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        // Fallback to English
        if (_strings.TryGetValue("en-US", out var fallbackStrings))
        {
            if (fallbackStrings.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return key;
    }

    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);
        return string.Format(template, args);
    }

    public IReadOnlyList<LanguageInfo> GetAvailableLanguages()
    {
        return LanguageInfo.AvailableLanguages;
    }
}
