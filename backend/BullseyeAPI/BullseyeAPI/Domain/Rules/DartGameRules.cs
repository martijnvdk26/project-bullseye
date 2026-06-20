namespace BullseyeAPI.Domain.Rules;

// Pure rules engine for "X01"-style dart games (301/501/701/170, etc), with
// no dependency on Game/Turn/Player entities so it's easy to unit test
// (see Tests/DartGameRulesTests.cs).
public class DartGameRules
{
    // Remaining scores that can never legally be reduced to zero in a single
    // 3-dart visit ending on a double - the dartboard has no combination of
    // up to 2 scoring darts plus one double that sums to these ("bogey")
    // numbers.
    private static readonly HashSet<int> ImpossibleCheckouts = new() { 169, 168, 166, 165, 163, 162, 159 };

    // "Double out" means the final dart must land on a double (or bullseye).
    // 501/301/701 etc and 170 require it; informal variants don't.
    public bool RequiresDoubleOut (string variant)
    {
        if (variant.EndsWith("01") || variant == "170")
        {
            return true;
        }

        return false;
    }

    // Overshooting is always a bust; leaving exactly 1 is a bust in
    // double-out variants since the smallest double is 2.
    public bool IsValidScore (int currentScore, int thrownValue, bool isDouble, string variant = "501")
    {
        int remaining = currentScore - thrownValue;

        if (remaining < 0){
            return false; // Bust
        }

        if (remaining == 0)
        {
            return !RequiresDoubleOut(variant) || isDouble; // Must end on a double if required
        }

        if (remaining == 1 && RequiresDoubleOut(variant))
        {
            return false; // Cannot leave 1 if double out is required
        }
        return true; // Valid score
    }

    // Separate from IsValidScore since a valid throw isn't necessarily a win.
    public bool IsWinningThrow(int currentScore, int thrownValue, bool isDouble, string variant = "501")
    {
        int remaining = currentScore - thrownValue;

        return remaining == 0 && (!RequiresDoubleOut(variant) || isDouble);
    }

    // Whether a player sitting on `remainingScore` has a legal shot at
    // finishing the leg this visit - i.e. whether "checkout %" even applies
    // here. Used both to classify checkout attempts for stats, and to
    // reject a manually-entered turn that claims to finish from a score
    // that could never legally end on a double (the bogey numbers above).
    public bool IsCheckoutPossible(int remainingScore, string variant)
    {
        if (!RequiresDoubleOut(variant)) return false;
        if (remainingScore < 2 || remainingScore > 170) return false;

        return !ImpossibleCheckouts.Contains(remainingScore);
    }
}