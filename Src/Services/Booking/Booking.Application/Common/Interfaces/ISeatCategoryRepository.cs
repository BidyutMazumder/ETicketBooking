namespace Booking.Application.Common.Interfaces;

public interface ISeatCategoryRepository
{
    Task<SeatCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SeatCategory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<SeatCategory>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<SeatCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<SeatCategory>> GetByTypeAsync(string seatType, CancellationToken cancellationToken = default);
    Task AddAsync(SeatCategory category, CancellationToken cancellationToken = default);
    Task UpdateAsync(SeatCategory category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
