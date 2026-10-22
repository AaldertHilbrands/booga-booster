## ADDED Requirements

### Requirement: Ride cycle duration control

The operation controls SHALL let the operator set a positive whole-number ride-cycle duration in minutes, defaulting to five minutes. The control SHALL initialize from server state and submit changes through the DigitalTwin command path. It SHALL be enabled only while the ride is not `Started`, `Stopping`, or `EmergencyStop`; the selected value SHALL apply to the next cycle. The control SHALL be keyboard operable and expose an accessible name, value, and role.

#### Scenario: Dashboard shows the server's configured duration

- **WHEN** the operation controls load with telemetry reporting a five-minute cycle duration
- **THEN** the duration control displays five minutes

#### Scenario: Operator changes the next cycle duration

- **WHEN** the operator submits a valid duration while the ride is `Safe`
- **THEN** the dashboard sends the requested duration to the DigitalTwin
- **AND** the displayed value updates when the server confirms the change

#### Scenario: Duration control is disabled during a cycle or stop

- **WHEN** the ride is `Started`, `Stopping`, or `EmergencyStop`
- **THEN** the duration control is disabled
- **AND** no duration-change request can be sent

#### Scenario: Duration control is accessible

- **WHEN** assistive technology inspects or keyboard-operates the duration control
- **THEN** its accessible name, numeric value, and input role are exposed
- **AND** the control can be changed and submitted without a pointer

### Requirement: Ride cycle and stop progress display

The operation controls SHALL display remaining cycle time while the ride is `Started`, and SHALL display controlled-stop progress and brake application while it is `Stopping`. These values SHALL follow server telemetry rather than an independently advancing browser clock.

#### Scenario: Running cycle shows remaining time

- **WHEN** telemetry reports a `Started` ride with positive cycle time remaining
- **THEN** the dashboard displays the server-reported remaining time

#### Scenario: Stopping cycle shows stop progress

- **WHEN** telemetry reports the ride is `Stopping`
- **THEN** the dashboard identifies a controlled stop in progress
- **AND** displays the reported stop-ramp and brake-application progress
