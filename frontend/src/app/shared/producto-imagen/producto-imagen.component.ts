import { Component, HostBinding, Input, OnChanges, SimpleChanges, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { cloudinaryResponsiveSrcset, cloudinaryResponsiveUrl } from '../cloudinary-image.util';

export type ProductoImagenVariant = 'option' | 'thumbnail' | 'line' | 'card' | 'hero' | 'gallery' | 'lightbox';

@Component({
  selector: 'app-producto-imagen',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div class="frame" [class.is-loaded]="loaded()" [class.is-fallback]="failed() || !src">
      @if (src && !failed()) {
        <img
          [src]="responsiveSrc"
          [attr.srcset]="responsiveSrcset"
          [attr.sizes]="responsiveSizes"
          [alt]="alt"
          [attr.width]="intrinsicSize"
          [attr.height]="intrinsicSize"
          [attr.loading]="loading"
          decoding="async"
          [attr.fetchpriority]="priority ? 'high' : null"
          (load)="onLoad()"
          (error)="onError()">
      } @else {
        <div class="fallback" role="img" [attr.aria-label]="fallbackLabel">
          <mat-icon aria-hidden="true">image_not_supported</mat-icon>
          <span>{{ fallbackText }}</span>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-width: 0;
      flex: 0 0 auto;
    }

    :host([data-variant='option']) { width: 34px; height: 34px; }
    :host([data-variant='thumbnail']) { width: 44px; height: 44px; }
    :host([data-variant='line']) { width: 52px; height: 52px; }
    :host([data-variant='card']) { width: 100%; max-width: 64px; aspect-ratio: 1; }
    :host([data-variant='hero']) { width: min(100%, 360px); aspect-ratio: 1; }
    :host([data-variant='gallery']) { width: 100%; height: 100%; }
    :host([data-variant='lightbox']) { width: min(92vw, 1100px); height: min(86dvh, 900px); }

    .frame {
      position: relative;
      width: 100%;
      height: 100%;
      min-width: 0;
      overflow: hidden;
      border: 1px solid var(--color-border);
      border-radius: 10px;
      background: color-mix(in srgb, var(--color-bg) 76%, var(--color-surface));
      color: var(--color-text-muted);
    }

    :host([data-variant='card']) .frame,
    :host([data-variant='hero']) .frame,
    :host([data-variant='gallery']) .frame {
      aspect-ratio: 1;
    }

    :host([data-variant='lightbox']) .frame {
      display: flex;
      align-items: center;
      justify-content: center;
      border-color: color-mix(in srgb, white 28%, transparent);
      background: #05070a;
    }

    img {
      display: block;
      width: 100%;
      height: 100%;
      object-fit: cover;
      opacity: 0;
      transition: opacity 160ms ease;
    }

    .is-loaded img { opacity: 1; }

    :host([data-variant='hero']) img,
    :host([data-variant='lightbox']) img {
      object-fit: contain;
    }

    .fallback {
      width: 100%;
      height: 100%;
      min-height: inherit;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 6px;
      padding: 8px;
      text-align: center;
      font-size: 11px;
      line-height: 1.25;
      overflow-wrap: anywhere;
    }

    .fallback mat-icon {
      width: 24px;
      height: 24px;
      font-size: 24px;
      color: currentColor;
    }

    :host([data-variant='option']) .fallback span,
    :host([data-variant='thumbnail']) .fallback span,
    :host([data-variant='line']) .fallback span,
    :host([data-variant='card']) .fallback span {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }

    :host([data-variant='option']) .frame,
    :host([data-variant='thumbnail']) .frame {
      border-radius: 8px;
    }

    @media (prefers-reduced-motion: reduce) {
      img { transition: none; }
    }
  `]
})
export class ProductoImagenComponent implements OnChanges {
  @Input() src: string | null | undefined;
  @Input() alt = 'Imagen del producto';
  @Input() fallbackLabel = 'Producto sin imagen disponible';
  @Input() fallbackText = 'Sin imagen';
  @Input() variant: ProductoImagenVariant = 'thumbnail';
  @Input() loading: 'lazy' | 'eager' = 'lazy';
  @Input() priority = false;

  readonly failed = signal(false);
  readonly loaded = signal(false);

  @HostBinding('attr.data-variant')
  get dataVariant(): ProductoImagenVariant {
    return this.variant;
  }

  get responsiveSrc(): string {
    return cloudinaryResponsiveUrl(this.src, Math.min(800, this.intrinsicSize));
  }

  get responsiveSrcset(): string | null {
    return cloudinaryResponsiveSrcset(this.src);
  }

  get responsiveSizes(): string {
    switch (this.variant) {
      case 'option': return '34px';
      case 'thumbnail': return '44px';
      case 'line': return '52px';
      case 'card': return '(max-width: 720px) 64px, 64px';
      case 'hero': return '(max-width: 720px) 92vw, 360px';
      case 'gallery': return '(max-width: 720px) 88vw, 480px';
      case 'lightbox': return '(max-width: 720px) 92vw, 800px';
    }
  }

  get intrinsicSize(): number {
    switch (this.variant) {
      case 'option': return 34;
      case 'thumbnail': return 44;
      case 'line': return 52;
      case 'card': return 64;
      case 'hero': return 720;
      case 'gallery': return 480;
      case 'lightbox': return 1400;
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['src']) {
      this.failed.set(false);
      this.loaded.set(false);
    }
  }

  onLoad(): void {
    this.loaded.set(true);
  }

  onError(): void {
    this.failed.set(true);
    this.loaded.set(false);
  }
}
