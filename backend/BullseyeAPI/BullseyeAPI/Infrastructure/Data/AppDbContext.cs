using BullseyeAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BullseyeAPI.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Turn> Turns => Set<Turn>();
    public DbSet<Score> Scores => Set<Score>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<GuestSession> GuestSessions => Set<GuestSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Zorg dat de GuestSession code uniek is
        modelBuilder.Entity<GuestSession>()
            .HasIndex(g => g.SessionCode)
            .IsUnique();
            
        // Relatie 1: Game heeft Turns
        modelBuilder.Entity<Game>()
            .HasMany(g => g.Turns)
            .WithOne(t => t.Game)
            .HasForeignKey(t => t.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // OPLOSSING: Relatie 2: Game <-> Players (Veel-op-veel voor deelname)
        modelBuilder.Entity<Game>()
            .HasMany(g => g.Players)
            .WithMany(p => p.Games);

        // OPLOSSING: Relatie 3: Game -> Winner (Één-op-veel)
        modelBuilder.Entity<Game>()
            .HasOne(g => g.Winner)
            .WithMany() // We hoeven in Player geen extra lijst met 'GewonnenGames' bij te houden
            .HasForeignKey(g => g.WinnerId)
            .OnDelete(DeleteBehavior.SetNull); // Als een speler uit de database wordt verwijderd, wordt de winnaar in de game 'null'
    }
}