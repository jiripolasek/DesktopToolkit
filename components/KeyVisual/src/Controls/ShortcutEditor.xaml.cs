// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Displays and edits an application keyboard shortcut with a WinUI dialog.</summary>
public sealed partial class ShortcutEditor : UserControl
{
    private readonly Style _defaultPreviewKeycapStyle;
    private EditSession? _session;

    internal ContentDialog? ActiveDialog => this._session?.Dialog;

    /// <summary>Identifies the <see cref="Strings"/> dependency property.</summary>
    public static DependencyProperty StringsProperty { get; } = DependencyProperty.Register(
        nameof(Strings), typeof(ShortcutEditorStrings), typeof(ShortcutEditor), new PropertyMetadata(null, OnPreviewChanged));

    /// <summary>Identifies the <see cref="Shortcut"/> dependency property.</summary>
    public static DependencyProperty ShortcutProperty { get; } = DependencyProperty.Register(
        nameof(Shortcut), typeof(KeyboardShortcut), typeof(ShortcutEditor), new PropertyMetadata(null, OnPreviewChanged));

    /// <summary>Identifies the <see cref="PreviewKeycapStyle"/> dependency property.</summary>
    public static DependencyProperty PreviewKeycapStyleProperty { get; } = DependencyProperty.Register(
        nameof(PreviewKeycapStyle), typeof(Style), typeof(ShortcutEditor), new PropertyMetadata(null, OnPreviewChanged));

