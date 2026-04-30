using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcGroupCreator.Models;
using VrcGroupCreator.Services;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;

namespace VrcGroupCreator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly VRChatApiService _apiService;
    private readonly AccountService _accountService;
    private readonly SettingsService _settingsService;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

    [ObservableProperty] private AccountInfo? _selectedAccount;
    public ObservableCollection<AccountInfo> Accounts { get; } = new();
    public ObservableCollection<GroupItemViewModel> UserGroups { get; } = new();

    [ObservableProperty] private string _namePrefix = string.Empty;
    [ObservableProperty] private string _shortCodePrefix = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int _groupCount = 1;
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private int _progressMax;

    public ObservableCollection<string> Logs { get; } = new();

    public MainViewModel(
        VRChatApiService apiService,
        AccountService accountService,
        SettingsService settingsService,
        IDialogService dialogService)
    {
        _apiService = apiService;
        _accountService = accountService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        
        // Load settings
        _namePrefix = _settingsService.Settings.NamePrefix;
        _shortCodePrefix = _settingsService.Settings.ShortCodePrefix;
        _description = _settingsService.Settings.Description;
        _groupCount = _settingsService.Settings.GroupCount;
        
        RefreshAccounts();
    }

    private void RefreshAccounts()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Accounts.Clear();
            foreach (var acc in _accountService.Accounts) Accounts.Add(acc);
        });
    }

    partial void OnNamePrefixChanged(string value) { _settingsService.Settings.NamePrefix = value; _settingsService.Save(); }
    partial void OnShortCodePrefixChanged(string value) { _settingsService.Settings.ShortCodePrefix = value; _settingsService.Save(); }
    partial void OnDescriptionChanged(string value) { _settingsService.Settings.Description = value; _settingsService.Save(); }
    partial void OnGroupCountChanged(int value) { _settingsService.Settings.GroupCount = value; _settingsService.Save(); }

    partial void OnSelectedAccountChanged(AccountInfo? value)
    {
        RemoveAccountCommand.NotifyCanExecuteChanged();
        if (value != null)
        {
            SwitchToAccount(value);
        }
        else
        {
            IsLoggedIn = false;
            StatusMessage = "Ready";
            UserGroups.Clear();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemoveAccount))]
    private void RemoveAccount()
    {
        if (SelectedAccount != null)
        {
            var accName = SelectedAccount.DisplayName;
            _accountService.RemoveAccount(SelectedAccount.UserId);
            Accounts.Remove(SelectedAccount);
            SelectedAccount = null;
            AddLog($"Removed account: {accName}");
        }
    }

    private bool CanRemoveAccount() => SelectedAccount != null;

    private async void SwitchToAccount(AccountInfo account)
    {
        IsBusy = true;
        StatusMessage = $"Switching to {account.DisplayName}...";

        // Decrypt stored credentials — used for silent re-login if cookie has expired
        var username = AccountService.Decrypt(account.EncryptedUsername);
        var password = AccountService.Decrypt(account.EncryptedPassword);

        var success = await _apiService.SetSessionAsync(
            account.AuthCookie, account.TwoFactorCookie,
            username, password, account, _accountService);

        if (success)
        {
            IsLoggedIn = true;
            StatusMessage = $"Logged in as {account.DisplayName}";
            AddLog($"Switched to account: {account.DisplayName}");
            _ = LoadGroupsAsync();
        }
        else
        {
            IsLoggedIn = false;
            StatusMessage = "Session expired. Please re-add the account.";
            AddLog($"Session expired for {account.DisplayName} — please re-add the account.");
        }

        IsBusy = false;
    }

    [RelayCommand]
    private void ShowAddAccount()
    {
        var loginWin = App.Services.GetRequiredService<VrcGroupCreator.Views.LoginWindow>();
        loginWin.Owner = App.Current.MainWindow;

        bool? result = loginWin.ShowDialog();
        var loginVm = loginWin.DataContext as LoginViewModel;
        if (result == true && loginVm?.Result != null)
        {
            AddLog("Login successful, saving account...");
            SaveAccount(loginVm.Result);
            _ = LoadGroupsAsync();
        }
        else
        {
            AddLog("Login cancelled or failed.");
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        var settingsWin = App.Services.GetRequiredService<VrcGroupCreator.Views.SettingsWindow>();
        settingsWin.Owner = App.Current.MainWindow;
        settingsWin.ShowDialog();
    }

    [RelayCommand]
    private async Task LoadGroupsAsync(bool silent = false)
    {
        if (!IsLoggedIn) return;
        
        IsBusy = true;
        if (!silent) StatusMessage = "Loading groups...";
        
        // Pass forceRefresh to bypass cache when loading groups explicitly or silently
        var groups = await _apiService.GetMyGroupsAsync(forceRefresh: true);
        
        App.Current.Dispatcher.Invoke(() =>
        {
            UserGroups.Clear();
            foreach (var g in groups)
            {
                if (g.OwnerId == _apiService.CurrentUserId)
                {
                    bool isProtected = _settingsService.Settings.ProtectedGroupIds.Contains(g.Id);
                    var item = new GroupItemViewModel(g, isProtected, _dialogService);
                    item.ProtectionChanged += OnGroupProtectionChanged;
                    UserGroups.Add(item);
                }
            }
        });
        
        IsBusy = false;
        if (!silent) StatusMessage = $"Loaded {UserGroups.Count} owned groups";
    }

    private void OnGroupProtectionChanged(GroupItemViewModel item)
    {
        if (item.IsProtected)
            _settingsService.Settings.ProtectedGroupIds.Add(item.Group.Id);
        else
            _settingsService.Settings.ProtectedGroupIds.Remove(item.Group.Id);
        _settingsService.Save();
    }

    [RelayCommand]
    private async Task DeleteSelectedGroupsAsync()
    {
        var selected = UserGroups.Where(g => g.IsSelected && !g.IsProtected).ToList();
        if (selected.Count == 0) return;

        if (!await _dialogService.ConfirmBulkDeleteAsync(selected.Count)) return;

        IsBusy = true;
        int deleted = 0;
        foreach (var item in selected)
        {
            StatusMessage = $"Deleting {item.Group.Name}...";
            var success = await _apiService.DeleteGroupAsync(item.Group.Id);
            if (success)
            {
                UserGroups.Remove(item);
                deleted++;
                AddLog($"Deleted group: {item.Group.Name} (#{item.Group.Discriminator})");
            }
            else
            {
                AddLog($"Failed to delete: {item.Group.Name}");
            }
            await Task.Delay(500); // small delay between bulk deletes
        }

        IsBusy = false;
        StatusMessage = $"Deleted {deleted} of {selected.Count} selected groups.";
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(GroupItemViewModel? item)
    {
        if (item == null) return;
        if (item.IsProtected) return; // extra guard
        
        var group = item.Group;

        if (!await _dialogService.ConfirmDeleteGroupAsync(group.Name, group.Discriminator)) return;
        
        IsBusy = true;
        var success = await _apiService.DeleteGroupAsync(group.Id);
        IsBusy = false;
        
        if (success)
        {
            AddLog($"Deleted group: {group.Name} (#{group.Discriminator})");
            UserGroups.Remove(item);
        }
        else
        {
            StatusMessage = $"Failed to delete {group.Name}";
            AddLog($"Failed to delete group: {group.Name}");
            Notifications.Enqueue($"Failed to delete {group.Name}");
        }
    }

    [RelayCommand]
    private async Task CreateGroupsAsync()
    {
        if (!IsLoggedIn) return;

        IsBusy = true;
        ProgressMax = GroupCount;
        ProgressValue = 0;
        AddLog($"Starting bulk creation of {GroupCount} groups...");

        for (int i = 1; i <= GroupCount; i++)
        {
            StatusMessage = $"Creating group {i} of {GroupCount}...";
            
            var name = NamePrefix;
            var shortCode = ShortCodePrefix;
            
            // ShortCode must be 3-6 chars
            if (shortCode.Length > 6) shortCode = shortCode.Substring(0, 6);
            while (shortCode.Length < 3) shortCode += "0";

            var request = new GroupCreateRequest
            {
                Name = name,
                ShortCode = shortCode,
                Description = Description,
                RoleTemplate = "default",
                JoinState = "open",
                Privacy = "default"
            };

            var result = await _apiService.CreateGroupAsync(request);

            if (string.IsNullOrEmpty(result.Error))
            {
                AddLog($"✓ Created: {result.Name} ({result.ShortCode}) - ID: {result.Id}");
            }
            else
            {
                AddLog($"✗ Failed {name}: {result.Error}");
            }

            ProgressValue = i;
            
            // Rate limit delay to be safe
            await Task.Delay(1000);
        }

        IsBusy = false;
        StatusMessage = "Done";
        AddLog("Bulk creation process completed.");
        await LoadGroupsAsync();
    }

    private void SaveAccount(LoginResult result)
    {
        if (result.UserId == null || result.DisplayName == null || result.AuthCookie == null)
        {
            AddLog($"Failed to save account: Missing data (UID: {result.UserId != null}, Name: {result.DisplayName != null}, Cookie: {result.AuthCookie != null})");
            return;
        }

        var acc = new AccountInfo
        {
            UserId = result.UserId,
            DisplayName = result.DisplayName,
            AuthCookie = result.AuthCookie,
            TwoFactorCookie = result.TwoFactorCookie,
            // Encrypt with DPAPI so the app can silently re-authenticate after IP change or cookie expiry
            EncryptedUsername = result.Username != null ? AccountService.Encrypt(result.Username) : null,
            EncryptedPassword = result.Password != null ? AccountService.Encrypt(result.Password) : null,
        };

        _accountService.SaveAccount(acc);
        AddLog($"Account {acc.DisplayName} saved to local storage.");
        RefreshAccounts();
        SelectedAccount = Accounts.FirstOrDefault(a => a.UserId == acc.UserId);
    }

    private void AddLog(string message)
    {
        // VRChat returns this exact phrase when the IP-based rate limit is hit
        if (message.Contains("you are doing it too quickly", StringComparison.OrdinalIgnoreCase))
            message += " ⚠ This is an IP-based rate limit — not account-specific. To maximize api abuse you should use a vpn.";

        App.Current.Dispatcher.BeginInvoke(() => 
        {
            Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        });
    }
}
