// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Provides localizable labels and composite format strings for flyout status and accessibility.</summary>
/// <remarks>Blank values use localized defaults. Format arguments use the current culture.</remarks>
public sealed partial class SearchableFlyoutStrings
{
    private readonly string? _commandsName;
    private readonly string? _choicesName;
    private readonly string? _keyboardHelp;
    private readonly string? _commandCountFormat;
    private readonly string? _choiceCountFormat;
    private readonly string? _filterSubmenuFormat;
    private string? _matchingCommandCountFormat;
    private string? _matchingChoiceCountFormat;
    private string? _matchingCommandFormat;
    private string? _matchingCommandsFormat;
    private string? _matchingChoiceFormat;
    private string? _matchingChoicesFormat;

    internal static string DefaultFilterBoxAutomationName => WinUIResourceStrings.Get(nameof(DefaultFilterBoxAutomationName));

    internal static string DefaultFilterPlaceholderText => WinUIResourceStrings.Get(nameof(DefaultFilterPlaceholderText));

    internal static string DefaultNoResultsText => WinUIResourceStrings.Get(nameof(DefaultNoResultsText));

    /// <summary>Gets the shared localized labels and format strings.</summary>
    public static SearchableFlyoutStrings Default { get; } = new();

    /// <summary>Gets the accessible name of a command list.</summary>
    public string CommandsName
    {
        get => WinUIResourceStrings.GetOverride(this._commandsName, nameof(this.CommandsName));
        init => this._commandsName = value;
    }

    /// <summary>Gets the accessible name of a list of choices.</summary>
    public string ChoicesName
    {
        get => WinUIResourceStrings.GetOverride(this._choicesName, nameof(this.ChoicesName));
        init => this._choicesName = value;
    }

    /// <summary>Gets the format for any number of matching commands. The {0} argument is the match count.</summary>
    public string MatchingCommandCountFormat
    {
        get => WinUIResourceStrings.GetOverride(this._matchingCommandCountFormat, nameof(this.MatchingCommandCountFormat));
        init => this._matchingCommandCountFormat = value;
    }

    /// <summary>Gets the format for any number of matching choices. The {0} argument is the match count.</summary>
    public string MatchingChoiceCountFormat
    {
        get => WinUIResourceStrings.GetOverride(this._matchingChoiceCountFormat, nameof(this.MatchingChoiceCountFormat));
        init => this._matchingChoiceCountFormat = value;
    }

    /// <summary>Gets the format for one matching command. The {0} argument is the match count.</summary>
    [Obsolete("Use MatchingCommandCountFormat for all counts.")]
    public string MatchingCommandFormat
    {
        get => WithFallback(this._matchingCommandFormat, this.MatchingCommandCountFormat);
        init => this._matchingCommandFormat = value;
    }

    /// <summary>Gets the format for zero or multiple matching commands. The {0} argument is the match count.</summary>
    [Obsolete("Use MatchingCommandCountFormat for all counts.")]
    public string MatchingCommandsFormat
    {
        get => WithFallback(this._matchingCommandsFormat, this.MatchingCommandCountFormat);
        init => this._matchingCommandsFormat = value;
    }

    /// <summary>Gets the format for one matching choice. The {0} argument is the match count.</summary>
    [Obsolete("Use MatchingChoiceCountFormat for all counts.")]
    public string MatchingChoiceFormat
    {
        get => WithFallback(this._matchingChoiceFormat, this.MatchingChoiceCountFormat);
        init => this._matchingChoiceFormat = value;
    }

    /// <summary>Gets the format for zero or multiple matching choices. The {0} argument is the match count.</summary>
    [Obsolete("Use MatchingChoiceCountFormat for all counts.")]
    public string MatchingChoicesFormat
    {
        get => WithFallback(this._matchingChoicesFormat, this.MatchingChoiceCountFormat);
        init => this._matchingChoicesFormat = value;
    }

    /// <summary>Gets the command count format. The {0} and {1} arguments are the matching and total command counts.</summary>
    public string CommandCountFormat
    {
        get => WinUIResourceStrings.GetOverride(this._commandCountFormat, nameof(this.CommandCountFormat));
        init => this._commandCountFormat = value;
    }

    /// <summary>Gets the choice count format. The {0} and {1} arguments are the matching and total choice counts.</summary>
    public string ChoiceCountFormat
    {
        get => WinUIResourceStrings.GetOverride(this._choiceCountFormat, nameof(this.ChoiceCountFormat));
        init => this._choiceCountFormat = value;
    }

    /// <summary>Gets the accessible label format for a submenu filter. The {0} argument is the parent item label.</summary>
    public string FilterSubmenuFormat
    {
        get => WinUIResourceStrings.GetOverride(this._filterSubmenuFormat, nameof(this.FilterSubmenuFormat));
        init => this._filterSubmenuFormat = value;
    }

    /// <summary>Gets the keyboard navigation instructions exposed to assistive technology.</summary>
    public string KeyboardHelp
    {
        get => WinUIResourceStrings.GetOverride(this._keyboardHelp, nameof(this.KeyboardHelp));
        init => this._keyboardHelp = value;
    }

    /// <summary>Initializes a new instance of the <see cref="SearchableFlyoutStrings"/> class.</summary>
    public SearchableFlyoutStrings() { }

    internal string ListName(bool choices = false)
    {
        return choices ? this.ChoicesName : this.CommandsName;
    }

    internal string Matches(int count, bool choices = false)
    {
        // Honor explicit overrides from the first preview.
        var legacyFormat = choices ? count == 1 ? this._matchingChoiceFormat : this._matchingChoicesFormat :
            count == 1 ? this._matchingCommandFormat : this._matchingCommandsFormat;
        var format = choices ? this._matchingChoiceCountFormat : this._matchingCommandCountFormat;
        var key = choices ? nameof(this.MatchingChoiceCountFormat) : nameof(this.MatchingCommandCountFormat);
        return WinUIResourceStrings.Format(string.IsNullOrWhiteSpace(format) ? legacyFormat : format, key, count);
    }

    internal string Count(int count, int total, bool choices = false)
    {
        return choices
            ? WinUIResourceStrings.Format(this._choiceCountFormat, nameof(this.ChoiceCountFormat), count, total)
            : WinUIResourceStrings.Format(this._commandCountFormat, nameof(this.CommandCountFormat), count, total);
    }

    internal string Submenu(string? itemLabel, string filterName)
    {
        return string.IsNullOrWhiteSpace(itemLabel)
            ? filterName
            : WinUIResourceStrings.Format(this._filterSubmenuFormat, nameof(this.FilterSubmenuFormat), itemLabel);
    }

    internal static string WithFallback(string? text, string fallback)
    {
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }
}
