using Camisa10.Domain.Model;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Camisa10.Api.Data;

/// <summary>
/// Contexto do EF Core usando o provedor oficial do MongoDB.
/// Cada carreira é um documento na coleção "careers", com o estado do jogo em subdocumentos (owned types).
/// </summary>
public class Camisa10DbContext(DbContextOptions<Camisa10DbContext> options) : DbContext(options)
{
    public DbSet<Career> Careers => Set<Career>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tipos que vivem dentro do documento da carreira.
        modelBuilder.Owned<Attributes>();
        modelBuilder.Owned<Traits>();
        modelBuilder.Owned<UsedEvent>();
        modelBuilder.Owned<ScheduledEvent>();
        modelBuilder.Owned<CupState>();
        modelBuilder.Owned<SeasonRecord>();
        modelBuilder.Owned<MarketOffer>();
        modelBuilder.Owned<PickResult>();
        modelBuilder.Owned<DiceRoll>();
        modelBuilder.Owned<PenaltyShot>();
        modelBuilder.Owned<ClubSeasonResult>();
        modelBuilder.Owned<TableRow>();
        modelBuilder.Owned<Competition>();
        modelBuilder.Owned<CompetitionResult>();
        modelBuilder.Owned<MatchState>();
        modelBuilder.Owned<MatchLine>();
        modelBuilder.Owned<LeagueRow>();
        modelBuilder.Owned<MatchRecord>();
        modelBuilder.Owned<BracketRound>();
        modelBuilder.Owned<BracketTie>();
        modelBuilder.Owned<Chip>();
        modelBuilder.Owned<SeasonReport>();
        modelBuilder.Owned<GameEvent>();
        modelBuilder.Owned<EventOption>();
        modelBuilder.Owned<Fx>();

        modelBuilder.Entity<Career>(e =>
        {
            e.ToCollection("careers");
            e.HasKey(c => c.Id);
            e.Property(c => c.Phase).HasConversion<string>();
            e.Property(c => c.ReturnPhase).HasConversion<string>();
            e.Property(c => c.Status).HasConversion<string>();
            e.Property(c => c.Version).IsConcurrencyToken();
            e.HasIndex(c => new { c.PlayerId, c.Status, c.UpdatedAt });
        });
    }
}
