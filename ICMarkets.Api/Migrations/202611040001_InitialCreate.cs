using ICMarkets.Api.Domain;
using ICMarkets.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ICMarkets.Api.Migrations;

[DbContext(typeof(BlockchainDbContext))]
[Migration("202611040001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Snapshots",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Network = table.Column<BlockchainNetwork>(type: "TEXT", maxLength: 32, nullable: false)
                    .Annotation("Relational:ColumnType", "TEXT"),
                DataJson = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Snapshots", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Snapshots_Network_CreatedAt",
            table: "Snapshots",
            columns: new[] { "Network", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "Snapshots");

    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        BlockchainModel.Configure(modelBuilder);
}

internal static class BlockchainModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.14");
        modelBuilder.Entity("ICMarkets.Api.Domain.BlockchainSnapshot", entity =>
        {
            entity.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<DateTime>("CreatedAt")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<BlockchainData>("Data")
                .HasConversion<string>()
                .HasColumnType("TEXT")
                .HasColumnName("DataJson")
                .IsRequired();

            entity.Property<BlockchainNetwork>("Network")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("TEXT");

            entity.HasKey("Id");
            entity.HasIndex("Network", "CreatedAt");
            entity.ToTable("Snapshots");
        });
#pragma warning restore 612, 618
    }
}
