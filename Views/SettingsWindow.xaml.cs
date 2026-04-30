using System.Windows;
using VrcGroupCreator.ViewModels;

namespace VrcGroupCreator.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    // X button — revert live-preview changes then close
    private void CloseAndRevert_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.Revert();
        Close();
    }

    // Save & Close button — changes are already saved, just close
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

