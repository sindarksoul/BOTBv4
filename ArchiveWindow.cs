using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using WinForms = System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using Orientation = System.Windows.Controls.Orientation;
using MessageBox = System.Windows.MessageBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace BOTB;

public sealed class ArchiveWindow : Window
{
    private readonly TextBox archivePath = new() { IsReadOnly = true, MinWidth = 350 };
    private readonly TextBox destinationPath = new() { IsReadOnly = true, MinWidth = 350 };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    private const int RobotCount = 20;
    private readonly TextBlock robotProgress = new() { FontSize = 22, Margin = new Thickness(0, 10, 0, 3), TextWrapping = TextWrapping.NoWrap };
    private readonly TextBlock spinnerLabel = new() { Text = "", FontFamily = new FontFamily("Consolas") };
    private readonly DispatcherTimer preparationTimer = new() { Interval = TimeSpan.FromMilliseconds(130) };
    private readonly char[] spinnerFrames = { '|', '/', '-', '\\' };
    private int spinnerIndex;
    private int extractionPercent;
    private readonly TextBlock progressLabel = new() { Text = "Ready - 0%" };
    private readonly Button extractButton = new() { Content = "Extract Archive", MinWidth = 140, Height = 32 };
    private readonly Button archiveButton = new() { Content = "Browse Archive", Margin = new Thickness(8, 0, 0, 0) };
    private readonly Button destinationButton = new() { Content = "Browse Destination", Margin = new Thickness(8, 0, 0, 0) };
    private readonly string sevenZipPath = Path.Combine(AppContext.BaseDirectory, "Tools", "7zip", "7z.exe");

