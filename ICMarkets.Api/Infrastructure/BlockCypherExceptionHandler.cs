using Microsoft.AspNetCore.Diagnostics;
using ICMarkets.Api.Application;

namespace ICMarkets.Api.Infrastructure;

public sealed class BlockCypherExceptionHandler : IExceptionHandler
{
    private readonly ILogger<BlockCypherExceptionHandler> _logger;

    public BlockCypherExceptionHandler(ILogger<BlockCypherExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            HttpRequestException => (
                Status: StatusCodes.Status502BadGateway,
                Title: "BlockCypher request failed",
                Detail: "The upstream blockchain service request failed."),
            InvalidUpstreamResponseException => (
                Status: StatusCodes.Status502BadGateway,
                Title: "BlockCypher returned an invalid response",
                Detail: "The upstream blockchain service returned data in an unexpected format."),
            Polly.CircuitBreaker.BrokenCircuitException => (
                Status: StatusCodes.Status503ServiceUnavailable,
                Title: "BlockCypher is temporarily unavailable",
                Detail: "Requests are paused after repeated failures. Please retry shortly."),
            Polly.Timeout.TimeoutRejectedException => (
                Status: StatusCodes.Status504GatewayTimeout,
                Title: "BlockCypher request timed out",
                Detail: "The upstream service did not respond within the allowed time."),
            OperationCanceledException when !httpContext.RequestAborted.IsCancellationRequested => (
                Status: StatusCodes.Status504GatewayTimeout,
                Title: "BlockCypher request timed out",
                Detail: "The upstream service did not respond within the allowed time."),
            _ => default
        };

        if (problem.Status == 0)
            return false;

        _logger.LogWarning(exception, "BlockCypher request failed; returning HTTP {StatusCode}.", problem.Status);

        await Results.Problem(
                statusCode: problem.Status,
                title: problem.Title,
                detail: problem.Detail)
            .ExecuteAsync(httpContext);
        return true;
    }
}
