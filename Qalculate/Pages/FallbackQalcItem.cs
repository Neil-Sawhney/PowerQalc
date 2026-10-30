// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.System;

namespace Qalculate;

internal sealed partial class FallbackQalcItem : FallbackCommandItem
{
    private static readonly IconInfo AppIcon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

    private readonly NoOpCommand _noOpCommand = new();
    private readonly SettingsManager _settings;
    private readonly QalculatePage _page;
    private readonly CommandContextItem _openPageItem;
    private readonly Lock _updateLock = new();
    private CancellationTokenSource? _evaluationCts;
    private int _queryVersion;

    public FallbackQalcItem(SettingsManager settings, QalculatePage page)
        : base(new NoOpCommand(), "PowerQalc", "neilsawhney.powerqalc.fallback")
    {
        _settings = settings;
        _page = page;
        _openPageItem = new CommandContextItem(_page)
        {
            Title = "Open PowerQalc",
        };

        Command = _noOpCommand;
        Title = string.Empty;
        Subtitle = string.Empty;
        Icon = AppIcon;
    }

    // CmdPal calls this on a background thread and only refreshes the list if Title
    // changed by the time it returns, so the result must be ready before returning.
    public override void UpdateQuery(string query)
    {
        var version = Interlocked.Increment(ref _queryVersion);
        var evaluationCts = new CancellationTokenSource();
        Interlocked.Exchange(ref _evaluationCts, evaluationCts)?.Cancel();

        if (!LooksLikeExpression(query) || LooksIncomplete(query))
        {
            HideIfCurrent(version);
            return;
        }

        Evaluate(query, version, evaluationCts.Token);
    }

    private void Evaluate(string query, int version, CancellationToken cancellationToken)
    {
        try
        {
            var result = QalculateService.EvaluateAsync(
                query,
                _settings.Session,
                cancellationToken).GetAwaiter().GetResult();

            if (!IsCurrentQuery(version, cancellationToken))
            {
                return;
            }

            if (!result.Success
                || string.IsNullOrWhiteSpace(result.Output)
                || !IsUsefulResult(query, result.Output))
            {
                HideIfCurrent(version);
                return;
            }

            Show(query, result.Output, version);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            HideIfCurrent(version);
        }
    }

    private void Show(string query, string output, int version)
    {
        var trimmedQuery = query.Trim();
        var copyCommand = new CopyAndSaveCalculationCommand(_settings, trimmedQuery, output);
        var saveCommand = new SaveCalculationCommand(_settings, trimmedQuery, output, null);

        lock (_updateLock)
        {
            if (version != _queryVersion)
            {
                return;
            }

            Command = copyCommand;
            Title = output;

            // Subtitle must include the original query so CmdPal still matches 1+2
            // after the title becomes the numeric result.
            Subtitle = trimmedQuery;
            MoreCommands =
            [
                _openPageItem,
                new CommandContextItem(saveCommand)
                {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(
                        true, false, false, false, (int)VirtualKey.Enter),
                },
            ];
        }
    }

    private void HideIfCurrent(int version)
    {
        lock (_updateLock)
        {
            if (version != _queryVersion)
            {
                return;
            }

            Command = _noOpCommand;
            Title = string.Empty;
            Subtitle = string.Empty;
            MoreCommands = [];
        }
    }

    private bool IsCurrentQuery(int version, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested && version == Volatile.Read(ref _queryVersion);

    internal static bool LooksLikeExpression(string query)
    {
        var text = query.Trim();
        if (text.Length == 0 || LooksLikePathOrUrl(text) || LooksLikeProductName(text))
        {
            return false;
        }

        if (ContainsConversionKeyword(text))
        {
            return true;
        }

        return StartsLikeNumericExpression(text)
            || FunctionCallRegex().IsMatch(text)
            || MathOperatorRegex().IsMatch(text);
    }

    private static bool LooksIncomplete(string query)
    {
        var text = query.TrimEnd();
        if (text.Length == 0)
        {
            return true;
        }

        if (text.EndsWith(" to", StringComparison.OrdinalIgnoreCase)
            || text.EndsWith(" in", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return text[^1] is '+' or '-' or '*' or '/' or '^' or '(' or ',' or '×' or '÷' or '−';
    }

    private static bool ContainsConversionKeyword(string text) =>
        text.Contains(" to ", StringComparison.OrdinalIgnoreCase)
        || text.Contains(" in ", StringComparison.OrdinalIgnoreCase);

    private static bool StartsLikeNumericExpression(string text)
    {
        var i = 0;
        if (text[i] is '+' or '-')
        {
            i++;
            if (i >= text.Length)
            {
                return false;
            }
        }

        if (text[i] == '(')
        {
            return true;
        }

        if (text[i] == '.')
        {
            return i + 1 < text.Length && char.IsDigit(text[i + 1]);
        }

        return char.IsDigit(text[i]);
    }

    private static bool LooksLikeProductName(string text)
    {
        if (text.Contains(' ', StringComparison.Ordinal)
            || text.Contains('(', StringComparison.Ordinal)
            || MathOperatorRegex().IsMatch(text))
        {
            return false;
        }

        var hasDigit = false;
        var hasLetter = false;
        foreach (var c in text)
        {
            if (char.IsDigit(c))
            {
                hasDigit = true;
            }
            else if (char.IsLetter(c))
            {
                hasLetter = true;
            }
        }

        return hasDigit && hasLetter && !LeadingNumberWithShortUnitRegex().IsMatch(text);
    }

    private static bool LooksLikePathOrUrl(string text) =>
        text.Contains('\\', StringComparison.Ordinal)
        || text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || text.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
        || (text.Length >= 3
            && char.IsAsciiLetter(text[0])
            && text[1] == ':'
            && (text[2] == '\\' || text[2] == '/'));

    private static bool IsUsefulResult(string query, string output)
    {
        var trimmedQuery = query.Trim();
        var trimmedOutput = output.Trim();
        if (trimmedOutput.Length == 0)
        {
            return false;
        }

        if (!string.Equals(trimmedQuery, trimmedOutput, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return StartsLikeNumericExpression(trimmedQuery);
    }

    [GeneratedRegex(@"[A-Za-z_]\w*\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex FunctionCallRegex();

    [GeneratedRegex(@"[+\*/^=%!×÷−]|\d\s*-\s*\S|\s-\s", RegexOptions.CultureInvariant)]
    private static partial Regex MathOperatorRegex();

    [GeneratedRegex(@"^[+-]?(\d+(\.\d*)?|\.\d+)([eE][+-]?\d+)?[a-zA-Z]{0,4}$", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingNumberWithShortUnitRegex();
}
