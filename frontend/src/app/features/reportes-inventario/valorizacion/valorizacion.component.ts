import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PermisosRuntimeService } from '../../../core/auth/permisos-runtime.service';
import { ReporteInventarioValorizacionResumenDto, ReporteInventarioValorizacionService } from '../../../core/services/reporte-inventario-valorizacion.service';

@Component({
  selector: 'app-reporte-inventario-valorizacion', standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatProgressSpinnerModule],
  template: `<section class="report-page" aria-labelledby="valorizacion-title"><header><h2 id="valorizacion-title">Valorización de inventario</h2><p>Resumen server-side; los importes financieros respetan la redacción del API.</p></header>@if (loading) { <div class="state" role="status"><mat-spinner diameter="32"></mat-spinner><span>Cargando valorización…</span></div> } @else if (error) { <div class="state error" role="alert"><span>{{ error }}</span><button mat-stroked-button type="button" (click)="cargar()">Reintentar</button></div> } @else if (!puedeVerFinanzas()) { <div class="state" role="status">El reporte está disponible, pero los importes financieros están ocultos por permisos.</div> } @else if (resumen) { <div class="cards"><mat-card><mat-card-header><mat-card-title>Costo total</mat-card-title></mat-card-header><mat-card-content>{{ resumen.valorInventarioCosto | currency }}</mat-card-content></mat-card><mat-card><mat-card-header><mat-card-title>Mercadería</mat-card-title></mat-card-header><mat-card-content>{{ resumen.valorInventarioCostoMercaderia | currency }}</mat-card-content></mat-card><mat-card><mat-card-header><mat-card-title>Insumos administrativos</mat-card-title></mat-card-header><mat-card-content>{{ resumen.valorInventarioCostoInsumosAdministrativos | currency }}</mat-card-content></mat-card><mat-card><mat-card-header><mat-card-title>Venta potencial</mat-card-title></mat-card-header><mat-card-content>{{ resumen.valorPotencialVentaMercaderia | currency }}</mat-card-content></mat-card></div> }</section>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`.report-page{display:grid;gap:1rem}.report-page header h2,.report-page header p{margin:0}.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:1rem}.state{display:flex;align-items:center;gap:.75rem;padding:1rem;border:1px solid var(--border-color,#d1d5db);border-radius:.65rem}.error{border-color:#b91c1c}`]
})
export class ValorizacionComponent implements OnInit {
  private readonly service = inject(ReporteInventarioValorizacionService); private readonly permisos = inject(PermisosRuntimeService);
  resumen: ReporteInventarioValorizacionResumenDto | null = null; loading = false; error = '';
  ngOnInit(): void { this.cargar(); }
  puedeVerFinanzas(): boolean { return this.permisos.puede('Finanzas', 'Ver'); }
  cargar(): void { this.loading = true; this.error = ''; this.service.getResumen().subscribe({ next: res => { this.resumen = res.data; this.loading = false; }, error: () => { this.error = 'No se pudo cargar la valorización.'; this.loading = false; } }); }
}
