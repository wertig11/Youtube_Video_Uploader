using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using YouTubeVideoUploader.Application.Factories;
using YouTubeVideoUploader.Application.Services;
using YouTubeVideoUploader.Application.Strategies;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Infrastructure.FileSystem;
using YouTubeVideoUploader.Infrastructure.Persistence;
using YouTubeVideoUploader.Infrastructure.YouTube;
using YouTubeVideoUploader.UI.ViewModels;

namespace YouTubeVideoUploader.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Setup logging
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app_log.txt");
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                // 1. Persistence & Infrastructure Setup
                string presetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets.json");
                string uploadLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploaded_log.txt");

                services.AddSingleton<IFileSystemRepository, FileSystemRepository>();
                services.AddSingleton<IUploadLogRepository>(sp => new TxtUploadLogRepository(uploadLogPath));
                services.AddSingleton<IPresetStore>(sp => new JsonPresetStore(presetsPath));
                services.AddSingleton<IAuthenticationService, GoogleAuthService>();
                services.AddSingleton<IYouTubeGateway, YouTubeApiAdapter>();

                // 2. Application Services Setup
                services.AddTransient<IRenameService, RenameService>();
                services.AddTransient<IUploadOrchestrator, UploadOrchestrator>();
                services.AddTransient<TagGenerator>();
                services.AddTransient<ScheduleCalculator>();
                services.AddTransient<IUploadJobFactory, UploadJobFactory>();

                // Strategies
                services.AddTransient<IRenameStrategy, ListBasedRenameStrategy>();
                services.AddTransient<IRenameStrategy, DateBasedRenameStrategy>();

                // 3. UI Layer ViewModels Setup
                services.AddTransient<RenameViewModel>();
                services.AddTransient<UploadViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<MainViewModel>();

                // Main Window
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
