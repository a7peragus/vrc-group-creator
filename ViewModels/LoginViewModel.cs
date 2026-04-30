using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcGroupCreator.Models;
using VrcGroupCreator.Services;

namespace VrcGroupCreator.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly VRChatApiService _apiService;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _twoFactorCode = string.Empty;
    [ObservableProperty] private bool _requires2FA;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isBusy;

    public LoginResult? Result { get; private set; }
    public Action? RequestClose { get; set; }

    public LoginViewModel(VRChatApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsBusy = true;
        StatusMessage = "Logging in...";
        System.Diagnostics.Debug.WriteLine($"[LOGIN] Attempting login for {Username}");
        var result = await _apiService.LoginAsync(Username, Password);
        System.Diagnostics.Debug.WriteLine($"[LOGIN] Result: {result.Success}, 2FA: {result.Requires2FA}, Msg: {result.Message}");
        IsBusy = false;

        if (result.Success)
        {
            // Attach plaintext credentials so SaveAccount can encrypt + persist them
            result.Username = Username;
            result.Password = Password;
            Result = result;
            System.Windows.Application.Current.Dispatcher.Invoke(() => RequestClose?.Invoke());
        }
        else if (result.Requires2FA)
        {
            Requires2FA = true;
            StatusMessage = "2FA Required";
        }
        else
        {
            StatusMessage = $"Login failed: {result.Message}";
        }
    }

    [RelayCommand]
    private async Task Verify2FAAsync()
    {
        IsBusy = true;
        StatusMessage = "Verifying 2FA...";
        System.Diagnostics.Debug.WriteLine($"[2FA] Verifying code for {Username}");
        var result = await _apiService.Verify2FAAsync(TwoFactorCode);
        System.Diagnostics.Debug.WriteLine($"[2FA] Result: {result.Success}, Msg: {result.Message}");
        IsBusy = false;

        if (result.Success)
        {
            result.Username = Username;
            result.Password = Password;
            Result = result;
            System.Windows.Application.Current.Dispatcher.Invoke(() => RequestClose?.Invoke());
        }
        else
        {
            StatusMessage = $"2FA Verification failed: {result.Message}";
        }
    }
}