    /// <summary>Identifies the <see cref="DefaultShortcut"/> dependency property.</summary>
    public static DependencyProperty DefaultShortcutProperty { get; } = DependencyProperty.Register(
        nameof(DefaultShortcut), typeof(KeyboardShortcut), typeof(ShortcutEditor), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="AllowClear"/> dependency property.</summary>
    public static DependencyProperty AllowClearProperty { get; } = DependencyProperty.Register(
        nameof(AllowClear), typeof(bool), typeof(ShortcutEditor), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IncludeWindowsKey"/> dependency property.</summary>
    public static DependencyProperty IncludeWindowsKeyProperty { get; } = DependencyProperty.Register(
        nameof(IncludeWindowsKey), typeof(bool), typeof(ShortcutEditor), new PropertyMetadata(false));

    /// <summary>Gets or sets the labels, instructions, and feedback. Changes apply to the next dialog.</summary>
    public ShortcutEditorStrings Strings
    {
        get => (ShortcutEditorStrings?)this.GetValue(StringsProperty) ?? ShortcutEditorStrings.Default;
        set => this.SetValue(StringsProperty, value);
    }

    /// <summary>Gets or sets the saved shortcut. Null represents an unassigned shortcut.</summary>
    public KeyboardShortcut? Shortcut
    {
        get => (KeyboardShortcut?)this.GetValue(ShortcutProperty);
        set => this.SetValue(ShortcutProperty, value);
    }

    /// <summary>Gets or sets the style for the keycaps in the edit button. Null uses the accent style.</summary>
    public Style? PreviewKeycapStyle
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
        get => (Style?)this.GetValue(PreviewKeycapStyleProperty);
        set => this.SetValue(PreviewKeycapStyleProperty, value);
    }

    /// <summary>Gets or sets the shortcut made available through the Use default button.</summary>
    public KeyboardShortcut? DefaultShortcut
    {
        get => (KeyboardShortcut?)this.GetValue(DefaultShortcutProperty);
        set => this.SetValue(DefaultShortcutProperty, value);
    }

    /// <summary>Gets or sets whether the user can clear the shortcut.</summary>
    public bool AllowClear
    {
        get => (bool)this.GetValue(AllowClearProperty);
        set => this.SetValue(AllowClearProperty, value);
    }

    /// <summary>Gets or sets whether to add the Windows modifier to each newly captured shortcut.</summary>
    public bool IncludeWindowsKey
    {
        get => (bool)this.GetValue(IncludeWindowsKeyProperty);
        set => this.SetValue(IncludeWindowsKeyProperty, value);
    }

    /// <summary>Gets or sets a callback that returns an error message for a restricted shortcut, or null to allow it.</summary>
    /// <remarks>The callback runs on the UI thread while editing and when the user submits the shortcut.</remarks>
    public Func<KeyboardShortcut, string?>? ValidateShortcut { get; set; }

    /// <summary>Gets or sets an asynchronous callback that returns an error message for a restricted shortcut, or null to allow it.</summary>
    /// <remarks>Takes precedence over <see cref="ValidateShortcut"/>. The token is canceled when the draft changes or the dialog closes.</remarks>
    public Func<KeyboardShortcut, CancellationToken, Task<string?>>? ValidateShortcutAsync { get; set; }

    /// <summary>Gets or sets a callback that finds an existing assignment for a shortcut, or returns null if it is available.</summary>
    /// <remarks>The callback runs on the UI thread while editing and when the user submits the shortcut.</remarks>
    public Func<KeyboardShortcut, ShortcutConflict?>? FindConflict { get; set; }

    /// <summary>Gets or sets an asynchronous callback that finds an existing assignment, or returns null if the shortcut is available.</summary>
    /// <remarks>Takes precedence over <see cref="FindConflict"/>. The token is canceled when the draft changes or the dialog closes.</remarks>
    public Func<KeyboardShortcut, CancellationToken, Task<ShortcutConflict?>>? FindConflictAsync { get; set; }

    /// <summary>Creates a keyboard shortcut editor.</summary>
    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    public ShortcutEditor()
    {
        this.InitializeComponent();
        this._defaultPreviewKeycapStyle = (Style)this.Resources["AccentKeyVisualStyle"];
        WinUIResourceStrings.ObserveChanges(this, this.UpdatePreview);
        this.UpdatePreview();
        this.Unloaded += (_, _) => this._session?.Dismiss();
    }

    /// <summary>Shows the editor dialog and returns whether the user changed the shortcut.</summary>
    /// <remarks>Returns false if another content dialog is already open on the UI thread.</remarks>
    public async Task<bool> EditAsync()
    {
        if (this.ActiveDialog is not null || this.XamlRoot is not { } xamlRoot || !this.IsEnabled)
        {
            return false;
        }

        var strings = this.Strings;
        var capture = new ShortcutCaptureView(this.Shortcut, this.DefaultShortcut, this.AllowClear, this.IncludeWindowsKey, strings);
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            RequestedTheme = this.ActualTheme,
            Title = strings.DialogTitle,
            Content = capture,
            PrimaryButtonText = strings.SaveButtonText,
            CloseButtonText = strings.CancelButtonText,
        };
        using var session = new EditSession(this, dialog, capture, strings);
        this._session = session;

        try
        {
            session.RefreshDraft();
            await dialog.ShowAsync();
            return session.ShortcutChanged;
        }
        catch (COMException error) when (error.HResult == unchecked((int)0x80000019))
        {
            // WinUI permits only one open ContentDialog on the UI thread.
            return false;
        }
        finally
        {
            this._session = null;
        }
    }

    internal Task<bool> TrySubmitDraftAsync(ShortcutCaptureView capture)
    {
        return this._session is { } session && ReferenceEquals(session.Dialog.Content, capture)
            ? session.ReviewAsync(submit: true)
            : Task.FromResult(false);
    }

    private async Task<DraftReview> ReviewDraftAsync(
        ShortcutCaptureView capture,
        ShortcutEditorStrings strings,
        CancellationToken cancellationToken)
    {
        if (!capture.CanSave)
        {
            return default;
        }

        if (capture.Draft is not { } draft || this.Shortcut == draft)
        {
            return new DraftReview(true);
        }

        try
        {
            var error = this.ValidateShortcutAsync is { } validateAsync
                ? await validateAsync(draft, cancellationToken).WaitAsync(cancellationToken)
                : this.ValidateShortcut?.Invoke(draft);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(error))
            {
                return new DraftReview(false, error);
            }

            var conflict = this.FindConflictAsync is { } findAsync
                ? await findAsync(draft, cancellationToken).WaitAsync(cancellationToken)
                : this.FindConflict?.Invoke(draft);
            return conflict switch
            {
                null => new DraftReview(true),
                { CanReplace: false } => new DraftReview(
                    false, WinUIResourceStrings.Format(strings.ConflictMessageFormat, nameof(ShortcutEditorStrings.ConflictMessageFormat), conflict.OwnerName)),
                _ => new DraftReview(
                    true,
                    WinUIResourceStrings.Format(
                        strings.ReplaceableConflictMessageFormat, nameof(ShortcutEditorStrings.ReplaceableConflictMessageFormat),
                        conflict.OwnerName, strings.ReplaceButtonText),
                    conflict),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            Trace.TraceError("Shortcut check failed: {0}", error);
            return new DraftReview(false, strings.CheckFailedMessage);
        }
    }

