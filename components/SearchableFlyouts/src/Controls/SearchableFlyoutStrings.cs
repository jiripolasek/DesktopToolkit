// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Provides localizable labels and composite format strings for flyout status and accessibility.</summary>
/// <remarks>Format arguments use the current culture. Set properties during initialization and assign the instance to the control's Strings property.</remarks>
public sealed partial class SearchableFlyoutStrings
{
    /// <summary>Gets the shared English labels and format strings.</summary>
    public static SearchableFlyoutStrings Default { get; } = new();

    /// <summary>Gets the accessible name of a command list.</summary>
    public string CommandsName { get; init; } = "Commands";

    /// <summary>Gets the accessible name of a list of choices.</summary>
    public string ChoicesName { get; init; } = "Choices";

    /// <summary>Gets the format for one matching command. The {0} argument is the match count.</summary>
    public string MatchingCommandFormat { get; init; } = "{0} matching command";

    /// <summary>Gets the format for zero or multiple matching commands. The {0} argument is the match count.</summary>
    public string MatchingCommandsFormat { get; init; } = "{0} matching commands";

    /// <summary>Gets the format for one matching choice. The {0} argument is the match count.</summary>
    public string MatchingChoiceFormat { get; init; } = "{0} matching choice";

    /// <summary>Gets the format for zero or multiple matching choices. The {0} argument is the match count.</summary>
    public string MatchingChoicesFormat { get; init; } = "{0} matching choices";

    /// <summary>Gets the command count format. The {0} and {1} arguments are the matching and total command counts.</summary>
    public string CommandCountFormat { get; init; } = "{0:N0} of {1:N0} commands";

    /// <summary>Gets the choice count format. The {0} and {1} arguments are the matching and total choice counts.</summary>
    public string ChoiceCountFormat { get; init; } = "{0:N0} of {1:N0} choices";

    /// <summary>Gets the accessible label format for a submenu filter. The {0} argument is the parent item label.</summary>
    public string FilterSubmenuFormat { get; init; } = "Filter {0}";

    /// <summary>Gets the keyboard navigation instructions exposed to assistive technology.</summary>
    public string KeyboardHelp { get; init; }
        = "Type to filter. Use Up or Down to move between search and results. Press Enter to choose or Escape to close.";

    /// <summary>Initializes a new instance of the <see cref="SearchableFlyoutStrings"/> class.</summary>
    public SearchableFlyoutStrings() { }

    internal string Matches(int count, bool choices = false)
    {
        return string.Format(
            CultureInfo.CurrentCulture,
            choices ? count == 1 ? this.MatchingChoiceFormat : this.MatchingChoicesFormat :
            count == 1 ? this.MatchingCommandFormat : this.MatchingCommandsFormat, count);
    }

    internal string Count(int count, int total, bool choices = false)
    {
        return string.Format(CultureInfo.CurrentCulture, choices ? this.ChoiceCountFormat : this.CommandCountFormat, count, total);
    }

    internal string Submenu(string text)
    {
        return string.Format(CultureInfo.CurrentCulture, this.FilterSubmenuFormat, text);
    }
}
