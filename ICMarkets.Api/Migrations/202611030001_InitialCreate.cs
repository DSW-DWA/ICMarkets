using ICMarkets.Api.Domain;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using ICMarkets.Api.Infrastructure;

namespace ICMarkets.Api.Migrations;

[DbContext(typeof(BlockchainDbContext))]
[Migration("202611030001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        BlockchainModel.Configure(modelBuilder);

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // IF NOT EXISTS also adopts databases previously created with EnsureCreated.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "Snapshots" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Snapshots" PRIMARY KEY AUTOINCREMENT,
                "CreatedAt" TEXT NOT NULL,
                "Network" TEXT NOT NULL,
                "ResponseJson" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_Snapshots_Network_CreatedAt"
                ON "Snapshots" ("Network", "CreatedAt");
            UPDATE "Snapshots"
            SET "CreatedAt" = replace("CreatedAt", '+00:00', '')
            WHERE typeof("CreatedAt") = 'text';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "Snapshots");
}

internal static class BlockchainModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        modelBuilder.Entity("ICMarkets.Api.Domain.BlockchainSnapshot", entity =>
        {
            entity.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<DateTime>("CreatedAt")
                .HasColumnType("TEXT");

            entity.Property<BlockchainNetwork>("Network")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("TEXT");

            entity.Property<string>("ResponseJson")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.HasKey("Id");
            entity.HasIndex("Network", "CreatedAt");
            entity.ToTable("Snapshots");
        });
#pragma warning restore 612, 618
    }
}
