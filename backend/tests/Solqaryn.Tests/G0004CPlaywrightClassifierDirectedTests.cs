using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

public sealed class G0004CPlaywrightClassifierDirectedTests
{
    [Fact]
    public async Task Directed_scope_runs_the_actual_Playwright_classifier_tests()
    {
        var repoRoot = FindRepositoryRoot();
        var testFile = Path.Combine(repoRoot, "frontend", "scripts", "e2e-result-classifier.test.mjs");
        Assert.True(File.Exists(testFile), $"Missing classifier test: {testFile}");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("--test");
        // Force TAP so the fail-closed summary parser receives portable # tests/pass/fail counters.
        process.StartInfo.ArgumentList.Add("--test-reporter=tap");
        process.StartInfo.ArgumentList.Add(testFile);

        Assert.True(process.Start(), "Could not start Node.js for the G0-004.C directed test.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("G0-004.C Node.js classifier tests exceeded 90 seconds.");
        }

        var output = await stdoutTask;
        var error = await stderrTask;
        Assert.True(process.ExitCode == 0, $"Node classifier tests failed.\n{output}\n{error}");

        var totalMatch = Regex.Match(output, @"(?m)^# tests (\d+)\s*$");
        var passedMatch = Regex.Match(output, @"(?m)^# pass (\d+)\s*$");
        var failedMatch = Regex.Match(output, @"(?m)^# fail (\d+)\s*$");
        Assert.True(totalMatch.Success && passedMatch.Success && failedMatch.Success,
            $"Node test summary is missing; directed scope cannot certify zero/unreported tests.\n{output}");
        var total = int.Parse(totalMatch.Groups[1].Value);
        var passed = int.Parse(passedMatch.Groups[1].Value);
        var failed = int.Parse(failedMatch.Groups[1].Value);
        Assert.True(total > 0 && passed == total && failed == 0,
            $"G0-004.C requires nonzero all-passing Node tests (total={total}, passed={passed}, failed={failed}).");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "frontend", "scripts", "e2e-result-classifier.test.mjs")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root for G0-004.C.");
    }
}
