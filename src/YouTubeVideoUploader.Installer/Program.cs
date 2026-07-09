using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

namespace YouTubeVideoUploader.Installer;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // 1. Welcome Prompts
        DialogResult result = MessageBox.Show(
            "Do you want to install YouTube Video Uploader on your computer?\n\nThis will install the application to your local AppData folder and configure shortcuts.",
            "YouTube Video Uploader Setup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            // 2. Define install target path (local AppData - doesn't require admin privileges)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string installPath = Path.Combine(appData, "YouTubeVideoUploader");

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            // 3. Extract embedded resource
            string zipPath = Path.Combine(Path.GetTempPath(), "app_payload.zip");
            
            // Extract the embedded zip to temp file
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "YouTubeVideoUploader.Installer.app.zip";
            
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName) 
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}"))
            using (FileStream fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
            {
                resourceStream.CopyTo(fileStream);
            }

            // Unzip to install path, extracting files and overwriting existing ones
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Skip directory entries themselves
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    
                    string destinationPath = Path.GetFullPath(Path.Combine(installPath, entry.FullName));
                    
                    // Create subdirectory if needed
                    string? dir = Path.GetDirectoryName(destinationPath);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            // Delete temp zip file
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            // 4. Ask about Desktop Shortcut
            DialogResult shortcutResult = MessageBox.Show(
                "Installation completed successfully!\n\nWould you like to create a desktop shortcut?",
                "Create Shortcut",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (shortcutResult == DialogResult.Yes)
            {
                CreateShortcut(installPath);
            }

            MessageBox.Show(
                "YouTube Video Uploader has been installed successfully!\n\nYou can launch it using your desktop shortcut or directly from your local folder.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during installation:\n\n{ex.Message}\n{ex.StackTrace}",
                "Installation Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    static void CreateShortcut(string installPath)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string shortcutPath = Path.Combine(desktopPath, "YouTube Video Uploader.lnk");
        string targetExePath = Path.Combine(installPath, "YouTubeVideoUploader.UI.exe");

        // Create shortcut using Windows Script Host COM object dynamically
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType != null)
        {
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell != null)
            {
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetExePath;
                shortcut.WorkingDirectory = installPath;
                shortcut.Description = "YouTube Video Uploader Batch Renamer & Publisher";
                shortcut.Save();
            }
        }
    }
}
