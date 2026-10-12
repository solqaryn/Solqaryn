import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { debounceTime, Subject } from 'rxjs';
import { PermisosRuntimeService } from '../../../core/auth/permisos-runtime.service';
import {
  CreateNotaCreditoProveedor,
  EstadoNotaCreditoProveedor,
  EstadoNotaCreditoProveedorNombre,
  NotaCreditoProveedor,
  UpdateNotaCreditoProveedor
} from '../../../core/models/nota-credito-proveedor.model';
import { NotaCreditoProveedorService } from '../../../services/nota-credito-proveedor.service';

const UI = [
  CommonModule,
  RouterLink,
  MatButtonModule,
  MatCardModule,
  MatFormFieldModule,
  MatIconModule,
  MatInputModule,
  MatSelectModule,
  MatSnackBarModule,
  MatProgressSpinnerModule
];

export function estadoNombre(e: EstadoNotaCreditoProveedor): string {
  if (e === 1 || e === 'Borrador') return 'Borrador';
  if (e === 2 || e === 'Registrada') return 'Registrada';
  if (e === 3 || e === 'Anulada') return 'Anulada';
  return String(e);
}

export function esEstadoRegistrada(e: EstadoNotaCreditoProveedor): boolean {
  return estadoNombre(e) === 'Registrada';
}

