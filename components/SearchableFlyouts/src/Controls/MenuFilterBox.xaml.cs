// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.UI.Content;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Core;
using VirtualKey = Windows.System.VirtualKey;

namespace JPSoftworks.DesktopToolkit.Controls;

internal static class MenuFilterBox
{
    private static readonly ConditionalWeakTable<TextBox, FilterState> States = new();

    internal static void SetKeyboardHelp(this TextBox editor, string help)
    {
        State(editor).SetKeyboardHelp(help);
    }

    internal static void SetResultStatus(this TextBox editor, string status)
    {
        State(editor).SetResultStatus(status);
    }

    internal static void CancelAnnouncements(this TextBox editor)
    {
        if (States.TryGetValue(editor, out var state))
        {
            state.CancelAnnouncements();
        }
    }

    private static FilterState State(TextBox editor)
    {
        return States.GetValue(editor, value => new FilterState(value));
    }

    internal static void FocusForEditing(this TextBox editor)
    {
        var start = editor.SelectionStart;
        var length = editor.SelectionLength;
        editor.Focus(FocusState.Keyboard);
        editor.Select(start, length);
    }

    internal static void InsertCharacter(this TextBox editor, char character)
    {
        editor.FocusForEditing();
        var caret = editor.SelectionStart;
        editor.SelectedText = character.ToString();
        editor.Select(caret + 1, 0);
    }

    internal static bool HandleListEditingKey(this TextBox editor, KeyRoutedEventArgs args)
    {
        var control = IsKeyDown(VirtualKey.Control) && !IsKeyDown(VirtualKey.Menu);
        if (args.Key is VirtualKey.Back or VirtualKey.Delete)
        {
            Edit(editor, args.Key == VirtualKey.Back, control);
        }
        else if (control && args.Key is VirtualKey.A or VirtualKey.V or VirtualKey.F)
        {
            editor.FocusForEditing();
            if (args.Key == VirtualKey.V)
            {
                editor.PasteFromClipboard();
            }
            else
            {
                editor.SelectAll();
            }
        }
        else
        {
            return false;
        }

        args.Handled = true;
        return true;
    }

    internal static bool IsKeyDown(VirtualKey key)
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
    }

    private static void Edit(TextBox editor, bool backwards, bool wholeWord)
    {
        editor.FocusForEditing();
        var text = editor.Text;
        var start = editor.SelectionStart;
        var end = start + editor.SelectionLength;
        if (start == end)
        {
            if (backwards && start > 0)
            {
                if (wholeWord)
                {
                    while (start > 0 && char.IsWhiteSpace(text[start - 1]))
                    {
                        start--;
                    }

                    while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
                    {
                        start--;
                    }
                }
                else
                {
                    start = StringInfo.ParseCombiningCharacters(text).Last(index => index < start);
                }
            }
            else if (!backwards && end < text.Length)
            {
                if (wholeWord)
                {
                    while (end < text.Length && !char.IsWhiteSpace(text[end]))
                    {
                        end++;
                    }

                    while (end < text.Length && char.IsWhiteSpace(text[end]))
                    {
                        end++;
                    }
                }
                else
                {
                    end = StringInfo.ParseCombiningCharacters(text).FirstOrDefault(index => index > end, text.Length);
                }
            }
        }

        editor.Select(start, end - start);
        editor.SelectedText = string.Empty;
        editor.Select(start, 0);
    }

    private sealed class FilterState
    {
        private readonly TextBox _editor;
        private readonly DispatcherQueueTimer _timer;
        private ContentIsland? _pointerIsland;
        private InputPointerSource? _pointerSource;
        private InputCursor? _previousCursor;
        private InputSystemCursor? _editingCursor;
        private bool _cursorRefreshQueued;
        private string _keyboardHelp = string.Empty;
        private string _status = string.Empty;
        private string _announcedStatus = string.Empty;

        internal FilterState(TextBox editor)
        {
            this._editor = editor;
            this._timer = editor.DispatcherQueue.CreateTimer();
            this._timer.Interval = TimeSpan.FromMilliseconds(350);
            this._timer.IsRepeating = false;
            this._timer.Tick += this.OnTick;
            editor.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(this.OnPointerMoved), true);
            editor.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(this.OnPointerMoved), true);
            editor.PointerExited += (_, _) => this.RestoreCursor();
            editor.Unloaded += (_, _) =>
            {
                this.CancelAnnouncements();
                this.RestoreCursor();
            };
        }

        [WinRT.DynamicWindowsRuntimeCast(typeof(DependencyObject))]
        [WinRT.DynamicWindowsRuntimeCast(typeof(ButtonBase))]
        private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            for (var child = args.OriginalSource as DependencyObject;
                 child is not null && child != this._editor;
                 child = VisualTreeHelper.GetParent(child))
            {
                if (child is ButtonBase)
                {
                    this.RestoreCursor();
                    return;
                }
            }

            var island = ContentIsland.GetByVisual(ElementCompositionPreview.GetElementVisual(this._editor));
            if (island is null || island.IsClosed)
            {
                return;
            }

            var source = InputPointerSource.GetForIsland(island);
            if (this._pointerSource != source)
            {
                this.RestoreCursor();
                this._pointerIsland = island;
                this._pointerSource = source;
                this._previousCursor = source.Cursor;
                this._editingCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
            }

            // Windowed popups do not inherit the text editor's cursor.
            source.Cursor = this._editingCursor;
            if (!this._cursorRefreshQueued)
            {
                this._cursorRefreshQueued = this._editor.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, this.RefreshCursor);
            }
        }

        private void RefreshCursor()
        {
            this._cursorRefreshQueued = false;
            if (this._pointerIsland is { IsClosed: false } && this._pointerSource is { } source && this._editingCursor is { } cursor)
            {
                source.Cursor = cursor;
            }
        }

        private void RestoreCursor()
        {
            if (this._pointerIsland is { IsClosed: false } && this._pointerSource is { } source && source.Cursor == this._editingCursor)
            {
                source.Cursor = this._previousCursor;
            }

            this._editingCursor?.Dispose();
            this._editingCursor = null;
            this._previousCursor = null;
            this._pointerSource = null;
            this._pointerIsland = null;
        }

        internal void SetKeyboardHelp(string help)
        {
            this._keyboardHelp = help;
            this.UpdateHelpText();
        }

        internal void SetResultStatus(string status)
        {
            this._status = status;
            AutomationProperties.SetItemStatus(this._editor, status);
            this.UpdateHelpText();
            this._timer.Stop();
            this._timer.Start();
        }

        internal void CancelAnnouncements()
        {
            this._timer.Stop();
            this._announcedStatus = string.Empty;
        }

        private void UpdateHelpText()
        {
            AutomationProperties.SetHelpText(
                this._editor, string.IsNullOrEmpty(this._status) ? this._keyboardHelp : $"{this._keyboardHelp} {this._status}".Trim());
        }

        private void OnTick(DispatcherQueueTimer sender, object args)
        {
            this._timer.Stop();
            if (!this._editor.IsLoaded || this._editor.FocusState == FocusState.Unfocused || this._status == this._announcedStatus)
            {
                return;
            }

            this._announcedStatus = this._status;
            FrameworkElementAutomationPeer.CreatePeerForElement(this._editor)?.RaiseNotificationEvent(
                AutomationNotificationKind.Other, AutomationNotificationProcessing.CurrentThenMostRecent, this._status, "SearchResults");
        }
    }
}
