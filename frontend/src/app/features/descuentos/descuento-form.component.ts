import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { DescuentoService } from '../../services/descuento.service';

function parseIds(texto: string | null | undefined): number[] {
  if (!texto) return [];
  return texto.split(',').map((s) => Number(s.trim())).filter((n) => !isNaN(n) && n > 0);
}

@Component({
  selector: 'app-descuento-form',
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
  templateUrl: './descuento-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './descuento-form.component.scss'
})
export class DescuentoFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  private descuentoId: number | null = null;

  form = this.fb.group({
    nombre: ['', Validators.required],
    descripcion: [''],
    codigoPromocional: [''],
    tipo: ['Porcentaje' as 'Porcentaje' | 'MontoFijo', Validators.required],
    valor: [0, [Validators.required, Validators.min(0)]],
    fechaInicio: [''],
    fechaFin: [''],
    montoMinimo: [null as number | null],
    montoMaximoDescuento: [null as number | null],
    cantidadMinima: [null as number | null],
    requiereAprobacion: [false],
    acumulable: [false],
    prioridad: [100, Validators.required],
    limiteTotalUsos: [null as number | null],
    limiteUsosPorCliente: [null as number | null],
    // Simplificación (documentada): IDs separados por coma en vez de
    // autocompletado visual de productos/categorías/clientes/roles.
    productoIdsTexto: [''],
    categoriaIdsTexto: [''],
    clienteIdsTexto: [''],
    rolIdsTexto: ['']
  });

  constructor(private service: DescuentoService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.descuentoId = Number(idParam);
      this.service.getById(this.descuentoId).subscribe((res) => {
        const d = res.data;
        this.form.patchValue({
          nombre: d.nombre, descripcion: d.descripcion, codigoPromocional: d.codigoPromocional,
          tipo: d.tipo, valor: d.valor,
          fechaInicio: d.fechaInicio?.substring(0, 10), fechaFin: d.fechaFin?.substring(0, 10),
          montoMinimo: d.montoMinimo ?? null, montoMaximoDescuento: d.montoMaximoDescuento ?? null,
          cantidadMinima: d.cantidadMinima ?? null,
          requiereAprobacion: d.requiereAprobacion, acumulable: d.acumulable, prioridad: d.prioridad,
          limiteTotalUsos: d.limiteTotalUsos ?? null, limiteUsosPorCliente: d.limiteUsosPorCliente ?? null,
          productoIdsTexto: d.productoIds.join(', '),
          categoriaIdsTexto: d.categoriaIds.join(', '),
          clienteIdsTexto: d.clienteIds.join(', '),
          rolIdsTexto: d.rolIds.join(', ')
        });
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.errorMessage.set(null);

    const v = this.form.getRawValue();
    const valor = {
      nombre: v.nombre!, descripcion: v.descripcion || undefined, codigoPromocional: v.codigoPromocional || undefined,
      tipo: v.tipo!, valor: v.valor!,
      fechaInicio: v.fechaInicio || undefined, fechaFin: v.fechaFin || undefined,
      montoMinimo: v.montoMinimo ?? undefined, montoMaximoDescuento: v.montoMaximoDescuento ?? undefined,
      cantidadMinima: v.cantidadMinima ?? undefined,
      requiereAprobacion: !!v.requiereAprobacion, acumulable: !!v.acumulable, prioridad: v.prioridad!,
      limiteTotalUsos: v.limiteTotalUsos ?? undefined, limiteUsosPorCliente: v.limiteUsosPorCliente ?? undefined,
      productoIds: parseIds(v.productoIdsTexto), categoriaIds: parseIds(v.categoriaIdsTexto),
      clienteIds: parseIds(v.clienteIdsTexto), rolIds: parseIds(v.rolIdsTexto)
    };

    const request$ = this.isEdit() ? this.service.update(this.descuentoId!, valor) : this.service.create(valor);
    request$.subscribe({
      next: () => { this.saving.set(false); this.router.navigate(['/descuentos']); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el descuento.'); }
    });
  }
}
