## MODIFIED Requirements

### Requirement: The brake is applied automatically while stopping

Entering the normal `Stopping` state SHALL begin a deterministic controlled-stop profile. Over the configured stop-ramp duration, commanded mill and hub drive power SHALL progressively decrease to zero while engine-brake torque SHALL progressively increase from zero toward full application. The profile SHALL use simulation time, SHALL not drive a motor in reverse, and SHALL stop applying additional brake torque if the ride reaches rest early. The ride SHALL remain in `Stopping` until rest, then enter `Offloading` and release the brake and safety constraints. Entering `EmergencyStop` SHALL remain distinct: it SHALL cut drive power and apply full braking immediately. Entering `Started` SHALL release the engine brake.

#### Scenario: Normal stopping progressively reduces power and applies brakes

- **WHEN** the ride enters `Stopping` while moving
- **THEN** commanded mill and hub power decrease over successive simulation ticks
- **AND** engine-brake torque increases over successive simulation ticks
- **AND** drive power reaches zero no later than the end of the stop-ramp duration

#### Scenario: Normal stop does not reverse the drive

- **WHEN** a normal stop ramp is in progress
- **THEN** drive power does not reverse direction to brake
- **AND** braking torque only opposes current rotation

#### Scenario: Ride settles before the ramp completes

- **WHEN** the ride reaches complete rest before the stop-ramp duration expires
- **THEN** it enters `Offloading` without applying further braking torque

#### Scenario: Emergency stop applies full braking immediately

- **WHEN** the ride enters `EmergencyStop`
- **THEN** mill and hub power are cut immediately
- **AND** full engine-brake torque is applied without the normal stop ramp

#### Scenario: Started releases the engine brake

- **WHEN** a new cycle enters `Started`
- **THEN** the engine brake is released
- **AND** the next cycle begins with zero elapsed cycle time

### Requirement: Telemetry reports progressive brake application

Ride telemetry SHALL retain the existing brake-engaged state and SHALL additionally report normalized engine-brake application from zero to full. During a normal stop, the reported value SHALL follow the applied brake torque; during Emergency Stop, it SHALL report full application.

#### Scenario: Telemetry follows a normal stop ramp

- **WHEN** the ride is in `Stopping`
- **THEN** telemetry reports the current normalized brake application
- **AND** that value increases as the stop ramp progresses unless the ride has already reached rest

#### Scenario: Emergency-stop telemetry reports full application

- **WHEN** the ride is in `EmergencyStop`
- **THEN** telemetry reports full engine-brake application