    private static async Task<bool> TryReplaceAsync(ShortcutConflict conflict)
    {
        try
        {
            return conflict.TryReplaceAsync is { } tryReplaceAsync ? await tryReplaceAsync() : conflict.TryReplace?.Invoke() == true;
        }
        catch (Exception error)
        {
            Trace.TraceError("Shortcut replacement failed: {0}", error);
            return false;
        }
    }

    private static void OnPreviewChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((ShortcutEditor)sender).UpdatePreview();
    }

    private async void OnEditClick(object sender, RoutedEventArgs args)
    {
        await this.EditAsync();
    }

    private void UpdatePreview()
    {
        var shortcut = this.Shortcut;
        this.AssignedContent.Visibility = shortcut is null ? Visibility.Collapsed : Visibility.Visible;
        this.AssignContent.Visibility = shortcut is null ? Visibility.Visible : Visibility.Collapsed;
        var strings = this.Strings;
        this.AssignText.Text = strings.AssignButtonText;
        AutomationProperties.SetName(this.EditButton, shortcut is null ? strings.AssignButtonText : strings.EditButtonText);
        AutomationProperties.SetHelpText(this.EditButton, shortcut?.ToString() ?? strings.UnassignedText);
        this.AssignedPreview.KeycapStyle = this.PreviewKeycapStyle ?? this._defaultPreviewKeycapStyle;
        this.AssignedPreview.Shortcut = shortcut;
    }

    private readonly record struct DraftReview(bool CanSubmit, string? Message = null, ShortcutConflict? Conflict = null)
    {
        public string? ReplaceableOwnerId => this.Conflict is { CanReplace: true } conflict ? conflict.OwnerId : null;
    }

    private sealed partial class EditSession : IDisposable
    {
        private readonly ShortcutEditor _editor;
        private readonly ShortcutCaptureView _capture;
        private readonly ShortcutEditorStrings _strings;
        private CancellationTokenSource? _lookup;
        private string? _offeredOwnerId;
        private bool _closing;
        private bool _replacing;
        private bool _dismissRequested;

        public ContentDialog Dialog { get; }

        public bool ShortcutChanged { get; private set; }

        public EditSession(ShortcutEditor editor, ContentDialog dialog, ShortcutCaptureView capture, ShortcutEditorStrings strings)
        {
            this._editor = editor;
            this._capture = capture;
            this._strings = strings;
            this.Dialog = dialog;
            this.Dialog.Opened += this.OnOpened;
            this.Dialog.Closing += this.OnClosing;
            this.Dialog.PrimaryButtonClick += this.OnPrimaryButtonClick;
            this._capture.DraftChanged += this.RefreshDraft;
        }

        public void Dispose()
        {
            this.StopLookup();
            this.Dialog.Opened -= this.OnOpened;
            this.Dialog.Closing -= this.OnClosing;
            this.Dialog.PrimaryButtonClick -= this.OnPrimaryButtonClick;
            this._capture.DraftChanged -= this.RefreshDraft;
        }

        public void RefreshDraft()
        {
            _ = this.ReviewAsync(submit: false);
        }

        public async Task<bool> ReviewAsync(bool submit)
        {
            if (this._closing || this._replacing || (submit && !this.Dialog.IsPrimaryButtonEnabled))
            {
                return false;
            }

            var previous = this._editor.Shortcut;
            var draft = this._capture.Draft;
            var offeredOwnerId = this._offeredOwnerId;
            var pending = this._lookup;
            using var lookup = new CancellationTokenSource();
            this._lookup = lookup;
            Cancel(pending);
            try
            {
                var reviewTask = this._editor.ReviewDraftAsync(this._capture, this._strings, lookup.Token);
                if (!reviewTask.IsCompleted && this.IsCurrent(lookup))
                {
                    this.ShowStatus(this._strings.CheckingMessage);
                }

                var review = await reviewTask;
                if (!this.IsCurrent(lookup))
                {
                    return false;
                }

                if (!submit || !review.CanSubmit || review.ReplaceableOwnerId != offeredOwnerId)
                {
                    this.ApplyReview(review);
                    return false;
                }

                if (review.Conflict is { CanReplace: true } conflict)
                {
                    return await this.ReplaceAsync(conflict, review, previous, draft);
                }

                this.Commit(previous, draft);
                return true;
            }
            catch (OperationCanceledException) when (lookup.IsCancellationRequested)
            {
                return false;
            }
            finally
            {
                if (ReferenceEquals(this._lookup, lookup))
                {
                    this._lookup = null;
                }
            }
        }

        private async Task<bool> ReplaceAsync(
            ShortcutConflict conflict,
            DraftReview review,
            KeyboardShortcut? previous,
            KeyboardShortcut? draft)
        {
            this._replacing = true;
            try
            {
                var replacement = TryReplaceAsync(conflict);
                if (!replacement.IsCompleted)
                {
                    this._capture.IsEnabled = false;
                    this.ShowStatus(
                        WinUIResourceStrings.Format(
                            this._strings.ReplacingMessageFormat, nameof(ShortcutEditorStrings.ReplacingMessageFormat), conflict.OwnerName));
                }

                if (!await replacement)
                {
                    this.ApplyReview(
                        review with
                        {
                            Message = WinUIResourceStrings.Format(
                                this._strings.ReplacementFailedMessageFormat, nameof(ShortcutEditorStrings.ReplacementFailedMessageFormat),
                                conflict.OwnerName),
                        }, KeyVisualState.Error);
                    return false;
                }

                this.Commit(previous, draft);
                return true;
            }
            finally
            {
                this._capture.IsEnabled = true;
                this._replacing = false;
            }
        }

        private void Commit(KeyboardShortcut? previous, KeyboardShortcut? draft)
        {
            if (this._editor.Shortcut != draft)
            {
                this._editor.Shortcut = draft;
            }

            this.ShortcutChanged |= previous != draft;
        }

        private bool IsCurrent(CancellationTokenSource lookup)
        {
            return !this._closing && ReferenceEquals(this._lookup, lookup);
        }

        private void ShowStatus(string message)
        {
            this.Dialog.IsPrimaryButtonEnabled = false;
            this._capture.RenderDraft(message, KeyVisualState.Normal);
        }

        private void ApplyReview(DraftReview review, KeyVisualState? state = null)
        {
            this._offeredOwnerId = review.ReplaceableOwnerId;
            this._capture.RenderDraft(review.Message, state ?? (review.CanSubmit ? KeyVisualState.Warning : KeyVisualState.Error));
            var action = this._offeredOwnerId is null ? this._strings.SaveButtonText : this._strings.ReplaceButtonText;
            if (this.Dialog.PrimaryButtonText != action)
            {
                this.Dialog.PrimaryButtonText = action;
            }

            this.Dialog.IsPrimaryButtonEnabled = review.CanSubmit;
        }

        private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            this._capture.FocusCapture();
        }

        private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var submission = this.ReviewAsync(submit: true);
            if (submission.IsCompletedSuccessfully)
            {
                args.Cancel = !submission.Result;
                return;
            }

            var deferral = args.GetDeferral();
            try
            {
                args.Cancel = !await submission;
            }
            finally
            {
                deferral.Complete();
                if (this._dismissRequested && !this._closing)
                {
                    this.Dialog.Hide();
                }
            }
        }

        private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (this._replacing)
            {
                args.Cancel = true;
                return;
            }

            this.StopLookup();
        }

        public void Dismiss()
        {
            this._dismissRequested = true;
            if (!this._replacing)
            {
                this.Dialog.Hide();
            }
        }

        private void StopLookup()
        {
            this._closing = true;
            var lookup = this._lookup;
            this._lookup = null;
            Cancel(lookup);
        }

        private static void Cancel(CancellationTokenSource? lookup)
        {
            try
            {
                lookup?.Cancel();
            }
            catch (AggregateException error)
            {
                Trace.TraceError("Shortcut check cancellation failed: {0}", error);
            }
        }
    }
}
