import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { RideState } from '../../models/ride.models';

@Component({
  selector: 'bb-cycle-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (state() === 'started') {
      <section aria-labelledby="cycle-status-heading">
        <h2 id="cycle-status-heading">Ride cycle</h2>
        <p class="remaining">
          <span>Time remaining</span>
          <output>{{ formatTime(remainingSeconds()) }}</output>
        </p>
      </section>
    } @else if (state() === 'stopping') {
      <section class="stop-status" aria-labelledby="stop-status-heading">
        <h2 id="stop-status-heading">Controlled stop</h2>
        <div class="progress-item">
          <div class="progress-heading">
            <span id="stop-ramp-label">Power ramp</span>
            <output>{{ formatProgress(stopRampProgress()) }}</output>
          </div>
          <progress
            aria-labelledby="stop-ramp-label"
            max="1"
            [value]="stopRampProgress()"
          ></progress>
        </div>
        <div class="progress-item">
          <div class="progress-heading">
            <span id="brake-application-label">Engine brake</span>
            <output>{{ formatProgress(brakeApplication()) }}</output>
          </div>
          <progress
            aria-labelledby="brake-application-label"
            max="1"
            [value]="brakeApplication()"
          ></progress>
        </div>
      </section>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    h2 {
      margin: 0 0 0.75rem;
      font-size: 1rem;
    }
    .remaining,
    .progress-heading {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 0.75rem;
      margin: 0;
    }
    output {
      font-variant-numeric: tabular-nums;
      font-weight: 600;
    }
    .stop-status,
    .progress-item {
      display: grid;
      gap: 0.65rem;
    }
    .progress-item {
      gap: 0.25rem;
    }
    progress {
      display: block;
      width: 100%;
      height: 0.75rem;
      accent-color: var(--bb-accent);
    }
  `,
})
export class CycleStatus {
  readonly state = input.required<RideState>();
  readonly remainingSeconds = input.required<number>();
  readonly stopRampProgress = input.required<number>();
  readonly brakeApplication = input.required<number>();

  protected formatTime(value: number): string {
    const seconds = Math.ceil(Math.max(0, value));
    const minutesPart = Math.floor(seconds / 60);
    const secondsPart = String(seconds % 60).padStart(2, '0');
    return `${minutesPart}:${secondsPart}`;
  }

  protected formatProgress(value: number): string {
    return `${Math.round(value * 100)}%`;
  }
}
