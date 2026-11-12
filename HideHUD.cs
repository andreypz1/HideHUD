using System;
using System.Collections.Generic;
using System.IO;
using RedLoader;
using SonsSdk;
using UnityEngine;

namespace HideHUD;

public class HideHUD : SonsMod
{
    public HideHUD()
    {
        OnUpdateCallback = HudController.Tick;
    }

    protected override void OnInitializeMod()
    {
        Config.Init();
    }

    protected override void OnSdkInitialized()
    {
    }

    protected override void OnGameStart()
    {
    }
}
internal static class HudController
{
    private const KeyCode DefaultKey = KeyCode.F4;
    private const float RescanInterval = 0.25f;

    // Выключенные нами Canvas'ы (ключ — InstanceID).
    private static readonly Dictionary<int, Canvas> Hidden = new();

    private static KeyCode _toggleKey = DefaultKey;
    private static bool _keyLoaded;
    private static bool _active;
    private static float _nextScan;

    public static void Tick()
    {
        try
        {
            if (!_keyLoaded)
            {
                LoadKey();
                _keyLoaded = true;
            }

            if (Input.GetKeyDown(_toggleKey))
            {
                _active = !_active;
                if (_active) HideAll();
                else ShowAll();
                RLog.Msg(_active ? "[HideHUD] HUD hidden" : "[HideHUD] HUD shown");
                return;
            }

            if (_active && Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + RescanInterval;
                HideAll();
            }
        }
        catch (Exception e)
        {
            RLog.Error("[HideHUD] " + e);
        }
    }

    private static void LoadKey()
    {
        try
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "UserData", "HideHUD_Key.txt");
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, DefaultKey.ToString());
                return;
            }

            var text = File.ReadAllText(path).Trim();
            if (Enum.TryParse(text, true, out KeyCode parsed))
                _toggleKey = parsed;
            else
                RLog.Warning($"[HideHUD] Unknown key '{text}' in HideHUD_Key.txt, using {DefaultKey}");
        }
        catch (Exception e)
        {
            RLog.Warning("[HideHUD] Could not read key file: " + e.Message);
        }
        finally
        {
            RLog.Msg($"[HideHUD] Toggle key: {_toggleKey}");
        }
    }

    private static void HideAll()
    {
        var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            try
            {
                if (c == null || !c.enabled) continue;

                if (c.renderMode == RenderMode.WorldSpace) continue;

                c.enabled = false;
                Hidden[c.GetInstanceID()] = c;
            }
            catch (Exception e)
            {
                RLog.Warning("[HideHUD] " + e.Message);
            }
        }
    }

    private static void ShowAll()
    {
        foreach (var kv in Hidden)
        {
            try
            {
                if (kv.Value != null) kv.Value.enabled = true;
            }
            catch (Exception e)
            {
                RLog.Warning("[HideHUD] " + e.Message);
            }
        }
        Hidden.Clear();
    }
}