// SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
// SPDX-License-Identifier: BSD-2-Clause

namespace Ssc.Tests;

public class ConfirmationTests
{
    private static (bool Answer, string Output) Ask(string input, bool yes = false, bool isInteractive = true)
    {
        var output = new StringWriter();
        var answer = Confirmation.Ask("Delete?", yes, isInteractive, new StringReader(input), output);
        return (answer, output.ToString());
    }

    [Fact]
    public void Ask_Yes_SkipsPrompt()
    {
        var (answer, output) = Ask("", yes: true);
        Assert.True(answer);
        Assert.Empty(output);
    }

    [Fact]
    public void Ask_NotInteractive_ReturnsFalse()
    {
        var (answer, output) = Ask("y", isInteractive: false);
        Assert.False(answer);
        Assert.Equal("Delete? [y/N] N (no terminal, use --yes)" + Environment.NewLine, output);
    }

    [Theory]
    [InlineData("y")]
    [InlineData(" Y ")]
    [InlineData("yes")]
    [InlineData("YES")]
    public void Ask_AcceptsYes(string input)
    {
        var (answer, output) = Ask(input);
        Assert.True(answer);
        Assert.Equal("Delete? [y/N] ", output);
    }

    [Theory]
    [InlineData("")]
    [InlineData("n")]
    [InlineData("yep")]
    public void Ask_AnythingElseIsNo(string input) => Assert.False(Ask(input).Answer);

    [Fact]
    public void Ask_EndOfInputIsNo() => Assert.False(Confirmation.Ask("Delete?", false, true, TextReader.Null, TextWriter.Null));
}
