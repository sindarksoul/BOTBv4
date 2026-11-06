using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using System.Collections.Generic;

namespace BOTB
{
    public partial class MainWindow : Window
    {
        private static readonly string[] multilineTargets = { "Passwords.txt", "All Passwords.txt", "passwords.txt" };
        private static readonly string[] urlKeys = { "url", "uri" };
        private static readonly string[] loginKeys = { "login", "user", "username", "email" };
        private static readonly string[] passKeys = { "password", "pass", "pwd" };

        public MainWindow()
        {
            InitializeComponent();
            SingleLineRadio.Checked += SingleLineRadio_Checked;
            MultiLineRadio.Checked += MultiLineRadio_Checked;
        }

        private void AppendStatus(string text)
        {
            statusBox?.AppendText(text);
            statusBox?.ScrollToEnd();
        }

        private void DeveloperLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void OpenTelegram_Click(object sender, RoutedEventArgs e)
        {
            new TelegramWindow { Owner = this }.Show();
        }

        private void ExtractArchive_Click(object sender, RoutedEventArgs e)
        {
            new ArchiveWindow { Owner = this }.ShowDialog();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (SingleLineRadio.IsChecked == true)
            {
                using (var dialog = new WinForms.OpenFileDialog())
                {
                    dialog.Filter = "Text Files (*.txt)|*.txt";
                    dialog.Title = "Select a text file";
                    dialog.Multiselect = false;
                    if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        folderPathBox.Text = dialog.FileName;
                        AppendStatus($"Selected file: {dialog.FileName}\n");
                    }
                }
            }
            else
            {
                using (var dialog = new WinForms.FolderBrowserDialog())
                {
                    if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        folderPathBox.Text = dialog.SelectedPath;
                        AppendStatus($"Selected folder: {dialog.SelectedPath}\n");
                    }
                }
            }
        }

        private void StartProcessing_Click(object sender, RoutedEventArgs e)
        {
            string? input = folderPathBox.Text;
            string[] keywords = keywordBox.Text
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Take(2)
                .ToArray();
            bool isMultiLine = MultiLineRadio.IsChecked == true;

            if (isMultiLine)
            {
                AppendStatus($"[+] Starting extraction in folder: {input}\n");
                AppendStatus($"[+] Using keywords: {string.Join(", ", keywords)}\n");
                AppendStatus("[+] Multi-line mode activated.\n");

                Task.Run(() =>
                {
                    try
                    {
                        ScanMultilineMode(input ?? string.Empty, keywords);
                        Dispatcher.Invoke(() => AppendStatus("[+] Extraction complete!\n"));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => AppendStatus($"[ERROR] {ex}\n"));
                    }
                });
            }
            else
            {
                AppendStatus($"[+] Starting extraction in file: {input}\n");
                AppendStatus($"[+] Using keywords: {string.Join(", ", keywords)}\n");
                AppendStatus("[+] Single-line mode activated.\n");

                Task.Run(() =>
                {
                    try
                    {
                        ScanSingleLineFile(input ?? string.Empty, keywords);
                        Dispatcher.Invoke(() => AppendStatus("[+] Extraction complete!\n"));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => AppendStatus($"[ERROR] {ex}\n"));
                    }
                });
            }
        }

        // ---- SMART MULTI-LINE MODE WITH DEDUPLICATION ----
        private void ScanMultilineMode(string root, string[] keywords)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    Dispatcher.Invoke(() => AppendStatus("[ERROR] Invalid folder path.\n"));
                    return;
                }

                string[] targetFiles;
                try
                {
                    targetFiles = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories)
                        .Where(f => multilineTargets.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AppendStatus($"[ERROR] Directory.GetFiles failed: {ex.Message}\n"));
                    return;
                }

                if (targetFiles.Length == 0)
                {
                    Dispatcher.Invoke(() => AppendStatus("[DEBUG] No multi-line target files found! Make sure you have Passwords.txt or All Passwords.txt files in your folder.\n"));
                    return;
                }

                // Build dictionary: keyword -> list of captures
                var outputPerKeyword = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var keyword in keywords)
                    outputPerKeyword[keyword] = new HashSet<string>();

                int fileCount = 0;
                foreach (var file in targetFiles)
                {
                    fileCount++;
                    Dispatcher.Invoke(() => AppendStatus($"[DEBUG] Scanning file {fileCount}/{targetFiles.Length}: {file}\n"));
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        var block = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var line in lines.Append(""))
                        {
                            var trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed))
                            {
                                string login = block.FirstOrDefault(kv => loginKeys.Contains(kv.Key)).Value ?? "";
                                string pass = block.FirstOrDefault(kv => passKeys.Contains(kv.Key)).Value ?? "";
                                string url = block.FirstOrDefault(kv => urlKeys.Contains(kv.Key)).Value ?? "";

                                if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(pass))
                                {
                                    foreach (var keyword in keywords)
                                    {
                                        // Match keyword in URL or login (not password!)
                                        if ((!string.IsNullOrEmpty(url) && url.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                                            || (!string.IsNullOrEmpty(login) && login.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            string cred = $"{login}:{pass}";
                                            outputPerKeyword[keyword].Add(cred);
                                        }
                                    }
                                }
                                block.Clear();
                                continue;
                            }

                            var sepIdx = trimmed.IndexOf(':');
                            if (sepIdx > 0)
                            {
                                string field = trimmed.Substring(0, sepIdx).Trim().ToLowerInvariant();
                                string value = trimmed.Substring(sepIdx + 1).Trim();
                                if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(value))
                                    block[field] = value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => AppendStatus($"[ERROR] Reading {file}: {ex.Message}\n"));
                    }
                }

                // Write out per-keyword files
                int totalMatches = 0;
                foreach (var kv in outputPerKeyword)
                {
                    if (kv.Value.Count > 0)
                    {
                        string outPath = Path.Combine(root ?? "", $"{kv.Key.Replace(".", "").Replace(" ", "_")}.txt");
                        File.WriteAllLines(outPath, kv.Value);
                        Dispatcher.Invoke(() => AppendStatus($"[+] Saved {kv.Value.Count} matches to: {outPath}\n"));
                        totalMatches += kv.Value.Count;
                    }
                }

                Dispatcher.Invoke(() => AppendStatus($"[+] Multi-line extraction found {totalMatches} matches total.\n"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AppendStatus($"[FATAL ERROR] {ex}\n"));
            }
        }

        // ---- SINGLE-LINE MODE ----
        private void ScanSingleLineFile(string filePath, string[] keywords)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    Dispatcher.Invoke(() => AppendStatus("[ERROR] Invalid file path.\n"));
                    return;
                }

                // Build dictionary: keyword -> list of captures
                var outputPerKeyword = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var keyword in keywords)
                    outputPerKeyword[keyword] = new HashSet<string>();

                int lineCount = 0;
                foreach (var line in File.ReadLines(filePath))
                {
                    lineCount++;
                    foreach (var keyword in keywords)
                    {
                        // Extract credential if keyword matches in line (like V1)
                        var cred = ExtractCredential(line, keyword);
                        if (!string.IsNullOrEmpty(cred))
                        {
                            outputPerKeyword[keyword].Add(cred);
                        }
                    }
                    if (lineCount % 5000 == 0)
                    {
                        Dispatcher.Invoke(() => AppendStatus($"[DEBUG] Processed {lineCount} lines...\n"));
                    }
                }

                // Write per-keyword files
                int totalMatches = 0;
                foreach (var kv in outputPerKeyword)
                {
                    if (kv.Value.Count > 0)
                    {
                        string outPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", $"{kv.Key.Replace(".", "").Replace(" ", "_")}.txt");
                        File.WriteAllLines(outPath, kv.Value);
                        Dispatcher.Invoke(() => AppendStatus($"[+] Saved {kv.Value.Count} matches to: {outPath}\n"));
                        totalMatches += kv.Value.Count;
                    }
                }
                Dispatcher.Invoke(() => AppendStatus($"[+] Single-line extraction found {totalMatches} matches total.\n"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AppendStatus($"[ERROR] Reading {filePath}: {ex.Message}\n"));
            }
        }

        // Extract just email:password (or user:pass) after keyword
        private static string? ExtractCredential(string line, string keyword)
        {
            int idx = line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                string after = line.Substring(idx + keyword.Length).TrimStart(':', ' ', '/');
                var parts = after.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    // Check if first part is an email (basic check)
                    if (parts[0].Contains('@'))
                        return $"{parts[0]}:{parts[1]}";
                    else if (parts.Length > 2 && parts[1].Contains('@'))
                        return $"{parts[1]}:{parts[2]}";
                    else
                        return $"{parts[0]}:{parts[1]}";
                }
            }
            return null;
        }

        private void SingleLineRadio_Checked(object sender, RoutedEventArgs e)
        {
            AppendStatus("Single-line mode activated.\n");
        }

        private void MultiLineRadio_Checked(object sender, RoutedEventArgs e)
        {
            AppendStatus("Multi-line mode activated.\n");
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string? folder = null;
            if (SingleLineRadio.IsChecked == true)
            {
                folder = Path.GetDirectoryName(folderPathBox.Text);
            }
            else
            {
                folder = folderPathBox.Text;
            }
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
            else
            {
                AppendStatus("[ERROR] Output folder does not exist.\n");
            }
        }

    }
}
