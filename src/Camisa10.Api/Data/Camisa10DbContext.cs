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
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<Duel> Duels => Set<Duel>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AuthSession> Sessions => Set<AuthSession>();

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
        modelBuilder.Owned<PitchReply>();
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
        modelBuilder.Owned<Trophy>();
        modelBuilder.Owned<DuelRef>();
        modelBuilder.Owned<MesaMember>();
        modelBuilder.Owned<MesaFeedItem>();
        modelBuilder.Owned<MesaResult>();
        modelBuilder.Owned<LeagueResult>();
        modelBuilder.Owned<DuelSide>();
        modelBuilder.Owned<DuelMoment>();
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

        modelBuilder.Entity<Mesa>(e =>
        {
            e.ToCollection("mesas");
            e.HasKey(m => m.Id);
            e.Property(m => m.Version).IsConcurrencyToken();
            e.HasIndex(m => m.Code);
        });

        modelBuilder.Entity<Duel>(e =>
        {
            e.ToCollection("duels");
            e.HasKey(d => d.Id);
            e.Property(d => d.Version).IsConcurrencyToken();
            e.HasIndex(d => d.MesaId);
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.ToCollection("accounts");
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Email).IsUnique();
            e.HasIndex(a => a.PlayerId);
        });

        modelBuilder.Entity<AuthSession>(e =>
        {
            e.ToCollection("sessions");
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.AccountId);
        });
    }
}
