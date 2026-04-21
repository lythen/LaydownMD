using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 设置服务实现
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings _currentSettings;

    public AppSettings CurrentSettings => _currentSettings;

    public SettingsService()
    {
        _settingsPath = GetSettingsFilePath();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _currentSettings = new AppSettings();
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            _currentSettings = new AppSettings();
            return _currentSettings;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            _currentSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) 
                ?? new AppSettings();
        }
        catch
        {
            _currentSettings = new AppSettings();
        }

        return _currentSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json);
        _currentSettings = settings;
    }

    public async Task AddRecentFileAsync(string filePath)
    {
        var recentFiles = _currentSettings.RecentFiles.ToList();
        recentFiles.Remove(filePath);
        recentFiles.Insert(0, filePath);

        if (recentFiles.Count > _currentSettings.MaxRecentFiles)
        {
            recentFiles = recentFiles.Take(_currentSettings.MaxRecentFiles).ToList();
        }

        _currentSettings.RecentFiles = recentFiles;
        await SaveSettingsAsync(_currentSettings);
    }

    public string GetSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "LythenMarkdown", "settings.json");
    }
}
