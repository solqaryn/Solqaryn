import { MatDialogModule } from '@angular/material/dialog';
import { crearEditorInstitucional } from '../../shared/modal-institucional/acciones-institucionales';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ImpuestoService } from '../../services/impuesto.service';

function parseIds(texto: string | null | undefined): number[] {
  if (!texto) return [];
  return texto.split(',').map((s) => Number(s.trim())).filter((n) => !isNaN(n) && n > 0);
}

@Component({
  selector: 'app-impuesto-form',
  standalone: true,
  imports: [MatDialogModule, 
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule
],
  templateUrl: './impuesto-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './impuesto-form.component.scss'
})
export class ImpuestoFormComponent implements OnInit {
  readonly editor = crearEditorInstitucional('Impuestos');
  readonly loading = signal(false);
  cancelarModal(): void { if (!this.saving()) this.editor.cerrar(false); }

  private readonly fb = inject(FormBuilder);
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  private impuestoId: number | null = null;

  form = this.fb.group({
    nombre: ['', Validators.required],
    codigo: ['', Validators.required],
    descripcion: [''],
    tipo: ['Porcentaje' as 'Porcentaje' | 'MontoFijo', Validators.required],
    tasa: [0, [Validators.required, Validators.min(0)]],
    montoFijo: [null as number | null],
    fechaInicio: [''],
    fechaFin: [''],
    incluidoEnPrecio: [false],
    seCalculaAntesDescuento: [false],
    acumulativo: [true],
    prioridad: [100, Validators.required],
    requiereRetencion: [false],
    aplicaVenta: [true],
    aplicaCompra: [false],
    productoIdsTexto: [''],
    categoriaIdsTexto: [''],
    clienteExentoIdsTexto: [''],
    proveedorExentoIdsTexto: ['']
  });

  constructor(private service: ImpuestoService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    const idParam = this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id');
    if (this.editor.ref && !this.editor.valido(idParam ? 'Editar' : 'Crear')) { this.editor.cerrar(false); return; }
    if (idParam) {
      this.isEdit.set(true);
      this.impuestoId = Number(idParam);
      this.loading.set(true);
      this.service.getById(this.impuestoId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({ next: (res) => {
        this.loading.set(false);
        const i = res.data;
        this.form.patchValue({
          nombre: i.nombre, codigo: i.codigo, descripcion: i.descripcion,
          tipo: i.tipo, tasa: i.tasa, montoFijo: i.montoFijo ?? null,
          fechaInicio: i.fechaInicio?.substring(0, 10), fechaFin: i.fechaFin?.substring(0, 10),
          incluidoEnPrecio: i.incluidoEnPrecio, seCalculaAntesDescuento: i.seCalculaAntesDescuento,
          acumulativo: i.acumulativo, prioridad: i.prioridad, requiereRetencion: i.requiereRetencion,
          aplicaVenta: i.operaciones.includes('Venta'), aplicaCompra: i.operaciones.includes('Compra'),
          productoIdsTexto: i.productoIds.join(', '), categoriaIdsTexto: i.categoriaIds.join(', '),
          clienteExentoIdsTexto: i.clienteExentoIds.join(', '), proveedorExentoIdsTexto: i.proveedorExentoIds.join(', ')
        });
        // Codigo es la identidad técnica estable usada por el seed y por
        // snapshots/integraciones. Puede definirse al crear, no renombrarse.
        this.form.controls.codigo.disable({ emitEvent: false });
      }, error: () => { this.loading.set(false); this.form.disable(); this.errorMessage.set('No se pudo cargar el registro.'); } });
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.form.disabled || this.loading() || this.saving()) return;
    this.saving.set(true);
    this.errorMessage.set(null);

    const v = this.form.getRawValue();
    const operaciones: string[] = [];
    if (v.aplicaVenta) operaciones.push('Venta');
    if (v.aplicaCompra) operaciones.push('Compra');

    const valor = {
      nombre: v.nombre!, codigo: v.codigo!, descripcion: v.descripcion || undefined,
      tipo: v.tipo!, tasa: v.tasa!, montoFijo: v.montoFijo ?? undefined,
      fechaInicio: v.fechaInicio || undefined, fechaFin: v.fechaFin || undefined,
      incluidoEnPrecio: !!v.incluidoEnPrecio, seCalculaAntesDescuento: !!v.seCalculaAntesDescuento,
      acumulativo: !!v.acumulativo, prioridad: v.prioridad!, requiereRetencion: !!v.requiereRetencion,
      operaciones,
      productoIds: parseIds(v.productoIdsTexto), categoriaIds: parseIds(v.categoriaIdsTexto),
      clienteExentoIds: parseIds(v.clienteExentoIdsTexto), proveedorExentoIds: parseIds(v.proveedorExentoIdsTexto)
    };

    const accion = this.isEdit() ? 'Editar' : 'Crear';
    if (!await this.editor.confirmar(accion, { titulo: 'Confirmar ' + (this.isEdit() ? 'actualización' : 'creación'), mensaje: '¿Deseas guardar los datos del registro?', confirmarTexto: 'Guardar' })) { this.saving.set(false); return; }
    const request$ = this.isEdit() ? this.service.update(this.impuestoId!, valor) : this.service.create(valor);
    request$.pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => { this.saving.set(false); if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/impuestos']); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el impuesto.'); }
    });
  }
}
