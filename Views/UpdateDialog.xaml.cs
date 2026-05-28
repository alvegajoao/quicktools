using System.Windows;
using System.Windows.Input;

namespace QuickTools.Views;

public partial class UpdateDialog : Window
{
    public UpdateDialog(
        string title,
        string message,
        string primaryButtonText,
        string? secondaryButtonText = null,
        bool isError = false)
    {
        InitializeComponent();

        Title = title;
        DialogTitleText.Text = title;
        DialogMessageText.Text = message;
        PrimaryButton.Content = primaryButtonText;
        SecondaryButton.Content = secondaryButtonText ?? "";
        SecondaryButton.Visibility = string.IsNullOrWhiteSpace(secondaryButtonText)
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (isError)
        {
            IconBadge.Background = (System.Windows.Media.Brush)FindResource("DangerBrush");
            IconGlyph.Text = "\uEA39";
        }
        else
        {
            IconGlyph.Text = "\uE777";
        }

        Loaded += (_, _) => PrimaryButton.Focus();
    }

    public static bool ShowConfirmation(Window? owner, string title, string message, string primaryButtonText, string secondaryButtonText)
    {
        var dialog = new UpdateDialog(title, message, primaryButtonText, secondaryButtonText);
        Configure(dialog, owner);
        return dialog.ShowDialog() == true;
    }

    public static void ShowError(Window? owner, string title, string message)
    {
        var dialog = new UpdateDialog(title, message, "OK", isError: true);
        Configure(dialog, owner);
        dialog.ShowDialog();
    }

    private static void Configure(UpdateDialog dialog, Window? owner)
    {
        if (owner is not null && owner.IsLoaded)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return;
        }

        dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
