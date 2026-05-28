using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using QuickTools.Models;

namespace QuickTools.Services;

public sealed class QuickActionService
{
    public static readonly IReadOnlyList<QuickAction> AllActions =
    [
        new() { Id = "mute", Icon = "\uE74F" },
        new() { Id = "vol_up", Icon = "\uE767" },
        new() { Id = "vol_down", Icon = "\uE993" },
        new() { Id = "play_pause", Icon = "\uE768" },
        new() { Id = "screenshot", Icon = "\uE722" },
        new() { Id = "lock", Icon = "\uE72E" },
        new() { Id = "dark_mode", Icon = "\uE708" },
        new() { Id = "clipboard", Icon = "\uE8C8" },
        new() { Id = "calculator", Icon = "\uE8EF" },
        new() { Id = "taskmgr", Icon = "\uE9D9" },
        new() { Id = "wifi", Icon = "\uE701" },
        new() { Id = "settings", Icon = "\uE713" },
    ];

    public void Execute(string actionId)
    {
        switch (actionId)
        {
            case "mute":
                SendAudioCommand(8);
                break;
            case "vol_up":
                SendAudioCommand(10);
                break;
            case "vol_down":
                SendAudioCommand(9);
                break;
            case "play_pause":
                SendAudioCommand(14);
                break;
            case "screenshot":
                SendKey(0x2C);
                break;
            case "lock":
                LockWorkStation();
                break;
            case "dark_mode":
                ToggleDarkMode();
                break;
            case "clipboard":
                SendWinV();
                break;
            case "calculator":
                Launch("calc");
                break;
            case "taskmgr":
                Launch("taskmgr");
                break;
            case "wifi":
                ToggleWifi();
                break;
            case "settings":
                Launch("ms-settings:");
                break;
        }
    }

    private static void SendAudioCommand(int appCommand)
    {
        var hwnd = FindWindow("Shell_TrayWnd", null);
        if (hwnd == IntPtr.Zero)
        {
            hwnd = GetDesktopWindow();
        }

        SendMessage(hwnd, 0x0319, hwnd, new IntPtr(appCommand << 16));
    }

    private static void SendKey(int vk)
    {
        Input[] inputs = [CreateKey(vk, false), CreateKey(vk, true)];
        SendInputNative((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }

    private static void SendWinV()
    {
        Input[] inputs =
        [
            CreateKey(0x5B, false),
            CreateKey(0x56, false),
            CreateKey(0x56, true),
            CreateKey(0x5B, true),
        ];

        SendInputNative((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }

    private static void ToggleDarkMode()
    {
        const string path = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using var key = Registry.CurrentUser.OpenSubKey(path, writable: true);
        if (key is null)
        {
            return;
        }

        var current = (int)(key.GetValue("AppsUseLightTheme") ?? 1);
        var next = current == 1 ? 0 : 1;
        key.SetValue("AppsUseLightTheme", next, RegistryValueKind.DWord);
        key.SetValue("SystemUsesLightTheme", next, RegistryValueKind.DWord);
    }

    private static void ToggleWifi()
    {
        try
        {
            var result = RunCmd("netsh interface show interface");
            var wifiLine = result.Split('\n')
                .FirstOrDefault(line => line.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("Wireless", StringComparison.OrdinalIgnoreCase));

            if (wifiLine is null)
            {
                return;
            }

            var isEnabled = wifiLine.Contains("Enabled", StringComparison.OrdinalIgnoreCase)
                && !wifiLine.Contains("Disabled", StringComparison.OrdinalIgnoreCase);
            var adapterName = wifiLine.Trim().Split(["  "], StringSplitOptions.RemoveEmptyEntries).Last().Trim();
            var action = isEnabled ? "disable" : "enable";
            RunCmd($"netsh interface set interface \"{adapterName}\" {action}", asAdmin: true);
        }
        catch
        {
            // Best effort: changing Wi-Fi can require elevation or vary by adapter name.
        }
    }

    private static void Launch(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    private static string RunCmd(string args, bool asAdmin = false)
    {
        var startInfo = new ProcessStartInfo("cmd.exe", $"/c {args}")
        {
            RedirectStandardOutput = !asAdmin,
            UseShellExecute = asAdmin,
            CreateNoWindow = true,
            Verb = asAdmin ? "runas" : ""
        };

        using var process = Process.Start(startInfo);
        return asAdmin ? "" : process?.StandardOutput.ReadToEnd() ?? "";
    }

    private static Input CreateKey(int vk, bool keyUp) => new()
    {
        Type = 1,
        KeyboardInput = new KeyboardInput
        {
            VirtualKey = (ushort)vk,
            Flags = keyUp ? 0x0002u : 0u
        }
    };

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    [DllImport("user32.dll", EntryPoint = "SendInput")]
    private static extern uint SendInputNative(uint n, Input[] inputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public KeyboardInput KeyboardInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public char ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
