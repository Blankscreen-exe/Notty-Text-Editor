using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Notty.App.Editor;
using Notty.App.Services;
using Notty.App.ViewModels;

namespace Notty.App;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;
    private readonly MarkdownColorizer _markdownColorizer;
    private readonly AlertBackgroundRenderer _alertRenderer = new();
    private readonly CodeBlockBackgroundRenderer _codeRenderer = new();
    private CompletionWindow? _completionWindow;
    private readonly DialogService _dialogs = new();

    // Guards the two-way bridge between AvalonEdit and EditorViewModel.Text
    // so a programmatic load does not get echoed back as a user edit.
    private bool _syncingFromViewModel;

    public MainWindow()
    {
        InitializeComponent();
        _markdownColorizer = new MarkdownColorizer(Editor);
        DataContextChanged += OnDataContextChanged;
        Editor.TextChanged += OnEditorTextChanged;
        Editor.TextArea.TextEntered += OnEditorTextEntered;
        Editor.PreviewKeyDown += OnEditorPreviewKeyDown;
    }

    // Smart list editing for markdown: Enter continues/exits a list, Tab/Shift+Tab indent it.
    // Skipped while the slash popup is open so Enter/Tab there commit the selected command.
    private void OnEditorPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_completionWindow is not null || _vm is null || !IsMarkdownFile(_vm.Editor.CurrentFilePath))
            return;

        var mods = e.KeyboardDevice.Modifiers;

        if (e.Key == System.Windows.Input.Key.Enter && mods == System.Windows.Input.ModifierKeys.None)
        {
            if (ListContinuation.HandleEnter(Editor))
                e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Tab &&
                 (mods == System.Windows.Input.ModifierKeys.None || mods == System.Windows.Input.ModifierKeys.Shift))
        {
            if (ListContinuation.HandleTab(Editor, outdent: mods == System.Windows.Input.ModifierKeys.Shift))
                e.Handled = true;
        }
    }

    // Pop the "/" command menu when a slash is typed at the start of a token.
    private void OnEditorTextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (_vm is null || _completionWindow is not null || e.Text != "/")
            return;

        var path = _vm.Editor.CurrentFilePath;
        if (path is null)
            return;

        // Only trigger at a line start or after whitespace, so slashes in paths/URLs are left alone.
        var caret = Editor.CaretOffset;
        var slash = caret - 1;
        if (slash > 0 && !char.IsWhiteSpace(Editor.Document.GetCharAt(slash - 1)))
            return;

        // Don't pop inside a fenced code block — slashes are normal code there.
        if (IsMarkdownFile(path) &&
            MarkdownFences.IsInCodeBlock(Editor.Document, Editor.Document.GetLineByOffset(caret)))
            return;

        ShowSlashCompletion(path);
    }

    private void ShowSlashCompletion(string path)
    {
        var commands = SlashCommands.For(path);
        if (commands.Count == 0)
            return;

        var prompts = new SlashPrompts(_dialogs, path);

        _completionWindow = new CompletionWindow(Editor.TextArea);
        foreach (var command in commands)
            _completionWindow.CompletionList.CompletionData.Add(new SlashCommandCompletionData(command, prompts));

        _completionWindow.Closed += (_, _) => _completionWindow = null;
        _completionWindow.Show();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.Editor.PropertyChanged -= OnEditorViewModelChanged;
            _vm.PropertyChanged -= OnMainViewModelChanged;
        }

        _vm = DataContext as MainViewModel;

        if (_vm is not null)
        {
            _vm.Editor.PropertyChanged += OnEditorViewModelChanged;
            _vm.PropertyChanged += OnMainViewModelChanged;
            ApplyEditorOptions();
            UpdateMarkdownPreview();
        }
    }

    // Tab width and current-line highlight live on TextEditor.Options (not bindable DPs),
    // so apply them in code whenever the view model's values change.
    private void OnMainViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.EditorTabWidth):
            case nameof(MainViewModel.EditorHighlightCurrentLine):
                ApplyEditorOptions();
                break;
            case nameof(MainViewModel.MarkdownPreviewEnabled):
            case nameof(MainViewModel.EditorFontSize): // heading sizes scale off the base size
                UpdateMarkdownPreview();
                break;
        }
    }

    // Adds or removes the inline markdown styling depending on the toggle and whether
    // the open document is a .md file.
    private void UpdateMarkdownPreview()
    {
        var active = _vm?.MarkdownPreviewEnabled == true && IsMarkdownFile(_vm.Editor.CurrentFilePath);

        var transformers = Editor.TextArea.TextView.LineTransformers;
        if (active && !transformers.Contains(_markdownColorizer))
            transformers.Add(_markdownColorizer);
        else if (!active && transformers.Contains(_markdownColorizer))
            transformers.Remove(_markdownColorizer);

        var renderers = Editor.TextArea.TextView.BackgroundRenderers;
        if (active && !renderers.Contains(_codeRenderer))
            renderers.Add(_codeRenderer);
        else if (!active && renderers.Contains(_codeRenderer))
            renderers.Remove(_codeRenderer);

        if (active && !renderers.Contains(_alertRenderer))
            renderers.Add(_alertRenderer);
        else if (!active && renderers.Contains(_alertRenderer))
            renderers.Remove(_alertRenderer);

        Editor.TextArea.TextView.Redraw();
    }

    private static bool IsMarkdownFile(string? path) =>
        path is not null && Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase);

    private void ApplyEditorOptions()
    {
        if (_vm is null)
            return;

        Editor.Options.IndentationSize = _vm.EditorTabWidth;
        Editor.Options.HighlightCurrentLine = _vm.EditorHighlightCurrentLine;
    }

    private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_vm is not null && e.NewValue is NodeViewModel node)
            _vm.SelectedNode = node;
    }

    // WPF doesn't select a tree item on right-click, so the context menu would target the
    // wrong node. Select the item under the cursor before its menu opens.
    private void FolderTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        for (var source = e.OriginalSource as DependencyObject; source is not null;
             source = VisualTreeHelper.GetParent(source))
        {
            if (source is TreeViewItem item)
            {
                item.IsSelected = true;
                break;
            }
        }
    }

    private void OnEditorViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When a different file is loaded, push its content into the editor control.
        if (e.PropertyName == nameof(EditorViewModel.CurrentFilePath) && _vm is not null)
        {
            _syncingFromViewModel = true;
            try
            {
                Editor.Text = _vm.Editor.Text;
            }
            finally
            {
                _syncingFromViewModel = false;
            }

            UpdateMarkdownPreview(); // styling depends on whether the new file is markdown
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_syncingFromViewModel || _vm is null)
            return;

        _vm.Editor.Text = Editor.Text;

        // Alerts, fenced code and tables span multiple lines, so an edit on one line can change how
        // its neighbours render. Repaint the visible lines to keep those blocks in sync.
        if (_vm.MarkdownPreviewEnabled && IsMarkdownFile(_vm.Editor.CurrentFilePath))
            Editor.TextArea.TextView.Redraw();
    }
}
