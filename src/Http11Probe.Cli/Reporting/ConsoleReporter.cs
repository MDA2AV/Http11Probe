using Http11Probe.Runner;
using Http11Probe.TestCases;

namespace Http11Probe.Cli.Reporting;

public static class ConsoleReporter
{
    public static void PrintHeader()
    {
        Console.WriteLine();
        Console.WriteLine("  {0,-35} {1,-10} {2,-14} {3,-6} {4}", "Test ID", "Verdict", "Expected", "Status", "Details");
        Console.WriteLine("  " + new string('─', 95));
    }

    public static void PrintRow(TestResult result)
    {
        if (result.Verdict == TestVerdict.Skip)
            return;

        var (color, symbol) = result.Verdict switch
        {
            TestVerdict.Pass => (ConsoleColor.Green, "PASS"),
            TestVerdict.Fail => (ConsoleColor.Red, "FAIL"),
            TestVerdict.Warn => (ConsoleColor.Yellow, "WARN"),
            TestVerdict.Error => (ConsoleColor.Magenta, "ERR "),
            _ => (ConsoleColor.Gray, "SKIP")
        };

        if (!result.TestCase.Scored)
            symbol += "*";

        var statusStr = result.Response is not null
            ? result.Response.StatusCode.ToString()
            : result.ConnectionState.ToString();

        var detail = result.ErrorMessage
                     ?? result.Response?.ReasonPhrase
                     ?? string.Empty;

        if (detail.Length > 30)
            detail = detail[..30] + "...";

        var expected = result.TestCase.Expected.GetDescription();
        if (expected.Length > 14)
            expected = expected[..14];

        var docsUrl = DocsUrlMap.GetUrl(result.TestCase.Id);

        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write("  {0,-35} {1,-10}", result.TestCase.Id, symbol);
        Console.ForegroundColor = prev;
        Console.Write(" {0,-14} {1,-6} {2}", expected, statusStr, detail);

        if (docsUrl is not null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  {docsUrl}");
            Console.ForegroundColor = prev;
        }

        Console.WriteLine();
    }

    public static void PrintSummary(TestRunReport report)
    {
        Console.WriteLine("  " + new string('─', 80));
        Console.WriteLine();

        var scoredCount = report.PassCount + report.FailCount;
        var prev = Console.ForegroundColor;
        Console.Write("  Score: ");
        Console.ForegroundColor = report.FailCount == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write($"{report.PassCount}/{scoredCount}");
        Console.ForegroundColor = prev;

        if (report.FailCount > 0)
        {
            Console.Write(" (");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{report.FailCount} failed");
            Console.ForegroundColor = prev;
            Console.Write(")");
        }

        if (report.WarnCount > 0)
        {
            Console.Write("  ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{report.WarnCount} warnings");
            Console.ForegroundColor = prev;
        }

        if (report.ErrorCount > 0)
        {
            Console.Write("  ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{report.ErrorCount} errors");
            Console.ForegroundColor = prev;
        }

        if (report.UnscoredCount > 0)
            Console.Write($"  {report.UnscoredCount} unscored");

        if (report.SkipCount > 0)
            Console.Write($"  {report.SkipCount} skipped");

        var total = report.Results.Count - report.SkipCount;
        Console.WriteLine($"  ({total} tests, {report.TotalDuration.TotalSeconds:F1}s)");
        Console.WriteLine();
    }
}
