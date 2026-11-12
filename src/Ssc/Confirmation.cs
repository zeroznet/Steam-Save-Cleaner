namespace Ssc;

public static class Confirmation
{
    public static bool Ask(string question, bool yes, bool isInteractive, TextReader input, TextWriter output)
    {
        if (yes)
        {
            return true;
        }
        if (!isInteractive)
        {
            output.WriteLine($"{question} [y/N] N (no terminal, use --yes)");
            return false;
        }
        output.Write($"{question} [y/N] ");
        output.Flush();
        var answer = input.ReadLine()?.Trim();
        return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
