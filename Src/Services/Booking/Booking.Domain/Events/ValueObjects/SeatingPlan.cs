namespace Booking.Domain.Events.ValueObjects;

/// <summary>
/// Represents a seating plan configuration for bulk seat generation.
/// Allows mapping specific rows or sections to a SeatType and its corresponding Money price.
/// </summary>
public sealed class SeatingPlanRow
{
    private SeatingPlanRow(
        string row,
        int startNumber,
        int endNumber,
        SeatType seatType,
        Money price)
    {
        Row = row;
        StartNumber = startNumber;
        EndNumber = endNumber;
        SeatType = seatType;
        Price = price;
    }

    public string Row { get; }
    public int StartNumber { get; }
    public int EndNumber { get; }
    public SeatType SeatType { get; }
    public Money Price { get; }

    /// <summary>
    /// Creates a seating plan row configuration with validation.
    /// </summary>
    public static Response<SeatingPlanRow> Create(
        string row,
        int startNumber,
        int endNumber,
        SeatType seatType,
        Money price)
    {
        if (string.IsNullOrWhiteSpace(row))
            return Response<SeatingPlanRow>.Failure(new Error("SeatingPlan.InvalidRow", "Row cannot be empty"));

        if (startNumber <= 0)
            return Response<SeatingPlanRow>.Failure(new Error("SeatingPlan.InvalidStartNumber", "Start number must be greater than zero"));

        if (endNumber < startNumber)
            return Response<SeatingPlanRow>.Failure(new Error("SeatingPlan.InvalidEndNumber", "End number must be greater than or equal to start number"));

        if (seatType is null)
            return Response<SeatingPlanRow>.Failure(new Error("SeatingPlan.NullSeatType", "Seat type cannot be null"));

        if (price is null)
            return Response<SeatingPlanRow>.Failure(new Error("SeatingPlan.NullPrice", "Price cannot be null"));

        return Response<SeatingPlanRow>.Success(new SeatingPlanRow(row, startNumber, endNumber, seatType, price));
    }

    public override bool Equals(object? obj) =>
        obj is SeatingPlanRow other &&
        Row == other.Row &&
        StartNumber == other.StartNumber &&
        EndNumber == other.EndNumber;

    public override int GetHashCode() =>
        HashCode.Combine(Row, StartNumber, EndNumber);
}

/// <summary>
/// Seating plan containing multiple row configurations for bulk seat generation.
/// </summary>
public sealed class SeatingPlan
{
    private readonly List<SeatingPlanRow> _rows = [];

    private SeatingPlan(List<SeatingPlanRow> rows)
    {
        _rows = rows ?? [];
    }

    public IReadOnlyList<SeatingPlanRow> Rows => _rows.AsReadOnly();

    /// <summary>
    /// Creates a seating plan from a collection of row configurations.
    /// </summary>
    public static Response<SeatingPlan> Create(params SeatingPlanRow[] rows)
    {
        if (rows is null || rows.Length == 0)
            return Response<SeatingPlan>.Failure(new Error("SeatingPlan.Empty", "Seating plan must contain at least one row configuration"));

        // Validate no duplicate rows with overlapping seat numbers
        for (int i = 0; i < rows.Length; i++)
        {
            for (int j = i + 1; j < rows.Length; j++)
            {
                if (rows[i].Row == rows[j].Row &&
                    ((rows[i].StartNumber <= rows[j].EndNumber && rows[i].EndNumber >= rows[j].StartNumber)))
                {
                    return Response<SeatingPlan>.Failure(new Error(
                        "SeatingPlan.OverlappingSeats",
                        $"Row {rows[i].Row} has overlapping seat numbers"));
                }
            }
        }

        return Response<SeatingPlan>.Success(new SeatingPlan(rows.ToList()));
    }

    /// <summary>
    /// Gets the total number of seats in the seating plan.
    /// </summary>
    public int GetTotalSeatCount() =>
        _rows.Sum(r => r.EndNumber - r.StartNumber + 1);
}
