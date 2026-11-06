using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Orientation = System.Windows.Controls.Orientation;
using Forms = System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace BOTB;

public sealed class TelegramWindow : Window
{
    private const string TelegramExe = @"C:\Users\Admin\AppData\Roaming\Telegram Desktop\Telegram.exe";
    private readonly Forms.Panel nativePanel = new() { Dock = Forms.DockStyle.Fill };
    private readonly TextBlock status = new() { Text = "Telegram Desktop manages your sign-in.", Foreground = Brushes.LightGreen };
    private IntPtr telegramHwnd;
    private IntPtr originalParent;
    private IntPtr originalStyle;
    private bool embedded;

    private const int GWL_STYLE = -16;
    private const long WS_CHILD = 0x40000000L;
    private const long WS_POPUP = 0x80000000L;
    private const long WS_CAPTION = 0x00C00000L;
    private const long WS_THICKFRAME = 0x00040000L;
    private const int SW_RESTORE = 9;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int cmd);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);

    public TelegramWindow()
    {
        Title = "BOTB | Telegram Desktop";
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181d23"));
        Width = 1100; Height = 750; MinWidth = 650; MinHeight = 450;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var layout = new DockPanel { Margin = new Thickness(12) };
        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var attach = MakeButton("Try Embed (Experimental)");
        var open = MakeButton("Open Separately");
        var detach = MakeButton("Detach");
        attach.Click += async (_, _) => await AttachAsync();
        open.Click += (_, _) => OpenSeparate();
        detach.Click += (_, _) => Detach();
        header.Children.Add(attach); header.Children.Add(open); header.Children.Add(detach);
        DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
        status.Margin = new Thickness(0, 0, 0, 8);
        DockPanel.SetDock(status, Dock.Bottom); layout.Children.Add(status);
        var border = new Border { Background = Brushes.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#03d0ff")), BorderThickness = new Thickness(1) };
        border.Child = new WindowsFormsHost { Child = nativePanel };
        layout.Children.Add(border);
        Content = layout;
        nativePanel.Resize += (_, _) => FitEmbedded();
        Closing += async (_, _) =>
        {
            Detach();
            await ExitTelegramProcessesAsync();
        };
        Loaded += (_, _) => { OpenSeparate(); status.Text = "Telegram is running in stable mode. Cross-process embedding can freeze input; use Try Embed only if desired."; };
    }

    private static Button MakeButton(string label) => new()
    {
        Content = label, Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(13, 7, 13, 7),
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202533")),
        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#03d0ff")),
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#03d0ff"))
    };

    private Process? FindTelegram()
    {
        foreach (var process in Process.GetProcessesByName("Telegram"))
        {
            try
            {
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero && !process.HasExited)
                    return process;
            }
            catch { process.Dispose(); }
        }
        return null;
    }

    private void OpenSeparate()
    {
        Detach();
        try
        {
            var running = FindTelegram();
            if (running != null) { ShowWindow(running.MainWindowHandle, SW_RESTORE); SetForegroundWindow(running.MainWindowHandle); status.Text = "Telegram opened in stable desktop mode."; return; }
            if (!File.Exists(TelegramExe)) throw new FileNotFoundException("Telegram Desktop was not found.", TelegramExe);
            Process.Start(new ProcessStartInfo(TelegramExe) { UseShellExecute = true });
            status.Text = "Telegram started separately. Sign in through Telegram Desktop if needed.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Telegram", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async Task AttachAsync()
    {
        if (embedded) return;
        try
        {
            var process = FindTelegram();
            if (process == null)
            {
                if (!File.Exists(TelegramExe)) throw new FileNotFoundException("Telegram Desktop was not found.", TelegramExe);
                Process.Start(new ProcessStartInfo(TelegramExe) { UseShellExecute = true });
                for (int attempt = 0; attempt < 30 && process == null; attempt++)
                { await Task.Delay(300); process = FindTelegram(); }
            }
            if (process == null) { status.Text = "Telegram is starting. Press Embed Telegram to retry."; return; }
            telegramHwnd = process.MainWindowHandle;
            if (telegramHwnd == IntPtr.Zero) throw new InvalidOperationException("Telegram has no visible window.");
            ShowWindow(telegramHwnd, SW_RESTORE);
            originalStyle = GetWindowLongPtr(telegramHwnd, GWL_STYLE);
            originalParent = SetParent(telegramHwnd, nativePanel.Handle);
            if (originalParent == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
                throw new InvalidOperationException("Windows did not allow Telegram to be embedded.");
            long newStyle = (originalStyle.ToInt64() | WS_CHILD) & ~WS_POPUP & ~WS_CAPTION & ~WS_THICKFRAME;
            SetWindowLongPtr(telegramHwnd, GWL_STYLE, new IntPtr(newStyle));
            SetWindowPos(telegramHwnd, IntPtr.Zero, 0, 0, nativePanel.Width, nativePanel.Height, SWP_NOZORDER | SWP_FRAMECHANGED);
            embedded = true;
            FitEmbedded();
            status.Text = "Experimental embedding active. If Telegram does not respond, click Open Separately to restore normal input.";
        }
        catch (Exception ex)
        {
            Detach();
            status.Text = "Embedding unavailable: " + ex.Message + " Use Open Separately.";
        }
    }

    private void FitEmbedded()
    {
        if (embedded && IsWindow(telegramHwnd))
            MoveWindow(telegramHwnd, 0, 0, Math.Max(1, nativePanel.Width), Math.Max(1, nativePanel.Height), true);
    }

    private static async Task ExitTelegramProcessesAsync()
    {
        // Shut down only the installed Telegram Desktop application, not AyuGram
        // or another executable that happens to share the process name.
        var processes = Process.GetProcessesByName("Telegram");
        foreach (var process in processes)
        {
            try
            {
                if (process.HasExited) continue;
                string? path = process.MainModule?.FileName;
                if (!string.Equals(Path.GetFullPath(path ?? ""), Path.GetFullPath(TelegramExe),
                    StringComparison.OrdinalIgnoreCase)) continue;

                // Give Telegram a chance to save state and exit normally.
                process.CloseMainWindow();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException)
                {
                    // Telegram may minimize to tray rather than exit on a close request.
                    if (!process.HasExited) process.Kill(entireProcessTree: false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Telegram exit request failed: " + ex.Message);
            }
            finally { process.Dispose(); }
        }
    }

    private void Detach()
    {
        if (telegramHwnd != IntPtr.Zero && IsWindow(telegramHwnd))
        {
            try
            {
                SetParent(telegramHwnd, originalParent);
                if (originalStyle != IntPtr.Zero) SetWindowLongPtr(telegramHwnd, GWL_STYLE, originalStyle);
                SetWindowPos(telegramHwnd, IntPtr.Zero, 80, 80, 1000, 700, SWP_NOZORDER | SWP_FRAMECHANGED);
                ShowWindow(telegramHwnd, SW_RESTORE);
            }
            catch { }
        }
        telegramHwnd = IntPtr.Zero;
        originalParent = IntPtr.Zero;
        embedded = false;
    }
}
