using Chronowalker.Core.Models;

namespace Chronowalker.Core.Tests.Models;

/// <summary>Tests Chronowalker option validation and deadline mapping.</summary>
[TestClass]
public sealed class ChronowalkerOptionsTests
{
    /// <summary>Verifies that the legacy 30-day preset writes the required adjusted value.</summary>
    [TestMethod]
    public void GetWrittenDeadline_ThirtyDayPreset_ReturnsThirtyOne()
    {
        var options = new ChronowalkerOptions(18, 30, false, GamePlatform.SteamOrGog);

        int result = options.GetWrittenDeadline();

        Assert.AreEqual(31, result);
    }

    /// <summary>Verifies that a custom value is written exactly as entered.</summary>
    [TestMethod]
    public void GetWrittenDeadline_CustomThirty_ReturnsThirty()
    {
        var options = new ChronowalkerOptions(18, 30, true, GamePlatform.SteamOrGog);

        int result = options.GetWrittenDeadline();

        Assert.AreEqual(30, result);
    }

    /// <summary>Verifies that an unsupported segment value is rejected.</summary>
    [TestMethod]
    public void Validate_UnsupportedSegments_Throws()
    {
        var options = new ChronowalkerOptions(10, 366, true, GamePlatform.SteamOrGog);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(options.Validate);
    }

    /// <summary>Verifies that unresolved platforms cannot reach file-writing operations.</summary>
    [TestMethod]
    public void Validate_UnresolvedPlatform_Throws()
    {
        var options = new ChronowalkerOptions(18, 366, true, GamePlatform.Unknown);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(options.Validate);
    }
}
