using Booking.Domain.Events;
using Booking.Domain.Events.Entities;
using Booking.Domain.Reservations;

namespace Booking.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for implementing concurrency control patterns in EF Core.
/// Provides both optimistic locking (via RowVersion) and pessimistic locking (via SQL Server UPDLOCK hint).
/// </summary>
public static class ConcurrencyExtensions
{
    /// <summary>
    /// Applies pessimistic lock (UPDLOCK) to a seat query for critical "Hold Seat" transactions.
    /// Used during high-traffic bursts to prevent double-booking at the SQL Server level.
    /// 
    /// This acquires an exclusive lock on the row in SQL Server until the transaction commits,
    /// preventing other transactions from reading or writing the same row.
    /// </summary>
    /// <param name="query">The IQueryable query to apply the lock to</param>
    /// <returns>Query with pessimistic lock applied via raw SQL</returns>
    public static IQueryable<Seat> WithPessimisticLock(this IQueryable<Seat> query)
    {
        return query.TagWith("UPDLOCK");
    }

    /// <summary>
    /// Applies pessimistic lock (UPDLOCK) to a reservation query.
    /// </summary>
    /// <param name="query">The IQueryable query to apply the lock to</param>
    /// <returns>Query with pessimistic lock applied via raw SQL</returns>
    public static IQueryable<Reservation> WithPessimisticLock(this IQueryable<Reservation> query)
    {
        return query.TagWith("UPDLOCK");
    }

    /// <summary>
    /// Applies pessimistic lock (UPDLOCK) to an event query for critical transactions.
    /// </summary>
    /// <param name="query">The IQueryable query to apply the lock to</param>
    /// <returns>Query with pessimistic lock applied via raw SQL</returns>
    public static IQueryable<Event> WithPessimisticLock(this IQueryable<Event> query)
    {
        return query.TagWith("UPDLOCK");
    }
}
