using System.Diagnostics;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// G0-006.C: the directed xUnit gate runs the actual Node regression suite.
/// Synthetic receipts in that suite never certify a real production closure.
/// </summary>
public sealed class G0006CTraceabilityDirectedTests
{
    [Fact]
    public async Task G0006C_Traceability_RunsThreeCausalNodeTests()
    {
        var root = FindRepositoryRoot();
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("--test");
        start.ArgumentList.Add("--test-reporter=tap");
        start.ArgumentList.Add("scripts/tests/g0-006-c-trazabilidad.test.mjs");

        using var process = new Process { StartInfo = start };
        Assert.True(process.Start(), "Unable to run the actual Node suite.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            throw new TimeoutException("The causal Node suite exceeded 90 seconds.");
        }

        var output = (await stdout) + Environment.NewLine + (await stderr);
        Assert.True(process.ExitCode == 0, "Node suite failed: " + output);
        Assert.Contains("# tests 3", output);
        Assert.Contains("# pass 3", output);
        Assert.Contains("# fail 0", output);
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "scripts", "tests",
                "g0-006-c-trazabilidad.test.mjs");
            if (File.Exists(path))
                return dir.FullName;
        }

        throw new FileNotFoundException(
            "Missing original scripts/tests/g0-006-c-trazabilidad.test.mjs");
    }
}
