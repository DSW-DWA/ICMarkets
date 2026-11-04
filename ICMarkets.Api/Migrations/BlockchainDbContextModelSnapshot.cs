using ICMarkets.Api.Domain;
using ICMarkets.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ICMarkets.Api.Migrations;

[DbContext(typeof(BlockchainDbContext))]
public sealed class BlockchainDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) =>
        BlockchainModel.Configure(modelBuilder);
}
