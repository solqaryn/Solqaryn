
import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export type FeedbackStateType = 'loading' | 'empty' | 'success' | 'error' | 'warning';

@Component({
  selector: 'app-feedback-state',
  standalone: true,
  imports: [MatIconModule, MatProgressSpinnerModule],
  template: `
    <section
      class="app-feedback"
      [class]="'app-feedback app-feedback--' + type"
      [attr.role]="role"
      [attr.aria-live]="live"
      [attr.aria-busy]="type === 'loading' ? 'true' : null">
      <div class="app-feedback__icon" aria-hidden="true">
        @if (type === 'loading') {
          <mat-spinner diameter="22"></mat-spinner>
        } @else {
          <mat-icon>{{ icono }}</mat-icon>
        }
      </div>
      <div class="app-feedback__copy">
        <h2 class="app-feedback__title">{{ title }}</h2>
        @if (message) {
          <p class="app-feedback__message">{{ message }}</p>
        }
        <ng-content></ng-content>
      </div>
    </section>
  `
})
export class FeedbackStateComponent {
  @Input({ required: true }) type: FeedbackStateType = 'empty';
  @Input({ required: true }) title = '';
  @Input() message = '';

  get role(): 'alert' | 'status' {
    return this.type === 'error' ? 'alert' : 'status';
  }

  get live(): 'assertive' | 'polite' {
    return this.type === 'error' ? 'assertive' : 'polite';
  }

  get icono(): string {
    return ({
      empty: 'inbox',
      success: 'check_circle',
      error: 'error_outline',
      warning: 'warning_amber',
      loading: 'progress_activity'
    } satisfies Record<FeedbackStateType, string>)[this.type];
  }
}
