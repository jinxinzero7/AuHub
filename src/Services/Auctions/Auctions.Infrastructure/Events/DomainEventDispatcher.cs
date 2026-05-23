using Auctions.Domain.Events;
using Auctions.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Auctions.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var handlers = _serviceProvider.GetServices<IDomainEventHandler>()
            .Where(h => h.CanHandle(domainEvent));

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }

    public async Task DispatchAllAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}

public interface IDomainEventHandler
{
    bool CanHandle(IDomainEvent domainEvent);
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

public class DomainEventHandler<T> : IDomainEventHandler where T : IDomainEvent
{
    private readonly Func<T, CancellationToken, Task> _handler;

    public DomainEventHandler(Func<T, CancellationToken, Task> handler)
    {
        _handler = handler;
    }

    public bool CanHandle(IDomainEvent domainEvent) => domainEvent is T;

    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is T typedEvent)
        {
            await _handler(typedEvent, cancellationToken);
        }
    }
}
