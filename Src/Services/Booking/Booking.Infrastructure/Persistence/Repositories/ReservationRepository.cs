using Booking.Application.Common.Interfaces;
using Booking.Domain.Reservations;
using Booking.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly BookingDbContext _context;

    public ReservationRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets a reservation with pessimistic lock for critical payment/confirmation transactions.
    /// Acquires UPDLOCK at SQL Server level to prevent concurrent modifications.
    /// </summary>
    public async Task<Reservation?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .WithPessimisticLock()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Reservation?> GetByEventAndSeatAsync(Guid eventId, Guid seatId, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.SeatId == seatId, cancellationToken);
    }

    /// <summary>
    /// Gets a reservation by event and seat with pessimistic lock.
    /// Used in high-concurrency seat hold scenarios to prevent double-booking.
    /// </summary>
    public async Task<Reservation?> GetByEventAndSeatWithLockAsync(
        Guid eventId,
        Guid seatId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .WithPessimisticLock()
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.SeatId == seatId, cancellationToken);
    }

    public async Task<List<Reservation>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Reservation>> GetExpiredHoldsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Reservations
            .Where(r => r.Status.Value == "Pending" && r.HoldExpiresAtUtc < now)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.Reservations.AddAsync(reservation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reservation = await GetByIdAsync(id, cancellationToken);
        if (reservation is not null)
        {
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
