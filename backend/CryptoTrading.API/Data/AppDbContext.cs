using Microsoft.EntityFrameworkCore;
using CryptoTrading.API.Models;

namespace CryptoTrading.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<MarketSnapshot> MarketSnapshots => Set<MarketSnapshot>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();
    public DbSet<AIConsensus> AIConsensuses => Set<AIConsensus>();
    public DbSet<TradeJournal> TradeJournals => Set<TradeJournal>();
    public DbSet<Backtest> Backtests => Set<Backtest>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WatchlistItem
        modelBuilder.Entity<WatchlistItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.IsFavorite).HasDefaultValue(false);
            entity.Property(e => e.DefaultTimeframe).HasMaxLength(10).HasDefaultValue("1h");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.HasIndex(e => e.Symbol).IsUnique();
        });

        // MarketSnapshot
        modelBuilder.Entity<MarketSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Price).HasColumnType("decimal(18,8)");
            entity.Property(e => e.Volume24h).HasColumnType("decimal(28,8)");
            entity.Property(e => e.Change24h).HasColumnType("decimal(10,4)");
            entity.HasIndex(e => new { e.Symbol, e.Timestamp });
        });

        // Signal
        modelBuilder.Entity<Signal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Direction).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Score).IsRequired();
            entity.Property(e => e.MaxScore).HasDefaultValue(7);
            entity.Property(e => e.ConfidenceLevel).HasMaxLength(20);
            entity.Property(e => e.EntryPrice).HasColumnType("decimal(18,8)");
            entity.Property(e => e.StopLoss).HasColumnType("decimal(18,8)");
            entity.Property(e => e.TakeProfit1).HasColumnType("decimal(18,8)");
            entity.Property(e => e.TakeProfit2).HasColumnType("decimal(18,8)");
            entity.Property(e => e.TakeProfit3).HasColumnType("decimal(18,8)");
            entity.Property(e => e.RiskReward).HasColumnType("decimal(10,2)");
            entity.HasIndex(e => new { e.Symbol, e.Timestamp });
        });

        // AIAnalysis
        modelBuilder.Entity<AIAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ModelName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Decision).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Confidence).IsRequired();
            entity.Property(e => e.MarketCondition).HasMaxLength(20);
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.EntryValid).HasDefaultValue(false);
            entity.Property(e => e.StopLossValid).HasDefaultValue(false);
            entity.Property(e => e.RiskRewardValid).HasDefaultValue(false);
            entity.HasIndex(e => new { e.Symbol, e.Timestamp });
        });

        // AIConsensus
        modelBuilder.Entity<AIConsensus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ConsensusDecision).IsRequired().HasMaxLength(10);
            entity.Property(e => e.AgreeCount).IsRequired();
            entity.Property(e => e.TotalModels).IsRequired();
            entity.HasIndex(e => new { e.Symbol, e.Timestamp });
        });

        // TradeJournal
        modelBuilder.Entity<TradeJournal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Direction).IsRequired().HasMaxLength(10);
            entity.Property(e => e.EntryPrice).HasColumnType("decimal(18,8)");
            entity.Property(e => e.StopLoss).HasColumnType("decimal(18,8)");
            entity.Property(e => e.TakeProfit).HasColumnType("decimal(18,8)");
            entity.Property(e => e.Timeframe).HasMaxLength(10);
            entity.Property(e => e.SetupType).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.ScreenshotPath).HasMaxLength(500);
            entity.Property(e => e.Result).HasMaxLength(20);
            entity.Property(e => e.PnL).HasColumnType("decimal(18,8)");
            entity.Property(e => e.RMultiple).HasColumnType("decimal(10,2)");
            entity.HasIndex(e => new { e.Symbol, e.EntryTime });
        });

        // Backtest
        modelBuilder.Entity<Backtest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Timeframe).HasMaxLength(10);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.MinimumScore).IsRequired();
            entity.Property(e => e.TotalTrades).IsRequired();
            entity.Property(e => e.Wins).IsRequired();
            entity.Property(e => e.Losses).IsRequired();
            entity.Property(e => e.WinRate).HasColumnType("decimal(10,4)");
            entity.Property(e => e.ProfitFactor).HasColumnType("decimal(10,4)");
            entity.Property(e => e.NetPnL).HasColumnType("decimal(18,8)");
            entity.Property(e => e.AverageWin).HasColumnType("decimal(18,8)");
            entity.Property(e => e.AverageLoss).HasColumnType("decimal(18,8)");
            entity.Property(e => e.MaxDrawdown).HasColumnType("decimal(18,8)");
            entity.Property(e => e.AverageR).HasColumnType("decimal(10,4)");
        });

        // AppSetting
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(2000);
            entity.Property(e => e.Description).HasMaxLength(500);
        });
    }
}