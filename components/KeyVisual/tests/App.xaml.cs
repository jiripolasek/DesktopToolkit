// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls.KeyVisualControlTests;

public sealed partial class App : Application
{
    private readonly StackPanel _host = new();
    private Window _window = null!;

    public App()
    {
        this.UnhandledException += (_, args) => Console.Error.WriteLine(args.Exception);
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        this._window = new Window { Content = this._host, Title = "KeyVisual control tests" };
        this._host.Loaded += this.RunTests;
        try
        {
            this._window.AppWindow.IsShownInSwitchers = false;
        }
        catch (NotImplementedException)
        {
            // Alternate desktops have no window switcher.
        }

        this._window.AppWindow.Show(false);
    }

    private async void RunTests(object sender, RoutedEventArgs args)
    {
        this._host.Loaded -= this.RunTests;
        Func<Task>[] tests =
        [
            this.ChangesBetweenTextGlyphAndLogo,
            this.KeycapStylesKeepLettersAndSymbolsSquare,
            this.NamedStylesUseValidationAndDisabledStates,
            this.ExposesKeyLabel,
            this.ShortcutPresenterUpdatesItsChordAndStyle,
            this.ShortcutEditorPreviewsHeldModifiersAndValidation,
            this.ShortcutEditorSavesValidDraftAndCancels,
            this.ShortcutEditorPreservesOpenHostDialog,
            this.ShortcutEditorResetsAndClears,
            this.ShortcutEditorAppliesCallerValidation,
            this.ShortcutEditorIgnoresObsoleteValidation,
            this.ShortcutEditorReplacesOnlyTheOfferedAssignment,
            this.ShortcutEditorIgnoresObsoleteConflictLookups,
            this.ShortcutEditorAwaitsReplacementWithoutChangingTheDraft,
        ];
        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                await test();
                Console.WriteLine($"PASS {test.Method.Name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {test.Method.Name}: {exception}");
            }
            finally
            {
                this._host.Children.Clear();
            }
        }

        Console.WriteLine($"Passed: {tests.Length - failed}, Failed: {failed}, Skipped: 0");
        Environment.ExitCode = failed == 0 ? 0 : 1;
        this._window.Close();
    }

    private async Task ChangesBetweenTextGlyphAndLogo()
    {
        var key = new KeyVisual { Content = VirtualKey.Up };
        var root = await this.LoadKey(key);
        var presenter = (KeyCharPresenter)root.Children.Single();
        var presenterRoot = ApplyTemplate(presenter);
        var label = (TextBlock)presenterRoot.FindName("KeyText");
        var glyph = (FontIcon)presenterRoot.FindName("KeyGlyph");
        var glyphBox = (Viewbox)presenterRoot.FindName("KeyGlyphBox");
        var windows = (Viewbox)presenterRoot.FindName("KeyWindows");
        Check(label.Text == "Up" && label.Visibility == Visibility.Visible, "The initial arrow label is missing.");

        key.RenderKeyAsGlyph = true;
        Check(
            glyph.Glyph == "\uE74A" && glyphBox.Visibility == Visibility.Visible && label.Visibility == Visibility.Collapsed,
            "Enabling symbols did not replace the label with the arrow glyph.");

        key.Content = (int)VirtualKey.LeftWindows;
        Check(
            windows.Visibility == Visibility.Visible && glyphBox.Visibility == Visibility.Collapsed,
            "Changing the key did not replace the arrow with the Windows logo.");

        key.Content = null;
        Check(
            label.Text.Length == 0 && label.Visibility == Visibility.Visible && windows.Visibility == Visibility.Collapsed,
            "Clearing the key left the old logo or text visible.");
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private async Task KeycapStylesKeepLettersAndSymbolsSquare()
    {
        var styles = new ResourceDictionary
        {
            Source = new Uri("ms-appx:///JPSoftworks.DesktopToolkit.Controls.KeyVisual/Themes/KeyVisualStyles.xaml"),
        };
        (string Name, double Size)[] sizes = [("SmallKeyVisualStyle", 22), ("DefaultKeyVisualStyle", 26), ("LargeKeyVisualStyle", 48)];
        foreach (var (name, size) in sizes)
        {
            foreach (var content in new[] { "A", "W", "Shift", "Up", "Windows" })
            {
                var key = new KeyVisual { Content = content, Style = (Style)styles[name], RenderKeyAsGlyph = true };
                await this.LoadKey(key);
                key.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                Check(
                    Math.Abs(key.DesiredSize.Width - size) < 0.1 && Math.Abs(key.DesiredSize.Height - size) < 0.1,
                    $"{name} gave {content} a size of {key.DesiredSize}; expected {size} by {size}.");
                this._host.Children.Remove(key);
            }
        }

        var textOnly = new KeyVisual { Content = "Windows", Style = (Style)styles["TextOnlyKeyVisualStyle"], RenderKeyAsGlyph = true, };
        var root = await this.LoadKey(textOnly);
        var presenter = (KeyCharPresenter)root.Children.Single();
        var text = (TextBlock)ApplyTemplate(presenter).FindName("KeyText");
        Check(
            text.Text == "Windows" && text.Visibility == Visibility.Visible && textOnly.BorderThickness.Left == 0,
            "The text-only style did not render the logo key as unframed text.");
    }

    private async Task NamedStylesUseValidationAndDisabledStates()
    {
        var styles = new ResourceDictionary
        {
            Source = new Uri("ms-appx:///JPSoftworks.DesktopToolkit.Controls.KeyVisual/Themes/KeyVisualStyles.xaml"),
        };
        foreach (var name in new[] { "DefaultKeyVisualStyle", "SubtleKeyVisualStyle", "AccentKeyVisualStyle" })
        {
            var key = new KeyVisual { Content = "Tab", Style = (Style)styles[name], State = KeyVisualState.Warning };
            var root = await this.LoadKey(key);
            var group = VisualStateManager.GetVisualStateGroups(root).Single();
            Check(group.CurrentState.Name == "Warning", $"{name} did not apply the initial warning.");
            key.State = KeyVisualState.Error;
            Check(group.CurrentState.Name == "Invalid", $"{name} did not update to the error state.");
            key.State = KeyVisualState.Normal;
            key.IsEnabled = false;
            Check(
                group.CurrentState.Name == "Disabled",
                $"{name} remained in {group.CurrentState.Name} after disabling (IsEnabled={key.IsEnabled}).");
            key.IsEnabled = true;
            Check(group.CurrentState.Name == "Normal", $"{name} did not return to the normal state.");
        }
    }

    private async Task ExposesKeyLabel()
    {
        var key = new KeyVisual { Content = VirtualKey.Enter, RenderKeyAsGlyph = true };
        await this.LoadKey(key);
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(key);
        Check(
            peer.GetName() == "Enter" && peer.GetAutomationControlType() == AutomationControlType.Text && !key.IsTabStop,
            "The glyph keycap did not expose a non-interactive text label.");
        key.Content = "Space";
        Check(peer.GetName() == "Space", "The accessible name did not follow the key.");
        AutomationProperties.SetName(key, "Space bar");
        Check(peer.GetName() == "Space bar", "The accessible name override was ignored.");
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(StackPanel))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    private async Task ShortcutPresenterUpdatesItsChordAndStyle()
    {
        var shortcut = new KeyboardShortcut(
            VirtualKey.Up,
            VirtualKeyModifiers.Windows | VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu | VirtualKeyModifiers.Shift);
        var presenter = new ShortcutPresenter { Shortcut = shortcut };
        await this.LoadElement(presenter);

        var keys = (StackPanel)presenter.FindName("Keys");
        var standard = (Style)presenter.Resources["DefaultKeyVisualStyle"];
        var compact = (Style)presenter.Resources["SmallKeyVisualStyle"];
        object[] expected = [VirtualKey.LeftWindows, VirtualKey.Control, VirtualKey.Menu, VirtualKey.Shift, VirtualKey.Up];
        Check(
            keys.Children.OfType<KeyVisual>().Select(key => key.Content).SequenceEqual(expected)
            && keys.Children.OfType<KeyVisual>().All(key => key.Style == standard),
            "The presenter did not render all keys in order with the default style.");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(presenter);
        Check(
            !presenter.IsTabStop
            && peer.GetName() == shortcut.ToString()
            && keys.Children.OfType<KeyVisual>().All(key => AutomationProperties.GetAccessibilityView(key) == AccessibilityView.Raw),
            "The chord was not exposed as a single passive label.");

        presenter.KeycapStyle = compact;
        presenter.Shortcut = new KeyboardShortcut(VirtualKey.N, VirtualKeyModifiers.Control);
        Check(
            keys.Children.Count == 2 && keys.Children.OfType<KeyVisual>().All(key => key.Style == compact) && peer.GetName() == "Ctrl + N",
            "The presenter did not update the chord, style, and accessible name.");
        presenter.KeycapStyle = null;
        Check(keys.Children.OfType<KeyVisual>().All(key => key.Style == standard), "Clearing the style did not restore the default.");

        presenter.Shortcut = null;
        Check(keys.Children.Count == 0 && peer.GetName().Length == 0, "Clearing the shortcut left visible or accessible keys.");
    }

    private async Task ShortcutEditorPreservesOpenHostDialog()
    {
        var shortcut = new KeyboardShortcut(VirtualKey.K, VirtualKeyModifiers.Control);
        var editor = new ShortcutEditor { Shortcut = shortcut };
        var host = new ContentDialog { XamlRoot = this._host.XamlRoot, Content = editor, CloseButtonText = "Close" };
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Opened += (_, _) => opened.TrySetResult();
        var showing = host.ShowAsync();
        try
        {
            await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await WaitUntil(() => editor.IsLoaded && editor.XamlRoot is not null);
            Check(!await editor.EditAsync(), "A second dialog reported a change.");
            Check(editor.ActiveDialog is null, "A rejected edit kept a dialog session.");
            Check(showing.Status == Windows.Foundation.AsyncStatus.Started, "The host dialog closed during the edit request.");
            Check(editor.Shortcut == shortcut, "The rejected edit changed the saved shortcut.");
        }
        finally
        {
            host.Hide();
            await showing;
        }

        host.Content = null;
        await this.LoadElement(editor);
        var edit = editor.EditAsync();
        var (dialog, _) = await GetDialog(editor);
        dialog.Hide();
        Check(!await edit.WaitAsync(TimeSpan.FromSeconds(5)), "The editor could not reopen normally.");
    }

    private async Task ShortcutEditorSavesValidDraftAndCancels()
    {
        var original = new KeyboardShortcut(VirtualKey.F1, VirtualKeyModifiers.Shift);
        var editor = new ShortcutEditor { Shortcut = original };
        await this.LoadElement(editor);

        var edit = editor.EditAsync();
        var (dialog, capture) = await GetDialog(editor);
        Check(!await editor.EditAsync(), "Opened a second dialog for the same editor.");
        Check(
            !capture.CaptureKey(VirtualKey.Tab, VirtualKeyModifiers.Shift)
            && !capture.CaptureKey(VirtualKey.Escape, VirtualKeyModifiers.None)
            && capture.Draft == original, "The capture field consumed dialog navigation.");
        Check(
            capture.CaptureKey(VirtualKey.A, VirtualKeyModifiers.None)
            && !dialog.IsPrimaryButtonEnabled
            && !await editor.TrySubmitDraftAsync(capture), "A bare key enabled Save.");

        var recorded = new KeyboardShortcut(VirtualKey.K, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu);
        Check(
            capture.CaptureKey(recorded.Key, recorded.Modifiers) && capture.Draft == recorded && dialog.IsPrimaryButtonEnabled,
            "A valid chord did not become the pending shortcut.");
        Check(editor.Shortcut == original, "Recording changed the saved shortcut.");
        dialog.Hide();
        Check(!await edit.WaitAsync(TimeSpan.FromSeconds(5)) && editor.Shortcut == original, "Cancel did not preserve the saved shortcut.");

        edit = editor.EditAsync();
        (dialog, capture) = await GetDialog(editor);
        capture.CaptureKey(recorded.Key, recorded.Modifiers);
        Check(await editor.TrySubmitDraftAsync(capture) && editor.Shortcut == recorded, "Save did not commit the recorded shortcut.");
        dialog.Hide();
        await edit.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(StackPanel))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private async Task ShortcutEditorPreviewsHeldModifiersAndValidation()
    {
        var original = new KeyboardShortcut(VirtualKey.F1, VirtualKeyModifiers.Shift);
        var editor = new ShortcutEditor { Shortcut = original, AllowClear = true };
        await this.LoadElement(editor);

        var edit = editor.EditAsync();
        var (dialog, capture) = await GetDialog(editor);
        try
        {
            var presenter = (ShortcutPresenter)capture.FindName("CapturePreview");
            var keys = (StackPanel)presenter.FindName("Keys");
            var surface = (ShortcutCaptureSurface)capture.FindName("CaptureSurface");
            var feedback = (TextBlock)capture.FindName("ValidationMessage");
            Check(
                presenter.HorizontalAlignment == HorizontalAlignment.Center
                && keys.Spacing == 8
                && AutomationProperties.GetAccessibilityView(presenter) == AccessibilityView.Raw,
                "The live preview did not use the centered, passive capture layout.");

            capture.CaptureKey(VirtualKey.LeftControl, VirtualKeyModifiers.None);
            CheckKeys(KeyVisualState.Normal, VirtualKey.Control);
            Check(
                capture.Draft == original && AutomationProperties.GetHelpText(surface) == "Ctrl",
                "Pressing a modifier changed the draft or omitted the accessible preview.");

            capture.CaptureKey(VirtualKey.RightShift, VirtualKeyModifiers.Control);
            CheckKeys(KeyVisualState.Normal, VirtualKey.Control, VirtualKey.Shift);
            capture.ReleaseKey(VirtualKey.LeftControl, VirtualKeyModifiers.Shift);
            CheckKeys(KeyVisualState.Normal, VirtualKey.Shift);
            capture.ReleaseKey(VirtualKey.RightShift, VirtualKeyModifiers.None);
            CheckKeys(KeyVisualState.Normal, VirtualKey.Shift, VirtualKey.F1);

            capture.CaptureKey(VirtualKey.A, VirtualKeyModifiers.None);
            CheckKeys(KeyVisualState.Error, VirtualKey.A);
            Check(
                !dialog.IsPrimaryButtonEnabled && feedback.Visibility == Visibility.Visible,
                "A bare key did not block Save or explain its error.");

            const VirtualKeyModifiers altGr = VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu;
            capture.CaptureKey(VirtualKey.Control, VirtualKeyModifiers.None);
            CheckKeys(KeyVisualState.Normal, VirtualKey.Control);
            Check(feedback.Visibility == Visibility.Collapsed, "The previous draft error appeared on a partial chord.");
            capture.CaptureKey(VirtualKey.K, altGr);
            CheckKeys(KeyVisualState.Warning, VirtualKey.Control, VirtualKey.Menu, VirtualKey.K);
            Check(
                dialog.IsPrimaryButtonEnabled && feedback.Text.Contains("AltGr", StringComparison.Ordinal),
                "The AltGr warning or Save action was lost.");

            capture.CaptureKey(VirtualKey.Control, VirtualKeyModifiers.None);
            capture.CaptureKey(VirtualKey.K, altGr);
            capture.ReleaseKey(VirtualKey.Control, VirtualKeyModifiers.None);
            CheckKeys(KeyVisualState.Warning, VirtualKey.Control, VirtualKey.Menu, VirtualKey.K);
            Check(feedback.Visibility == Visibility.Visible, "Repeating the same chord did not restore its warning.");

            capture.ClearDraft();
            CheckKeys(KeyVisualState.Normal);
            Check(
                AutomationProperties.GetHelpText(surface) == "No shortcut assigned",
                "Clearing the draft left a stale accessible shortcut.");

            void CheckKeys(KeyVisualState state, params object[] expected)
            {
                var keycaps = keys.Children.OfType<KeyVisual>();
                Check(
                    keycaps.Select(key => key.Content).SequenceEqual(expected)
                    && keycaps.All(key => key.State == state && key.MinHeight == 48 && key.MinWidth == 48 && key.FontSize == 18),
                    "The presenter did not render the expected keys with the capture size and state.");
            }
        }
        finally
        {
            dialog.Hide();
            await edit.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(StackPanel))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    private async Task ShortcutEditorResetsAndClears()
    {
        var defaultShortcut = new KeyboardShortcut(VirtualKey.K, VirtualKeyModifiers.Windows | VirtualKeyModifiers.Control);
        var original = new KeyboardShortcut(VirtualKey.Space, VirtualKeyModifiers.Control);
        var editor = new ShortcutEditor
        {
            Shortcut = original, DefaultShortcut = defaultShortcut, AllowClear = true, IncludeWindowsKey = true,
        };
        await this.LoadElement(editor);

        var accent = (Style)editor.Resources["AccentKeyVisualStyle"];
        var neutral = (Style)editor.Resources["DefaultKeyVisualStyle"];
        var preview = (ShortcutPresenter)editor.FindName("AssignedPreview");
        var previewKeys = (StackPanel)preview.FindName("Keys");
        Check(
            preview.Shortcut == original && AutomationProperties.GetAccessibilityView(preview) == AccessibilityView.Raw,
            "The editor did not give its presenter the assigned shortcut or keep the button's accessible label.");
        Check(
            previewKeys.Children.Count == 2 && previewKeys.Children.OfType<KeyVisual>().All(key => key.Style == accent),
            "The assigned shortcut did not use accent keycaps by default.");
        editor.PreviewKeycapStyle = neutral;
        Check(
            previewKeys.Children.Count == 2 && previewKeys.Children.OfType<KeyVisual>().All(key => key.Style == neutral),
            "The assigned shortcut did not use the selected preview style.");
        editor.PreviewKeycapStyle = null;
        Check(
            previewKeys.Children.OfType<KeyVisual>().All(key => key.Style == accent),
            "Clearing the preview style did not restore the accent style.");
        editor.PreviewKeycapStyle = neutral;

        var edit = editor.EditAsync();
        var (dialog, capture) = await GetDialog(editor);
        capture.ResetDraft();
        Check(capture.Draft == defaultShortcut, "Reset did not stage the default shortcut.");
        capture.CaptureKey(VirtualKey.A, VirtualKeyModifiers.None);
        Check(
            capture.Draft == new KeyboardShortcut(VirtualKey.A, VirtualKeyModifiers.Windows),
            "The developer's Windows modifier option was not applied to the recorded key.");
        capture.ClearDraft();
        Check(
            capture.Draft is null && editor.Shortcut == original && dialog.IsPrimaryButtonEnabled,
            "Clearing should stage a valid unassigned shortcut.");
        Check(
            await editor.TrySubmitDraftAsync(capture)
            && editor.Shortcut is null
            && preview.Shortcut is null
            && previewKeys.Children.Count == 0, "Saving a cleared shortcut did not unassign it.");
        dialog.Hide();
        await edit.WaitAsync(TimeSpan.FromSeconds(5));

        editor.IncludeWindowsKey = false;
        edit = editor.EditAsync();
        (dialog, capture) = await GetDialog(editor);
        capture.ResetDraft();
        capture.CaptureKey(VirtualKey.A, VirtualKeyModifiers.Control);
        Check(
            capture.Draft == new KeyboardShortcut(VirtualKey.A, VirtualKeyModifiers.Control),
            "The Windows modifier leaked from the default shortcut into a new recording.");
        dialog.Hide();
        await edit.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private async Task ShortcutEditorAppliesCallerValidation()
    {
        var original = new KeyboardShortcut(VirtualKey.F1, VirtualKeyModifiers.Shift);
        var reserved = new KeyboardShortcut(VirtualKey.P, VirtualKeyModifiers.Control);
        var lockShortcut = new KeyboardShortcut(VirtualKey.L, VirtualKeyModifiers.Control);
        var conflictChecks = 0;
        var restriction = "Reserved for printing.";
        var editor = new ShortcutEditor
        {
            Shortcut = original,
            DefaultShortcut = reserved,
            AllowClear = true,
            ValidateShortcut = shortcut => shortcut == reserved ? restriction : null,
            FindConflict = shortcut =>
            {
                conflictChecks++;
                return shortcut == lockShortcut ? new ShortcutConflict("lock", "Lock workspace") : null;
            },
        };
        await this.LoadElement(editor);

        var edit = editor.EditAsync();
        var (dialog, capture) = await GetDialog(editor);
        capture.ResetDraft();
        Check(
            !dialog.IsPrimaryButtonEnabled && Feedback(capture) == restriction && conflictChecks == 0,
            "A restricted default reached conflict lookup or enabled Save.");
        capture.CaptureKey(lockShortcut.Key, lockShortcut.Modifiers);
        Check(
            !dialog.IsPrimaryButtonEnabled && Feedback(capture).Contains("Lock workspace", StringComparison.Ordinal),
            "A non-replaceable assignment did not show its owner and block Save.");

        var available = new KeyboardShortcut(VirtualKey.K, VirtualKeyModifiers.Control);
        capture.CaptureKey(available.Key, available.Modifiers);
        Check(dialog.IsPrimaryButtonEnabled && dialog.PrimaryButtonText == "Save", "A free chord could not be saved.");
        editor.ValidateShortcut = _ => restriction;
        Check(
            !await editor.TrySubmitDraftAsync(capture) && editor.Shortcut == original && !dialog.IsPrimaryButtonEnabled,
            "A restriction added after recording did not block Save.");

        var checksBeforeClear = conflictChecks;
        capture.ClearDraft();
        Check(
            await editor.TrySubmitDraftAsync(capture) && editor.Shortcut is null && conflictChecks == checksBeforeClear,
            "Clearing the assignment did not bypass the checks.");
        dialog.Hide();
        await edit.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private async Task ShortcutEditorIgnoresObsoleteValidation()
    {
        var validations = new List<(CancellationToken Token, TaskCompletionSource<string?> Result)>();
        var conflictChecks = new List<KeyboardShortcut>();
        var current = new KeyboardShortcut(VirtualKey.J, VirtualKeyModifiers.Control);
        KeyboardShortcut[] expectedConflictChecks = [current];
        var editor = new ShortcutEditor
        {
            ValidateShortcut = _ => "The synchronous validator should not be used.",
            ValidateShortcutAsync = (_, token) =>
            {
                var result = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                validations.Add((token, result));
                return result.Task;
            },
            FindConflict = shortcut =>
            {
                conflictChecks.Add(shortcut);
                return null;
            },
        };
        await this.LoadElement(editor);

        var edit = editor.EditAsync();
        var (dialog, capture) = await GetDialog(editor);
        capture.CaptureKey(VirtualKey.P, VirtualKeyModifiers.Control);
        capture.CaptureKey(VirtualKey.LeftShift, VirtualKeyModifiers.None);
        Check(
            validations.Count == 1 && !validations[0].Token.IsCancellationRequested,
            "Previewing a held modifier interrupted validation of the staged chord.");
        capture.ReleaseKey(VirtualKey.LeftShift, VirtualKeyModifiers.None);
        capture.CaptureKey(VirtualKey.L, VirtualKeyModifiers.Control);
        capture.CaptureKey(current.Key, current.Modifiers);
        Check(
            validations.Count == 3
            && validations[0].Token.IsCancellationRequested
            && validations[1].Token.IsCancellationRequested
            && !dialog.IsPrimaryButtonEnabled
            && conflictChecks.Count == 0, "Pending validation did not cancel older checks or defer conflict lookup.");

        validations[2].Result.SetResult(null);
        await WaitUntil(() => dialog.IsPrimaryButtonEnabled);
        validations[0].Result.SetResult("Obsolete restriction.");
        validations[1].Result.SetResult(null);
        await Task.Yield();
        Check(
            Feedback(capture).Length == 0 && dialog.IsPrimaryButtonEnabled && conflictChecks.SequenceEqual(expectedConflictChecks),
            "An obsolete validation affected the current draft or started a conflict check.");

        var submit = editor.TrySubmitDraftAsync(capture);
        Check(validations.Count == 4 && !dialog.IsPrimaryButtonEnabled, "Submission did not wait for validation.");
        const string restricted = "This shortcut is now restricted.";
        validations[3].Result.SetResult(restricted);
        Check(
            !await submit.WaitAsync(TimeSpan.FromSeconds(5))
            && editor.Shortcut is null
            && Feedback(capture) == restricted
            && conflictChecks.SequenceEqual(expectedConflictChecks), "The submission restriction was ignored or allowed a conflict check.");

        capture.CaptureKey(VirtualKey.P, VirtualKeyModifiers.Control);
        dialog.Hide();
        Check(
            !await edit.WaitAsync(TimeSpan.FromSeconds(5)) && validations[4].Token.IsCancellationRequested,
            "Closing the dialog did not cancel validation.");
        validations[4].Result.SetResult(null);
    }

    private async Task ShortcutEditorReplacesOnlyTheOfferedAssignment()
    {
        var original = new KeyboardShortcut(VirtualKey.F1, VirtualKeyModifiers.Shift);
        var requested = new KeyboardShortcut(VirtualKey.K, VirtualKeyModifiers.Control);
        var ownerId = "first";
        var replacementAttempts = 0;
        var succeeds = false;
        var editor = new ShortcutEditor
        {
            Shortcut = original,
            FindConflict = _ => new ShortcutConflict(
                ownerId, "Open search", () =>
                {
                    replacementAttempts++;
                    return succeeds;
                }),
        };
        await this.LoadElement(editor);

        var edit = editor.EditAsync();
        var (dialog, capture) = await GetDialog(editor);
        capture.CaptureKey(requested.Key, requested.Modifiers);
        Check(
            dialog.IsPrimaryButtonEnabled
            && dialog.PrimaryButtonText == "Replace"
            && Feedback(capture).Contains("Open search", StringComparison.Ordinal)
            && replacementAttempts == 0, "Recording a conflicting chord did not offer an explicit replacement.");
        dialog.Hide();
        Check(
            !await edit.WaitAsync(TimeSpan.FromSeconds(5)) && editor.Shortcut == original && replacementAttempts == 0,
            "Closing the dialog attempted to replace the existing assignment.");

        edit = editor.EditAsync();
        (dialog, capture) = await GetDialog(editor);
        capture.CaptureKey(requested.Key, requested.Modifiers);
        ownerId = "second";
        Check(
            !await editor.TrySubmitDraftAsync(capture) && replacementAttempts == 0 && editor.Shortcut == original,
            "The editor replaced an assignment other than the one it offered.");
        Check(
            !await editor.TrySubmitDraftAsync(capture)
            && replacementAttempts == 1
            && editor.Shortcut == original
            && Feedback(capture).Contains("Could not replace", StringComparison.Ordinal),
            "A failed replacement changed the saved shortcut or provided no feedback.");
        succeeds = true;
        Check(
            await editor.TrySubmitDraftAsync(capture) && editor.Shortcut == requested && replacementAttempts == 2,
            "A successful replacement did not commit the requested chord.");
        dialog.Hide();
        await edit.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private async Task ShortcutEditorIgnoresObsoleteConflictLookups()
    {
        var lookups = new List<(CancellationToken Token, TaskCompletionSource<ShortcutConflict?> Result)>();
        var editor = new ShortcutEditor
        {
            FindConflictAsync = (_, token) =>
            {
                var result = new TaskCompletionSource<ShortcutConflict?>(TaskCreationOptions.RunContinuationsAsynchronously);
                lookups.Add((token, result));
                return result.Task;
            },
        };
        await this.LoadElement(editor);

        var edit = editor.EditAsync();
        var (dialog, capture) = await GetDialog(editor);
        capture.CaptureKey(VirtualKey.K, VirtualKeyModifiers.Control);
        capture.CaptureKey(VirtualKey.J, VirtualKeyModifiers.Control);
        capture.CaptureKey(VirtualKey.K, VirtualKeyModifiers.Control);
        Check(
            lookups.Count == 3
            && lookups[0].Token.IsCancellationRequested
            && lookups[1].Token.IsCancellationRequested
            && !dialog.IsPrimaryButtonEnabled
            && Feedback(capture).Contains("Checking shortcut", StringComparison.Ordinal),
            "Changing the draft did not cancel the obsolete lookups or disable Save while checking.");

        lookups[2].Result.SetResult(null);
        await WaitUntil(() => dialog.IsPrimaryButtonEnabled);
        var submission = editor.TrySubmitDraftAsync(capture);
        capture.CaptureKey(VirtualKey.L, VirtualKeyModifiers.Control);
        Check(
            !await submission.WaitAsync(TimeSpan.FromSeconds(5)) && lookups[3].Token.IsCancellationRequested && editor.Shortcut is null,
            "A stale submission was allowed to save.");

        lookups[4].Result.SetResult(null);
        lookups[0].Result.SetResult(new ShortcutConflict("old-k", "Old K owner"));
        lookups[1].Result.SetResult(new ShortcutConflict("old-j", "Old J owner"));
        lookups[3].Result.SetResult(new ShortcutConflict("old-submit", "Old submission owner"));
        await WaitUntil(() => dialog.IsPrimaryButtonEnabled);
        Check(
            dialog.PrimaryButtonText == "Save" && Feedback(capture).Length == 0,
            "A late response overwrote the current shortcut's result.");

        capture.CaptureKey(VirtualKey.P, VirtualKeyModifiers.Control);
        dialog.Hide();
        Check(
            !await edit.WaitAsync(TimeSpan.FromSeconds(5)) && lookups[5].Token.IsCancellationRequested,
            "Closing the dialog did not cancel its pending lookup.");
        lookups[5].Result.SetResult(null);
    }

    private async Task ShortcutEditorAwaitsReplacementWithoutChangingTheDraft()
    {
        var requested = new KeyboardShortcut(VirtualKey.K, VirtualKeyModifiers.Control);
        var original = new KeyboardShortcut(VirtualKey.Space, VirtualKeyModifiers.Control);
        var replacement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var editor = new ShortcutEditor
        {
            Shortcut = original,
            FindConflictAsync = (_, _) => Task.FromResult<ShortcutConflict?>(
                new ShortcutConflict("search", "Open search")
                {
                    TryReplaceAsync = () =>
                    {
                        attempts++;
                        return replacement.Task;
                    },
                }),
        };
        await this.LoadElement(editor);

        var edit = editor.EditAsync();
        var (dialog, capture) = await GetDialog(editor);
        capture.CaptureKey(requested.Key, requested.Modifiers);
        Check(
            dialog.PrimaryButtonText == "Replace" && dialog.IsPrimaryButtonEnabled && attempts == 0,
            "An asynchronous replacement was not offered.");

        var submission = editor.TrySubmitDraftAsync(capture);
        Check(
            attempts == 1
            && !submission.IsCompleted
            && !dialog.IsPrimaryButtonEnabled
            && !capture.IsEnabled
            && !capture.CaptureKey(VirtualKey.J, VirtualKeyModifiers.Control)
            && editor.Shortcut == original
            && Feedback(capture).Contains("Replacing", StringComparison.Ordinal),
            "The editor did not hold the confirmed draft while awaiting replacement.");
        Check(!await editor.TrySubmitDraftAsync(capture) && attempts == 1, "The editor started a duplicate replacement.");
        replacement.SetResult(true);
        Check(
            await submission.WaitAsync(TimeSpan.FromSeconds(5)) && capture.IsEnabled && editor.Shortcut == requested,
            "The successful asynchronous replacement did not commit the confirmed draft.");
        dialog.Hide();
        await edit.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private static string Feedback(ShortcutCaptureView capture)
    {
        return ((TextBlock)capture.FindName("ValidationMessage")).Text;
    }

    private static async Task<(ContentDialog Dialog, ShortcutCaptureView Capture)> GetDialog(ShortcutEditor editor)
    {
        var dialog = editor.ActiveDialog ?? throw new InvalidOperationException("The editor dialog did not open.");
        var capture = (ShortcutCaptureView)dialog.Content;
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.Opened += OnOpened;
        try
        {
            await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return (dialog, capture);
        }
        finally
        {
            dialog.Opened -= OnOpened;
        }

        void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            opened.TrySetResult();
        }
    }

    private async Task<Grid> LoadKey(KeyVisual key)
    {
        await this.LoadElement(key);
        return ApplyTemplate(key);
    }

    private async Task LoadElement(FrameworkElement element)
    {
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        element.Loaded += OnLoaded;
        this._host.Children.Add(element);
        await loaded.Task.WaitAsync(TimeSpan.FromSeconds(5));

        void OnLoaded(object sender, RoutedEventArgs args)
        {
            element.Loaded -= OnLoaded;
            loaded.TrySetResult();
        }
    }

    private static Grid ApplyTemplate(Control control)
    {
        control.ApplyTemplate();
        return (Grid)VisualTreeHelper.GetChild(control, 0);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
