namespace Shared.Kernel.Domain.Abstractions;

/// <summary>
/// Marker interface for all application messages.
/// </summary>
public interface IMessage { }

/// <summary>
/// Marker interface for messages that represent notifications.
/// </summary>
public interface INotification : IMessage { }

/// <summary>
/// Marker interface for domain events raised by entities.
/// </summary>
public interface IDomainEvent : INotification { }