    public ArchiveWindow()
    {
        Title = "BOTB - Extract Archive (7-Zip)";
        Width = 640; Height = 335; MinWidth = 570; MinHeight = 310;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush("#181d23");
        Foreground = Brushes.White;
        FontFamily = new FontFamily("Segoe UI");
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock
        {
            Text = "EXTRACT ARCHIVE · 7-ZIP",
            FontSize = 22, FontWeight = FontWeights.Bold,
            Foreground = Brush("#03d0ff"), Margin = new Thickness(0, 0, 0, 15)
        });
        panel.Children.Add(new TextBlock { Text = "Archive file (.zip, .7z, .rar, etc.)", Foreground = Brushes.White });
        var archiveRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 12) };
        archiveRow.Children.Add(archivePath); archiveRow.Children.Add(archiveButton); panel.Children.Add(archiveRow);
        panel.Children.Add(new TextBlock { Text = "Destination parent folder", Foreground = Brushes.White });
        var destinationRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 12) };
        destinationRow.Children.Add(destinationPath); destinationRow.Children.Add(destinationButton); panel.Children.Add(destinationRow);
        panel.Children.Add(extractButton);
        panel.Children.Add(robotProgress);
        panel.Children.Add(spinnerLabel);
        panel.Children.Add(progressLabel);
        foreach (var field in new[] { archivePath, destinationPath })
        {
            field.Background = Brush("#232a36");
            field.Foreground = Brushes.White;
            field.BorderBrush = Brush("#03d0ff");
            field.CaretBrush = Brush("#03d0ff");
            field.Padding = new Thickness(6, 3, 6, 3);
            field.VerticalContentAlignment = VerticalAlignment.Center;
            field.Height = 30;
        }
        foreach (var button in new[] { archiveButton, destinationButton, extractButton })
        {
            button.Background = Brush("#202533");
            button.Foreground = Brush("#03d0ff");
            button.BorderBrush = Brush("#03d0ff");
            button.FontWeight = FontWeights.SemiBold;
            button.Padding = new Thickness(10, 4, 10, 4);
        }
        extractButton.Background = Brush("#03d0ff");
        extractButton.Foreground = Brush("#181d23");
        robotProgress.FontFamily = new FontFamily("Segoe UI Emoji");
        spinnerLabel.Foreground = Brush("#03d0ff");
        preparationTimer.Tick += (_, _) =>
        {
            spinnerLabel.Text = $" {spinnerFrames[spinnerIndex++ % spinnerFrames.Length]}  Preparing archive...";
        };
        RenderRobots(0);
        progressLabel.Foreground = Brush("#26fda3");
        status.Foreground = Brush("#26fda3");
        status.FontFamily = new FontFamily("Consolas");
        status.Margin = new Thickness(0, 12, 0, 0);
        panel.Children.Add(status);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        archiveButton.Click += (_, _) =>
        {
            using var dialog = new WinForms.OpenFileDialog
            {
                Title = "Choose an archive to extract",
                Filter = "Archives|*.zip;*.7z;*.rar;*.tar;*.gz;*.bz2;*.xz;*.cab;*.iso|All files|*.*"
            };
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                archivePath.Text = dialog.FileName;
                if (string.IsNullOrWhiteSpace(destinationPath.Text))
                    destinationPath.Text = Path.GetDirectoryName(dialog.FileName) ?? "";
            }
        };
        destinationButton.Click += (_, _) =>
        {
            using var dialog = new WinForms.FolderBrowserDialog { Description = "Select destination parent folder" };
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                destinationPath.Text = dialog.SelectedPath;
        };
        extractButton.Click += Extract_Click;
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private void RenderRobots(int percent)
    {
        extractionPercent = Math.Clamp(percent, 0, 100);
        int active = (int)Math.Ceiling(extractionPercent * RobotCount / 100.0);
        robotProgress.Inlines.Clear();
        for (int i = 0; i < RobotCount; i++)
            robotProgress.Inlines.Add(new Run("🤖")
            {
                Foreground = i < active ? Brush("#03d0ff") : Brush("#4d5965")
            });
    }

    private async void Extract_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(archivePath.Text) || !Directory.Exists(destinationPath.Text))
        {
            MessageBox.Show(this, "Select an existing archive and destination folder.", "Missing selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!File.Exists(sevenZipPath))
        {
            MessageBox.Show(this, "Bundled 7-Zip engine was not found.", "7-Zip unavailable",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        RenderRobots(0);
        progressLabel.Text = "Preparing - 0%";
        extractButton.IsEnabled = false;
        archiveButton.IsEnabled = false;
        destinationButton.IsEnabled = false;
        string? stagingFolder = null;
        try
        {
            var archive = Path.GetFullPath(archivePath.Text);
            var parent = Path.GetFullPath(destinationPath.Text);
            var finalFolder = Path.Combine(parent, Path.GetFileNameWithoutExtension(archive));
            if (Directory.Exists(finalFolder) || File.Exists(finalFolder))
                throw new IOException("The destination folder already exists. Choose a different parent folder.");

            stagingFolder = Path.Combine(parent, ".botb-extract-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stagingFolder);
            status.Text = "Checking archive...";
            spinnerIndex = 0;
            preparationTimer.Start();
            (int ExitCode, string Output) listing;
            try { listing = await RunSevenZipAsync("l", archive, null, null, true); }
            finally { preparationTimer.Stop(); spinnerLabel.Text = ""; }
            bool encrypted = listing.Output.Contains("Encrypted = +", StringComparison.OrdinalIgnoreCase)
                || listing.Output.Contains("Wrong password", StringComparison.OrdinalIgnoreCase)
                || listing.Output.Contains("Enter password", StringComparison.OrdinalIgnoreCase);

            string? password = null;
            if (encrypted)
            {
                password = RequestPassword();
                if (password == null) { status.Text = "Cancelled."; return; }
            }
            else if (listing.ExitCode != 0)
                throw new InvalidDataException("7-Zip could not read this archive. It may be damaged or encrypted.");

            status.Text = "Extracting into a temporary folder...";
            var progress = new Progress<int>(percent =>
            {
                RenderRobots(percent);
                progressLabel.Text = $"Extracting - {percent}%";
            });
            var result = await RunSevenZipAsync("x", archive, stagingFolder, password, false, progress);
            if (result.ExitCode != 0 && !encrypted &&
                (result.Output.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                 result.Output.Contains("encrypted", StringComparison.OrdinalIgnoreCase)))
            {
                password = RequestPassword();
                if (password == null) { status.Text = "Cancelled."; return; }
                Directory.Delete(stagingFolder, true);
                Directory.CreateDirectory(stagingFolder);
                RenderRobots(0);
                progressLabel.Text = "Retrying - 0%";
                result = await RunSevenZipAsync("x", archive, stagingFolder, password, false, progress);
            }
            if (result.ExitCode != 0)
                throw new InvalidDataException("7-Zip could not extract the archive (exit code " + result.ExitCode +
                    "). Check its integrity and, if protected, verify the password.");

            Directory.Move(stagingFolder, finalFolder);
            stagingFolder = null;
            RenderRobots(100);
            progressLabel.Text = "Completed - 100%";
            status.Text = "Extraction complete: " + finalFolder;
            Close();
        }
        catch (Exception ex)
        {
            progressLabel.Text = "Extraction failed at " + extractionPercent.ToString() + "%";
            status.Text = "Extraction failed: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Extraction failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            preparationTimer.Stop();
            spinnerLabel.Text = "";
            if (stagingFolder != null)
            {
                try { if (Directory.Exists(stagingFolder)) Directory.Delete(stagingFolder, true); }
                catch { status.Text += "\nWarning: temporary extraction folder could not be deleted."; }
            }
            extractButton.IsEnabled = true;
            archiveButton.IsEnabled = true;
            destinationButton.IsEnabled = true;
        }
    }

    private string? RequestPassword()
    {
        var dialog = new Window
        {
            Title = "Password-protected archive",
            Width = 390, Height = 175, ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this,
            Background = Brush("#181d23"), Foreground = Brushes.White
        };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Enter the archive password:", Foreground = Brushes.White });
        var input = new PasswordBox { Margin = new Thickness(0, 8, 0, 12) };
        input.Background = Brush("#232a36");
        input.Foreground = Brushes.White;
        input.BorderBrush = Brush("#03d0ff");
        panel.Children.Add(input);
        var ok = new Button { Content = "Unlock and extract", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Background = Brush("#202533");
        ok.Foreground = Brush("#03d0ff");
        ok.BorderBrush = Brush("#03d0ff");
        ok.Click += (_, _) => { dialog.DialogResult = true; };
        panel.Children.Add(ok);
        dialog.Content = panel;
        input.Loaded += (_, _) => input.Focus();
        return dialog.ShowDialog() == true ? input.Password : null;
    }

    private async Task<(int ExitCode, string Output)> RunSevenZipAsync(
        string action, string archive, string? output, string? password, bool list,
        IProgress<int>? progress = null)
    {
        var info = new ProcessStartInfo(sevenZipPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true
        };
        info.ArgumentList.Add(action);
        info.ArgumentList.Add("-y");
        info.ArgumentList.Add(list ? "-bd" : "-bsp1");
        info.ArgumentList.Add("-bso1");
        info.ArgumentList.Add("-bse1");
        if (list) info.ArgumentList.Add("-slt");
        if (output != null) info.ArgumentList.Add("-o" + output);
        if (password != null) info.ArgumentList.Add("-p" + password);
        info.ArgumentList.Add(archive);
        using var process = Process.Start(info) ?? throw new IOException("Could not launch 7-Zip.");
        await process.StandardInput.WriteLineAsync("");
        process.StandardInput.Close();

        var stderr = process.StandardError.ReadToEndAsync();
        var outputText = new StringBuilder();
        var segment = new StringBuilder();
        int lastProgress = -1;
        var buffer = new char[512];
        int count;
        while ((count = await process.StandardOutput.ReadAsync(buffer.AsMemory())) > 0)
        {
            for (int i = 0; i < count; i++)
            {
                char ch = buffer[i];
                if (ch == '\r' || ch == '\n')
                {
                    if (segment.Length > 0)
                    {
                        string line = segment.ToString();
                        var match = Regex.Match(line, @"(?:^|\s)(\d{1,3})%(?=\s|$)");
                        if (progress != null && match.Success &&
                            int.TryParse(match.Groups[1].Value, out int percent) &&
                            percent >= 0 && percent <= 100 && percent != lastProgress)
                        {
                            lastProgress = percent;
                            progress.Report(percent);
                        }
                        if (outputText.Length < 40000)
                            outputText.AppendLine(line.Length > 500 ? line[..500] : line);
                        segment.Clear();
                    }
                }
                else if (segment.Length < 1024)
                    segment.Append(ch);
            }
        }
        if (segment.Length > 0 && outputText.Length < 40000)
            outputText.AppendLine(segment.ToString());
        await process.WaitForExitAsync();
        return (process.ExitCode, outputText + "\n" + await stderr);
    }
}
