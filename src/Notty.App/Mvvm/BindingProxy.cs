using System.Windows;

namespace Notty.App.Mvvm;

/// <summary>
/// Freezable proxy that carries a DataContext into places the visual tree can't reach —
/// notably ContextMenu items, which live outside the tree. Hold the MainViewModel here so
/// menu items can bind to its commands via Source={StaticResource …}.
/// </summary>
public sealed class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
