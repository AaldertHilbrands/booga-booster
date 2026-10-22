## ADDED Requirements

### Requirement: Ride cycle duration is configurable

The DigitalTwin SHALL use a ride-cycle duration that defaults to five minutes. An operator SHALL be able to configure a positive whole-number duration in minutes from the dashboard while the ride is not `Started`, `Stopping`, or `EmergencyStop`. A valid setting SHALL apply to the next cycle. An invalid setting SHALL be rejected without changing the current setting. The setting SHALL be visible in ride telemetry and SHALL reset to five minutes when the application process restarts.

#### Scenario: New ride uses the default cycle duration

- **WHEN** a new DigitalTwin ride is created
- **THEN** its configured cycle duration is five minutes

#### Scenario: Operator configures the next cycle

- **WHEN** the operator sets the cycle duration to a positive whole number of minutes while the ride is `Safe`
- **THEN** the server accepts the value
- **AND** telemetry reports the updated configured duration
- **AND** the next cycle uses that duration

#### Scenario: Invalid duration is rejected

- **WHEN** the operator submits a zero, negative, fractional, or unrepresentable duration
- **THEN** the server returns a client error
- **AND** the configured duration remains unchanged

#### Scenario: Duration cannot change during a cycle or stop

- **WHEN** the ride is `Started`, `Stopping`, or `EmergencyStop`
- **AND** the operator attempts to change the configured duration
- **THEN** the request is rejected
- **AND** the active cycle's captured duration remains unchanged

### Requirement: Cycle duration starts and expires deterministically

The ride SHALL capture the configured cycle duration when it enters `Started`. The timer SHALL advance using fixed simulation time for each tick spent in `Started`, independent of commanded mill power. When elapsed cycle time reaches the captured duration, the simulation SHALL automatically enter `Stopping` without an operator command. The cycle timer SHALL reset on the next successful entry into `Started`.

#### Scenario: Cycle clock starts on the Started transition

- **WHEN** a ride successfully transitions from `Safe` to `Started`
- **THEN** its elapsed cycle time is zero
- **AND** the captured cycle duration is the currently configured duration

#### Scenario: Cycle clock advances in simulation time

- **WHEN** the ride remains `Started` for simulation steps
- **THEN** elapsed cycle time advances by each fixed step, including when mill power is zero or the operator has engaged the engine brake

#### Scenario: Cycle expiry begins a controlled stop

- **WHEN** elapsed cycle time reaches the captured duration
- **THEN** the ride automatically enters `Stopping`
- **AND** the configured cycle countdown reports zero
- **AND** the ride does not enter `EmergencyStop`

#### Scenario: New cycle captures a changed duration

- **WHEN** an operator changes the configured duration while the ride is not running and later starts a new cycle
- **THEN** the new cycle uses the changed duration
- **AND** elapsed time from a prior cycle is not retained

### Requirement: Cycle expiry ramps down drive and braking

When a normal controlled stop begins, the simulation SHALL progressively reduce mill and hub drive power to zero and progressively increase braking torque over the configured stop-ramp duration. The ramp SHALL use simulation time and SHALL not reverse any motor or apply power that opposes the stop. The ride SHALL remain in `Stopping` until it reaches rest, then SHALL follow the existing `Offloading` transition. Safety constraints SHALL remain locked until the ride is at rest. Emergency Stop SHALL retain its immediate full-brake behavior.

#### Scenario: Cycle expiry progressively reduces power and increases braking

- **WHEN** the configured ride-cycle duration expires while the ride is moving
- **THEN** commanded mill and hub power decrease over successive simulation ticks
- **AND** applied engine-brake torque increases over successive simulation ticks
- **AND** both drive powers reach zero no later than the end of the stop ramp

#### Scenario: Safety constraints remain locked during the stop ramp

- **WHEN** the ride is in `Stopping` and still moving
- **THEN** restraints remain locked
- **AND** they are released only after the ride reaches rest and enters `Offloading`

#### Scenario: Stop ramp settles early at rest

- **WHEN** the ride reaches a complete rest before the configured ramp duration elapses
- **THEN** it immediately proceeds to `Offloading`
- **AND** it does not continue applying braking torque that could drive the rig in reverse

#### Scenario: Emergency Stop remains immediate

- **WHEN** an Emergency Stop is requested during a ride cycle
- **THEN** engine power is cut and full braking is applied immediately
- **AND** the normal cycle-end ramp is bypassed

### Requirement: Cycle and stop progress are observable

Ride telemetry SHALL report the configured cycle duration, remaining cycle time, stop-ramp progress, and current normalized brake application. Remaining cycle time SHALL be zero during `Stopping`. The dashboard SHALL display the remaining time while `Started` and the stop progress while `Stopping`.

#### Scenario: Telemetry reports cycle countdown

- **WHEN** telemetry is read while the ride is `Started`
- **THEN** it includes the configured duration and non-negative remaining cycle time

#### Scenario: Telemetry reports stop progress

- **WHEN** telemetry is read while the ride is `Stopping`
- **THEN** it includes normalized stop-ramp progress and brake application
- **AND** the dashboard indicates that a controlled stop is in progress
