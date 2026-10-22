using FourDotnet.BoogaBooster.Core;
using FourDotnet.BoogaBooster.DigitalTwin.Abstractions;
using FourDotnet.BoogaBooster.DigitalTwin.Domain;
using Xunit;

namespace FourDotnet.BoogaBooster.DigitalTwin.Tests;

/// <summary>Deterministic ride-cycle timing and ordinary-stop ramp behavior.</summary>
public sealed class RideCycleLimitTests
{
    [Fact]
    public void A_new_ride_uses_the_five_minute_default_and_zero_progress()
    {
        var telemetry = Ride.Create().ToTelemetry();

        Assert.Equal(5, telemetry.CycleDurationMinutes);
        Assert.Equal(0d, telemetry.RemainingCycleTimeSeconds);
        Assert.Equal(0d, telemetry.StopRampProgress);
        Assert.Equal(0d, telemetry.BrakeApplication);
    }

    [Fact]
    public void A_valid_duration_is_configured_for_the_next_cycle()
    {
        var ride = Ride.Create();

        ride.SetCycleDurationMinutes(12);
        ride.RequestTransition(RideState.Loading);
        ride.RequestTransition(RideState.Safe);
        ride.RequestTransition(RideState.Started);

        Assert.Equal(12, ride.ToTelemetry().CycleDurationMinutes);
        Assert.Equal(TimeSpan.FromMinutes(12), ride.RemainingCycleTime);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    public void Invalid_or_unrepresentable_duration_is_rejected_without_changing_the_setting(long minutes)
    {
        var ride = Ride.Create();

        Assert.Throws<DomainValidationException>(() => ride.SetCycleDurationMinutes(minutes));

        Assert.Equal(RideParameters.DefaultCycleDurationMinutes, ride.CycleDurationMinutes);
    }

    [Theory]
    [InlineData(RideState.Started)]
    [InlineData(RideState.Stopping)]
    [InlineData(RideState.EmergencyStop)]
    public void Cycle_duration_cannot_change_during_a_cycle_or_stop(RideState state)
    {
        var ride = RideInState(state);

        Assert.Throws<DomainValidationException>(() => ride.SetCycleDurationMinutes(9));

        Assert.Equal(RideParameters.DefaultCycleDurationMinutes, ride.CycleDurationMinutes);
    }

    [Fact]
    public void Cycle_timer_advances_while_started_even_when_all_drive_power_is_zero()
    {
        var ride = RideStartedWithDuration(1);
        var initialRemaining = ride.RemainingCycleTime;
        ride.SetMainEnginePower(EnginePower.Off);
        ride.SetHubEnginePower(EnginePower.Off);

        AdvanceSteps(ride, steps: 120);

        Assert.Equal(RideState.Started, ride.CurrentState);
        Assert.Equal(
            initialRemaining - TimeSpan.FromTicks(TestHelpers.Dt.Ticks * 120L),
            ride.RemainingCycleTime);
    }

    [Fact]
    public void Cycle_expiry_enters_stopping_on_the_first_fixed_step_at_the_limit()
    {
        var ride = RideStartedWithDuration(1);
        ride.SetMainEnginePower(new EnginePower(100));
        ride.SetHubEnginePower(new EnginePower(100));

        AdvanceSteps(ride, steps: 50 * 120);
        Assert.True(Math.Abs(ride.Mill.AngularVelocity) > 0d);
        ride.SetMainEnginePower(EnginePower.Off);
        ride.SetHubEnginePower(EnginePower.Off);
        AdvanceSteps(ride, steps: 1200);

        Assert.Equal(RideState.Started, ride.CurrentState);
        Assert.True(ride.RemainingCycleTime > TimeSpan.Zero);

        ride.Advance(TestHelpers.Dt);

        Assert.Equal(RideState.Stopping, ride.CurrentState);
        Assert.Equal(TimeSpan.Zero, ride.RemainingCycleTime);
        Assert.DoesNotContain(RideState.Stopping, ride.AvailableTransitions);
        Assert.True(ride.ConstraintsLocked);
    }

    [Fact]
    public void A_new_cycle_resets_its_timer_and_captures_the_current_duration()
    {
        var ride = RideStartedWithDuration(2);
        AdvanceSteps(ride, steps: 120);
        Assert.True(ride.RemainingCycleTime < TimeSpan.FromMinutes(2));

        ride.RequestTransition(RideState.Stopping);
        ride.Advance(TestHelpers.Dt);
        Assert.Equal(RideState.Idle, ride.CurrentState);

        ride.RequestTransition(RideState.Loading);
        ride.RequestTransition(RideState.Safe);
        ride.RequestTransition(RideState.Started);

        Assert.Equal(TimeSpan.FromMinutes(2), ride.RemainingCycleTime);
    }

    [Fact]
    public void Normal_stopping_progressively_reduces_power_and_applies_brakes()
    {
        var ride = RideStartedWithDuration(RideParameters.DefaultCycleDurationMinutes);
        ride.SetMainEnginePower(new EnginePower(80));
        ride.SetHubEnginePower(new EnginePower(60));
        AdvanceSteps(ride, steps: 20 * 120);
        ride.RequestTransition(RideState.Stopping);

        var previousMillPower = ride.Mill.ConsumedPowerWatts;
        var previousHubPower = ride.Mill.GetHub(0).ConsumedPowerWatts;
        var previousBrake = ride.BrakeApplication;
        for (var step = 0; step < 60; step++)
        {
            ride.Advance(TestHelpers.Dt);
            var telemetry = ride.ToTelemetry();

            Assert.Equal(RideState.Stopping, telemetry.State);
            Assert.True(telemetry.Mill.PowerWatts < previousMillPower);
            Assert.True(telemetry.Hubs[0].PowerWatts < previousHubPower);
            Assert.True(telemetry.BrakeApplication > previousBrake);
            Assert.InRange(telemetry.StopRampProgress, 0d, 1d);

            previousMillPower = telemetry.Mill.PowerWatts;
            previousHubPower = telemetry.Hubs[0].PowerWatts;
            previousBrake = telemetry.BrakeApplication;
        }
    }

    [Theory]
    [InlineData(MotorDirection.Forward)]
    [InlineData(MotorDirection.Reverse)]
    public void Normal_stop_preserves_the_selected_rotation_direction(MotorDirection direction)
    {
        var ride = RideStartedWithDuration(1);
        ride.SetMainEngineDirection(direction);
        ride.SetHubEngineDirection(direction);
        ride.SetMainEnginePower(new EnginePower(100));
        ride.SetHubEnginePower(new EnginePower(100));
        AdvanceSteps(ride, steps: 10 * 120);
        var initialMillSpeed = ride.Mill.AngularVelocity;
        ride.RequestTransition(RideState.Stopping);

        ride.Advance(TestHelpers.Dt);

        Assert.Equal(direction, ride.Mill.Direction);
        Assert.All(ride.Mill.Hubs, hub => Assert.Equal(direction, hub.Direction));
        Assert.Equal(direction.Sign(), Math.Sign(ride.Mill.AngularVelocity));
        Assert.Equal(direction.Sign(), Math.Sign(initialMillSpeed));
    }

    [Fact]
    public void Normal_stop_cuts_a_drive_that_would_apply_torque_against_current_rotation()
    {
        var ride = RideStartedWithDuration(1);
        ride.SetMainEnginePower(new EnginePower(100));
        AdvanceSteps(ride, steps: 5 * 120);
        Assert.True(ride.Mill.AngularVelocity > 0d);

        ride.SetMainEngineDirection(MotorDirection.Reverse);
        ride.RequestTransition(RideState.Stopping);
        ride.Advance(TestHelpers.Dt);

        Assert.Equal(MotorDirection.Reverse, ride.Mill.Direction);
        Assert.Equal(0d, ride.Mill.ConsumedPowerWatts);
        Assert.True(ride.Mill.AngularVelocity >= 0d);
        Assert.True(ride.BrakeApplication > 0d);
    }

    [Fact]
    public void Normal_stop_reaches_zero_drive_power_by_the_ramp_end_or_earlier_rest()
    {
        var ride = RideStartedWithDuration(RideParameters.DefaultCycleDurationMinutes);
        ride.SetMainEnginePower(new EnginePower(100));
        ride.SetHubEnginePower(new EnginePower(100));
        AdvanceSteps(ride, steps: 20 * 120);
        ride.RequestTransition(RideState.Stopping);

        var rampSteps = (int)Math.Ceiling(
            RideParameters.NormalStopRampDuration.Ticks / (double)TestHelpers.Dt.Ticks);
        AdvanceSteps(ride, steps: rampSteps);

        var telemetry = ride.ToTelemetry();
        Assert.Equal(0d, telemetry.Mill.PowerWatts);
        Assert.All(telemetry.Hubs, hub => Assert.Equal(0d, hub.PowerWatts));
        Assert.InRange(telemetry.BrakeApplication, 0d, 1d);
        Assert.InRange(telemetry.StopRampProgress, 0d, 1d);
    }

    [Fact]
    public void A_stopping_ride_already_at_rest_offloads_without_applying_brake_or_reversing()
    {
        var ride = RideStartedWithDuration(1);
        ride.SetMainEnginePower(new EnginePower(50));
        ride.RequestTransition(RideState.Stopping);

        ride.Advance(TestHelpers.Dt);

        Assert.Equal(RideState.Idle, ride.CurrentState);
        Assert.True(ride.Mill.IsAtRest);
        Assert.Equal(0d, ride.BrakeApplication);
        Assert.Equal(0d, ride.Mill.ConsumedPowerWatts);
    }

    [Fact]
    public void Operator_drive_and_brake_commands_cannot_interrupt_normal_stopping()
    {
        var ride = RideStartedWithDuration(1);
        ride.RequestTransition(RideState.Stopping);

        Assert.Throws<DomainValidationException>(() => ride.SetMainEnginePower(new EnginePower(10)));
        Assert.Throws<DomainValidationException>(() => ride.SetHubEnginePower(new EnginePower(10)));
        Assert.Throws<DomainValidationException>(() => ride.SetMainEngineDirection(MotorDirection.Reverse));
        Assert.Throws<DomainValidationException>(() => ride.SetHubEngineDirection(MotorDirection.Reverse));
        Assert.Throws<DomainValidationException>(() => ride.SetEngineBrakes(false));
        Assert.Throws<DomainValidationException>(() => ride.SetGondolaBrake(0, 0, GondolaBrakeState.Released));

        Assert.Equal(RideState.Stopping, ride.CurrentState);
        Assert.Equal(0d, ride.BrakeApplication);
    }

    [Fact]
    public void Emergency_stop_remains_immediate_and_uses_full_braking()
    {
        var ride = RideStartedWithDuration(1);
        ride.SetMainEnginePower(new EnginePower(80));
        ride.SetHubEnginePower(new EnginePower(60));

        ride.RequestTransition(RideState.EmergencyStop);

        var telemetry = ride.ToTelemetry();
        Assert.Equal(RideState.EmergencyStop, telemetry.State);
        Assert.Equal(0d, telemetry.Mill.PowerWatts);
        Assert.All(telemetry.Hubs, hub => Assert.Equal(0d, hub.PowerWatts));
        Assert.True(telemetry.BrakesEngaged);
        Assert.Equal(1d, telemetry.BrakeApplication);
        Assert.Equal(0d, telemetry.StopRampProgress);
    }

    private static Ride RideStartedWithDuration(long durationMinutes)
    {
        var ride = Ride.Create();
        ride.SetCycleDurationMinutes(durationMinutes);
        ride.RequestTransition(RideState.Loading);
        ride.RequestTransition(RideState.Safe);
        ride.RequestTransition(RideState.Started);
        return ride;
    }

    private static Ride RideInState(RideState state)
    {
        var ride = RideStartedWithDuration(RideParameters.DefaultCycleDurationMinutes);
        if (state == RideState.Started)
        {
            return ride;
        }

        if (state == RideState.Stopping)
        {
            ride.RequestTransition(state);
            return ride;
        }

        ride.RequestTransition(RideState.EmergencyStop);
        return ride;
    }

    private static void AdvanceSteps(Ride ride, int steps)
    {
        for (var step = 0; step < steps; step++)
        {
            ride.Advance(TestHelpers.Dt);
        }
    }
}
