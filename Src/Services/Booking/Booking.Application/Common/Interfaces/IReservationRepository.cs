namespace Booking.Application.Common.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Reservation?> GetByEventAndSeatAsync(Guid eventId, Guid seatId, CancellationToken cancellationToken = default);
    Task<List<Reservation>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Reservation>> GetExpiredHoldsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a reservation with pessimistic lock (UPDLOCK) for critical payment/confirmation transactions.
    /// Prevents concurrent modifications during high-traffic scenarios.
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reservation with pessimistic lock acquired</returns>
    Task<Reservation?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a reservation by event and seat with pessimistic lock.
    /// Used in high-concurrency seat hold scenarios.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="seatId">Seat ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reservation with pessimistic lock acquired</returns>
    Task<Reservation?> GetByEventAndSeatWithLockAsync(
        Guid eventId,
        Guid seatId,
        CancellationToken cancellationToken = default);
}
