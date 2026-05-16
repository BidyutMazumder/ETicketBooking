using Booking.Application.Common.Interfaces;
using Booking.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class SeatCategoryRepository : ISeatCategoryRepository
{
    private readonly BookingDbContext _context;

    public SeatCategoryRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<SeatCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SeatCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<SeatCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SeatCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SeatCategory>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SeatCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<SeatCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.SeatCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name.Value == name, cancellationToken);
    }

    public async Task<List<SeatCategory>> GetByTypeAsync(string seatType, CancellationToken cancellationToken = default)
    {
        return await _context.SeatCategories
            .AsNoTracking()
            .Where(c => c.SeatType.Value == seatType)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SeatCategory category, CancellationToken cancellationToken = default)
    {
        await _context.SeatCategories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SeatCategory category, CancellationToken cancellationToken = default)
    {
        _context.SeatCategories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetByIdAsync(id, cancellationToken);
        if (category is not null)
        {
            _context.SeatCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
