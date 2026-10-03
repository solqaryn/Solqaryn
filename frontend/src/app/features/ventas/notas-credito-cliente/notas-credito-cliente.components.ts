import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PermisosRuntimeService } from '../../../core/auth/permisos-runtime.service';
import { NotaCreditoClienteService } from './nota-credito-cliente.service';
import { CreateNotaCreditoCliente, NotaCreditoCliente } from './nota-credito-cliente.model';

const PANEL_STYLES = `
  :host{display:block}.page{max-width:980px;margin:0 auto;padding:24px}.hero{display:flex;gap:16px;align-items:flex-start;justify-content:space-between;margin-bottom:20px}.hero h1{margin:0 0 6px}.muted{color:var(--text-secondary,#667085)}.card{background:var(--surface,#fff);border:1px solid rgba(127,127,127,.22);border-radius:16px;padding:20px;box-shadow:0 10px 30px rgba(15,23,42,.06)}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.field{display:flex;flex-direction:column;gap:6px}.field.full{grid-column:1/-1}label{font-weight:600}input,textarea{border:1px solid rgba(127,127,127,.35);border-radius:10px;padding:11px 12px;font:inherit;background:transparent;color:inherit}textarea{min-height:96px;resize:vertical}.actions{display:flex;gap:10px;flex-wrap:wrap;margin-top:18px}.btn{display:inline-flex;align-items:center;justify-content:center;border:0;border-radius:10px;padding:10px 14px;font:inherit;font-weight:700;cursor:pointer;text-decoration:none}.btn.primary{background:#2563eb;color:#fff}.btn.secondary{background:rgba(127,127,127,.12);color:inherit}.btn:disabled{opacity:.55;cursor:not-allowed}.alert{margin-top:14px;padding:12px 14px;border-radius:10px;background:#fff1f2;color:#9f1239}.facts{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.fact{padding:12px;border-radius:10px;background:rgba(127,127,127,.08)}.fact strong{display:block;font-size:.82rem;color:var(--text-secondary,#667085);margin-bottom:4px}@media(max-width:720px){.page{padding:16px}.hero{flex-direction:column}.grid,.facts{grid-template-columns:1fr}.field.full{grid-column:auto}}
`;

