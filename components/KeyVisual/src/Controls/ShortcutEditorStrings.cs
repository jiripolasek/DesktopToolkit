// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Provides the labels, capture instructions, and feedback for a keyboard shortcut editor.</summary>
/// <remarks>Blank values use the localized defaults.</remarks>
public sealed partial class ShortcutEditorStrings
{
    private readonly string? _dialogTitle;
    private readonly string? _assignButtonText;
    private readonly string? _editButtonText;
    private readonly string? _saveButtonText;
    private readonly string? _replaceButtonText;
    private readonly string? _cancelButtonText;
    private readonly string? _useDefaultButtonText;
    private readonly string? _clearButtonText;
    private readonly string? _captureName;
    private readonly string? _unassignedText;
    private readonly string? _captureInstructions;
    private readonly string? _captureWithWindowsKeyInstructions;
    private readonly string? _altGrWarning;
    private readonly string? _conflictMessageFormat;
    private readonly string? _replaceableConflictMessageFormat;
    private readonly string? _checkingMessage;
    private readonly string? _checkFailedMessage;
    private readonly string? _replacingMessageFormat;
    private readonly string? _replacementFailedMessageFormat;

    /// <summary>Gets the shared localized strings.</summary>
    public static ShortcutEditorStrings Default { get; } = new();

    /// <summary>Gets the title of the editor dialog.</summary>
    public string DialogTitle
    {
        get => WinUIResourceStrings.GetOverride(this._dialogTitle, nameof(this.DialogTitle));
        init => this._dialogTitle = value;
    }

    /// <summary>Gets the text of the button for an unassigned shortcut.</summary>
    public string AssignButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._assignButtonText, nameof(this.AssignButtonText));
        init => this._assignButtonText = value;
    }

    /// <summary>Gets the accessible name of the button for an assigned shortcut.</summary>
    public string EditButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._editButtonText, nameof(this.EditButtonText));
        init => this._editButtonText = value;
    }

    /// <summary>Gets the text of the dialog action that saves the shortcut.</summary>
    public string SaveButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._saveButtonText, nameof(this.SaveButtonText));
        init => this._saveButtonText = value;
    }

    /// <summary>Gets the text of the dialog action that replaces a conflicting shortcut.</summary>
    public string ReplaceButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._replaceButtonText, nameof(this.ReplaceButtonText));
        init => this._replaceButtonText = value;
    }

    /// <summary>Gets the text of the dialog action that cancels editing.</summary>
    public string CancelButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._cancelButtonText, nameof(this.CancelButtonText));
        init => this._cancelButtonText = value;
    }

    /// <summary>Gets the text of the action that restores the default shortcut.</summary>
    public string UseDefaultButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._useDefaultButtonText, nameof(this.UseDefaultButtonText));
        init => this._useDefaultButtonText = value;
    }

    /// <summary>Gets the text of the action that clears the shortcut.</summary>
    public string ClearButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._clearButtonText, nameof(this.ClearButtonText));
        init => this._clearButtonText = value;
    }

    /// <summary>Gets the accessible name of the shortcut capture surface.</summary>
    public string CaptureName
    {
        get => WinUIResourceStrings.GetOverride(this._captureName, nameof(this.CaptureName));
        init => this._captureName = value;
    }

    /// <summary>Gets the text for a missing shortcut.</summary>
    public string UnassignedText
    {
        get => WinUIResourceStrings.GetOverride(this._unassignedText, nameof(this.UnassignedText));
        init => this._unassignedText = value;
    }

    /// <summary>Gets the instructions for recording a shortcut with a modifier key.</summary>
    public string CaptureInstructions
    {
        get => WinUIResourceStrings.GetOverride(this._captureInstructions, nameof(this.CaptureInstructions));
        init => this._captureInstructions = value;
    }

    /// <summary>Gets the instructions when the Windows key is included automatically.</summary>
    public string CaptureWithWindowsKeyInstructions
    {
        get => WinUIResourceStrings.GetOverride(this._captureWithWindowsKeyInstructions, nameof(this.CaptureWithWindowsKeyInstructions));
        init => this._captureWithWindowsKeyInstructions = value;
    }

    /// <summary>Gets the warning for shortcuts that may conflict with AltGr.</summary>
    public string AltGrWarning
    {
        get => WinUIResourceStrings.GetOverride(this._altGrWarning, nameof(this.AltGrWarning));
        init => this._altGrWarning = value;
    }

    /// <summary>Gets the conflict message format when replacement is unavailable. The argument is the owner name.</summary>
    public string ConflictMessageFormat
    {
        get => WinUIResourceStrings.GetOverride(this._conflictMessageFormat, nameof(this.ConflictMessageFormat));
        init => this._conflictMessageFormat = value;
    }

    /// <summary>Gets the replacement prompt format. Arguments are the owner name and replacement button text.</summary>
    public string ReplaceableConflictMessageFormat
    {
        get => WinUIResourceStrings.GetOverride(this._replaceableConflictMessageFormat, nameof(this.ReplaceableConflictMessageFormat));
        init => this._replaceableConflictMessageFormat = value;
    }

    /// <summary>Gets the status displayed while checking a shortcut.</summary>
    public string CheckingMessage
    {
        get => WinUIResourceStrings.GetOverride(this._checkingMessage, nameof(this.CheckingMessage));
        init => this._checkingMessage = value;
    }

    /// <summary>Gets the error displayed when the shortcut check fails.</summary>
    public string CheckFailedMessage
    {
        get => WinUIResourceStrings.GetOverride(this._checkFailedMessage, nameof(this.CheckFailedMessage));
        init => this._checkFailedMessage = value;
    }

    /// <summary>Gets the replacement progress format. The argument is the owner name.</summary>
    public string ReplacingMessageFormat
    {
        get => WinUIResourceStrings.GetOverride(this._replacingMessageFormat, nameof(this.ReplacingMessageFormat));
        init => this._replacingMessageFormat = value;
    }

    /// <summary>Gets the replacement failure format. The argument is the owner name.</summary>
    public string ReplacementFailedMessageFormat
    {
        get => WinUIResourceStrings.GetOverride(this._replacementFailedMessageFormat, nameof(this.ReplacementFailedMessageFormat));
        init => this._replacementFailedMessageFormat = value;
    }

    /// <summary>Initializes a new set of shortcut editor strings.</summary>
    public ShortcutEditorStrings() { }
}
