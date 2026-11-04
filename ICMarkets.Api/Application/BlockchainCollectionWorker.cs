using ICMarkets.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ICMarkets.Api.Application;

public sealed class BlockchainCollectionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CollectionSettings> options,
    ILogger<BlockchainCollectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Scheduled blockchain collection is disabled.");
            return;
        }

        if (!settings.CollectOnStartup)
            await Task.Delay(settings.Interval, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();
                var outcomes = await blockchainService.CollectAllAsync(stoppingToken);
                var failedCount = outcomes.Count(outcome => !outcome.Succeeded);
                if (failedCount == 0)
                    logger.LogInformation("Scheduled collection stored {SnapshotCount} blockchain snapshots.", outcomes.Count);
                else
                    logger.LogWarning(
                        "Scheduled collection stored {SuccessfulCount} snapshots; {FailedCount} networks failed.",
                        outcomes.Count - failedCount,
                        failedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled blockchain collection failed. It will be retried after {Interval}.", settings.Interval);
            }

            try
            {
                await Task.Delay(settings.Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
