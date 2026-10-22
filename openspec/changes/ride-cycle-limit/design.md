# Design: Ride Cycle Limit

## Context

`Ride` is the DigitalTwin aggregate root. It owns lifecycle transitions and advances motion in fixed simulation steps (`RideParameters.TimeStep`, 1/120 s). Entering `Started` currently has no duration limit. The normal `Stopping` entry currently cuts commanded drive power and engages the full engine brake immediately; `EmergencyStop` is also immediate. Motor power is controlled separately for the mill and hubs, and the dashboard receives full ride snapshots over SSE.

The physics notes describe a controlled deceleration profile of approximately eight seconds (`docs/03-motor-power-and-torque.md`, section 3.5) and list an eight-second deceleration ramp in `docs/appendix-parameters.md`. The new normal stop should use that as an initial tuning reference, while preserving the immediate emergency-stop behavior.

## Goals

- Give operators a dashboard-controlled ride-cycle limit with a five-minute default.
- End a cycle automatically and predictably, independent of wall-clock scheduling jitter.
- Make cycle expiry use a normal controlled stop: drive power falls progressively and brake torque rises progressively.
- Keep restraints locked until the ride reaches rest, then use the existing offloading lifecycle.
- Show the selected limit, remaining cycle time, and stop progress on the dashboard.

## Non-Goals

- Persisting the operator-selected duration across application restarts.
- Allowing an operator to extend or shorten a cycle after it has entered `Started`.
- Changing the Emergency Stop response or using it for ordinary cycle expiry.
- Adding a separate ride lifecycle state for cycle expiry.

## Decisions

### 1. The cycle limit is an aggregate setting

The active `Ride` owns the configured cycle duration and elapsed time for the current cycle. The default is five minutes. The setting is held in memory and resets to the default when the application process restarts. It is editable only while the ride is not `Started`, `Stopping`, or `EmergencyStop`; a change is applied to the next cycle.

The dashboard accepts a positive whole number of minutes. The backend validates the value and rejects invalid settings without changing the active limit. The initial proposal does not impose a product-specific maximum; the implementation must guard duration conversion/overflow and keep the UI input bounded to supported values.

### 2. Cycle timing uses simulation time

The cycle timer starts when the guarded transition into `Started` succeeds. It advances by the fixed `dt` for each simulation tick while the ride remains `Started`, even if the operator temporarily sets mill power to zero or engages the engine brake. This defines a ride cycle, rather than powered-motor accumulation. Re-entering `Started` for a new cycle resets elapsed time and captures the currently configured duration.

When elapsed simulation time reaches the captured limit, the aggregate automatically enters `Stopping`. The cycle countdown reaches zero at this transition; the controlled stop is additional time after the configured ride duration.

### 3. Normal stopping uses a deterministic ramp

The normal `Stopping` profile runs over the documented eight-second ramp reference. During each fixed simulation step it:

- reduces the mill and hub commanded drive power progressively to zero, starting from the values at stop entry;
- increases the engine-brake torque scale progressively from zero toward full braking torque;
- prevents operator power/direction commands from interrupting the stop;
- settles early if the rig reaches rest before the profile completes.

The mill and hubs are ramped together because they are all driven axes in the same ride cycle. Restraints remain locked throughout. At rest, the existing transition to `Offloading` releases safety constraints. The Emergency Stop path continues to cut power and apply full braking immediately.

The physics implementation must verify the profile against the angular-acceleration, felt-G and jerk constraints in `docs/02-rotational-dynamics.md`, `docs/03-motor-power-and-torque.md`, and `docs/05-forces-and-g-forces.md`; the eight-second value is a starting tuning parameter, not proof of safety by itself.

### 4. Telemetry drives a truthful dashboard

Ride telemetry includes the configured duration, remaining cycle duration, and normalized stop-ramp progress/brake application. Remaining time is zero once stopping begins. The existing brake-engaged flag remains available for compatibility and reports whether any automatic brake application is active; a numeric brake-application field exposes the progressive value.

The dashboard shows the configured duration and a countdown during `Started`, then shows stopping progress during `Stopping`. The duration control is disabled during a cycle or stop. Its current value is initialized from server telemetry and follows confirmed server state.

### 5. Keep endpoints and DTOs in the owning module

A module-owned command and endpoint change the duration through the `Ride` aggregate. The request/response contract belongs in DigitalTwin Abstractions under the feature's `DataTransferObjects` namespace. The endpoint dispatches a command and contains no business logic. The C# implementation must verify this design against the authoritative style guide and applicable ADRs before coding.

## Risks and Trade-offs

- Progressive braking changes physics for every normal `Stopping` transition. Tests must cover cycle expiry and operator-requested normal stop, while proving Emergency Stop remains immediate.
- The current boolean engine-brake model cannot represent partial application. A normalized torque/application value is needed internally; telemetry should expose it without conflating partial braking with Emergency Stop.
- The eight-second profile must be checked with loaded and empty ride inertia, both rotation directions, and the documented felt-G/jerk calculations. If the profile violates the safety envelope, tune it and update the parameter derivation before implementation is considered complete.
- The dashboard setting is process-local in this proposal. Durable operator preferences can be proposed separately if required.

## Open Questions

- Confirm the supported dashboard duration range with ride-operation policy. This proposal requires positive whole-minute values and intentionally avoids inventing a product maximum.
