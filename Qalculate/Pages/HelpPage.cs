// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Qalculate;

internal sealed partial class HelpPage : ContentPage
{
    private const string QalcManualUrl = "https://qalculate.github.io/manual/qalc.html";
    private const string FunctionsManualUrl = "https://qalculate.github.io/manual/index.html";

    public HelpPage(HistoryStore history)
    {
        Icon = new IconInfo("\uE946");
        Title = HistoryStore.HelpTitle;
        Name = "Help";

        Commands =
        [
            new CommandContextItem(new ClearHistoryCommand(history))
            {
                Title = "Clear calculation history",
            },
            new CommandContextItem(new OpenUrlCommand(QalcManualUrl))
            {
                Title = "Qalculate manual (web)",
            },
            new CommandContextItem(new OpenUrlCommand(FunctionsManualUrl))
            {
                Title = "Functions & units (web)",
            },
        ];
    }

    public override IContent[] GetContent() =>
    [
        new MarkdownContent(
            """
            # PowerQalc

            A calculator in Command Palette. Type an expression in the main search box — PowerQalc appears as a fallback result, so you do not need to open the extension first. Open **PowerQalc** for history, `ans()`, and variables.

            **Enter** copies the result. **Ctrl+Enter** saves it to history without copying. On the PowerQalc page, clear the search box to see past results.
            """),
        new MarkdownContent(
            """
            ## Convert units with *to*

            Put **to** and the unit you want at the end:

            - 100 miles to km
            - 72 F to C
            - 5 feet + 8 inches to cm
            - 10 mph * 2 hours to miles

            You can combine different units in one line — Qalculate converts as needed.
            """),
        new MarkdownContent(
            """
            ## Percentages

            - 240 * 15%
            - 15% of 240
            - 100 + 10%
            """),
        new MarkdownContent(
            """
            ## Previous answer

            After a result, type **ans** to reuse it: ans+1, ans*2, or 10% of ans.

            For equations with multiple answers, try ans(1) and ans(2).
            """),
        new MarkdownContent(
            """
            ## More you can try

            - Mixed units: 5 miles + 10 km
            - Algebra: x^2 + 2x = 0
            - Word problems: 10 mph * x = 20 mi to min
            - Dates: today + 3 weeks
            - Constants: 1 ly to km, planck to eV s

            Advanced: store a value with B:=10 (use a letter other than x when solving equations). Type delete B to remove it.
            """),
        new MarkdownContent(
            """
            ## Reference

            [Qalculate manual](https://qalculate.github.io/manual/qalc.html) · [Functions & units](https://qalculate.github.io/manual/index.html)

            Currency and some other features may use the internet through Qalculate; your expressions stay on your PC.
            """),
    ];
}
