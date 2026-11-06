// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RZeroF1.Minecraft.SeedMap;

/// <summary>
/// 定义会话状态的持久化接口，应用数据路径与存储实现由宿主指定。
/// </summary>
public interface ISessionStore
{
    string? Read();
    void Write(string json);
    void BackupInvalid(string json, DateTimeOffset timestamp);
}

/// <summary>适用于桌面宿主的文件存储实现。</summary>
public sealed class FileSessionStore : ISessionStore
{
    public string FilePath { get; }

    public FileSessionStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("A session file path is required.", nameof(filePath));
        FilePath = Path.GetFullPath(filePath);
    }

    public string? Read() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;

    public void Write(string json)
    {
        string? directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, FilePath, true);
    }

    public void BackupInvalid(string json, DateTimeOffset timestamp)
    {
        string backup = FilePath + ".invalid-" + timestamp.UtcDateTime.ToString("yyyyMMddHHmmss");
        File.WriteAllText(backup, json);
    }
}

public sealed class SessionOptions
{
    public ISessionStore? Store { get; init; }
    public Action<Exception>? Error { get; init; }
}

/// <summary>供一个或多个宿主控件共享的轻量会话状态。</summary>
public sealed class Session
{
    private readonly ISessionStore? _store;
    private readonly Action<Exception>? _error;
    private bool _loaded;

    public Workspace Workspace { get; private set; } = new();
    public Profile Profile { get; private set; } = new();
    public event Action<object?>? Changed;

    public Session(SessionOptions? options = null)
    {
        _store = options?.Store;
        _error = options?.Error;
    }

    public void Load()
    {
        if (_loaded) return;
        _loaded = true;
        string? json = null;
        try
        {
            json = _store?.Read();
            if (!string.IsNullOrWhiteSpace(json))
            {
                Workspace = JsonSerializer.Deserialize(json, JsonContext.Default.Workspace)
                    ?? throw new InvalidDataException("地图配置为空。");
                if (Workspace.SchemaVersion != 1) throw new InvalidDataException("不支持的地图配置版本。");
            }

            Profile = Workspace.Profiles.FirstOrDefault(p => p.Id == Workspace.ActiveId)
                ?? Workspace.Profiles.FirstOrDefault() ?? new();
            Normalize(Profile);
        }
        catch (Exception ex)
        {
            if (json is not null)
            {
                try { _store?.BackupInvalid(json, DateTimeOffset.UtcNow); }
                catch (Exception backupError) { _error?.Invoke(backupError); }
            }
            _error?.Invoke(ex);
            Workspace = new();
            Profile = new();
        }

        if (!Workspace.Profiles.Contains(Profile)) Workspace.Profiles.Add(Profile);
        Workspace.ActiveId = Profile.Id;
    }

    private static void Normalize(Profile profile)
    {
        profile.CenterX = Api.ClampCoordinate(profile.CenterX);
        profile.CenterZ = Api.ClampCoordinate(profile.CenterZ);
        profile.PixelsPerBlock = Math.Clamp(
            double.IsFinite(profile.PixelsPerBlock) ? profile.PixelsPerBlock : 0.25,
            1.0 / 256,
            8);
        profile.Y = Math.Clamp(profile.Y, -64, 320);
        profile.Biomes ??= new();
        profile.Structures ??= new();
        profile.Waypoints ??= new();
        profile.Seed = Api.ParseSeed(profile.SeedText);
    }

    public void Select(string seed, string version, int dimension, int y, object? source = null)
    {
        long value = Api.ParseSeed(seed);
        var profile = Workspace.Profiles.FirstOrDefault(p =>
            p.Seed == value && p.Version == version && p.Dimension == dimension);
        if (profile is null)
        {
            profile = new()
            {
                SeedText = seed.Trim(),
                Seed = value,
                Version = version,
                Dimension = dimension,
                Y = y
            };
            Workspace.Profiles.Add(profile);
        }

        profile.SeedText = seed.Trim();
        profile.Y = y;
        profile.LastUsedUtc = DateTime.UtcNow;
        profile.InHistory = true;
        Profile = profile;
        Workspace.ActiveId = profile.Id;
        Workspace.Profiles = Workspace.Profiles
            .OrderByDescending(p => p.Favorite)
            .ThenByDescending(p => p.LastUsedUtc)
            .Where((p, i) => p.Favorite || p.Waypoints is { Count: > 0 } || i < 30 || ReferenceEquals(p, Profile))
            .ToList();
        Notify(source);
    }

    public void Restore(Profile profile, object? source = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Profile = profile;
        Workspace.ActiveId = profile.Id;
        profile.LastUsedUtc = DateTime.UtcNow;
        profile.InHistory = true;
        Normalize(profile);
        Notify(source);
    }

    public void Notify(object? source = null) => Changed?.Invoke(source);

    public void Save()
    {
        if (_store is null) return;
        _store.Write(JsonSerializer.Serialize(Workspace, JsonContext.Default.Workspace));
    }
}

[JsonSerializable(typeof(Workspace))]
internal partial class JsonContext : JsonSerializerContext
{
}
