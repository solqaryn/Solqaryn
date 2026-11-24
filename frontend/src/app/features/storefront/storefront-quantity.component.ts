import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'div[appStorefrontQuantity]',
  standalone: true,
  templateUrl: './storefront-quantity.component.html',
  styleUrl: './storefront-quantity.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontQuantityComponent {
  readonly inputId = input.required<string>();
  readonly inputLabel = input.required<string>();
  readonly decreaseLabel = input.required<string>();
  readonly increaseLabel = input.required<string>();
  readonly minimum = input.required<number>();
  readonly maximum = input.required<number>();
  readonly quantity = input.required<number>();
  readonly inputDisabled = input(false);
  readonly decreaseDisabled = input.required<boolean>();
  readonly increaseDisabled = input.required<boolean>();
  readonly decrease = output<void>();
  readonly increase = output<void>();
  readonly quantityChange = output<string>();
}
