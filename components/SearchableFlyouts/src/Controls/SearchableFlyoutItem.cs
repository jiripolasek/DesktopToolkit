// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Describes a command, submenu, separator, or dropdown choice.</summary>
[ContentProperty(Name = nameof(Items))]
public sealed partial class SearchableFlyoutItem : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private string _keywords = string.Empty;
    private string _glyph = string.Empty;
    private string _shortcutText = string.Empty;
    private string _automationName = string.Empty;
    private string _automationHelpText = string.Empty;
    private string _automationId = string.Empty;
    private bool _isEnabled = true;
    private bool _isCheckable;
    private bool _hasChildren;
    private bool _isChecked;
    private bool _isSeparator;
    private bool _isVisible = true;
    private IconSource? _iconSource;
    private ICommand? _command;
    private object? _commandParameter;

    /// <summary>Gets or sets the label displayed for this item and used for filtering.</summary>
    public string Text
    {
        get => this._text;
        set => this.SetProperty(ref this._text, value ?? string.Empty);
    }

    /// <summary>Gets or sets additional searchable text for this item.</summary>
    public string Keywords
    {
        get => this._keywords;
        set => this.SetProperty(ref this._keywords, value ?? string.Empty);
    }

    /// <summary>Gets or sets the symbol font glyph displayed when <see cref="IconSource"/> is null.</summary>
    public string Glyph
    {
        get => this._glyph;
        set => this.SetProperty(ref this._glyph, value ?? string.Empty);
    }

    /// <summary>Gets or sets the displayed shortcut hint. Register the keyboard accelerator in the host application.</summary>
    public string KeyboardAcceleratorText
    {
        get => this._shortcutText;
        set => this.SetProperty(ref this._shortcutText, value ?? string.Empty);
    }

    /// <summary>Gets or sets the accessible name of the row. An empty name uses <see cref="Text"/>.</summary>
    public string AutomationName
    {
        get => this._automationName;
        set => this.SetProperty(ref this._automationName, value ?? string.Empty);
    }

    /// <summary>Gets or sets the help text exposed by the row to UI Automation.</summary>
    public string AutomationHelpText
    {
        get => this._automationHelpText;
        set => this.SetProperty(ref this._automationHelpText, value ?? string.Empty);
    }

    /// <summary>Gets or sets the UI Automation identifier for the row.</summary>
    public string AutomationId
    {
        get => this._automationId;
        set => this.SetProperty(ref this._automationId, value ?? string.Empty);
    }

    /// <summary>Gets or sets whether the item is enabled. The default is true.</summary>
    public bool IsEnabled
    {
        get => this._isEnabled;
        set => this.SetProperty(ref this._isEnabled, value);
    }

    /// <summary>Gets or sets whether invoking the item as a command toggles its checked state.</summary>
    public bool IsCheckable
    {
        get => this._isCheckable;
        set => this.SetProperty(ref this._isCheckable, value);
    }

    /// <summary>Gets or sets whether a checkable command is checked.</summary>
    public bool IsChecked
    {
        get => this._isChecked;
        set => this.SetProperty(ref this._isChecked, value);
    }

    /// <summary>Gets or sets whether this item is a noninteractive separator between groups.</summary>
    public bool IsSeparator
    {
        get => this._isSeparator;
        set => this.SetProperty(ref this._isSeparator, value);
    }

    /// <summary>Gets or sets whether the item participates in filtering and navigation. The default is true.</summary>
    public bool IsVisible
    {
        get => this._isVisible;
        set => this.SetProperty(ref this._isVisible, value);
    }

    /// <summary>Gets or sets the icon displayed in preference to <see cref="Glyph"/>.</summary>
    public IconSource? IconSource
    {
        get => this._iconSource;
        set => this.SetProperty(ref this._iconSource, value);
    }

    /// <summary>Gets or sets the command executed when this leaf item is invoked in a command flyout.</summary>
    public ICommand? Command
    {
        get => this._command;
        set => this.SetProperty(ref this._command, value);
    }

    /// <summary>Gets or sets the argument passed to <see cref="Command"/> for execution and availability checks.</summary>
    public object? CommandParameter
    {
        get => this._commandParameter;
        set => this.SetProperty(ref this._commandParameter, value);
    }

    /// <summary>Gets or sets an application-defined value associated with the item.</summary>
    public object? Tag { get; set; }

    /// <summary>Gets the child items displayed in a submenu. This is the default XAML content property.</summary>
    public ObservableCollection<SearchableFlyoutItem> Items { get; } = [];

    internal bool HasChildren => this._hasChildren;

    internal bool CanInvoke => this.IsVisible
                               && !this.IsSeparator
                               && this.IsEnabled
                               && (this.HasChildren || (this.Command?.CanExecute(this.CommandParameter) ?? true));

    /// <summary>Initializes a new instance of the <see cref="SearchableFlyoutItem"/> class.</summary>
    public SearchableFlyoutItem()
    {
        this.Items.CollectionChanged += this.OnChildrenChanged;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyCanExecuteChanged()
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CanInvoke)));
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        var hasChildren = this.Items.Count != 0;
        if (this._hasChildren == hasChildren)
        {
            return;
        }

        this._hasChildren = hasChildren;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.HasChildren)));
        this.NotifyCanExecuteChanged();
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        this.NotifyCanExecuteChanged();
    }
}
