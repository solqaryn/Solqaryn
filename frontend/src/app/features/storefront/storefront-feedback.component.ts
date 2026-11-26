import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Una región viva persistente para la confirmación de las acciones del usuario. */
@Component({
  selector: 'div[app-storefront-feedback]',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'role': 'status',
    'aria-live': 'polite',
    'aria-atomic': 'true',
    '[class.feedback-toast]': 'visible()',
    '[class.sr-only]': '!visible()'
  },
  template: '{{ mensaje() }}',
  styleUrl: './storefront-feedback.component.scss'
})
export class StorefrontFeedbackComponent {
  readonly mensaje = input.required<string>();
  readonly mostrarAviso = input(false);
  protected readonly visible = computed(() => this.mostrarAviso() && Boolean(this.mensaje()));
}
