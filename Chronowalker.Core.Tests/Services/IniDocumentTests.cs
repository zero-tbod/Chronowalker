using Chronowalker.Core.Services;

namespace Chronowalker.Core.Tests.Services;

/// <summary>Tests focused INI parsing and mutation behavior.</summary>
[TestClass]
public sealed class IniDocumentTests
{
    /// <summary>Verifies that unrelated sections and comments survive a value update.</summary>
    [TestMethod]
    public void SetValue_ExistingKey_PreservesUnrelatedLines()
    {
        IniDocument document = IniDocument.Parse("; comment\r\n[Other]\r\nKeep=Yes\r\n[Target]\r\nValue=Old\r\n");

        document.SetValue("Target", "Value", "New");

        StringAssert.Contains(document.ToString(), "; comment\r\n[Other]\r\nKeep=Yes");
        Assert.AreEqual("New", document.GetValue("Target", "Value"));
    }

    /// <summary>Verifies that a missing section is appended once.</summary>
    [TestMethod]
    public void SetValue_MissingSection_AppendsSection()
    {
        IniDocument document = IniDocument.Parse("[Other]\r\nKeep=Yes\r\n");

        document.SetValue("Target", "Value", "New");

        Assert.AreEqual("New", document.GetValue("Target", "Value"));
        Assert.AreEqual(1, CountOccurrences(document.ToString(), "[Target]"));
    }

    /// <summary>Verifies that every duplicate managed key is removed.</summary>
    [TestMethod]
    public void RemoveValue_DuplicateKeys_RemovesEveryOccurrence()
    {
        IniDocument document = IniDocument.Parse("[Target]\r\nValue=One\r\nValue=Two\r\nKeep=Yes\r\n");

        document.RemoveValue("Target", "Value");

        Assert.IsNull(document.GetValue("Target", "Value"));
        StringAssert.Contains(document.ToString(), "Keep=Yes");
    }

    private static int CountOccurrences(string value, string needle)
    {
        return value.Split(needle, StringSplitOptions.None).Length - 1;
    }
}
