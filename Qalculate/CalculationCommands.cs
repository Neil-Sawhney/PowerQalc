// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Qalculate;

internal sealed partial class CopyAndSaveCalculationCommand : InvokableCommand
{
    private readonly SettingsManager _settings;
    private readonly string _query;
    private readonly string _result;

    public CopyAndSaveCalculationCommand(SettingsManager settings, string query, string result)
    {
        _settings = settings;
        _query = query;
        _result = result;
        Name = "Copy result";
        Icon = new IconInfo("\uE8C8");
    }

    public override CommandResult Invoke()
    {
        TrySaveToHistory();
        return (CommandResult)new CopyTextCommand(_result).Invoke();
    }

    private void TrySaveToHistory()
    {
        if (_settings.SaveHistory)
        {
            _settings.History.Add(_query, _result);
        }
    }
}

internal sealed partial class SaveCalculationCommand : InvokableCommand
{
    private readonly SettingsManager _settings;
    private readonly string _query;
    private readonly string _result;
    private readonly Action? _showHistory;

    public SaveCalculationCommand(SettingsManager settings, string query, string result, Action? showHistory)
    {
        _settings = settings;
        _query = query;
        _result = result;
        _showHistory = showHistory;
        Name = "Save to history";
        Icon = new IconInfo("\uE81C");
    }

    public override CommandResult Invoke()
    {
        if (!_settings.SaveHistory)
        {
            new ToastStatusMessage("History saving is disabled in settings").Show();
            return CommandResult.KeepOpen();
        }

        _settings.History.Add(_query, _result);
        _showHistory?.Invoke();
        new ToastStatusMessage("Saved to history").Show();
        return CommandResult.KeepOpen();
    }
}
