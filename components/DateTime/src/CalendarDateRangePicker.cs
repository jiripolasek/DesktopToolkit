// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Presents a flyout for editing and committing an inclusive date range.</summary>
[TemplateVisualState(Name = "Unselected", GroupName = "SelectionStates")]
[TemplateVisualState(Name = "UnselectedDisabled", GroupName = "SelectionStates")]
[TemplateVisualState(Name = "Selected", GroupName = "SelectionStates")]
[TemplatePart(Name = "PART_Header", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Button", Type = typeof(DropDownButton))]
[TemplatePart(Name = "PART_StartCalendar", Type = typeof(CalendarView))]
[TemplatePart(Name = "PART_EndCalendar", Type = typeof(CalendarView))]
[TemplatePart(Name = "PART_StartDate", Type = typeof(CalendarDatePicker))]
[TemplatePart(Name = "PART_EndDate", Type = typeof(CalendarDatePicker))]
[TemplatePart(Name = "PART_Presets", Type = typeof(ComboBox))]
[TemplatePart(Name = "PART_FlyoutHeader", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_FlyoutFooter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_RangeSummary", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Error", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Apply", Type = typeof(Button))]
[TemplatePart(Name = "PART_Cancel", Type = typeof(Button))]
public sealed partial class CalendarDateRangePicker : Control
{
    /// <summary>Occurs when the draft is reset from the committed selection.</summary>
    public event EventHandler? DraftReset;

    /// <summary>Occurs when the valid draft range changes or becomes invalid.</summary>
    public event EventHandler? DraftRangeChanged;

    /// <summary>Occurs after Apply validates the draft, before a selection is committed.</summary>
    public event EventHandler<DateRangeSelectionApplyingEventArgs>? SelectionApplying;

    /// <summary>Occurs when the committed selection changes through input or binding.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Identifies the <see cref="Selection"/> dependency property.</summary>
    public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(
        nameof(Selection), typeof(DateRangeSelection), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnSelectionChanged));

    /// <summary>Identifies the <see cref="MinimumDate"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumDateProperty = DependencyProperty.Register(
        nameof(MinimumDate), typeof(DateTimeOffset), typeof(CalendarDateRangePicker),
        new PropertyMetadata(new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero), OnOptionsChanged));

    /// <summary>Identifies the <see cref="MaximumDate"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumDateProperty = DependencyProperty.Register(
        nameof(MaximumDate), typeof(DateTimeOffset), typeof(CalendarDateRangePicker),
        new PropertyMetadata(new DateTimeOffset(2100, 12, 31, 0, 0, 0, TimeSpan.Zero), OnOptionsChanged));

    /// <summary>Identifies the <see cref="BlackoutDates"/> dependency property.</summary>
    public static readonly DependencyProperty BlackoutDatesProperty = DependencyProperty.Register(
        nameof(BlackoutDates), typeof(DateTimeOffset[]), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnBlackoutOptionsChanged));

    /// <summary>Identifies the <see cref="BlackoutMode"/> dependency property.</summary>
    public static readonly DependencyProperty BlackoutModeProperty = DependencyProperty.Register(
        nameof(BlackoutMode), typeof(DateRangeBlackoutMode), typeof(CalendarDateRangePicker),
        new PropertyMetadata(DateRangeBlackoutMode.RejectRange, OnBlackoutOptionsChanged));

    /// <summary>Identifies the <see cref="Presets"/> dependency property.</summary>
    public static readonly DependencyProperty PresetsProperty = DependencyProperty.Register(
        nameof(Presets), typeof(DateRangePreset[]), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnOptionsChanged));

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnTextChanged));

    /// <summary>Identifies the <see cref="FlyoutHeader"/> dependency property.</summary>
    public static readonly DependencyProperty FlyoutHeaderProperty = DependencyProperty.Register(
        nameof(FlyoutHeader), typeof(object), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnFlyoutContentChanged));

    /// <summary>Identifies the <see cref="FlyoutFooter"/> dependency property.</summary>
    public static readonly DependencyProperty FlyoutFooterProperty = DependencyProperty.Register(
        nameof(FlyoutFooter), typeof(object), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnFlyoutContentChanged));

    /// <summary>Identifies the <see cref="CalendarViewStyle"/> dependency property.</summary>
    public static readonly DependencyProperty CalendarViewStyleProperty = DependencyProperty.Register(
        nameof(CalendarViewStyle), typeof(Style), typeof(CalendarDateRangePicker), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="DateFormat"/> dependency property.</summary>
    public static readonly DependencyProperty DateFormatProperty = DependencyProperty.Register(
        nameof(DateFormat), typeof(string), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnDateFormatChanged));

    /// <summary>Identifies the <see cref="PlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(CalendarDateRangePicker),
        new PropertyMetadata(null, OnTextChanged));

    /// <summary>Identifies the <see cref="PresetPlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty PresetPlaceholderTextProperty = DependencyProperty.Register(
        nameof(PresetPlaceholderText), typeof(string), typeof(CalendarDateRangePicker),
        new PropertyMetadata(null, OnTextChanged));

    /// <summary>Identifies the <see cref="Strings"/> dependency property.</summary>
    public static readonly DependencyProperty StringsProperty = DependencyProperty.Register(
        nameof(Strings), typeof(CalendarDateRangePickerStrings), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnStringsChanged));

    /// <summary>Identifies the <see cref="ApplyButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty ApplyButtonTextProperty = DependencyProperty.Register(
        nameof(ApplyButtonText), typeof(string), typeof(CalendarDateRangePicker),
        new PropertyMetadata(null, OnTextChanged));

    /// <summary>Identifies the <see cref="CancelButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty CancelButtonTextProperty = DependencyProperty.Register(
        nameof(CancelButtonText), typeof(string), typeof(CalendarDateRangePicker),
        new PropertyMetadata(null, OnTextChanged));

    /// <summary>Identifies the <see cref="EffectivePlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty EffectivePlaceholderTextProperty = DependencyProperty.Register(
        nameof(EffectivePlaceholderText), typeof(string), typeof(CalendarDateRangePicker),
        new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="EffectivePresetPlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty EffectivePresetPlaceholderTextProperty = DependencyProperty.Register(
        nameof(EffectivePresetPlaceholderText), typeof(string), typeof(CalendarDateRangePicker),
        new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="EffectiveApplyButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty EffectiveApplyButtonTextProperty = DependencyProperty.Register(
        nameof(EffectiveApplyButtonText), typeof(string), typeof(CalendarDateRangePicker),
        new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="EffectiveCancelButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty EffectiveCancelButtonTextProperty = DependencyProperty.Register(
        nameof(EffectiveCancelButtonText), typeof(string), typeof(CalendarDateRangePicker),
        new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="RangeHighlightBrush"/> dependency property.</summary>
    public static readonly DependencyProperty RangeHighlightBrushProperty = DependencyProperty.Register(
        nameof(RangeHighlightBrush), typeof(Brush), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnRangeAppearanceChanged));

    /// <summary>Identifies the <see cref="RangeEndpointBrush"/> dependency property.</summary>
    public static readonly DependencyProperty RangeEndpointBrushProperty = DependencyProperty.Register(
        nameof(RangeEndpointBrush), typeof(Brush), typeof(CalendarDateRangePicker), new PropertyMetadata(null, OnRangeAppearanceChanged));

    private readonly Dictionary<CalendarViewDayItem, DayItemStyle> _dayItems = [];

    private readonly Dictionary<CalendarView, (long StyleToken, Windows.Globalization.DayOfWeek? RegionalWeekStart)> _calendarWeekStarts =
        [];

    private DateTimeOffset[] _blackoutDays = [];

    private TextBlock? _header;
    private DropDownButton? _button;
    private FlyoutBase? _flyout;
    private CalendarView? _startCalendar;
    private CalendarView? _endCalendar;
    private bool _settingDraft;
    private DateTimeOffset? _highlightStart;
    private DateTimeOffset? _highlightEnd;
    private CalendarDatePicker? _start;
    private CalendarDatePicker? _end;
    private ComboBox? _presets;
    private long _presetHeaderToken;
    private ContentPresenter? _flyoutHeader;
    private ContentPresenter? _flyoutFooter;
    private TextBlock? _rangeSummary;
    private TextBlock? _error;
    private Func<CalendarDateRangePickerStrings, string>? _localizedError;
    private Button? _apply;
    private Button? _cancel;
    private DateRangePreset[] _visiblePresets = [];

    /// <summary>Gets or sets the committed selection. Null clears the selection.</summary>
    public DateRangeSelection? Selection
    {
        get => (DateRangeSelection?)this.GetValue(SelectionProperty);
        set => this.SetValue(SelectionProperty, value);
    }

    /// <summary>Gets the current draft range, or null if the draft is incomplete or invalid.</summary>
    public DateRange? DraftRange { get; private set; }

    /// <summary>Gets or sets the earliest date accepted when applying a draft.</summary>
    public DateTimeOffset MinimumDate
    {
        get => (DateTimeOffset)this.GetValue(MinimumDateProperty);
        set => this.SetValue(MinimumDateProperty, value);
    }

    /// <summary>Gets or sets the latest date accepted when applying a draft.</summary>
    public DateTimeOffset MaximumDate
    {
        get => (DateTimeOffset)this.GetValue(MaximumDateProperty);
        set => this.SetValue(MaximumDateProperty, value);
    }

    /// <summary>Gets or sets the dates unavailable as endpoints. Replace the array to update the dates.</summary>
    public DateTimeOffset[]? BlackoutDates
    {
        get => (DateTimeOffset[]?)this.GetValue(BlackoutDatesProperty);
        set => this.SetValue(BlackoutDatesProperty, value);
    }

    /// <summary>Gets or sets the treatment of blackout dates within a range. The default is RejectRange.</summary>
    public DateRangeBlackoutMode BlackoutMode
    {
        get => (DateRangeBlackoutMode)this.GetValue(BlackoutModeProperty);
        set => this.SetValue(BlackoutModeProperty, value);
    }

    /// <summary>Gets or sets the presets. Replace the array to update the list.</summary>
    public DateRangePreset[]? Presets
    {
        get => (DateRangePreset[]?)this.GetValue(PresetsProperty);
        set => this.SetValue(PresetsProperty, value);
    }

    /// <summary>Gets or sets the optional label above the date range.</summary>
    public string? Header
    {
        get => (string?)this.GetValue(HeaderProperty);
        set => this.SetValue(HeaderProperty, value);
    }

    /// <summary>Gets or sets optional content above the flyout presets and calendars.</summary>
    public object? FlyoutHeader
    {
        get => this.GetValue(FlyoutHeaderProperty);
        set => this.SetValue(FlyoutHeaderProperty, value);
    }

    /// <summary>Gets or sets optional content below the flyout range summary.</summary>
    public object? FlyoutFooter
    {
        get => this.GetValue(FlyoutFooterProperty);
        set => this.SetValue(FlyoutFooterProperty, value);
    }

    /// <summary>Gets or sets the style used by both calendars in the default flyout.</summary>
    public Style? CalendarViewStyle
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
        get => (Style?)this.GetValue(CalendarViewStyleProperty);
        set => this.SetValue(CalendarViewStyleProperty, value);
    }

    /// <summary>Gets or sets the <see cref="DateOnly"/> format for dates on the button and in the flyout summary.</summary>
    /// <remarks>Use "O" for ISO 8601 dates. An unset or invalid format uses the localized date formats.</remarks>
    public string? DateFormat
    {
        get => (string?)this.GetValue(DateFormatProperty);
        set => WinUIResourceStrings.SetTextValue(this, DateFormatProperty, value);
    }

    /// <summary>Gets or sets the text displayed when no range is selected.</summary>
    /// <remarks>An empty string hides the prompt. Null or whitespace-only text uses <see cref="Strings"/>.</remarks>
    [AllowNull]
    public string PlaceholderText
    {
        get => WinUIResourceStrings.GetPlaceholderOverride((string?)this.GetValue(PlaceholderTextProperty))
            ?? this.Strings.PlaceholderText;
        set => WinUIResourceStrings.SetTextValue(this, PlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets the prompt displayed when no preset is selected.</summary>
    /// <remarks>An empty string hides the prompt. Null or whitespace-only text uses <see cref="Strings"/>.</remarks>
    [AllowNull]
    public string PresetPlaceholderText
    {
        get => WinUIResourceStrings.GetPlaceholderOverride((string?)this.GetValue(PresetPlaceholderTextProperty))
            ?? this.Strings.PresetPlaceholderText;
        set => WinUIResourceStrings.SetTextValue(this, PresetPlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets the prompts, labels, summaries, and feedback strings.</summary>
    public CalendarDateRangePickerStrings Strings
    {
        get => (CalendarDateRangePickerStrings?)this.GetValue(StringsProperty) ?? CalendarDateRangePickerStrings.Default;
        set => this.SetValue(StringsProperty, value);
    }

    /// <summary>Gets or sets the text for the button that commits the selection.</summary>
    [AllowNull]
    public string ApplyButtonText
    {
        get => CalendarDateRangePickerStrings.WithFallback((string?)this.GetValue(ApplyButtonTextProperty), this.Strings.ApplyButtonText);
        set => WinUIResourceStrings.SetTextValue(this, ApplyButtonTextProperty, value);
    }

    /// <summary>Gets or sets the text for the button that discards draft edits.</summary>
    [AllowNull]
    public string CancelButtonText
    {
        get => CalendarDateRangePickerStrings.WithFallback((string?)this.GetValue(CancelButtonTextProperty), this.Strings.CancelButtonText);
        set => WinUIResourceStrings.SetTextValue(this, CancelButtonTextProperty, value);
    }

    /// <summary>Gets the range prompt for templates, including localized fallbacks.</summary>
    public string EffectivePlaceholderText => (string)this.GetValue(EffectivePlaceholderTextProperty);

    /// <summary>Gets the preset prompt for templates, including localized fallbacks.</summary>
    public string EffectivePresetPlaceholderText => (string)this.GetValue(EffectivePresetPlaceholderTextProperty);

    /// <summary>Gets the Apply label for templates, including localized fallbacks.</summary>
    public string EffectiveApplyButtonText => (string)this.GetValue(EffectiveApplyButtonTextProperty);

    /// <summary>Gets the Cancel label for templates, including localized fallbacks.</summary>
    public string EffectiveCancelButtonText => (string)this.GetValue(EffectiveCancelButtonTextProperty);

    /// <summary>Gets or sets the brush for days between the two draft endpoints.</summary>
    public Brush? RangeHighlightBrush
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Brush))]
        get => (Brush?)this.GetValue(RangeHighlightBrushProperty);
        set => this.SetValue(RangeHighlightBrushProperty, value);
    }

    /// <summary>Gets or sets the brush for the draft start and end dates.</summary>
    public Brush? RangeEndpointBrush
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Brush))]
        get => (Brush?)this.GetValue(RangeEndpointBrushProperty);
        set => this.SetValue(RangeEndpointBrushProperty, value);
    }

    /// <summary>Initializes a date range picker with the native calendar flyout template.</summary>
    public CalendarDateRangePicker()
    {
        this.DefaultStyleKey = typeof(CalendarDateRangePicker);
        this.UpdateText();
        this.IsEnabledChanged += (_, _) => this.UpdateSelectionState();
        WinUIResourceStrings.ObserveChanges(this, this.UpdateStrings);
        this.RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => this.UpdateSummary());
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(DropDownButton))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(CalendarView))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(CalendarDatePicker))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(ComboBox))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(ContentPresenter))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Button))]
    protected override void OnApplyTemplate()
    {
        if (this._flyout is not null)
        {
            this._flyout.Opening -= this.OnOpening;
            this._flyout.Hide();
        }

        if (this._presets is not null)
        {
            this._presets.SelectionChanged -= this.OnPresetChanged;
            this._presets.UnregisterPropertyChangedCallback(ComboBox.HeaderProperty, this._presetHeaderToken);
        }

        if (this._apply is not null)
        {
            this._apply.Click -= this.OnApply;
        }

        if (this._cancel is not null)
        {
            this._cancel.Click -= this.OnCancel;
        }

        this.UnhookCalendar(this._startCalendar);
        this.UnhookCalendar(this._endCalendar);
        if (this._start is not null)
        {
            this._start.DateChanged -= this.OnDateChanged;
            this._start.CalendarViewDayItemChanging -= this.OnCalendarDayChanging;
        }

        if (this._end is not null)
        {
            this._end.DateChanged -= this.OnDateChanged;
            this._end.CalendarViewDayItemChanging -= this.OnCalendarDayChanging;
        }

        SetFlyoutContent(this._flyoutHeader, null);
        SetFlyoutContent(this._flyoutFooter, null);

        foreach (var (item, original) in this._dayItems)
        {
            RestoreDayStyle(item, original);
        }

        this._dayItems.Clear();

        base.OnApplyTemplate();
        this._header = this.GetTemplateChild("PART_Header") as TextBlock;
        this._button = this.GetTemplateChild("PART_Button") as DropDownButton;
        this._flyout = this._button?.Flyout;
        this._startCalendar = this.GetTemplateChild("PART_StartCalendar") as CalendarView;
        this._endCalendar = this.GetTemplateChild("PART_EndCalendar") as CalendarView;
        this._start = this.GetTemplateChild("PART_StartDate") as CalendarDatePicker;
        this._end = this.GetTemplateChild("PART_EndDate") as CalendarDatePicker;
        this._presets = this.GetTemplateChild("PART_Presets") as ComboBox;
        this._flyoutHeader = this.GetTemplateChild("PART_FlyoutHeader") as ContentPresenter;
        this._flyoutFooter = this.GetTemplateChild("PART_FlyoutFooter") as ContentPresenter;
        this._rangeSummary = this.GetTemplateChild("PART_RangeSummary") as TextBlock;
        this._error = this.GetTemplateChild("PART_Error") as TextBlock;
        this._apply = this.GetTemplateChild("PART_Apply") as Button;
        this._cancel = this.GetTemplateChild("PART_Cancel") as Button;

        var firstDay = (Windows.Globalization.DayOfWeek)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        SetDefaultFirstDayOfWeek(this._start, CalendarDatePicker.FirstDayOfWeekProperty, firstDay);
        SetDefaultFirstDayOfWeek(this._end, CalendarDatePicker.FirstDayOfWeekProperty, firstDay);

        if (this._flyout is not null)
        {
            this._flyout.Opening += this.OnOpening;
        }

        if (this._presets is not null)
        {
            this._presets.SelectionChanged += this.OnPresetChanged;
            this._presetHeaderToken = this._presets.RegisterPropertyChangedCallback(ComboBox.HeaderProperty, (_, _) => this.UpdatePresetLabel());
        }

        if (this._apply is not null)
        {
            this._apply.Click += this.OnApply;
        }

        if (this._cancel is not null)
        {
            this._cancel.Click += this.OnCancel;
        }

        this.HookCalendar(this._startCalendar);
        this.HookCalendar(this._endCalendar);
        if (this._start is not null)
        {
            this._start.DateChanged += this.OnDateChanged;
            this._start.CalendarViewDayItemChanging += this.OnCalendarDayChanging;
        }

        if (this._end is not null)
        {
            this._end.DateChanged += this.OnDateChanged;
            this._end.CalendarViewDayItemChanging += this.OnCalendarDayChanging;
        }

        SetFlyoutContent(this._flyoutHeader, this.FlyoutHeader);
        SetFlyoutContent(this._flyoutFooter, this.FlyoutFooter);

        this.UpdateSelectionState();
        this.UpdateLabels();
        this.UpdateText();
        this.UpdateOptions();
        this.ResetDraft();
    }

    private static void OnSelectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (CalendarDateRangePicker)sender;
        picker.UpdateSelectionState();
        picker.UpdateSummary();
        picker.ResetDraft();
        if (!Equals(args.OldValue, args.NewValue))
        {
            picker.SelectionChanged?.Invoke(picker, EventArgs.Empty);
        }
    }

    private static void OnOptionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((CalendarDateRangePicker)sender).UpdateOptions();
    }

    private static void OnBlackoutOptionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (CalendarDateRangePicker)sender;
        if (args.Property == BlackoutDatesProperty)
        {
            picker.UpdateBlackoutDates((DateTimeOffset[]?)args.NewValue);
        }

        picker.UpdatePresets();
        picker.ClearError();
        picker.UpdateDraftPresentation();
    }

    private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((CalendarDateRangePicker)sender).UpdateText();
    }

    private static void OnDateFormatChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (CalendarDateRangePicker)sender;
        picker.UpdateSummary();
        picker.UpdateDraftSummary();
    }

    private static void OnStringsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((CalendarDateRangePicker)sender).UpdateStrings();
    }

    private void UpdateStrings()
    {
        this.UpdateLabels();
        this.UpdateText();
        this.UpdateDraftSummary();
        this.UpdateHighlights();
        if (this._localizedError is { } error && this._error is not null)
        {
            this._error.Text = error(this.Strings);
        }
    }

    private static void OnFlyoutContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (CalendarDateRangePicker)sender;
        var presenter = args.Property == FlyoutHeaderProperty ? picker._flyoutHeader : picker._flyoutFooter;
        SetFlyoutContent(presenter, args.NewValue);
    }

    private static void OnRangeAppearanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((CalendarDateRangePicker)sender).UpdateHighlights();
    }

    private static void SetFlyoutContent(ContentPresenter? presenter, object? content)
    {
        if (presenter is not null)
        {
            presenter.Content = content;
            presenter.Visibility = content is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private void UpdateLabels()
    {
        var strings = this.Strings;
        if (this.GetTemplateChild("StartCalendarLabel") is TextBlock start)
        {
            start.Text = strings.StartDateLabel;
        }

        if (this.GetTemplateChild("EndCalendarLabel") is TextBlock end)
        {
            end.Text = strings.EndDateLabel;
        }

        if (this._startCalendar is { } startCalendar)
        {
            WinUIResourceStrings.SetDefaultAutomationName(startCalendar, strings.StartCalendarName);
        }

        if (this._endCalendar is { } endCalendar)
        {
            WinUIResourceStrings.SetDefaultAutomationName(endCalendar, strings.EndCalendarName);
        }

        if (this._start is { Header: null } startDate)
        {
            WinUIResourceStrings.SetDefaultAutomationName(startDate, strings.StartDateLabel);
        }

        if (this._end is { Header: null } endDate)
        {
            WinUIResourceStrings.SetDefaultAutomationName(endDate, strings.EndDateLabel);
        }

        this.UpdatePresetLabel();
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private void UpdatePresetLabel()
    {
        if (this._presets is { } presets)
        {
            string? name = this.Strings.PresetLabel;
            if (presets.Header is TextBlock { Name: "PresetLabel" } label)
            {
                label.Text = name;
            }
            else if (presets.Header is not null)
            {
                name = null;
            }

            WinUIResourceStrings.SetDefaultAutomationName(presets, name);
        }
    }

    private void UpdateText()
    {
        this.SetValue(EffectivePlaceholderTextProperty, this.PlaceholderText);
        this.SetValue(EffectivePresetPlaceholderTextProperty, this.PresetPlaceholderText);
        this.SetValue(EffectiveApplyButtonTextProperty, this.ApplyButtonText);
        this.SetValue(EffectiveCancelButtonTextProperty, this.CancelButtonText);
        this.UpdateSummary();
    }

    private void UpdateSelectionState()
    {
        if (this.Selection is null && !this.IsEnabled && VisualStateManager.GoToState(this, "UnselectedDisabled", false))
        {
            return;
        }

        VisualStateManager.GoToState(this, this.Selection is null ? "Unselected" : "Selected", false);
    }

    private void UpdateSummary()
    {
        var headerText = this.Header;
        var hasHeader = !string.IsNullOrWhiteSpace(headerText);
        if (this._header is { } header)
        {
            header.Visibility = hasHeader ? Visibility.Visible : Visibility.Collapsed;
        }

        if (this._button is not { } button)
        {
            return;
        }

        var strings = this.Strings;
        var text = this.Selection?.Range is { } range ? strings.FormatRange(range, this.DateFormat) : this.PlaceholderText;
        button.Content = text;
        var headerName = CalendarDateRangePickerStrings.WithFallback(headerText, strings.PickerName);
        var name = AutomationProperties.GetName(this);
        AutomationProperties.SetName(button, CalendarDateRangePickerStrings.WithFallback(name, headerName));
        AutomationProperties.SetItemStatus(button, text);
    }

    private void UpdateOptions()
    {
        var min = DateRange.CalendarDay(this.MinimumDate);
        var max = DateRange.CalendarDay(this.MaximumDate);
        SetBounds(this._start, min, max < min ? min : max);
        SetBounds(this._end, min, max < min ? min : max);
        SetCalendarBounds(this._startCalendar, min, max < min ? min : max);
        SetCalendarBounds(this._endCalendar, min, max < min ? min : max);
        this.UpdatePresets();
        this.UpdateDraftSummary();
    }

    private void UpdatePresets()
    {
        if (this._presets is not { } presets)
        {
            return;
        }

        var min = DateRange.CalendarDay(this.MinimumDate);
        var max = DateRange.CalendarDay(this.MaximumDate);
        this._visiblePresets = this.Presets?.Where(preset =>
                                   preset is { Range: not null, Label.Length: > 0 }
                                   && preset.Range.StartDate >= min
                                   && preset.Range.EndDate <= max
                                   && this.ValidateBlackoutDates(preset.Range, out _) is null).ToArray()
                               ?? [];
        presets.ItemsSource = this._visiblePresets.Select(preset => preset.Label).ToArray();
        presets.Visibility = this._visiblePresets.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateBlackoutDates(DateTimeOffset[]? dates)
    {
        this._blackoutDays = dates?.Select(DateRange.CalendarDay).Distinct().Order().ToArray() ?? [];

        var settingDraft = this._settingDraft;
        this._settingDraft = true;
        try
        {
            this.ClearBlockedEndpoint(this._startCalendar, this._start);
            this.ClearBlockedEndpoint(this._endCalendar, this._end);
            foreach (var (item, original) in this._dayItems)
            {
                this.UpdateDayBlackout(item, original);
            }
        }
        finally
        {
            this._settingDraft = settingDraft;
        }
    }

    private void ClearBlockedEndpoint(CalendarView? calendar, CalendarDatePicker? picker)
    {
        if (ReadDraftDate(calendar, picker) is not { } date || !this.IsBlackoutDate(date))
        {
            return;
        }

        if (calendar is not null)
        {
            calendar.SelectedDates.Clear();
        }
        else if (picker is not null)
        {
            picker.Date = null;
        }
    }

    private void OnOpening(object? sender, object args)
    {
        this.ResetDraft();
    }

    private void ResetDraft()
    {
        if (this._presets is not null)
        {
            this._presets.SelectedIndex = -1;
        }

        this.ClearError();
        this.SetDraft(this.Selection?.Range);
        this.DraftReset?.Invoke(this, EventArgs.Empty);
    }

    private void SetDraft(DateRange? range)
    {
        this._settingDraft = true;
        try
        {
            this.SetCalendarDate(this._startCalendar, range?.StartDate);
            this.SetCalendarDate(this._endCalendar, range?.EndDate);
            if (this._start is not null)
            {
                this._start.Date = this.GetSelectableDate(range?.StartDate);
            }

            if (this._end is not null)
            {
                this._end.Date = this.GetSelectableDate(range?.EndDate);
            }
        }
        finally
        {
            this._settingDraft = false;
        }

        this.UpdateDraftPresentation();
    }

    private void OnPresetChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this._presets is { SelectedIndex: >= 0 } presets && presets.SelectedIndex < this._visiblePresets.Length)
        {
            this.SetDraft(this._visiblePresets[presets.SelectedIndex].Range);
            this.ClearError();
        }
    }

    private void OnApply(object sender, RoutedEventArgs args)
    {
        if (this.ReadDraftRange(out var error, out var dayCount) is not { } range)
        {
            this.ShowLocalizedError(error!);
            return;
        }

        var applying = new DateRangeSelectionApplyingEventArgs(range, dayCount);
        this.SelectionApplying?.Invoke(this, applying);
        if (applying.ErrorMessage is { Length: > 0 } message)
        {
            this.ShowError(message);
            return;
        }

        var next = applying.Selection;
        if (next is null)
        {
            var compare = this.Selection?.ComparisonRange is not null;
            if (compare && !range.HasPreviousPeriod)
            {
                this.ShowLocalizedError(static strings => strings.PreviousPeriodError);
                return;
            }

            next = new DateRangeSelection(range, compare);
        }

        next = next.WithDayCount(dayCount);
        if (this.Selection != next)
        {
            this.Selection = next;
        }

        this._flyout?.Hide();
    }

    private DateRange? ReadDraftRange(out Func<CalendarDateRangePickerStrings, string>? error, out int dayCount)
    {
        dayCount = 0;
        if (ReadDraftDate(this._startCalendar, this._start) is not { } first || ReadDraftDate(this._endCalendar, this._end) is not { } last)
        {
            error = static strings => strings.MissingDatesError;
            return null;
        }

        if (last < first)
        {
            error = static strings => strings.ReversedDatesError;
            return null;
        }

        if (first < DateRange.CalendarDay(this.MinimumDate) || last > DateRange.CalendarDay(this.MaximumDate))
        {
            error = static strings => strings.OutOfBoundsError;
            return null;
        }

        var range = new DateRange(first, last);
        if (this.ValidateBlackoutDates(range, out var excludedDayCount) is { } blackoutError)
        {
            error = blackoutError;
            return null;
        }

        error = null;
        dayCount = range.DayCount - excludedDayCount;
        return range;
    }

    private Func<CalendarDateRangePickerStrings, string>? ValidateBlackoutDates(DateRange range, out int excludedDayCount)
    {
        excludedDayCount = 0;
        if (this._blackoutDays.Length == 0)
        {
            return null;
        }

        var startIndex = Array.BinarySearch(this._blackoutDays, range.StartDate);
        var endIndex = Array.BinarySearch(this._blackoutDays, range.EndDate);
        if (startIndex >= 0 || endIndex >= 0)
        {
            return static strings => strings.UnavailableEndpointError;
        }

        // Negative results encode the insertion indexes for the available endpoints.
        var blackoutCount = ~endIndex - ~startIndex;
        var mode = this.BlackoutMode;
        if (mode == DateRangeBlackoutMode.ExcludeFromCount)
        {
            excludedDayCount = blackoutCount;
            return null;
        }

        return blackoutCount > 0 && mode != DateRangeBlackoutMode.Ignore
            ? static strings => strings.UnavailableDateError
            : null;
    }

    private bool IsBlackoutDate(DateTimeOffset date)
    {
        return Array.BinarySearch(this._blackoutDays, date) >= 0;
    }

    private void OnCancel(object sender, RoutedEventArgs args)
    {
        this._flyout?.Hide();
    }

    private void ShowLocalizedError(Func<CalendarDateRangePickerStrings, string> error)
    {
        this.ShowError(error(this.Strings));
        this._localizedError = error;
    }

    private void ShowError(string text)
    {
        this._localizedError = null;
        if (this._error is { } error)
        {
            error.Text = text;
            error.Visibility = Visibility.Visible;
        }
    }

    private void ClearError()
    {
        this._localizedError = null;
        if (this._error is not null)
        {
            this._error.Visibility = Visibility.Collapsed;
        }
    }

    private void HookCalendar(CalendarView? calendar)
    {
        if (calendar is not null)
        {
            var styleToken = calendar.RegisterPropertyChangedCallback(StyleProperty, (_, _) => this.UpdateCalendarWeekStart(calendar));
            this._calendarWeekStarts[calendar] = (styleToken, null);
            this.UpdateCalendarWeekStart(calendar);
            calendar.CalendarViewDayItemChanging += this.OnCalendarDayChanging;
            calendar.SelectedDatesChanged += this.OnCalendarSelectionChanged;
        }
    }

    private void UnhookCalendar(CalendarView? calendar)
    {
        if (calendar is not null)
        {
            calendar.CalendarViewDayItemChanging -= this.OnCalendarDayChanging;
            calendar.SelectedDatesChanged -= this.OnCalendarSelectionChanged;
            if (this._calendarWeekStarts.Remove(calendar, out var weekStart))
            {
                calendar.UnregisterPropertyChangedCallback(StyleProperty, weekStart.StyleToken);
                ClearRegionalWeekStart(calendar, weekStart.RegionalWeekStart);
            }
        }
    }

    private void UpdateCalendarWeekStart(CalendarView calendar)
    {
        if (!this._calendarWeekStarts.TryGetValue(calendar, out var state))
        {
            return;
        }

        ClearRegionalWeekStart(calendar, state.RegionalWeekStart);
        var firstDay = (Windows.Globalization.DayOfWeek)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var appliesRegion = !HasWeekStartSetter(calendar.Style)
                            && SetDefaultFirstDayOfWeek(calendar, CalendarView.FirstDayOfWeekProperty, firstDay);
        this._calendarWeekStarts[calendar] = (state.StyleToken, appliesRegion ? firstDay : null);
    }

    private static void ClearRegionalWeekStart(CalendarView calendar, Windows.Globalization.DayOfWeek? regionalWeekStart)
    {
        if (regionalWeekStart is { } firstDay && Equals(calendar.ReadLocalValue(CalendarView.FirstDayOfWeekProperty), firstDay))
        {
            calendar.ClearValue(CalendarView.FirstDayOfWeekProperty);
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(Setter))]
    private static bool HasWeekStartSetter(Style? style)
    {
        for (var current = style; current is not null; current = current.BasedOn)
        {
            foreach (var setter in current.Setters)
            {
                if (setter is Setter propertySetter && propertySetter.Property == CalendarView.FirstDayOfWeekProperty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void OnCalendarSelectionChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
    {
        this.OnDraftEndpointChanged();
    }

    private void OnDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        this.OnDraftEndpointChanged();
    }

    private void OnDraftEndpointChanged()
    {
        if (!this._settingDraft)
        {
            if (this._presets is not null)
            {
                this._presets.SelectedIndex = -1;
            }

            this.ClearError();
            this.UpdateDraftPresentation();
        }
    }

    private void UpdateDraftPresentation()
    {
        this.UpdateHighlights();
        this.UpdateDraftSummary();
    }

    private void UpdateDraftSummary()
    {
        var range = this.ReadDraftRange(out _, out var dayCount);
        if (this._rangeSummary is { } summary)
        {
            summary.Text = range is null ? string.Empty : this.Strings.FormatSummary(range, dayCount, this.DateFormat);
            summary.Visibility = range is null ? Visibility.Collapsed : Visibility.Visible;
        }

        if (this.DraftRange != range)
        {
            this.DraftRange = range;
            this.DraftRangeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnCalendarDayChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
    {
        this.UpdateDayItem(args, highlightRange: true);
    }

    private void OnCalendarDayChanging(CalendarDatePicker sender, CalendarViewDayItemChangingEventArgs args)
    {
        this.UpdateDayItem(args, highlightRange: false);
    }

    private void UpdateDayItem(CalendarViewDayItemChangingEventArgs args, bool highlightRange)
    {
        var item = args.Item;
        if (args.InRecycleQueue)
        {
            if (this._dayItems.Remove(item, out var original))
            {
                RestoreDayStyle(item, original);
            }

            return;
        }

        if (!this._dayItems.TryGetValue(item, out var style))
        {
            style = new(
                item.ReadLocalValue(BackgroundProperty), item.ReadLocalValue(AutomationProperties.ItemStatusProperty),
                item.ReadLocalValue(CalendarViewDayItem.IsBlackoutProperty), highlightRange);
            this._dayItems.Add(item, style);
        }

        this.UpdateDayBlackout(item, style);
        this.UpdateDayStyle(item, style);
    }

    private void UpdateDayBlackout(CalendarViewDayItem item, DayItemStyle original)
    {
        var date = DateRange.CalendarDay(item.Date.ToLocalTime());
        var value = this.IsBlackoutDate(date) ? true : original.Blackout;
        SetLocalValue(item, CalendarViewDayItem.IsBlackoutProperty, value);
    }

    private void UpdateHighlights()
    {
        this._highlightStart = ReadDraftDate(this._startCalendar, this._start);
        this._highlightEnd = ReadDraftDate(this._endCalendar, this._end);
        foreach (var (item, original) in this._dayItems)
        {
            this.UpdateDayStyle(item, original);
        }
    }

    private void UpdateDayStyle(CalendarViewDayItem item, DayItemStyle original)
    {
        var date = DateRange.CalendarDay(item.Date.ToLocalTime());
        var blackout = this.IsBlackoutDate(date);
        var start = original.HighlightRange && date == this._highlightStart;
        var end = original.HighlightRange && date == this._highlightEnd;
        var inside = original.HighlightRange && date > this._highlightStart && date < this._highlightEnd;
        var brush = (blackout, start || end, inside) switch
        {
            (false, true, _) => this.RangeEndpointBrush,
            (false, _, true) => this.RangeHighlightBrush,
            (true, _, true) when this.BlackoutMode == DateRangeBlackoutMode.Ignore => this.RangeHighlightBrush,
            _ => null,
        };
        var strings = this.Strings;
        var status = (blackout, start, end, inside) switch
        {
            (true, _, _, _) => strings.UnavailableDateStatus,
            (_, true, true, _) => strings.SingleDateStatus,
            (_, true, false, _) => strings.StartDateStatus,
            (_, false, true, _) => strings.EndDateStatus,
            (_, _, _, true) => strings.InRangeStatus,
            _ => null,
        };

        SetLocalValue(item, BackgroundProperty, (object?)brush ?? original.Background);
        SetLocalValue(item, AutomationProperties.ItemStatusProperty, (object?)status ?? original.Status);
    }

    private static void RestoreDayStyle(CalendarViewDayItem item, DayItemStyle original)
    {
        SetLocalValue(item, BackgroundProperty, original.Background);
        SetLocalValue(item, AutomationProperties.ItemStatusProperty, original.Status);
        SetLocalValue(item, CalendarViewDayItem.IsBlackoutProperty, original.Blackout);
    }

    private static void SetLocalValue(DependencyObject item, DependencyProperty property, object? value)
    {
        if (Equals(item.ReadLocalValue(property), value))
        {
            return;
        }

        if (Equals(value, DependencyProperty.UnsetValue))
        {
            item.ClearValue(property);
        }
        else
        {
            item.SetValue(property, value);
        }
    }

    private static bool SetDefaultFirstDayOfWeek(
        DependencyObject? calendar,
        DependencyProperty property,
        Windows.Globalization.DayOfWeek firstDay)
    {
        if (calendar is not null && Equals(calendar.ReadLocalValue(property), DependencyProperty.UnsetValue))
        {
            calendar.SetValue(property, firstDay);
            return true;
        }

        return false;
    }

    private static DateTimeOffset? ReadDraftDate(CalendarView? calendar, CalendarDatePicker? picker)
    {
        var date = calendar is null ? picker?.Date : calendar.SelectedDates.Count > 0 ? calendar.SelectedDates[0] : (DateTimeOffset?)null;
        return date is { } day ? DateRange.CalendarDay(day.ToLocalTime()) : null;
    }

    private DateTimeOffset? GetSelectableDate(DateTimeOffset? date)
    {
        return date is { } day && !this.IsBlackoutDate(day) ? LocalDate(day) : null;
    }

    private void SetCalendarDate(CalendarView? calendar, DateTimeOffset? date)
    {
        if (calendar is null)
        {
            return;
        }

        calendar.SelectedDates.Clear();
        var display = date is { } day ? LocalDate(day) : DateTimeOffset.Now;
        if (date is { } selected && !this.IsBlackoutDate(selected) && display >= calendar.MinDate && display <= calendar.MaxDate)
        {
            calendar.SelectedDates.Add(display);
        }

        if (display < calendar.MinDate || display > calendar.MaxDate)
        {
            display = display < calendar.MinDate ? calendar.MinDate : calendar.MaxDate;
        }

        calendar.SetDisplayDate(display);
    }

    private static void SetCalendarBounds(CalendarView? calendar, DateTimeOffset min, DateTimeOffset max)
    {
        if (calendar is null)
        {
            return;
        }

        var first = LocalDate(min);
        var last = LocalDate(max);
        if (first > calendar.MaxDate)
        {
            calendar.MaxDate = last;
        }

        calendar.MinDate = first;
        calendar.MaxDate = last;
    }

    private static void SetBounds(CalendarDatePicker? picker, DateTimeOffset min, DateTimeOffset max)
    {
        if (picker is null)
        {
            return;
        }

        var first = LocalDate(min);
        var last = LocalDate(max);
        if (first > picker.MaxDate)
        {
            picker.MaxDate = last;
        }

        picker.MinDate = first;
        picker.MaxDate = last;
    }

    private static DateTimeOffset LocalDate(DateTimeOffset date)
    {
        return new(date.Date, TimeZoneInfo.Local.GetUtcOffset(date.Date));
    }

    private readonly record struct DayItemStyle(object? Background, object? Status, object? Blackout, bool HighlightRange);
}
