using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VrcGroupCreator.Services;
using VrcGroupCreator.ViewModels;

namespace VrcGroupCreator;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var settingsService = Services.GetRequiredService<SettingsService>();
        
        LoggingService.Initialize(settingsService.Settings.EnableFileLogging);
        LoggingService.Info("APP", "Application starting up");

        var themeService = Services.GetRequiredService<ThemeService>();
        themeService.ApplyTheme(settingsService.Settings.Colors);

        if (settingsService.Settings.EnableDebugConsole)
            LoggingService.SetConsoleEnabled(true);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<AccountService>();
        services.AddTransient<VRChatApiService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddTransient<VrcGroupCreator.Views.LoginWindow>();
        services.AddTransient<VrcGroupCreator.Views.SettingsWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LoggingService.Shutdown();
        base.OnExit(e);
    }
}
