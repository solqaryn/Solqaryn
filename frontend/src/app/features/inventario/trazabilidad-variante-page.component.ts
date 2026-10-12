
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TrazabilidadVariantePanelComponent } from '../productos/trazabilidad-variante-panel.component';

@Component({
  selector: 'app-trazabilidad-variante-page',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, TrazabilidadVariantePanelComponent],
  template: `
    <section class="page" aria-labelledby="trace-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="trace-title">Trazabilidad de variante</h1>
          <p class="subtitle">Configura lote, número de serie y vencimiento sin agregar campos innecesarios a las variantes que no los utilizan.</p>
        </div>
        <a mat-stroked-button routerLink="/inventario/existencias">
          <mat-icon>arrow_back</mat-icon> Existencias
        </a>
      </header>
    
      @if (error()) {
        <div class="feedback error" role="alert">
          <mat-icon>error_outline</mat-icon><span>{{ error() }}</span>
        </div>
      }
    
      @if (productoVarianteId() > 0) {
        <app-trazabilidad-variante-panel
          [productoVarianteId]="productoVarianteId()">
        </app-trazabilidad-variante-panel>
      }
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host{display:block}.page{max-width:1180px;margin:0 auto;padding:24px}.header{display:flex;align-items:flex-start;justify-content:space-between;gap:16px;margin-bottom:22px}.eyebrow{margin:0 0 4px;font-size:12px;font-weight:800;letter-spacing:.08em;text-transform:uppercase;opacity:.65}h1{margin:0;font-size:clamp(24px,3vw,34px)}.subtitle{margin:6px 0 0;max-width:760px;opacity:.72}.feedback{display:flex;align-items:center;gap:9px;padding:14px;border-radius:10px}.feedback.error{border:1px solid rgba(244,67,54,.28);background:rgba(244,67,54,.07)}@media(max-width:640px){.page{padding:16px}.header{flex-direction:column}}
  `]
})
export class TrazabilidadVariantePageComponent implements OnInit {
  readonly productoVarianteId = signal(0);
  readonly error = signal('');

  constructor(private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    const varianteId = Number(this.route.snapshot.paramMap.get('varianteId') ?? 0);
    if (!Number.isInteger(varianteId) || varianteId <= 0) {
      this.error.set('La variante indicada no es válida.');
      return;
    }
    this.productoVarianteId.set(varianteId);
  }
}
