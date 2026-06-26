using System.Reflection;
using System.Windows;

namespace Notty.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "Version 1.0" : $"Version {version.Major}.{version.Minor}.{version.Build}";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
