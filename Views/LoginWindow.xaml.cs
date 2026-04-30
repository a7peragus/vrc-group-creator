using System.Windows;
using VrcGroupCreator.ViewModels;

namespace VrcGroupCreator.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose = () => DialogResult = true;
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
            vm.LoginCommand.Execute(null);
        }
    }
}
