using System.Diagnostics;
using FourDotnet.BoogaBooster.Core.Cqrs;
using FourDotnet.BoogaBooster.DigitalTwin.Application;
using FourDotnet.BoogaBooster.DigitalTwin.Observability;

namespace FourDotnet.BoogaBooster.DigitalTwin.Features.SetRideCycleDuration;

/// <summary>Applies a validated duration to the ride's next cycle.</summary>
public sealed class SetRideCycleDurationCommandHandler : CommandHandler<SetRideCycleDurationCommand>
{
    private readonly IRideStore _store;

    public SetRideCycleDurationCommandHandler(IRideStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    protected override Task ExecuteAsync(
        SetRideCycleDurationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        _store.SetCycleDurationMinutes(command.DurationMinutes);
        return Task.CompletedTask;
    }

    protected override void EnrichActivity(Activity activity, SetRideCycleDurationCommand command)
    {
        activity.SetTag(RideTelemetryAttributes.CycleDurationMinutes, command.DurationMinutes);
        activity.SetTag(RideTelemetryAttributes.State, _store.CurrentState.ToString());
    }
}