@Component({
  selector: 'app-notas-credito-proveedor-list',
  standalone: true,
  imports: UI,
  template: `<section class="page"><header><div><h1>Notas de crédito de proveedor</h1><p>Gestión de créditos documentales asociados a facturas de proveedor.</p></div>@if(puedeCrear()){<a mat-flat-button color="primary" routerLink="/notas-credito-proveedor/nueva"><mat-icon>add</mat-icon>Nueva nota</a>}</header><mat-card><mat-card-content><div class="filters"><mat-form-field appearance="outline"><mat-label>Buscar</mat-label><input matInput [value]="search" (input)="buscar($any($event.target).value)"></mat-form-field><mat-form-field appearance="outline"><mat-label>Estado</mat-label><mat-select [value]="estado" (selectionChange)="filtrar($event.value)"><mat-option value="">Todos</mat-option><mat-option value="Borrador">Borrador</mat-option><mat-option value="Registrada">Registrada</mat-option><mat-option value="Anulada">Anulada</mat-option></mat-select></mat-form-field></div>@if(loading()){<div class="center"><mat-spinner diameter="40"></mat-spinner></div>}@else if(error()){<div class="center">No fue posible cargar las notas. <button mat-button (click)="cargar()">Reintentar</button></div>}@else{<div class="table"><table><thead><tr><th>Número</th><th>Proveedor</th><th>Factura</th><th>Emisión</th><th>Total</th><th>Estado</th><th></th></tr></thead><tbody>@for(n of notas();track n.id){<tr><td>{{n.numeroNotaCredito}}</td><td>{{n.proveedorNombreSnapshot}}</td><td>#{{n.facturaProveedorId}}</td><td>{{n.fechaEmisionUtc|date:'dd/MM/yyyy'}}</td><td>{{n.moneda}} {{n.totalCredito|number:'1.2-2'}}</td><td>{{nombreEstado(n.estado)}}</td><td><a mat-icon-button [routerLink]="['/notas-credito-proveedor',n.id]" aria-label="Ver nota de crédito"><mat-icon>visibility</mat-icon></a></td></tr>}@empty{<tr><td colspan="7">Sin resultados.</td></tr>}</tbody></table></div><footer><span>{{totalCount()}} registro(s)</span><div><button mat-button [disabled]="page<=1" (click)="pagina(page-1)">Anterior</button><span>Página {{page}}</span><button mat-button [disabled]="page*pageSize>=totalCount()" (click)="pagina(page+1)">Siguiente</button></div></footer>}</mat-card-content></mat-card></section>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`.page{max-width:1200px;margin:auto;padding:16px}header,footer{display:flex;justify-content:space-between;gap:16px;align-items:center}header{margin-bottom:16px}h1{margin:0}.filters{display:grid;grid-template-columns:1fr 220px;gap:12px}.center{text-align:center;padding:36px}.table{overflow:auto}table{width:100%;border-collapse:collapse}th,td{padding:12px;text-align:left;border-bottom:1px solid var(--color-border)}footer{padding-top:12px}footer div{display:flex;gap:8px;align-items:center}@media(max-width:700px){header,footer{align-items:flex-start;flex-direction:column}.filters{grid-template-columns:1fr}}`]
})
export class NotasCreditoProveedorListComponent implements OnInit {
  private readonly service = inject(NotaCreditoProveedorService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly q = new Subject<string>();
  readonly notas = signal<NotaCreditoProveedor[]>([]);
  readonly loading = signal(false);
  readonly error = signal(false);
  readonly totalCount = signal(0);
  readonly puedeCrear = signal(false);
  page = 1;
  pageSize = 10;
  search = '';
  estado: EstadoNotaCreditoProveedorNombre | '' = '';

  ngOnInit() {
    this.puedeCrear.set(this.permisos.puede('Compras', 'Crear'));
    this.q.pipe(debounceTime(300)).subscribe(() => {
      this.page = 1;
      this.cargar();
    });
    this.cargar();
  }

  buscar(v: string) { this.search = v; this.q.next(v); }
  filtrar(v: EstadoNotaCreditoProveedorNombre | '') { this.estado = v; this.page = 1; this.cargar(); }
  pagina(v: number) { this.page = Math.max(1, v); this.cargar(); }
  cargar() {
    this.loading.set(true);
    this.error.set(false);
    this.service.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      search: this.search.trim() || null,
      estado: this.estado || null,
      sortBy: 'FechaEmisionUtc',
      sortDirection: 'desc'
    }).subscribe({
      next: r => {
        this.notas.set(r.data.items);
        this.totalCount.set(r.data.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      }
    });
  }
  nombreEstado = estadoNombre;
}

@Component({
  selector: 'app-nota-credito-proveedor-form',
  standalone: true,
  imports: [...UI, ReactiveFormsModule],
  template: `<section class="page"><header><h1>{{esEdicion?'Editar':'Nueva'}} nota de crédito</h1><a mat-stroked-button routerLink="/notas-credito-proveedor">Cancelar</a></header>@if(cargando()){<div class="center"><mat-spinner diameter="40"></mat-spinner></div>}@else{<form [formGroup]="form" (ngSubmit)="guardar()"><mat-card><mat-card-content class="grid"><mat-form-field appearance="outline"><mat-label>Número</mat-label><input matInput formControlName="numeroNotaCredito"></mat-form-field>@if(!esEdicion){<mat-form-field appearance="outline"><mat-label>Factura proveedor ID</mat-label><input matInput type="number" formControlName="facturaProveedorId"></mat-form-field><mat-form-field appearance="outline"><mat-label>Devolución proveedor ID (opcional)</mat-label><input matInput type="number" formControlName="devolucionProveedorId"></mat-form-field>}<mat-form-field appearance="outline"><mat-label>Emisión</mat-label><input matInput type="date" formControlName="fechaEmisionUtc"></mat-form-field><mat-form-field appearance="outline"><mat-label>Moneda</mat-label><input matInput maxlength="3" formControlName="moneda"></mat-form-field><mat-form-field appearance="outline"><mat-label>Referencia fiscal</mat-label><input matInput maxlength="120" formControlName="referenciaFiscal"></mat-form-field><mat-form-field appearance="outline"><mat-label>Motivo</mat-label><textarea matInput maxlength="500" formControlName="motivo"></textarea></mat-form-field><mat-form-field appearance="outline"><mat-label>Observaciones</mat-label><textarea matInput maxlength="1000" formControlName="observaciones"></textarea></mat-form-field><mat-form-field appearance="outline"><mat-label>Subtotal crédito</mat-label><input matInput type="number" min="0" step="0.01" formControlName="subtotalCredito"></mat-form-field><mat-form-field appearance="outline"><mat-label>Impuesto crédito</mat-label><input matInput type="number" min="0" step="0.01" formControlName="impuestoCredito"></mat-form-field></mat-card-content></mat-card><div class="actions"><button mat-flat-button color="primary" type="submit" [disabled]="guardando()">Guardar</button></div></form>}</section>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`.page{max-width:980px;margin:auto;padding:16px}header,.actions{display:flex;justify-content:space-between;align-items:center;gap:12px;margin-bottom:16px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:10px}.center{text-align:center;padding:36px}@media(max-width:700px){.grid{grid-template-columns:1fr}}`]
})
export class NotaCreditoProveedorFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(NotaCreditoProveedorService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly snack = inject(MatSnackBar);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  esEdicion = false;
  notaId?: number;
  readonly form = this.fb.group({
    numeroNotaCredito: ['', [Validators.required, Validators.maxLength(80)]],
    facturaProveedorId: [null as number | null, Validators.required],
    devolucionProveedorId: [null as number | null],
    fechaEmisionUtc: [new Date().toISOString().slice(0, 10), Validators.required],
    moneda: ['HNL', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    referenciaFiscal: ['', Validators.maxLength(120)],
    motivo: ['', [Validators.required, Validators.maxLength(500)]],
    observaciones: ['', Validators.maxLength(1000)],
    subtotalCredito: [0, [Validators.required, Validators.min(0)]],
    impuestoCredito: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (Number.isInteger(id) && id > 0) {
      this.esEdicion = true;
      this.notaId = id;
      this.form.controls.facturaProveedorId.clearValidators();
      this.form.controls.facturaProveedorId.updateValueAndValidity();
      this.cargar(id);
    }
  }

  guardar() {
    const accion = this.esEdicion ? 'Editar' : 'Crear';
    if (!this.permisos.puede('Compras', accion)) {
      this.snack.open('No tiene permiso para esta acción.', 'Cerrar', { duration: 3500 });
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snack.open('Complete los datos requeridos.', 'Cerrar', { duration: 3500 });
      return;
    }
    const r = this.form.getRawValue();
    const common: UpdateNotaCreditoProveedor = {
      numeroNotaCredito: (r.numeroNotaCredito || '').trim(),
      fechaEmisionUtc: this.iso(r.fechaEmisionUtc || ''),
      moneda: (r.moneda || 'HNL').trim().toUpperCase(),
      referenciaFiscal: r.referenciaFiscal?.trim() || null,
      motivo: (r.motivo || '').trim(),
      observaciones: r.observaciones?.trim() || null,
      subtotalCredito: Number(r.subtotalCredito || 0),
      impuestoCredito: Number(r.impuestoCredito || 0)
    };
    this.guardando.set(true);
    const q = this.esEdicion && this.notaId
      ? this.service.update(this.notaId, common)
      : this.service.create({
          ...common,
          facturaProveedorId: Number(r.facturaProveedorId),
          devolucionProveedorId: r.devolucionProveedorId ? Number(r.devolucionProveedorId) : null
        } as CreateNotaCreditoProveedor);
    q.subscribe({
      next: x => {
        this.guardando.set(false);
        void this.router.navigate(['/notas-credito-proveedor', x.data.id]);
      },
      error: e => {
        this.guardando.set(false);
        this.snack.open(e.error?.detail || e.error?.message || 'No fue posible guardar.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  private cargar(id: number) {
    this.cargando.set(true);
    this.service.getById(id).subscribe({
      next: r => {
        const n = r.data;
        if (estadoNombre(n.estado) !== 'Borrador') {
          void this.router.navigate(['/notas-credito-proveedor', n.id]);
          return;
        }
        this.form.patchValue({
          numeroNotaCredito: n.numeroNotaCredito,
          fechaEmisionUtc: n.fechaEmisionUtc.slice(0, 10),
          moneda: n.moneda,
          referenciaFiscal: n.referenciaFiscal || '',
          motivo: n.motivo,
          observaciones: n.observaciones || '',
          subtotalCredito: n.subtotalCredito,
          impuestoCredito: n.impuestoCredito
        });
        this.cargando.set(false);
      },
      error: () => {
        this.cargando.set(false);
        this.snack.open('No fue posible cargar la nota.', 'Cerrar', { duration: 4000 });
      }
    });
  }

  private iso(v: string) { return new Date(v + 'T00:00:00.000Z').toISOString(); }
}

@Component({
  selector: 'app-nota-credito-proveedor-detail',
  standalone: true,
  imports: [...UI, ReactiveFormsModule],
  template: `<section class="page"><header><div><h1>Nota de crédito {{nota()?.numeroNotaCredito||''}}</h1><p>Detalle y ciclo de vida documental.</p></div><a mat-stroked-button routerLink="/notas-credito-proveedor">Volver</a></header>@if(loading()){<div class="center"><mat-spinner diameter="40"></mat-spinner></div>}@else if(error()){<div class="center">No fue posible cargar la nota.</div>}@else if(nota();as n){<mat-card><mat-card-content><dl><div><dt>Proveedor</dt><dd>{{n.proveedorNombreSnapshot}}</dd></div><div><dt>Factura</dt><dd>#{{n.facturaProveedorId}}</dd></div><div><dt>Emisión</dt><dd>{{n.fechaEmisionUtc|date:'dd/MM/yyyy'}}</dd></div><div><dt>Total</dt><dd>{{n.moneda}} {{n.totalCredito|number:'1.2-2'}}</dd></div><div><dt>Estado</dt><dd>{{nombreEstado(n.estado)}}</dd></div><div><dt>Motivo</dt><dd>{{n.motivo}}</dd></div></dl><div class="actions">@if(esBorrador(n)&&puedeEditar()){<a mat-stroked-button [routerLink]="['/notas-credito-proveedor',n.id,'editar']">Editar</a>}@if(esBorrador(n)&&puedeRegistrar()){<button mat-flat-button color="primary" (click)="registrar(n.id)">Registrar</button>}@if(esRegistrada(n)&&puedeAnular()){<mat-form-field appearance="outline"><mat-label>Motivo de anulación</mat-label><input matInput [formControl]="motivoAnulacion"></mat-form-field><button mat-stroked-button color="warn" [disabled]="motivoAnulacion.invalid" (click)="anular(n.id)">Anular</button>}</div></mat-card-content></mat-card>}</section>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`.page{max-width:980px;margin:auto;padding:16px}header{display:flex;justify-content:space-between;gap:12px;align-items:center;margin-bottom:16px}dl{display:grid;grid-template-columns:1fr 1fr;gap:16px}dt{font-size:.8rem;color:var(--color-text-muted)}dd{margin:4px 0 0}.actions{display:flex;gap:12px;align-items:center;flex-wrap:wrap;margin-top:20px}.center{text-align:center;padding:36px}@media(max-width:700px){dl{grid-template-columns:1fr}header{align-items:flex-start;flex-direction:column}}`]
})
export class NotaCreditoProveedorDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(NotaCreditoProveedorService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly snack = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  readonly nota = signal<NotaCreditoProveedor | null>(null);
  readonly loading = signal(false);
  readonly error = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeRegistrar = signal(false);
  readonly puedeAnular = signal(false);
  readonly motivoAnulacion = this.fb.control('', [Validators.required, Validators.maxLength(500)]);

  ngOnInit() {
    this.puedeEditar.set(this.permisos.puede('Compras', 'Editar'));
    this.puedeRegistrar.set(this.permisos.puede('Compras', 'Confirmar'));
    this.puedeAnular.set(this.permisos.puede('Compras', 'Anular'));
    this.cargar();
  }

  cargar() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id <= 0) {
      this.error.set(true);
      return;
    }
    this.loading.set(true);
    this.service.getById(id).subscribe({
      next: r => {
        this.nota.set(r.data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      }
    });
  }

  registrar(id: number) {
    this.service.registrar(id).subscribe({
      next: r => {
        this.nota.set(r.data);
        this.snack.open('Nota registrada.', 'Cerrar', { duration: 3000 });
      },
      error: e => this.snack.open(e.error?.detail || 'No fue posible registrar.', 'Cerrar', { duration: 4500 })
    });
  }

  anular(id: number) {
    if (this.motivoAnulacion.invalid) return;
    this.service.anular(id, this.motivoAnulacion.value || '').subscribe({
      next: r => {
        this.nota.set(r.data);
        this.motivoAnulacion.reset('');
        this.snack.open('Nota anulada.', 'Cerrar', { duration: 3000 });
      },
      error: e => this.snack.open(e.error?.detail || 'No fue posible anular.', 'Cerrar', { duration: 4500 })
    });
  }

  nombreEstado = estadoNombre;
  esBorrador(n: NotaCreditoProveedor) { return estadoNombre(n.estado) === 'Borrador'; }
  esRegistrada(n: NotaCreditoProveedor) { return esEstadoRegistrada(n.estado); }
  esAnulada(n: NotaCreditoProveedor) { return estadoNombre(n.estado) === 'Anulada'; }
}
