import { Component, OnInit, inject, signal } from '@angular/core';

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
  imports: [
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
  styleUrl: './impuesto-form.component.scss'
})
export class ImpuestoFormComponent implements OnInit {
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
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.impuestoId = Number(idParam);
      this.service.getById(this.impuestoId).subscribe((res) => {
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
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;
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

    const request$ = this.isEdit() ? this.service.update(this.impuestoId!, valor) : this.service.create(valor);
    request$.subscribe({
      next: () => { this.saving.set(false); this.router.navigate(['/impuestos']); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el impuesto.'); }
    });
  }
}
