using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YouTubeVideoUploader.Installer;

public class InstallForm : Form
{
    private Label lblStatus;
    private ProgressBar progressBar;
    private string installPath;
    private string currentExePath;

    public InstallForm(string installPath, string currentExePath)
    {
        this.installPath = installPath;
        this.currentExePath = currentExePath;

        // Form settings
        this.Text = "YouTube Video Uploader Setup";
        this.Size = new Size(460, 160);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // Label status
        lblStatus = new Label();
        lblStatus.Text = "Preparing installation...";
        lblStatus.Location = new Point(20, 20);
        lblStatus.Size = new Size(410, 30);
        lblStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        // Progress bar
        progressBar = new ProgressBar();
        progressBar.Location = new Point(20, 55);
        progressBar.Size = new Size(405, 23);
        progressBar.Style = ProgressBarStyle.Continuous;

        this.Controls.Add(lblStatus);
        this.Controls.Add(progressBar);

        // Run installation when form is shown
        this.Shown += async (s, e) => await StartInstallationAsync();
    }

    private async Task StartInstallationAsync()
    {
        try
        {
            // 1. Prepare directory
            lblStatus.Text = "Creating installation directory...";
            await Task.Delay(200);
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            // 2. Extract payload zip
            lblStatus.Text = "Extracting application payload...";
            string zipPath = Path.Combine(Path.GetTempPath(), "app_payload.zip");
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "YouTubeVideoUploader.Installer.app.zip";

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}"))
            using (FileStream fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
            {
                await resourceStream.CopyToAsync(fileStream);
            }

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                int totalFiles = archive.Entries.Count;
                progressBar.Maximum = totalFiles;
                int currentFile = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    string destinationPath = Path.GetFullPath(Path.Combine(installPath, entry.FullName));
                    string? dir = Path.GetDirectoryName(destinationPath);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    lblStatus.Text = $"Extracting: {entry.Name}";
                    
                    // Run extraction on background thread to keep UI alive
                    await Task.Run(() => entry.ExtractToFile(destinationPath, overwrite: true));
                    
                    currentFile++;
                    progressBar.Value = currentFile;
                    await Task.Delay(25); // Slight delay so the user can see it processing
                }
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            // 3. Register Uninstaller
            lblStatus.Text = "Registering uninstaller...";
            await Task.Delay(200);
            string uninstallExePath = Path.Combine(installPath, "Uninstall.exe");
            await Task.Run(() => File.Copy(currentExePath, uninstallExePath, overwrite: true));

            // 4. Create Shortcuts in SpecialFolder.Programs
            lblStatus.Text = "Creating shortcuts...";
            await Task.Delay(200);
            string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "YouTube Video Uploader");
            if (!Directory.Exists(startMenuPath))
            {
                Directory.CreateDirectory(startMenuPath);
            }

            CreateShortcutFile(
                Path.Combine(startMenuPath, "YouTube Video Uploader.lnk"),
                Path.Combine(installPath, "YouTubeVideoUploader.UI.exe"),
                installPath,
                "YouTube Video Uploader Batch Renamer & Publisher");

            CreateShortcutFile(
                Path.Combine(startMenuPath, "Uninstall.lnk"),
                uninstallExePath,
                installPath,
                "Uninstall YouTube Video Uploader",
                "/uninstall");

            // 5. Notify Windows Shell to refresh Start Menu cache
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            // 6. Ask about Desktop Shortcut
            DialogResult shortcutResult = MessageBox.Show(
                "Would you like to create a desktop shortcut?",
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

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during installation:\n\n{ex.Message}",
                "Installation Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            this.DialogResult = DialogResult.Abort;
            this.Close();
        }
    }

    private void CreateShortcutFile(string shortcutPath, string targetExePath, string installPath, string description, string arguments = "")
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

    /// <summary>
    /// Notifies the Windows Shell that an event has occurred that affects its operation,
    /// forcing it to refresh the Start Menu program list cache.
    /// </summary>
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
}

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

        // 3. Launch UI Progress dialog form
        using (var form = new InstallForm(installPath, currentExePath))
        {
            Application.Run(form);
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

            // Start Menu Folder and Shortcuts (SpecialFolder.Programs)
            string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "YouTube Video Uploader");
            if (Directory.Exists(startMenuPath))
            {
                Directory.Delete(startMenuPath, true);
            }

            // 3. Delete App Files (except the currently running uninstaller EXE itself)
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

            // Show confirmation first so we don't hold the lock on the directory when showing MessageBox
            MessageBox.Show(
                "YouTube Video Uploader has been successfully uninstalled from your computer.",
                "Uninstall Completed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // 4. Delay-delete the uninstaller executable and the install directory itself
            // We use cmd.exe with choice to delay execution and delete the folder, then immediately exit.
            string cmd = $"/c choice /t 2 /d y /n > nul & rmdir /s /q \"{installPath}\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmd,
                CreateNoWindow = true,
                UseShellExecute = false
            });
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
}
