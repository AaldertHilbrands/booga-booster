## 1. Backend cycle timing and domain behavior

- [ ] 1.1 Add named defaults for the five-minute cycle limit and eight-second normal-stop ramp; document their units and rationale in the physics parameter appendix.
- [x] 1.2 Extend the ride aggregate with a validated configurable duration and per-cycle elapsed simulation time; capture the limit on entry to `Started`, reset elapsed time for each new cycle, and reject changes while `Started`, `Stopping`, or `EmergencyStop`.
- [x] 1.3 Automatically enter `Stopping` when the captured cycle duration expires, using fixed-step simulation time and without exposing the automatic transition as an operator action.
- [x] 1.4 Add progressive normal-stop drive-power and brake-torque controls for the mill and hubs; settle early at rest and preserve immediate full braking for `EmergencyStop`.
- [x] 1.5 Add deterministic domain tests for default/configured duration, validation, timer boundaries, timer reset, zero-power timing, automatic transition, progressive power/brake behavior, early rest, and unchanged Emergency Stop behavior.

## 2. Backend contract and endpoint

- [x] 2.1 Add a DigitalTwin feature command/handler for changing the cycle duration; keep request/response DTOs in the Abstractions project's feature-specific `DataTransferObjects` namespace and validate through the aggregate.
- [x] 2.2 Add a module-owned endpoint for cycle-duration configuration; endpoint code only parses the request and dispatches the command.
- [x] 2.3 Extend ride telemetry with configured duration, remaining cycle time, stop-ramp progress, and normalized brake application while retaining existing fields.
- [x] 2.4 Add handler, endpoint, and telemetry tests, including rejected updates during a running/stopping cycle and instrument the handler according to the module's observability conventions.

## 3. Physics documentation

- [ ] 3.1 Update `docs/03-motor-power-and-torque.md` to distinguish the normal cycle-stop ramp from Emergency Stop and describe the power/brake progression.
- [ ] 3.2 Verify or revise the eight-second ramp against empty and loaded inertia, both rotation directions, peak felt-G, and jerk; update the value if needed.
- [ ] 3.3 Update `docs/appendix-parameters.md` with the cycle default and verified stop-ramp parameter.

## 4. Dashboard

- [x] 4.1 Add cycle-duration configuration to the operator controls, initialized from server telemetry, submitted through the new API, and disabled during `Started`, `Stopping`, and `EmergencyStop`.
- [x] 4.2 Show server-reported remaining cycle time while `Started` and controlled-stop/brake progress while `Stopping`.
- [x] 4.3 Add focused component and service tests for initialization, valid/invalid changes, disabled states, countdown/progress mapping, and server-confirmed updates.
- [x] 4.4 Add or update AXE accessibility coverage for the duration input and stop-progress indication.

## 5. Verify

- [x] 5.1 Consult the authoritative C# style guide and verify domain, CQRS, DTO, endpoint, telemetry, and test placement against applicable ADRs before implementation.
- [x] 5.2 Run focused DigitalTwin tests, then the backend solution tests; verify changed module coverage remains at least 80%.
- [x] 5.3 Run focused Angular tests and AXE checks, then the frontend test suite.
- [ ] 5.4 Manually verify the default five-minute countdown, a changed duration on the next cycle, gradual mill/hub power reduction and brake application, normal offloading at rest, and immediate Emergency Stop behavior.
