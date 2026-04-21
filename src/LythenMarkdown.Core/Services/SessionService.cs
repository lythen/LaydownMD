using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 会话服务实现
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISettingsService _settingsService;
    private readonly JsonSerializerOptions _jsonOptions;
    private CancellationTokenSource? _autoSaveCts;
    private Func<SessionData>? _getCurrentSessionFunc;

    public SessionService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void SetSessionDataProvider(Func<SessionData> provider)
    {
        _getCurrentSessionFunc = provider;
    }

    public async Task SaveSessionAsync(SessionData session)
    {
        session.SavedAt = DateTime.Now;
        var path = GetSessionFilePath();
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(session, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<SessionData?> LoadSessionAsync()
    {
        var path = GetSessionFilePath();
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<SessionData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载会话失败: {ex.Message}");
            return null;
        }
    }

    public async Task ClearSessionAsync()
    {
        var path = GetSessionFilePath();
        if (File.Exists(path))
        {
            await Task.Run(() => File.Delete(path));
        }
    }

    public void StartAutoSave(int intervalSeconds)
    {
        StopAutoSave();
        _autoSaveCts = new CancellationTokenSource();
        var token = _autoSaveCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
                    if (!token.IsCancellationRequested && _getCurrentSessionFunc != null)
                    {
                        var session = _getCurrentSessionFunc();
                        await SaveSessionAsync(session);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    public void StopAutoSave()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
        _autoSaveCts = null;
    }

    public string GetSessionFilePath()
    {
        var basePath = _settingsService.GetSettingsFilePath();
        var directory = Path.GetDirectoryName(basePath) ?? "";
        return Path.Combine(directory, "session.json");
    }
}
