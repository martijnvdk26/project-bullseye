namespace BullseyeAPI.Domain.Rules;

public class DartGameRules
{

    public bool RequiresDoubleOut (string variant)
    {
        if (variant.EndsWith("01") || variant == "170")
        {
            return true;
        }

        return false;
    }

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

    public bool IsWinningThrow(int currentScore, int thrownValue, bool isDouble, string variant = "501")
    {
        int remaining = currentScore - thrownValue;
        
        return remaining == 0 && (!RequiresDoubleOut(variant) || isDouble);
    }    
    
}