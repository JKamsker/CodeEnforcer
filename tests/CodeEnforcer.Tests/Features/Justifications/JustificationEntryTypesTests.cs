using CodeEnforcer;

namespace CodeEnforcer.Tests;

public sealed class JustificationEntryTypesTests
{
    [Theory]
    [InlineData("file", "file")]
    [InlineData("files", "file")]
    [InlineData("folder", "folder")]
    [InlineData("folders", "folder")]
    [InlineData("root-folder", "root-folder")]
    [InlineData("rootFolders", "root-folder")]
    public void ParsesSupportedTypeNames(string value, string expected)
    {
        JustificationEntryType parsed = JustificationEntryTypes.Parse(value);

        Assert.Equal(expected, JustificationEntryTypes.Format(parsed));
    }

    [Fact]
    public void RejectsUnknownTypeName()
    {
        CodeEnforcerException exception = Assert.Throws<CodeEnforcerException>(() =>
            JustificationEntryTypes.Parse("other"));

        Assert.Contains("--type must be one of", exception.Message);
    }
}
