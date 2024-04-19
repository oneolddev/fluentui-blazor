using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;

namespace FluentUI.Demo.Shared.Components;

public partial class SiteSettingsPanel : IAsyncDisposable
{
    private const string JAVASCRIPT_FILE = "./_content/FluentUI.Demo.Shared/Components/SiteSettingsPanel.razor.js";
    private string? _status;
    private bool _popVisible;
    private bool _ltr = true;
    private FluentDesignTheme? _theme;

    [Inject]
    public required ILogger<SiteSettingsPanel> Logger { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;
    private IJSObjectReference? Module { get; set; }

    [Inject]
    public required GlobalState GlobalState { get; set; }

    public DesignThemeModes Mode { get; set; }

    public OfficeColor? OfficeColor { get; set; }

    public LocalizationDirection? Direction { get; set; }

    private static IEnumerable<DesignThemeModes> AllModes => Enum.GetValues<DesignThemeModes>();

    private static IEnumerable<OfficeColor?> AllOfficeColors
    {
        get
        {
            return Enum.GetValues<OfficeColor>().Select(i => (OfficeColor?)i);
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Direction = GlobalState.Dir;
            _ltr = !Direction.HasValue || Direction.Value == LocalizationDirection.LeftToRight;
            Module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", JAVASCRIPT_FILE);
        }
    }

    protected void HandleDirectionChanged(bool isLeftToRight)
    {
        _ltr = isLeftToRight;
        Direction = isLeftToRight ? LocalizationDirection.LeftToRight : LocalizationDirection.RightToLeft;
    }

    private async Task ResetSiteAsync()
    {
        var msg = "Site settings reset and cache cleared!";

        await Module!.InvokeVoidAsync("removeAll");
        _theme?.ClearLocalStorageAsync();

        Logger.LogInformation(msg);
        _status = msg;

        OfficeColor = OfficeColorUtilities.GetRandom();
        Mode = DesignThemeModes.System;
    }

    private static string? GetCustomColor(OfficeColor? color)
    {
        return color switch
        {
            null => OfficeColorUtilities.GetRandom(true).ToAttributeValue(),
            Microsoft.FluentUI.AspNetCore.Components.OfficeColor.Default => "#036ac4",
            _ => color.ToAttributeValue(),
        };

    }

    public ValueTask DisposeAsync()
    {
        if (Module is not null)
        {
            return Module.DisposeAsync();
        }
        return ValueTask.CompletedTask;
    }
}
