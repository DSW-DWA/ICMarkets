using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ICMarkets.Api.Application;
using ICMarkets.Api.Configuration;
using ICMarkets.Api.Contracts;
using ICMarkets.Api.Domain;
using ICMarkets.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration));

var connectionString = builder.Configuration.GetConnectionString("Blockchain")
    ?? throw new InvalidOperationException("ConnectionStrings:Blockchain must be configured.");
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("Cors:AllowedOrigins must be configured.");

builder.Services.AddOptions<BlockCypherSettings>()
    .BindConfiguration("BlockCypher")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<CollectionSettings>()
    .BindConfiguration("Collection")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Contains("*"))
        policy.AllowAnyOrigin();
    else
        policy.WithOrigins(allowedOrigins);

    policy.AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddDbContext<BlockchainDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IBlockchainSnapshotRepository, BlockchainSnapshotRepository>();
builder.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<BlockchainDbContext>());
builder.Services.AddScoped<IBlockchainService, BlockchainService>();
builder.Services.AddHostedService<BlockchainCollectionWorker>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<IBlockCypherClient, BlockCypherClient>((services, client) =>
    {
        client.BaseAddress = new Uri(services.GetRequiredService<IOptions<BlockCypherSettings>>().Value.BaseUrl);
        // Timeouts are applied by the resilience pipeline below.
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddStandardResilienceHandler(builder.Configuration.GetSection("HttpResilience"));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BlockCypherExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<BlockchainDbContext>();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BlockchainDbContext>();
    await db.Database.MigrateAsync();
}
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/health");
var blockchains = app.MapGroup("/api/blockchains")
    .WithTags("Blockchains");

blockchains.MapGet("/{network}/history", async (
        [EnumDataType(typeof(BlockchainNetwork))] BlockchainNetwork network,
        [AsParameters] HistoryQuery query,
        IBlockchainService blockchainService,
        CancellationToken cancellationToken) =>
    {
        var history = await blockchainService.GetHistoryAsync(network, query.From!.Value, query.To!.Value, query.Limit, cancellationToken);
        return Results.Ok(history.Select(snapshot => snapshot.ToResponse()).ToArray());
    })
    .WithName("GetBlockchainHistory")
    .WithSummary("Returns snapshots for a network within a time range, newest first")
    .Produces<IReadOnlyList<SnapshotResponse>>()
    .ProducesValidationProblem();

blockchains.MapPost("/collect", async (
        IBlockchainService blockchainService,
        CancellationToken cancellationToken) =>
    {
        var outcomes = await blockchainService.CollectAllAsync(cancellationToken);
        var results = outcomes.Select(outcome => new NetworkCollectionResponse(
            outcome.Network,
            outcome.Succeeded,
            outcome.Snapshot?.ToResponse(),
            outcome.Error)).ToArray();
        return Results.Ok(new CollectAllResponse(
            results,
            results.Count(result => result.Succeeded),
            results.Count(result => !result.Succeeded)));
    })
    .WithName("CollectAllBlockchainSnapshots")
    .WithSummary("Fetches every network independently, stores successful snapshots, and reports per-network failures")
    .Produces<CollectAllResponse>()
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
    .ProducesProblem(StatusCodes.Status504GatewayTimeout);

blockchains.MapPost("/{network}/collect", async (
        [EnumDataType(typeof(BlockchainNetwork))] BlockchainNetwork network,
        IBlockchainService blockchainService,
        CancellationToken cancellationToken) =>
    {
        var snapshot = await blockchainService.CollectAsync(network, cancellationToken);
        return Results.Created($"/api/blockchains/{network}/history", snapshot.ToResponse());
    })
    .WithName("CollectBlockchainSnapshot")
    .WithSummary("Fetches the latest blockchain summary and stores it")
    .Produces<SnapshotResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
    .ProducesProblem(StatusCodes.Status504GatewayTimeout)
    .ProducesValidationProblem();

app.Run();

public partial class Program;
