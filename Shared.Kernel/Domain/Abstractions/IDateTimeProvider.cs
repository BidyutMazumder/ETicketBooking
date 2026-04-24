namespace Shared.Kernel.Domain.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}