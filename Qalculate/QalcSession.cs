// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Qalculate;

internal sealed partial class QalcSession : IDisposable
{
    private const int TimeoutMilliseconds = 5000;

    private readonly QalcPathInfo _qalc;
    private readonly string _userDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private bool _ready;

    public QalcSession(QalcPathInfo qalc, string userDirectory)
    {
        _qalc = qalc;
        _userDirectory = userDirectory;
        Directory.CreateDirectory(_userDirectory);
    }

    public QalcPathInfo QalcPath => _qalc;

    public async Task<QalculateResult> EvaluateAsync(string expression, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return new QalculateResult(false, string.Empty, null);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Only the timeout may interrupt a request: abandoning a read would leave its
            // answer in the pipe, so it would cost a qalc restart on every keystroke.
            using var timeoutCts = new CancellationTokenSource(TimeoutMilliseconds);

            EnsureStarted(timeoutCts.Token);
            return await EvaluateCoreAsync(expression.Trim(), timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            Restart();
            return new QalculateResult(false, string.Empty, "Calculation timed out.");
        }
        catch (Exception ex)
        {
            Restart();
            return new QalculateResult(false, string.Empty, ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Wait();
        try
        {
            ShutdownProcess();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private void EnsureStarted(CancellationToken cancellationToken)
    {
        if (_process is { HasExited: false } && _ready)
        {
            return;
        }

        Restart();
        StartProcess();
    }

    private void StartProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _qalc.ExecutablePath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(_qalc.WorkingDirectory))
        {
            startInfo.WorkingDirectory = _qalc.WorkingDirectory;
        }

        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add("-t");
        startInfo.ArgumentList.Add("-s");
        startInfo.ArgumentList.Add("autocalc off");
        startInfo.ArgumentList.Add("-s");
        startInfo.ArgumentList.Add("color off");

        startInfo.Environment["QALCULATE_USER_DIR"] = _userDirectory;

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start qalc.");

        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;
        _ = _process.StandardError.ReadToEndAsync(CancellationToken.None);

        _ready = true;
    }

    private async Task<QalculateResult> EvaluateCoreAsync(string expression, CancellationToken cancellationToken)
    {
        if (_stdin is null || _stdout is null)
        {
            return new QalculateResult(false, string.Empty, "qalc session is not available.");
        }

        await _stdin.WriteLineAsync(expression.AsMemory(), cancellationToken).ConfigureAwait(false);
        await _stdin.FlushAsync(cancellationToken).ConfigureAwait(false);

        var output = await ReadExpressionResultAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(output))
        {
            return new QalculateResult(false, string.Empty, "No result from qalc.");
        }

        if (LooksLikeError(output))
        {
            return new QalculateResult(false, output, output);
        }

        return new QalculateResult(true, output, null);
    }

    private async Task<string> ReadExpressionResultAsync(CancellationToken cancellationToken)
    {
        if (_stdout is null)
        {
            return string.Empty;
        }

        var sawPrompt = false;

        while (true)
        {
            var line = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            var text = StripAnsi(line).TrimEnd();
            if (text.StartsWith("> ", StringComparison.Ordinal))
            {
                sawPrompt = true;
                continue;
            }

            if (!sawPrompt)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            return text.Trim();
        }

        return string.Empty;
    }

    private async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        if (_stdout is null || _process is { HasExited: true })
        {
            throw new InvalidOperationException("qalc exited unexpectedly.");
        }

        var readTask = _stdout.ReadLineAsync(cancellationToken).AsTask();
        return await readTask.ConfigureAwait(false);
    }

    private void Restart()
    {
        ShutdownProcess();
        _ready = false;
    }

    private void ShutdownProcess()
    {
        try
        {
            if (_process is { HasExited: false } && _stdin is not null)
            {
                _stdin.WriteLine("quit");
                _stdin.Flush();
                _process.WaitForExit(1000);
            }
        }
        catch
        {
        }

        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }

        _process?.Dispose();
        _process = null;
        _stdin = null;
        _stdout = null;
    }

    private static bool LooksLikeError(string output) =>
        output.Contains("undefined", StringComparison.OrdinalIgnoreCase)
        || output.Contains("error", StringComparison.OrdinalIgnoreCase)
        || output.Contains("syntax", StringComparison.OrdinalIgnoreCase)
        || output.Contains("failed", StringComparison.OrdinalIgnoreCase);

    private static string StripAnsi(string value) =>
        AnsiEscapeRegex().Replace(value, string.Empty);

    [GeneratedRegex(@"\x1b\[[0-9;]*m")]
    private static partial Regex AnsiEscapeRegex();
}
