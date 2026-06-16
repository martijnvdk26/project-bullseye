using Xunit;
using BullseyeAPI.Domain.Rules;

namespace Tests;

public class DartGameRulesTests
{
    private readonly DartGameRules _rules;
    
    public DartGameRulesTests()
    {
        _rules = new DartGameRules();
    }
    
    #region RequiresDoubleOut Tests (5 test cases)

    [Theory]
    [InlineData("501", true)]
    [InlineData("301", true)]
    [InlineData("170", true)]
    [InlineData("Around the clock", false)]
    [InlineData("Bob's 27", false)]
    public void RequiresDoubleOut_ShouldReturnExpectedResult_ForVariants(string variant, bool expected)
    {
        // Act
        bool result = _rules.RequiresDoubleOut(variant);
        
        Assert.Equal(expected, result);
    }
    #endregion
    
    #region IsWinningThrow Tests (7 testgevallen)

    [Theory]
    // Huidige stand, Geworpen, IsDouble, Variant, Verwacht resultaat
    [InlineData(40, 40, true, "501", true)]    // Winst: D20 gegooid voor de match
    [InlineData(40, 40, false, "501", false)]  // Geen winst: Wel 0 over, maar geen dubbel gegooid
    [InlineData(50, 50, true, "501", true)]    // Winst: Bullseye (D25) gegooid voor de match
    [InlineData(32, 32, true, "170", true)]    // Winst: D16 gegooid in een 170 variant
    [InlineData(40, 20, false, "501", false)]  // Geen winst: Te weinig gegooid (laat 20 over)
    [InlineData(10, 12, true, "501", false)]   // Geen winst: Bust gegooid (meer dan de stand)
    [InlineData(20, 20, false, "Around the Clock", true)] // Winst: 0 over zonder dubbel in non-double-out variant
    public void IsWinningThrow_ShouldIdentifyCheckoutsCorrectly(int currentScore, int thrownValue, bool isDouble, string variant, bool expected)
    {
        // Act
        bool result = _rules.IsWinningThrow(currentScore, thrownValue, isDouble, variant);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Randvoorwaarden & Edge Cases (5 testgevallen)

    [Fact]
    public void IsValidScore_WithDefaultVariant_ShouldUse501Rules()
    {
        // Act & Assert (Als variant wordt weggelaten, moet de default '501' gelden)
        bool resultLeavesOne = _rules.IsValidScore(10, 9, false); // Laat 1 over
        Assert.False(resultLeavesOne); // Moet false zijn wegens double-out
    }

    [Fact]
    public void IsWinningThrow_WithDefaultVariant_ShouldUse501Rules()
    {
        // Act & Assert
        bool resultValidWin = _rules.IsWinningThrow(40, 40, true); // D20 gegooid
        Assert.True(resultValidWin);
    }

    [Fact]
    public void IsValidScore_WhenRemainingIsExactlyTwo_WithNoDouble_ShouldBeValid()
    {
        // Act (Stand is 40, gooit 38 enkel, laat 2 over voor een dubbel)
        bool result = _rules.IsValidScore(40, 38, false, "501");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScore_WhenCurrentScoreIsZero_AndThrowIsZero_ShouldBeBust()
    {
        // Act (Mathematische edge case: stand is al 0)
        bool result = _rules.IsValidScore(0, 5, false, "501");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWinningThrow_WhenThrownValueExceedsCurrentScore_ShouldReturnFalse()
    {
        // Act 
        bool result = _rules.IsWinningThrow(10, 60, true, "501");

        // Assert
        Assert.False(result);
    }

    #endregion
}