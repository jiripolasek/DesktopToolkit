// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A measurement with optional label and timestamp for inspection.</summary>
public readonly record struct HeatmapChartCell(double Value, string? Label = null, DateTimeOffset? Timestamp = null);

/// <summary>A snapshot of a rectangular matrix indexed by row and column.</summary>
public sealed class HeatmapChartData
{
    private readonly HeatmapChartCell?[,] _cells;

    /// <summary>Gets the number of rows.</summary>
    public int RowCount => this._cells.GetLength(0);

    /// <summary>Gets the number of columns.</summary>
    public int ColumnCount => this._cells.GetLength(1);

    /// <summary>Gets the optional row captions.</summary>
    public IReadOnlyList<string?> RowLabels { get; }

    /// <summary>Gets the optional column captions.</summary>
    public IReadOnlyList<string?> ColumnLabels { get; }

    /// <summary>Gets the cell at the zero-based row and column.</summary>
    public HeatmapChartCell? this[int row, int column] => this._cells[row, column];

    internal int Count => this._cells.Length;

    internal bool HasValues { get; }

    internal double Minimum { get; }

    internal double Maximum { get; }

    /// <summary>Copies numeric cells and optional axis labels. Null cells are blank.</summary>
    public HeatmapChartData(double?[,] values, string?[]? rowLabels = null, string?[]? columnLabels = null)
        : this(CopyValues(values), rowLabels, columnLabels, copy: false)
    {
    }

    /// <summary>Copies cells with optional metadata and axis labels. Null cells are blank.</summary>
    public HeatmapChartData(HeatmapChartCell?[,] cells, string?[]? rowLabels = null, string?[]? columnLabels = null)
        : this(cells, rowLabels, columnLabels, copy: true)
    {
    }

    private HeatmapChartData(HeatmapChartCell?[,] cells, string?[]? rowLabels, string?[]? columnLabels, bool copy)
    {
        ArgumentNullException.ThrowIfNull(cells);
        this._cells = copy ? (HeatmapChartCell?[,])cells.Clone() : cells;
        this.RowLabels = CopyLabels(rowLabels, this.RowCount, nameof(rowLabels));
        this.ColumnLabels = CopyLabels(columnLabels, this.ColumnCount, nameof(columnLabels));

        var minimum = double.PositiveInfinity;
        var maximum = double.NegativeInfinity;
        foreach (var cell in this._cells)
        {
            if (cell is not { } item)
            {
                continue;
            }

            if (!double.IsFinite(item.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(cells), "Cell values must be finite.");
            }

            minimum = Math.Min(minimum, item.Value);
            maximum = Math.Max(maximum, item.Value);
        }

        this.HasValues = double.IsFinite(minimum);
        this.Minimum = this.HasValues ? minimum : 0;
        this.Maximum = this.HasValues ? maximum : 1;
    }

    private static IReadOnlyList<string?> CopyLabels(string?[]? labels, int count, string name)
    {
        if (labels is null)
        {
            return Array.Empty<string?>();
        }

        if (labels.Length != count)
        {
            throw new ArgumentException("Supply one caption for each row or column.", name);
        }

        return Array.AsReadOnly((string?[])labels.Clone());
    }

    private static HeatmapChartCell?[,] CopyValues(double?[,] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var cells = new HeatmapChartCell?[values.GetLength(0), values.GetLength(1)];
        for (var row = 0; row < cells.GetLength(0); row++)
        {
            for (var column = 0; column < cells.GetLength(1); column++)
            {
                if (values[row, column] is double value)
                {
                    cells[row, column] = new HeatmapChartCell(value);
                }
            }
        }

        return cells;
    }
}
