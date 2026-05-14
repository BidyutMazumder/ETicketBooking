using Booking.Application.Common.Interfaces;
using Booking.Domain.Events;
using Booking.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class EventRepository : IEventRepository
{
    private readonly BookingDbContext _context;

    public EventRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets an event with pessimistic lock for critical seat hold transactions.
    /// Acquires UPDLOCK at SQL Server level to prevent concurrent seat status modifications.
    /// </summary>
    public async Task<Event?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .WithPessimisticLock()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<Event>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Events.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Event @event, CancellationToken cancellationToken = default)
    {
        await _context.Events.AddAsync(@event, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        _context.Events.Update(@event);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await GetByIdAsync(id, cancellationToken);
        if (@event is not null)
        {
            _context.Events.Remove(@event);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
