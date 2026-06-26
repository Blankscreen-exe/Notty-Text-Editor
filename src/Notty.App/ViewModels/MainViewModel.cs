using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Notty.App.Mvvm;
using Notty.App.Services;
using Notty.Core.Models;
using Notty.Core.Services;

namespace Notty.App.ViewModels;

/// <summary>
/// Root view model. Switches between the welcome screen and the workspace view,
/// owns the folder tree (depth-capped, re-rootable) and the editor, and persists settings.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    /// <summary>Folder levels shown inline below the current root. Folders deeper than this become "doors".</summary>
    private const int MaxInlineDepth = 3;

    private readonly SettingsService _settingsService;
    private readonly WorkspaceScanner _scanner;
    private readonly WorkspaceService _workspace;
    private readonly DialogService _dialogs;
    private readonly ThemeManager _themeManager;
    private readonly AppSettings _settings;

    private bool _hasWorkspace;
    private string? _workspacePath;
    private string? _currentRootPath;
    private NodeViewModel? _selectedNode;
    private string _statusText = "Ready";

    private string _editorFontFamily = "Consolas";
    private double _editorFontSize = 14;
    private bool _editorWordWrap = true;
    private bool _editorShowLineNumbers = true;
    private int _editorTabWidth = 4;
    private bool _editorHighlightCurrentLine;
    private bool _markdownPreviewEnabled;

    public MainViewModel(
        SettingsService settingsService,
        WorkspaceScanner scanner,
        WorkspaceService workspace,
        NoteService notes,
        DialogService dialogs,
        ThemeManager themeManager)
    {
        _settingsService = settingsService;
        _scanner = scanner;
        _workspace = workspace;
        _dialogs = dialogs;
        _themeManager = themeManager;
        _settings = settingsService.Load();

        Editor = new EditorViewModel(notes, OnDocumentSaved);

        OpenWorkspaceCommand = new RelayCommand(OpenWorkspace);
        CreateWorkspaceCommand = new RelayCommand(CreateWorkspace);
        ChangeWorkspaceCommand = new RelayCommand(OpenWorkspace);
        SaveCommand = new RelayCommand(() => Editor.SaveNow(), () => Editor.HasDocument);
        AboutCommand = new RelayCommand(ShowAbout);
        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
        ApplyThemeCommand = new RelayCommand(o => { if (o is string name) ApplyTheme(name); });
        OpenSettingsCommand = new RelayCommand(OpenSettings);

        BuildThemeMenu();

        // Menu-bar commands target the current selection (or the current root).
        NewMarkdownDocumentCommand = new RelayCommand(() => CreateNote(TargetDirectory(), ".md"), () => HasWorkspace);
        NewTextDocumentCommand = new RelayCommand(() => CreateNote(TargetDirectory(), ".txt"), () => HasWorkspace);
        NewCategoryCommand = new RelayCommand(() => CreateCategory(TargetDirectory()), () => HasWorkspace);

        // Context-menu commands act on the right-clicked node (passed as the command parameter).
        NewMarkdownHereCommand = new RelayCommand(o => CreateNote(DirectoryOf(o), ".md"));
        NewTextHereCommand = new RelayCommand(o => CreateNote(DirectoryOf(o), ".txt"));
        NewCategoryHereCommand = new RelayCommand(o => CreateCategory(DirectoryOf(o)));
        RenameCommand = new RelayCommand(o => { if (o is NodeViewModel n) Rename(n); });
        DeleteCommand = new RelayCommand(o => { if (o is NodeViewModel n) Delete(n); });
        DuplicateCommand = new RelayCommand(
            o => { if (o is NodeViewModel n) Duplicate(n); },
            o => o is NodeViewModel { IsDirectory: false });
        RevealCommand = new RelayCommand(o => { if (o is NodeViewModel n) ShellOperations.RevealInExplorer(n.FullPath); });
        NavigateRootCommand = new RelayCommand(o => { if (o is string path) SetRoot(path); });

        RefreshEditorSettings();

        if (!string.IsNullOrWhiteSpace(_settings.WorkspacePath) && Directory.Exists(_settings.WorkspacePath))
            LoadWorkspace(_settings.WorkspacePath);
    }

    public EditorViewModel Editor { get; }

    public ObservableCollection<NodeViewModel> RootNodes { get; } = new();

    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; } = new();

    public ObservableCollection<ThemeMenuItem> AvailableThemes { get; } = new();

    public RelayCommand OpenWorkspaceCommand { get; }
    public RelayCommand CreateWorkspaceCommand { get; }
    public RelayCommand ChangeWorkspaceCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand AboutCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommand NewMarkdownDocumentCommand { get; }
    public RelayCommand NewTextDocumentCommand { get; }
    public RelayCommand NewCategoryCommand { get; }
    public RelayCommand NewMarkdownHereCommand { get; }
    public RelayCommand NewTextHereCommand { get; }
    public RelayCommand NewCategoryHereCommand { get; }
    public RelayCommand RenameCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand DuplicateCommand { get; }
    public RelayCommand RevealCommand { get; }
    public RelayCommand NavigateRootCommand { get; }
    public RelayCommand ApplyThemeCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }

    public bool HasWorkspace
    {
        get => _hasWorkspace;
        private set => SetProperty(ref _hasWorkspace, value);
    }

    public string? WorkspacePath
    {
        get => _workspacePath;
        private set
        {
            if (SetProperty(ref _workspacePath, value))
                OnPropertyChanged(nameof(WorkspaceName));
        }
    }

    public string WorkspaceName =>
        string.IsNullOrEmpty(_workspacePath) ? string.Empty : new DirectoryInfo(_workspacePath).Name;

    public NodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (!SetProperty(ref _selectedNode, value) || value is null)
                return;

            if (value.IsDoor)
                SetRoot(value.FullPath);          // re-root into folders beyond the depth cap
            else if (value.IsDirectory)
                return;                            // plain folders just stay selected
            else if (!value.IsSupported)
                StatusText = $"{value.Name} — unsupported file type (can't edit)";
            else
                OpenDocument(value.FullPath);
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    // Editor display settings — bound by the AvalonEdit control; updated from settings on apply.
    public string EditorFontFamily
    {
        get => _editorFontFamily;
        private set => SetProperty(ref _editorFontFamily, value);
    }

    public double EditorFontSize
    {
        get => _editorFontSize;
        private set => SetProperty(ref _editorFontSize, value);
    }

    public bool EditorWordWrap
    {
        get => _editorWordWrap;
        private set => SetProperty(ref _editorWordWrap, value);
    }

    public bool EditorShowLineNumbers
    {
        get => _editorShowLineNumbers;
        private set => SetProperty(ref _editorShowLineNumbers, value);
    }

    public int EditorTabWidth
    {
        get => _editorTabWidth;
        private set => SetProperty(ref _editorTabWidth, value);
    }

    public bool EditorHighlightCurrentLine
    {
        get => _editorHighlightCurrentLine;
        private set => SetProperty(ref _editorHighlightCurrentLine, value);
    }

    /// <summary>Inline markdown rendering in the editor (applies to .md files only). Persisted.</summary>
    public bool MarkdownPreviewEnabled
    {
        get => _markdownPreviewEnabled;
        set
        {
            if (SetProperty(ref _markdownPreviewEnabled, value))
            {
                _settings.MarkdownPreview = value;
                _settingsService.Save(_settings);
            }
        }
    }

    // ---- Workspace lifecycle ------------------------------------------------

    private void OpenWorkspace()
    {
        var folder = _dialogs.PickFolder("Open Workspace Folder");
        if (folder is not null)
            LoadWorkspace(folder);
    }

    private void CreateWorkspace()
    {
        var parent = _dialogs.PickFolder("Choose where to create the new workspace");
        if (parent is null)
            return;

        try
        {
            var created = _dialogs.CreateFolder(parent, "Notty Workspace");
            LoadWorkspace(created!);
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not create the workspace folder.\n\n{ex.Message}");
        }
    }

    private void LoadWorkspace(string path)
    {
        try
        {
            WorkspacePath = path;
            _currentRootPath = path;
            RebuildTree();
            RebuildBreadcrumbs();

            HasWorkspace = true;
            StatusText = $"Workspace: {path}";

            _settings.WorkspacePath = path;
            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not open the workspace.\n\n{ex.Message}");
        }
    }

    /// <summary>Re-roots the tree at <paramref name="path"/> (drill into a door, or climb via a breadcrumb).</summary>
    private void SetRoot(string path)
    {
        _currentRootPath = path;
        RebuildTree();
        RebuildBreadcrumbs();

        _selectedNode = null;
        OnPropertyChanged(nameof(SelectedNode));

        StatusText = PathsEqual(path, _workspacePath)
            ? $"Workspace: {path}"
            : $"In {Path.GetFileName(path)}";
    }

    // ---- File / category operations ----------------------------------------

    private void CreateNote(string directory, string extension)
    {
        if (!HasWorkspace)
            return;

        var kind = extension == ".md" ? "Markdown" : "text";
        var name = _dialogs.PromptForName("New Document", $"Name for the new {kind} document:", "Untitled");
        if (name is null)
            return;

        try
        {
            var path = _workspace.CreateNote(directory, name, extension);
            RebuildTree();
            RevealAndSelect(path);
            StatusText = $"Created {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not create the document.\n\n{ex.Message}");
        }
    }

    private void CreateCategory(string directory)
    {
        if (!HasWorkspace)
            return;

        var name = _dialogs.PromptForName("New Category", "Name for the new category (folder):", "New Category");
        if (name is null)
            return;

        try
        {
            var path = _workspace.CreateCategory(directory, name);
            RebuildTree();
            RevealAndSelect(path);
            StatusText = $"Created category {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not create the category.\n\n{ex.Message}");
        }
    }

    private void Rename(NodeViewModel node)
    {
        var isFile = !node.IsDirectory;
        var initial = isFile ? Path.GetFileNameWithoutExtension(node.Name) : node.Name;

        var input = _dialogs.PromptForName("Rename", $"New name for \"{node.Name}\":", initial);
        if (input is null)
            return;

        var newLeaf = isFile ? input + Path.GetExtension(node.Name) : input;

        try
        {
            var openPath = Editor.CurrentFilePath;
            var affectsOpen = openPath is not null &&
                (PathsEqual(openPath, node.FullPath) || IsUnder(openPath, node.FullPath));
            if (affectsOpen)
                Editor.SaveNow();

            var newPath = _workspace.Rename(node.FullPath, newLeaf);

            // If the open document moved with the rename, follow it to its new path.
            if (affectsOpen && openPath is not null)
            {
                var newOpen = PathsEqual(openPath, node.FullPath)
                    ? newPath
                    : Path.Combine(newPath, Path.GetRelativePath(node.FullPath, openPath));

                if (File.Exists(newOpen))
                    Editor.OpenFile(newOpen);
                else
                    Editor.CloseWithoutSaving();
            }

            RebuildTree();
            RevealAndSelect(newPath);
            StatusText = $"Renamed to {Path.GetFileName(newPath)}";
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not rename.\n\n{ex.Message}");
        }
    }

    private void Delete(NodeViewModel node)
    {
        var what = node.IsDirectory ? "category (and everything in it)" : "note";
        if (!_dialogs.Confirm($"Move this {what} to the Recycle Bin?\n\n{node.Name}"))
            return;

        try
        {
            var openPath = Editor.CurrentFilePath;
            if (openPath is not null && (PathsEqual(openPath, node.FullPath) || IsUnder(openPath, node.FullPath)))
                Editor.CloseWithoutSaving();

            ShellOperations.SendToRecycleBin(node.FullPath);
            RebuildTree();
            StatusText = $"Deleted {node.Name}";
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not delete.\n\n{ex.Message}");
        }
    }

    private void Duplicate(NodeViewModel node)
    {
        try
        {
            var copy = _workspace.Duplicate(node.FullPath);
            RebuildTree();
            RevealAndSelect(copy);
            StatusText = $"Duplicated to {Path.GetFileName(copy)}";
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not duplicate.\n\n{ex.Message}");
        }
    }

    // ---- Theme & settings ---------------------------------------------------

    private void BuildThemeMenu()
    {
        AvailableThemes.Clear();
        foreach (var palette in _themeManager.Available)
            AvailableThemes.Add(new ThemeMenuItem(
                palette.Name,
                string.Equals(palette.Name, _settings.Theme, StringComparison.OrdinalIgnoreCase)));
    }

    private void ApplyTheme(string name)
    {
        var applied = _themeManager.ApplyByName(name);
        _settings.Theme = applied.Name;
        _settingsService.Save(_settings);

        foreach (var item in AvailableThemes)
            item.IsSelected = string.Equals(item.Name, applied.Name, StringComparison.OrdinalIgnoreCase);
    }

    private void OpenSettings()
    {
        var settingsVm = new SettingsViewModel(
            _settings,
            AvailableThemes.Select(t => t.Name),
            applyTheme: ApplyTheme,
            applyEditor: ApplyEditorSettings,
            applyFiles: ApplyFileSettings);

        _dialogs.ShowSettings(settingsVm);
    }

    /// <summary>Pushes the editor settings from <see cref="_settings"/> into the bound properties.</summary>
    private void RefreshEditorSettings()
    {
        EditorFontFamily = _settings.EditorFontFamily;
        EditorFontSize = _settings.EditorFontSize;
        EditorWordWrap = _settings.EditorWordWrap;
        EditorShowLineNumbers = _settings.EditorShowLineNumbers;
        EditorTabWidth = _settings.EditorTabWidth;
        EditorHighlightCurrentLine = _settings.EditorHighlightCurrentLine;
        MarkdownPreviewEnabled = _settings.MarkdownPreview;
        Editor.AutoSave = _settings.AutoSave;
    }

    private void ApplyEditorSettings()
    {
        RefreshEditorSettings();
        _settingsService.Save(_settings);
    }

    private void ApplyFileSettings()
    {
        _settingsService.Save(_settings);
        RebuildTree();
        StatusText = _settings.ShowUnsupportedFiles ? "Showing all file types" : "Showing notes only";
    }

    private void ShowAbout() => _dialogs.ShowAbout();

    // ---- Tree building ------------------------------------------------------

    /// <summary>Folder new items are created in: the selected folder, the selected file's folder, or the current root.</summary>
    private string TargetDirectory()
    {
        if (SelectedNode is { } node)
            return node.IsDirectory ? node.FullPath : Path.GetDirectoryName(node.FullPath)!;

        return _currentRootPath!;
    }

    private string DirectoryOf(object? commandParameter)
    {
        if (commandParameter is NodeViewModel node)
            return node.IsDirectory ? node.FullPath : Path.GetDirectoryName(node.FullPath)!;

        return _currentRootPath!;
    }

    /// <summary>Re-scans the current root into the tree (depth-capped), preserving which folders were expanded.</summary>
    private void RebuildTree()
    {
        if (_currentRootPath is null)
            return;

        var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectExpanded(RootNodes, expanded);

        var root = _scanner.Scan(_currentRootPath, _settings.ShowUnsupportedFiles);
        RootNodes.Clear();
        foreach (var child in root.Children)
            RootNodes.Add(BuildNode(child, depth: 1));

        foreach (var node in EnumerateNodes(RootNodes))
            if (node.IsDirectory && expanded.Contains(node.FullPath))
                node.IsExpanded = true;
    }

    /// <summary>
    /// Builds a node and its descendants up to <see cref="MaxInlineDepth"/>. Folders below that depth
    /// are emitted as "doors" (visible, not expanded) that re-root the tree when clicked.
    /// </summary>
    private NodeViewModel BuildNode(WorkspaceNode model, int depth)
    {
        var isDoor = model.IsDirectory && depth > MaxInlineDepth;
        var vm = new NodeViewModel(model, isDoor);

        if (model.IsDirectory && depth <= MaxInlineDepth)
            foreach (var child in model.Children)
                vm.Children.Add(BuildNode(child, depth + 1));

        return vm;
    }

    private void RebuildBreadcrumbs()
    {
        Breadcrumbs.Clear();
        if (_workspacePath is null || _currentRootPath is null)
            return;

        var atRoot = PathsEqual(_currentRootPath, _workspacePath);
        Breadcrumbs.Add(new BreadcrumbItem(WorkspaceName, _workspacePath, isCurrent: atRoot));

        if (atRoot)
            return;

        var relative = Path.GetRelativePath(_workspacePath, _currentRootPath);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var cumulative = _workspacePath;

        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
                continue;
            cumulative = Path.Combine(cumulative, parts[i]);
            Breadcrumbs.Add(new BreadcrumbItem(parts[i], cumulative, isCurrent: i == parts.Length - 1));
        }
    }

    /// <summary>Expands ancestors of <paramref name="fullPath"/>, selects it, and opens/enters it as appropriate.</summary>
    private void RevealAndSelect(string fullPath)
    {
        var node = RevealIn(RootNodes, fullPath);
        if (node is null)
            return;

        node.IsSelected = true;
        SelectedNode = node;
    }

    private NodeViewModel? RevealIn(IEnumerable<NodeViewModel> nodes, string fullPath)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                return node;

            if (node.IsDirectory &&
                fullPath.StartsWith(node.FullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                node.IsExpanded = true;
                var found = RevealIn(node.Children, fullPath);
                if (found is not null)
                    return found;
            }
        }

        return null;
    }

    private static void CollectExpanded(IEnumerable<NodeViewModel> nodes, HashSet<string> into)
    {
        foreach (var node in nodes)
        {
            if (node.IsDirectory && node.IsExpanded)
                into.Add(node.FullPath);
            CollectExpanded(node.Children, into);
        }
    }

    private static IEnumerable<NodeViewModel> EnumerateNodes(IEnumerable<NodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in EnumerateNodes(node.Children))
                yield return child;
        }
    }

    // ---- Editor / misc ------------------------------------------------------

    private void OpenDocument(string path)
    {
        if (!File.Exists(path))
        {
            _dialogs.ShowError($"This file no longer exists:\n\n{path}");
            StatusText = "File not found";
            return;
        }

        try
        {
            Editor.OpenFile(path);
            StatusText = $"Editing {Path.GetFileName(path)}";

            _settingsService.AddRecentDocument(_settings, path, DateTimeOffset.Now);
            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not open the document.\n\n{ex.Message}");
        }
    }

    // Save state is surfaced by the status-bar indicator (Editor.SaveStatusText), so this just keeps
    // the recent-documents list fresh without clobbering the main status message.
    private void OnDocumentSaved(string path)
    {
    }

    /// <summary>Flushes any pending autosave. Call on application shutdown.</summary>
    public void FlushPendingSaves() => Editor.SaveNow();

    private static bool PathsEqual(string? a, string? b) =>
        a is not null && b is not null &&
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    private static bool IsUnder(string path, string folder) =>
        path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
