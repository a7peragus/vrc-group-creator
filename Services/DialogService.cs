using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MaterialDesignThemes.Wpf;

namespace VrcGroupCreator.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ConfirmDeleteGroupAsync(string groupName, string discriminator)
    {
        var content = new StackPanel { Margin = new Thickness(24), MinWidth = 300 };
        content.Children.Add(new TextBlock { 
            Text = "Confirm Deletion", 
            FontWeight = FontWeight.FromOpenTypeWeight(700),
            FontSize = 18,
            Margin = new Thickness(0,0,0,16)
        });
        
        var message = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,24) };
        message.Inlines.Add(new Run("Are you sure you want to delete group "));
        message.Inlines.Add(new Run(groupName) { FontWeight = FontWeights.SemiBold });
        message.Inlines.Add(new Run(" with discriminator "));
        message.Inlines.Add(new Run(discriminator) { FontWeight = FontWeights.Bold, FontSize = 26 });
        message.Inlines.Add(new Run("?"));
        content.Children.Add(message);
        
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancelBtn = new Button { 
            Content = "CANCEL", 
            Margin = new Thickness(0,0,8,0),
            Style = (Style)Application.Current.FindResource("MaterialDesignFlatButton"),
            Command = DialogHost.CloseDialogCommand,
            CommandParameter = false
        };
        var deleteBtn = new Button { 
            Content = "DELETE", 
            Style = (Style)Application.Current.FindResource("MaterialDesignFlatButton"),
            Foreground = System.Windows.Media.Brushes.Red,
            Command = DialogHost.CloseDialogCommand,
            CommandParameter = true
        };
        actions.Children.Add(cancelBtn);
        actions.Children.Add(deleteBtn);
        content.Children.Add(actions);

        var result = await DialogHost.Show(content, "RootDialog");
        return result is true;
    }

    public async Task<bool> ConfirmUnlockProtectionAsync(string groupName, string fullShortCode)
    {
        var content = new StackPanel { Margin = new Thickness(24), MinWidth = 320 };

        content.Children.Add(new TextBlock
        {
            Text = "⚠ Unlock Deletion Protection?",
            FontWeight = FontWeights.Bold,
            FontSize = 17,
            Margin = new Thickness(0, 0, 0, 12)
        });

        content.Children.Add(new TextBlock
        {
            Text = $"You are about to remove deletion protection from:\n\"{groupName}\" ({fullShortCode})\n\nThis will allow the group to be deleted. Are you sure?",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 24),
            Opacity = 0.87
        });

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var cancelBtn = new Button
        {
            Content = "KEEP PROTECTED",
            Margin = new Thickness(0, 0, 8, 0),
            Style = (Style)Application.Current.FindResource("MaterialDesignFlatButton"),
            Command = DialogHost.CloseDialogCommand,
            CommandParameter = false
        };

        var unlockBtn = new Button
        {
            Content = "UNLOCK",
            Style = (Style)Application.Current.FindResource("MaterialDesignFlatButton"),
            Foreground = System.Windows.Media.Brushes.OrangeRed,
            Command = DialogHost.CloseDialogCommand,
            CommandParameter = true
        };

        actions.Children.Add(cancelBtn);
        actions.Children.Add(unlockBtn);
        content.Children.Add(actions);

        var result = await DialogHost.Show(content, "RootDialog");
        return result is true;
    }

    public async Task<bool> ConfirmBulkDeleteAsync(int count)
    {
        var content = new StackPanel { Margin = new Thickness(24), MinWidth = 300 };
        content.Children.Add(new TextBlock
        {
            Text = "Confirm Bulk Deletion",
            FontWeight = FontWeight.FromOpenTypeWeight(700),
            FontSize = 18,
            Margin = new Thickness(0, 0, 0, 12)
        });
        content.Children.Add(new TextBlock
        {
            Text = $"Delete {count} selected group{(count == 1 ? "" : "s")}? This cannot be undone.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 24),
            Opacity = 0.87
        });

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(new Button
        {
            Content = "CANCEL",
            Margin = new Thickness(0, 0, 8, 0),
            Style = (Style)Application.Current.FindResource("MaterialDesignFlatButton"),
            Command = DialogHost.CloseDialogCommand,
            CommandParameter = false
        });
        actions.Children.Add(new Button
        {
            Content = $"DELETE {count}",
            Style = (Style)Application.Current.FindResource("MaterialDesignFlatButton"),
            Foreground = System.Windows.Media.Brushes.Red,
            Command = DialogHost.CloseDialogCommand,
            CommandParameter = true
        });
        content.Children.Add(actions);

        var result = await DialogHost.Show(content, "RootDialog");
        return result is true;
    }
}