@Component({
  selector: 'app-notas-credito-cliente-home',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <section class="page">
      <div class="hero">
        <div>
          <h1>Notas de crédito de cliente</h1>
          <p class="muted">Consulta una nota existente por su identificador o registra una nueva contra una factura autorizada.</p>
        </div>
        @if (permisos.puede('Ventas','Crear')) {
          <a class="btn primary" routerLink="/notas-credito-cliente/nueva">Nueva nota</a>
        }
      </div>
      <div class="card">
        <form (ngSubmit)="consultar()" class="grid">
          <div class="field full">
            <label for="nota-id">Identificador de la nota</label>
            <input id="nota-id" name="notaId" type="number" min="1" step="1" required [(ngModel)]="notaId" autocomplete="off">
          </div>
          <div class="actions field full">
            <button class="btn primary" type="submit" [disabled]="!notaId || notaId < 1">Consultar</button>
          </div>
        </form>
      </div>
    </section>
  `,
  styles: [PANEL_STYLES]
})
export class NotasCreditoClienteHomeComponent {
  notaId?: number;
  constructor(private readonly router: Router, public readonly permisos: PermisosRuntimeService) {}
  consultar(): void {
    const id = Number(this.notaId);
    if (Number.isInteger(id) && id > 0) void this.router.navigate(['/notas-credito-cliente', id]);
  }
}

@Component({
  selector: 'app-nota-credito-cliente-detail',
  standalone: true,
  imports: [DatePipe, DecimalPipe, RouterLink],
  template: `
    <section class="page">
      <div class="hero">
        <div><h1>Detalle de nota de crédito</h1><p class="muted">Información persistida por el API certificado.</p></div>
        <a class="btn secondary" routerLink="/notas-credito-cliente">Volver</a>
      </div>
      @if (cargando) { <div class="card" role="status">Cargando nota…</div> }
      @if (error) { <div class="alert" role="alert">{{ error }}</div> }
      @if (nota; as n) {
        <article class="card facts" aria-label="Detalle de la nota de crédito">
          <div class="fact"><strong>ID</strong>{{ n.id }}</div>
          <div class="fact"><strong>Factura</strong>{{ n.facturaId }}</div>
          <div class="fact"><strong>Venta</strong>{{ n.ventaId }}</div>
          <div class="fact"><strong>Moneda</strong>{{ n.moneda }}</div>
          <div class="fact"><strong>Monto crédito</strong>{{ n.montoCredito | number:'1.2-4' }}</div>
          <div class="fact"><strong>Creada</strong>{{ n.fechaCreacion | date:'medium' }}</div>
          <div class="fact full"><strong>Motivo</strong>{{ n.motivo }}</div>
          @if (n.observaciones) { <div class="fact full"><strong>Observaciones</strong>{{ n.observaciones }}</div> }
          @if (n.creadoPorNombreUsuario) { <div class="fact full"><strong>Creada por</strong>{{ n.creadoPorNombreUsuario }}</div> }
        </article>
      }
    </section>
  `,
  styles: [PANEL_STYLES]
})
export class NotaCreditoClienteDetailComponent implements OnInit {
  nota?: NotaCreditoCliente;
  cargando = true;
  error = '';
  constructor(private readonly route: ActivatedRoute, private readonly service: NotaCreditoClienteService) {}
  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id <= 0) { this.cargando = false; this.error = 'Identificador de nota inválido.'; return; }
    this.service.getById(id).subscribe({
      next: r => { this.nota = r.data; this.cargando = false; },
      error: () => { this.error = 'No fue posible cargar la nota de crédito.'; this.cargando = false; }
    });
  }
}

@Component({
  selector: 'app-nota-credito-cliente-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <section class="page">
      <div class="hero">
        <div>
          <h1>Nueva nota de crédito de cliente</h1>
          <p class="muted">Registra únicamente los campos autorizados por el API. La moneda y la venta se derivan de la factura origen.</p>
        </div>
        <a class="btn secondary" routerLink="/notas-credito-cliente">Cancelar</a>
      </div>
      <form class="card grid" (ngSubmit)="guardar()" #form="ngForm">
        <div class="field">
          <label for="factura-id">Factura</label>
          <input id="factura-id" name="facturaId" type="number" min="1" step="1" required [(ngModel)]="dto.facturaId">
        </div>
        <div class="field">
          <label for="monto">Monto de crédito</label>
          <input id="monto" name="montoCredito" type="number" min="0.0001" step="0.0001" required [(ngModel)]="dto.montoCredito">
        </div>
        <div class="field full">
          <label for="motivo">Motivo</label>
          <input id="motivo" name="motivo" maxlength="500" required [(ngModel)]="dto.motivo">
        </div>
        <div class="field full">
          <label for="observaciones">Observaciones <span class="muted">(opcional)</span></label>
          <textarea id="observaciones" name="observaciones" maxlength="1000" [(ngModel)]="dto.observaciones"></textarea>
        </div>
        @if (error) { <div class="alert field full" role="alert">{{ error }}</div> }
        <div class="actions field full">
          <button class="btn primary" type="submit" [disabled]="guardando || form.invalid">{{ guardando ? 'Guardando…' : 'Crear nota' }}</button>
        </div>
      </form>
    </section>
  `,
  styles: [PANEL_STYLES]
})
export class NotaCreditoClienteFormComponent {
  dto: CreateNotaCreditoCliente = { facturaId: 0, montoCredito: 0, motivo: '', observaciones: '' };
  guardando = false;
  error = '';
  constructor(private readonly service: NotaCreditoClienteService, private readonly router: Router) {}
  guardar(): void {
    const payload: CreateNotaCreditoCliente = {
      facturaId: Number(this.dto.facturaId),
      montoCredito: Number(this.dto.montoCredito),
      motivo: this.dto.motivo.trim(),
      observaciones: this.dto.observaciones?.trim() || null
    };
    if (!Number.isInteger(payload.facturaId) || payload.facturaId <= 0 || payload.montoCredito <= 0 || !payload.motivo) {
      this.error = 'Completa factura, monto y motivo con valores válidos.';
      return;
    }
    this.guardando = true; this.error = '';
    this.service.create(payload).subscribe({
      next: r => void this.router.navigate(['/notas-credito-cliente', r.data.id]),
      error: () => { this.error = 'No fue posible crear la nota de crédito. Revisa la factura y los datos ingresados.'; this.guardando = false; }
    });
  }
}
