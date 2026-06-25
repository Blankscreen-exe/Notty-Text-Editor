using Notty.App.Mvvm;

namespace Notty.App.ViewModels;

/// <summary>A theme entry in the Settings ▸ Theme submenu, with a checkmark for the active one.</summary>
public sealed class ThemeMenuItem : ObservableObject
{
    private bool _isSelected;

    public ThemeMenuItem(string name, bool isSelected)
    {
        Name = name;
        _isSelected = isSelected;
    }

    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
