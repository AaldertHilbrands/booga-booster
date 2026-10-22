using FourDotnet.BoogaBooster.Core.Cqrs;

namespace FourDotnet.BoogaBooster.DigitalTwin.Features.SetRideCycleDuration;

/// <summary>Changes the configured duration for the next ride cycle.</summary>
/// <param name="DurationMinutes">A positive whole number of minutes.</param>
public sealed record SetRideCycleDurationCommand(long DurationMinutes) : Command;
