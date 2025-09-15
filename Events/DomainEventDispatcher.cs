using Microsoft.Extensions.DependencyInjection;

namespace UserTasksAndChat.Events
{
    public interface IDomainEvent { }

    public interface IDomainEventHandler<T> where T : IDomainEvent
    {
        Task OnEventDispatch(T domainEvent, CancellationToken cancellationToken);
    }

    public class DomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public DomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in events)
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
                var handlers = _serviceProvider.GetServices(handlerType);

                foreach (var handler in handlers)
                {
                    var method = handlerType.GetMethod("OnEventDispatch");
                    await (Task)method!.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                }
            }
        }
    }

}
