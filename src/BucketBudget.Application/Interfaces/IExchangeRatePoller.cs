namespace BucketBudget.Application.Interfaces;

public interface IExchangeRatePoller
{
    Task PollAsync(CancellationToken cancellationToken = default);
}
