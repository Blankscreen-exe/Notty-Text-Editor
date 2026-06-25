using System.Windows;
using System.Windows.Input;

namespace Notty.App.Views;

public partial class InputDialog : Window
{
    private InputDialog(string title, string prompt, string initialValue)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        Input.Text = initialValue;
        Loaded += (_, _) =>
        {
            Input.Focus();
            Input.SelectAll();
        };
    }

    public string Value => Input.Text.Trim();

    /// <summary>Shows a modal prompt. Returns the entered text, or null if cancelled/empty.</summary>
    public static string? Prompt(Window owner, string title, string prompt, string initialValue = "")
    {
        var dialog = new InputDialog(title, prompt, initialValue) { Owner = owner };
        return dialog.ShowDialog() == true && dialog.Value.Length > 0 ? dialog.Value : null;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            DialogResult = true;
    }
}
