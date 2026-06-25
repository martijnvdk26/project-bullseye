namespace BullseyeAPI.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // True for the single, shared "Dartbot" Player row used as the computer
    // opponent in registered vs-bot games (see RegisteredSessionService).
    public bool IsBot { get; set; }

    // Gates login: a freshly-registered account starts unverified and can't
    // log in until the Resend verification link is clicked (see
    // PlayerService.VerifyEmailAsync).
    public bool EmailVerified { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiresAt { get; set; }

    //Statistics
    public decimal ThreeDartAverage { get; set; }
    public decimal CheckoutPercentage { get; set; }
    public decimal HighestFinish { get; set; }

    // Backing counters for CheckoutPercentage: how many turns left this
    // player with a legal shot at a double finish (per
    // DartGameRules.IsCheckoutPossible), and how many of those turns
    // actually ended the leg.
    public int CheckoutAttempts { get; set; }
    public int CheckoutHits { get; set; }
    
    // Relations
    public ICollection<Game> Games { get; set; } = new List<Game>();
    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
}