using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcGroupCreator.Models;
using VrcGroupCreator.Services;

namespace VrcGroupCreator.ViewModels;

public partial class GroupItemViewModel : ObservableObject
{
    public VrcGroup Group { get; }

    [ObservableProperty]
    private bool _isProtected;

    [ObservableProperty]
    private bool _isSelected;

    private readonly IDialogService _dialogService;

    // Raised so MainViewModel can persist the change
    public event Action<GroupItemViewModel>? ProtectionChanged;

    public GroupItemViewModel(VrcGroup group, bool isProtected, IDialogService dialogService)
    {
        Group = group;
        _isProtected = isProtected;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task ToggleProtectionAsync()
    {
        if (!IsProtected)
        {
            // Locking — no warning needed, just lock it
            IsProtected = true;
            ProtectionChanged?.Invoke(this);
            return;
        }

        var result = await _dialogService.ConfirmUnlockProtectionAsync(Group.Name, Group.FullShortCode);
        if (result)
        {
            IsProtected = false;
            ProtectionChanged?.Invoke(this);
        }
    }
}
