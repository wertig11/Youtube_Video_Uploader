using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

namespace YouTubeVideoUploader.Installer;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Check if we are running in uninstall mode
        bool isUninstall = args.Length > 0 && args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase);
        
        // Also detect if the executable itself is named "uninstall.exe"
        string currentExePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        string exeName = Path.GetFileNameWithoutExtension(currentExePath);
        if (exeName.Equals("uninstall", StringComparison.OrdinalIgnoreCase))
        {
            isUninstall = true;
        }

        if (isUninstall)
        {
            RunUninstall(currentExePath);
        }
        else
        {
            RunInstall(currentExePath);
        }
    }

    static void RunInstall(string currentExePath)
    {
        // 1. Welcome Prompt
        DialogResult result = MessageBox.Show(
            "Do you want to install YouTube Video Uploader on your computer?\n\nThis will extract all files to a directory of your choice, configure Start Menu shortcuts, and register an Uninstaller.",
            "YouTube Video Uploader Setup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            // 2. Select Directory
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YouTubeVideoUploader");
            string installPath = defaultPath;

            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the installation directory for YouTube Video Uploader:";
                folderDialog.SelectedPath = defaultPath;
                folderDialog.ShowNewFolderButton = true;
                
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    installPath = folderDialog.SelectedPath;
                }
                else
                {
                    return; // User cancelled
                }
            }

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            // 3. Extract embedded payload
            string zipPath = Path.Combine(Path.GetTempPath(), "app_payload.zip");
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "YouTubeVideoUploader.Installer.app.zip";

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}"))
            using (FileStream fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
            {
                resourceStream.CopyTo(fileStream);
            }

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    string destinationPath = Path.GetFullPath(Path.Combine(installPath, entry.FullName));
                    string? dir = Path.GetDirectoryName(destinationPath);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            // 4. Copy self to installation directory as Uninstall.exe
            string uninstallExePath = Path.Combine(installPath, "Uninstall.exe");
            File.Copy(currentExePath, uninstallExePath, overwrite: true);

            // 5. Create Start Menu shortcuts and folder
            string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "YouTube Video Uploader");
            if (!Directory.Exists(startMenuPath))
            {
                Directory.CreateDirectory(startMenuPath);
            }

            // Create App launch shortcut in Start Menu
            CreateShortcutFile(
                Path.Combine(startMenuPath, "YouTube Video Uploader.lnk"),
                Path.Combine(installPath, "YouTubeVideoUploader.UI.exe"),
                installPath,
                "YouTube Video Uploader Batch Renamer & Publisher");

            // Create Uninstall shortcut in Start Menu
            CreateShortcutFile(
                Path.Combine(startMenuPath, "Uninstall.lnk"),
                uninstallExePath,
                installPath,
                "Uninstall YouTube Video Uploader",
                "/uninstall");

            // 6. Ask about Desktop Shortcut
            DialogResult shortcutResult = MessageBox.Show(
                "Installation completed successfully!\n\nWould you like to create a desktop shortcut?",
                "Create Shortcut",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (shortcutResult == DialogResult.Yes)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                CreateShortcutFile(
                    Path.Combine(desktopPath, "YouTube Video Uploader.lnk"),
                    Path.Combine(installPath, "YouTubeVideoUploader.UI.exe"),
                    installPath,
                    "YouTube Video Uploader Batch Renamer & Publisher");
            }

            MessageBox.Show(
                $"YouTube Video Uploader has been installed successfully!\n\nInstallation Path: {installPath}",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during installation:\n\n{ex.Message}",
                "Installation Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    static void RunUninstall(string currentExePath)
    {
        // 1. Confirm Uninstall
        DialogResult result = MessageBox.Show(
            "Are you sure you want to uninstall YouTube Video Uploader and delete all of its components?",
            "YouTube Video Uploader Uninstall",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            string installPath = Path.GetDirectoryName(currentExePath) 
                ?? throw new InvalidOperationException("Could not determine installation path.");

            // 2. Delete Shortcuts
            // Desktop Shortcut
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string desktopShortcut = Path.Combine(desktopPath, "YouTube Video Uploader.lnk");
            if (File.Exists(desktopShortcut))
            {
                File.Delete(desktopShortcut);
            }

            // Start Menu Folder and Shortcuts
            string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "YouTube Video Uploader");
            if (Directory.Exists(startMenuPath))
            {
                Directory.Delete(startMenuPath, true);
            }

            // 3. Delete App Files (except the currently running uninstaller EXE)
            foreach (string file in Directory.GetFiles(installPath))
            {
                if (!string.Equals(file, currentExePath, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(file); } catch {}
                }
            }
            foreach (string subDir in Directory.GetDirectories(installPath))
            {
                try { Directory.Delete(subDir, true); } catch {}
            }

            // 4. Delay-delete the uninstaller executable and the install directory itself
            // We use cmd.exe to wait 1 second (so this process exits) then delete the folder
            string cmd = $"/c timeout /t 1 && rmdir /s /q \"{installPath}\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmd,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            MessageBox.Show(
                "YouTube Video Uploader has been successfully uninstalled from your computer.",
                "Uninstall Completed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during uninstallation:\n\n{ex.Message}",
                "Uninstall Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    static void CreateShortcutFile(string shortcutPath, string targetExePath, string installPath, string description, string arguments = "")
    {
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType != null)
        {
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell != null)
            {
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetExePath;
                shortcut.WorkingDirectory = installPath;
                shortcut.Description = description;
                if (!string.IsNullOrEmpty(arguments))
                {
                    shortcut.Arguments = arguments;
                }
                shortcut.Save();
            }
        }
    }
}
