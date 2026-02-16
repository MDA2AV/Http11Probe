using System.CommandLine;
using Http11Probe.Cli.Reporting;
using Http11Probe.Runner;
using Http11Probe.TestCases;
using Http11Probe.TestCases.Suites;

var hostOption = new Option<string>("--host") { Description = "Target hostname or IP address" };
hostOption.DefaultValueFactory = _ => "localhost";

var portOption = new Option<int>("--port") { Description = "Target port number" };
portOption.DefaultValueFactory = _ => 8080;

var categoryOption = new Option<TestCategory?>("--category") { Description = "Run only tests in this category (skip all others)" };

var testOption = new Option<string[]>("--test") { Description = "Run only specific test IDs, case-insensitive (repeatable)", Arity = ArgumentArity.OneOrMore };

var timeoutOption = new Option<int>("--timeout") { Description = "Connect and read timeout in seconds per test" };
timeoutOption.DefaultValueFactory = _ => 5;

var outputOption = new Option<string?>("--output") { Description = "Write JSON results to this file path" };

var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Print the raw server response for each test" };
verboseOption.DefaultValueFactory = _ => false;

var rootCommand = new RootCommand("Http11Probe — HTTP/1.1 server compliance & hardening tester")
{
    hostOption,
    portOption,
    categoryOption,
    testOption,
    timeoutOption,
    outputOption,
    verboseOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var host = parseResult.GetValue(hostOption)!;
    var port = parseResult.GetValue(portOption);
    var category = parseResult.GetValue(categoryOption);
    var timeout = parseResult.GetValue(timeoutOption);
    var testIds = parseResult.GetValue(testOption);
    var outputPath = parseResult.GetValue(outputOption);
    var verbose = parseResult.GetValue(verboseOption);

    Console.WriteLine($"  Http11Probe targeting {host}:{port}");
    Console.WriteLine();

    var options = new TestRunOptions
    {
        Host = host,
        Port = port,
        ConnectTimeout = TimeSpan.FromSeconds(timeout),
        ReadTimeout = TimeSpan.FromSeconds(timeout),
        CategoryFilter = category,
        TestIdFilter = testIds is { Length: > 0 }
            ? new HashSet<string>(testIds, StringComparer.OrdinalIgnoreCase)
            : null
    };

    var testCases = new List<ITestCase>();
    testCases.AddRange(ComplianceSuite.GetTestCases());
    testCases.AddRange(SmugglingSuite.GetTestCases());
    testCases.AddRange(SmugglingSuite.GetSequenceTestCases());
    testCases.AddRange(MalformedInputSuite.GetTestCases());
    testCases.AddRange(NormalizationSuite.GetTestCases());
    testCases.AddRange(CapabilitiesSuite.GetSequenceTestCases());

    var runner = new TestRunner(options);

    ConsoleReporter.PrintHeader();
    var report = await runner.RunAsync(testCases, result =>
    {
        ConsoleReporter.PrintRow(result);
        if (verbose)
            ConsoleReporter.PrintRawResponse(result);
    });
    ConsoleReporter.PrintSummary(report);

    if (outputPath is not null)
    {
        var json = JsonReporter.Generate(report);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
        Console.WriteLine($"  JSON report written to {outputPath}");
    }
});

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
