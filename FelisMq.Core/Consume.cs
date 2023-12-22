namespace FelisMq.Core;

public abstract class Consume<T> where T : class
{
    public abstract Task Process(T entity, CancellationToken cancellationToken = default);
}