# Ride Cycle Limit

## Why

The ride can currently remain in `Started` indefinitely unless an operator stops it. Operators need a configurable cycle duration and a predictable, gradual end to each run rather than an abrupt full-brake stop.

## What Changes

- Add a dashboard control for the ride-cycle duration, defaulting to 5 minutes.
- Start the cycle timer on entry to `Started`, measure it in deterministic simulation time, and keep that duration fixed for the active cycle.
- When the configured duration expires, automatically begin a normal controlled stop: progressively reduce drive power and increase brake application until the ride is at rest.
- Keep restraints and other safety constraints locked throughout the stop; continue to use the existing offloading flow after rest.
- Report the configured duration, remaining cycle time, and stop-ramp progress in ride telemetry for the dashboard.
- Preserve Emergency Stop as a distinct immediate protective action.

## Capabilities

### New Capabilities
- `ride-cycle-limit`: configurable ride-cycle duration, automatic expiry, and cycle-end stop ramp.

### Modified Capabilities
- `ride-operation-controls`: configure the cycle duration from the dashboard and display cycle/stop progress.
- `ride-state-machine`: automatically enter `Stopping` when the configured cycle duration expires.
- `engine-braking`: ramp drive power and brake application during a normal controlled stop while preserving immediate Emergency Stop behavior.

## Impact

- **DigitalTwin backend** — cycle-duration state and validation, automatic timer handling in the fixed-step simulation, a progressive normal-stop profile, updated telemetry, and a module-owned command/endpoint for dashboard configuration.
- **Angular dashboard** — accessible duration control, cycle countdown, and stopping progress; keep the duration fixed while a cycle is active.
- **Physics documentation** — document the normal-stop power/brake profile and its deceleration constraints; update the parameter appendix.
- **Tests** — backend deterministic timing and braking tests; frontend control/state and accessibility tests.

## Assumptions

- The five-minute default is the powered ride-cycle duration; the stop ramp begins when that duration expires, so the time to come fully to rest is additional.
- The setting is editable before a cycle and applies to the next cycle. A running cycle cannot be extended by changing the dashboard setting.
- Duration and ramp progression use simulation time, not wall-clock time. The configured value resets to its default when the application process restarts; durable configuration is not part of this change.
- The approximately 8-second deceleration described in `docs/03-motor-power-and-torque.md` is the initial reference for the normal stop ramp and must be checked against the ride's G-force constraints during implementation.
