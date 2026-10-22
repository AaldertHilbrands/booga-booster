## MODIFIED Requirements

### Requirement: Ride lifecycle states

The ride SHALL have exactly one lifecycle state at any time, drawn from: **Idle**, **Loading**, **Safe**, **Started**, **Stopping**, **Offloading**, and **Emergency Stop**. A freshly created ride SHALL be **Idle**.

- **Idle** — the ride is at rest and empty, doing nothing.
- **Loading** — groups are being loaded into the gondolas and passengers are securing their restraints.
- **Safe** — everyone is loaded and every safety condition is met; the ride is safe to start.
- **Started** — the ride is running with safety constraints locked.
- **Stopping** — a controlled stop is in progress: mill and hub drive power are progressively reduced and braking torque is progressively increased until the ride reaches rest.
- **Offloading** — safety constraints are released and passengers leave the gondolas.
- **Emergency Stop** — an emergency stop is in progress: drive power is cut and full brakes are applied immediately from whatever active state the ride was in.

## ADDED Requirements

### Requirement: Ride cycle expiry automatically begins stopping

When a ride's elapsed simulation time in `Started` reaches its captured cycle duration, the simulation SHALL automatically transition the ride to `Stopping`. This automatic transition SHALL NOT be exposed as an operator-triggered transition. `Stopping` SHALL continue to hold safety constraints until the ride reaches rest, after which the existing `Offloading` transition SHALL occur.

#### Scenario: Configured cycle expiry transitions to Stopping

- **WHEN** a ride is `Started` and its elapsed cycle time reaches the captured duration
- **THEN** the ride automatically enters `Stopping` on that simulation advance
- **AND** the transition does not require an HTTP operator command

#### Scenario: Automatic expiry is not offered as a manual transition

- **WHEN** telemetry is read while the ride is `Started`
- **THEN** `Stopping` is not listed as an available operator-triggered transition

#### Scenario: Cycle stop preserves safety constraints until rest

- **WHEN** cycle expiry has moved the ride into `Stopping` and the ride is still moving
- **THEN** safety constraints remain locked
- **AND** the ride enters `Offloading` only after reaching a complete rest
