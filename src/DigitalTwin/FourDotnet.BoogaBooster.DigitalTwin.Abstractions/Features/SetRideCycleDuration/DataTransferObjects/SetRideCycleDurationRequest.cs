namespace FourDotnet.BoogaBooster.DigitalTwin.Abstractions.Features.SetRideCycleDuration.DataTransferObjects;

/// <summary>The request body for configuring the duration of the next ride cycle.</summary>
/// <param name="DurationMinutes">A positive whole number of minutes.</param>
public sealed record SetRideCycleDurationRequest(long DurationMinutes);
