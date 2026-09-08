// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Qalculate;

public partial class QalculateCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly IFallbackCommandItem[] _fallbacks;
    private readonly SettingsManager _settings = new();

    public QalculateCommandsProvider()
    {
        DisplayName = "PowerQalc";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Settings = _settings.Settings;

        var helpPage = new HelpPage(_settings.History);
        var page = new QalculatePage(_settings, helpPage);
        _commands = [
            new CommandItem(page)
            {
                Title = DisplayName,
            },
            new CommandItem(helpPage)
            {
                Title = "PowerQalc help",
                Subtitle = "Conversions, percentages, and tips",
            },
        ];
        _fallbacks = [new FallbackQalcItem(_settings, page)];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;

    public override IFallbackCommandItem[] FallbackCommands() => _fallbacks;

    public override void Dispose()
    {
        _settings.Dispose();
        base.Dispose();
    }
}
