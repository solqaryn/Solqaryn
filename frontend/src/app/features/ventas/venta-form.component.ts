import { Component, OnInit, ViewChild, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, AbstractControl, FormBuilder, FormArray, FormControl, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError, finalize } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { VentaService } from '../../services/venta.service';
import { ProductoService } from '../../services/producto.service';
import { ClienteService } from '../../services/cliente.service';
import { CostoEnvioService } from '../../services/costo-envio.service';
import { Producto, ProductoEscaneadoVenta, ProductoVariante } from '../../core/models/producto.model';
import { Cliente } from '../../core/models/cliente.model';
import { CostoEnvio } from '../../core/models/costo-envio.model';
import { ResultadoCalculo } from '../../core/models/venta.model';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';
import { CodigoScannerInputComponent } from '../../shared/codigo-scanner-input/codigo-scanner-input.component';
import { MetodoPagoSelectComponent } from '../../shared/metodo-pago-select/metodo-pago-select.component';

@Component({
  selector: 'app-venta-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatCheckboxModule, ProductoImagenComponent,
    CodigoScannerInputComponent, MetodoPagoSelectComponent
  ],
  templateUrl: './venta-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './venta-form.component.scss'
})
export class VentaFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  @ViewChild(CodigoScannerInputComponent) private scannerInput?: CodigoScannerInputComponent;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly calculando = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly productos = signal<Producto[]>([]);
  readonly costosEnvio = signal<CostoEnvio[]>([]);
  readonly resultado = signal<ResultadoCalculo | null>(null);
  readonly procesandoEscaneo = signal(false);
  readonly mensajeEscaneo = signal<string | null>(null);
  readonly errorEscaneo = signal(false);
  private ventaId: number | null = null;

  readonly buscadorCliente = new FormControl('');
  readonly opcionesCliente = signal<Cliente[]>([]);
  readonly buscandoCliente = signal(false);
  readonly clienteSeleccionado = signal<Cliente | null>(null);
  readonly errorBusquedaCliente = signal<string | null>(null);
  private clienteId: number | null = null;

  readonly buscadorProducto = new FormControl<string | ProductoEscaneadoVenta>('');
  readonly opcionesProducto = signal<ProductoEscaneadoVenta[]>([]);
  readonly buscandoProducto = signal(false);
  readonly errorBusquedaProducto = signal<string | null>(null);
  readonly mensajeBusquedaProducto = signal<string | null>(null);

  form = this.fb.group({
    clienteNombre: ['Cliente final', Validators.required],
    clienteTelefono: [''], clienteIdentidadORTN: [''], clienteCorreo: [''], clienteDireccion: [''],
    metodoPago: ['Efectivo', Validators.required], estadoPago: ['Pendiente', Validators.required],
    codigoPromocional: [''], costoEnvioId: [null as number | null], envioExonerado: [false],
    motivoExoneracionEnvio: [''], notas: [''], detalles: this.fb.array([])
  });

  constructor(
    private ventaService: VentaService,
    private productoService: ProductoService,
    private clienteService: ClienteService,
    private costoEnvioService: CostoEnvioService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  get detalles(): FormArray { return this.form.get('detalles') as FormArray; }

  ngOnInit(): void {
    this.costoEnvioService.getAll().subscribe({
      next: (res) => {
        const activos = res.data.filter((x) => x.activo && x.estaVigente);
        this.costosEnvio.set(activos);
        if (!this.form.value.costoEnvioId) {
          const predeterminado = activos.find((x) => x.esPredeterminado);
          if (predeterminado) this.form.patchValue({ costoEnvioId: predeterminado.id });
        }
      },
      error: () => this.errorMessage.set('No se pudieron cargar los costos de envío vigentes.')
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true); this.ventaId = Number(idParam); this.cargarVenta(this.ventaId);
    } else this.agregarDetalle();

    this.buscadorCliente.valueChanges.pipe(
      debounceTime(350), distinctUntilChanged(),
      switchMap((termino) => {
        if (this.clienteSeleccionado() && termino !== this.clienteSeleccionado()!.nombre) {
          this.clienteSeleccionado.set(null); this.clienteId = null;
        }
        if (!termino || termino.trim().length < 2) { this.opcionesCliente.set([]); return of(null); }
        this.buscandoCliente.set(true); this.errorBusquedaCliente.set(null);
        return this.clienteService.buscar(termino).pipe(catchError(() => {
          this.errorBusquedaCliente.set('No se pudo buscar clientes. Intenta de nuevo.'); return of(null);
        }));
      })
    ).subscribe((res) => { this.buscandoCliente.set(false); if (res) this.opcionesCliente.set(res.data); });

    this.buscadorProducto.valueChanges.pipe(
      debounceTime(300),
      switchMap((valor) => {
        const termino = typeof valor === 'string' ? valor.trim() : '';
        this.mensajeBusquedaProducto.set(null);
        this.errorBusquedaProducto.set(null);
        if (termino.length < 2) {
          this.buscandoProducto.set(false);
          this.opcionesProducto.set([]);
          return of(null);
        }

        this.buscandoProducto.set(true);
        return this.ventaService.buscarProductos(termino, 30).pipe(
          catchError((err) => {
            this.errorBusquedaProducto.set(err.error?.message ?? 'No se pudieron buscar productos. Intenta de nuevo.');
            return of(null);
          }),
          finalize(() => this.buscandoProducto.set(false))
        );
      })
    ).subscribe((res) => this.opcionesProducto.set(res?.data ?? []));

    this.form.get('envioExonerado')!.valueChanges.subscribe((exonerado) => {
      const motivo = this.form.get('motivoExoneracionEnvio')!;
      if (exonerado) motivo.addValidators([Validators.required, Validators.maxLength(500)]);
      else { motivo.clearValidators(); motivo.setValue('', { emitEvent: false }); }
      motivo.updateValueAndValidity({ emitEvent: false });
    });

    this.form.valueChanges.pipe(
      debounceTime(500), distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
    ).subscribe(() => this.recalcular());
  }

  onClienteSeleccionado(event: MatAutocompleteSelectedEvent): void {
    const cliente: Cliente = event.option.value;
    this.clienteSeleccionado.set(cliente); this.clienteId = cliente.id;
    this.buscadorCliente.setValue(cliente.nombre, { emitEvent: false });
    this.form.patchValue({
      clienteNombre: cliente.nombre, clienteTelefono: cliente.telefono,
      clienteIdentidadORTN: cliente.identidadORTN, clienteCorreo: cliente.correo,
      clienteDireccion: cliente.direccion
    });
  }

  limpiarClienteSeleccionado(): void {
    this.clienteSeleccionado.set(null); this.clienteId = null; this.buscadorCliente.setValue('');
    this.form.patchValue({ clienteNombre: 'Cliente final', clienteTelefono: '', clienteIdentidadORTN: '', clienteCorreo: '', clienteDireccion: '' });
  }

  displayCliente(cliente: Cliente): string { return cliente?.nombre ?? ''; }

  displayProductoOperacion(item: ProductoEscaneadoVenta | string | null): string {
    if (!item || typeof item === 'string') return typeof item === 'string' ? item : '';
    const variante = item.colorNombre || (item.esVarianteTecnica ? 'Predeterminada' : item.sku);
    return `${item.productoNombre}${variante ? ` · ${variante}` : ''}`;
  }

  onProductoBuscadoSeleccionado(event: MatAutocompleteSelectedEvent): void {
    const item = event.option.value as ProductoEscaneadoVenta;
    const resultado = this.aplicarProductoOperacion(item);
    this.errorBusquedaProducto.set(resultado.ok ? null : resultado.mensaje);
    this.mensajeBusquedaProducto.set(resultado.ok ? resultado.mensaje : null);
    this.buscadorProducto.setValue('', { emitEvent: false });
    this.opcionesProducto.set([]);
  }

  procesarCodigoEscaner(codigo: string): void {
    if (this.procesandoEscaneo()) return;
    this.procesandoEscaneo.set(true);
    this.mensajeEscaneo.set(null);
    this.errorEscaneo.set(false);

    this.ventaService.buscarProductoPorCodigo(codigo).pipe(
      finalize(() => {
        this.procesandoEscaneo.set(false);
        this.scannerInput?.reenfocar();
      })
    ).subscribe({
      next: (res) => {
        const resultado = this.aplicarProductoOperacion(res.data);
        this.errorEscaneo.set(!resultado.ok);
        this.mensajeEscaneo.set(resultado.mensaje);
      },
      error: (err) => {
        this.errorEscaneo.set(true);
        this.mensajeEscaneo.set(err.error?.message ?? 'No se pudo resolver el SKU o código de barras.');
      }
    });
  }

  private aplicarProductoOperacion(item: ProductoEscaneadoVenta): { ok: boolean; mensaje: string } {
    const coincidencias = this.detalles.controls
      .map((grupo, index) => ({ grupo, index }))
      .filter(({ grupo }) =>
        Number(grupo.value.productoId) === item.productoId
        && Number(grupo.value.productoVarianteId) === item.productoVarianteId
      );

    if (coincidencias.length > 0) {
      const cantidadActual = coincidencias.reduce(
        (total, { grupo }) => total + Number(grupo.value.cantidad || 0),
        0
      );
      const nuevaCantidad = cantidadActual + 1;
      if (nuevaCantidad > item.cantidadDisponible) {
        return {
          ok: false,
          mensaje: `Stock insuficiente para ${item.productoNombre}. Disponible: ${item.cantidadDisponible}.`
        };
      }

      coincidencias[0].grupo.patchValue({
        cantidad: nuevaCantidad,
        precioUnitario: item.precio
      });
      coincidencias
        .slice(1)
        .map(({ index }) => index)
        .sort((a, b) => b - a)
        .forEach((index) => this.detalles.removeAt(index));

      return { ok: true, mensaje: `${item.productoNombre}: cantidad consolidada en ${nuevaCantidad}.` };
    }

    this.incorporarProductoOperacion(item);
    const filaVacia = this.detalles.controls.find((grupo) => !grupo.value.productoId);
    const valores = {
      productoId: item.productoId,
      productoVarianteId: item.productoVarianteId,
      cantidad: 1,
      precioUnitario: item.precio
    };
    if (filaVacia) filaVacia.patchValue(valores);
    else this.agregarDetalle(item.productoId, item.productoVarianteId, 1, item.precio);

    this.errorMessage.set(null);
    return {
      ok: true,
      mensaje: `${item.productoNombre}${item.colorNombre ? ` · ${item.colorNombre}` : ''} agregado a la venta.`
    };
  }

  private incorporarProductoOperacion(item: ProductoEscaneadoVenta): void {
    const variante: ProductoVariante = {
      id: item.productoVarianteId,
      productoId: item.productoId,
      productoNombre: item.productoNombre,
      colorId: item.colorId ?? 0,
      colorNombre: item.colorNombre ?? (item.esVarianteTecnica ? 'Predeterminada' : 'Sin color'),
      sku: item.sku,
      codigoBarras: item.codigoBarras ?? undefined,
      cantidad: item.cantidadDisponible,
      umbralStockBajo: 0,
      costo: 0,
      precio: item.precio,
      activo: true,
      tieneStockBajo: false,
      estaAgotada: item.cantidadDisponible <= 0,
      estadoInventario: item.cantidadDisponible > 0 ? 'Disponible' : 'Agotado',
      fechaCreacion: '',
      fechaActualizacion: ''
    };

    const actuales = [...this.productos()];
    const indice = actuales.findIndex((producto) => producto.id === item.productoId);
    if (indice >= 0) {
      const producto = actuales[indice];
      const variantes = [...(producto.variantes ?? [])];
      const indiceVariante = variantes.findIndex((actual) => actual.id === item.productoVarianteId);
      if (indiceVariante >= 0) variantes[indiceVariante] = { ...variantes[indiceVariante], ...variante };
      else variantes.push(variante);
      actuales[indice] = {
        ...producto,
        cantidad: variantes.reduce((total, actual) => total + actual.cantidad, 0),
        imagenPrincipalUrl: item.imagenMiniaturaUrl ?? producto.imagenPrincipalUrl,
        variantes,
        totalVariantes: variantes.length
      };
    } else {
      actuales.push({
        id: item.productoId,
        nombre: item.productoNombre,
        marca: item.marca,
        modelo: item.modelo,
        cantidad: item.cantidadDisponible,
        costo: 0,
        precio: item.precio,
        precioMinimo: item.precio,
        precioMaximo: item.precio,
        umbralStockBajo: 0,
        tieneStockBajo: false,
        estaAgotado: item.cantidadDisponible <= 0,
        estadoInventario: item.cantidadDisponible > 0 ? 'Disponible' : 'Agotado',
        activo: true,
        imagenPrincipalUrl: item.imagenMiniaturaUrl ?? undefined,
        imagenes: [],
        totalImagenes: 0,
        variantes: [variante],
        totalVariantes: 1,
        usaVariantes: true,
        fechaCreacion: '',
        fechaActualizacion: ''
      });
    }
    this.productos.set(actuales.sort((a, b) => a.nombre.localeCompare(b.nombre)));
  }

  private cargarVenta(id: number): void {
    this.loading.set(true);
    this.ventaService.getById(id).subscribe({
      next: (res) => {
        const v = res.data;
        this.form.patchValue({
          clienteNombre: v.clienteNombre, clienteTelefono: v.clienteTelefono,
          clienteIdentidadORTN: v.clienteIdentidadORTN, clienteCorreo: v.clienteCorreo,
          clienteDireccion: v.clienteDireccion, metodoPago: v.metodoPago, estadoPago: v.estadoPago,
          costoEnvioId: v.costoEnvioId ?? null, envioExonerado: v.envioExonerado,
          motivoExoneracionEnvio: v.motivoExoneracionEnvio ?? '', notas: v.notas
        });
        this.buscadorCliente.setValue(v.clienteNombre, { emitEvent: false });
        v.detalles.forEach((d) => this.agregarDetalle(d.productoId, d.productoVarianteId ?? null, d.cantidad, d.precioUnitario));
        this.hidratarProductosReferenciados(v.detalles.map((detalle) => detalle.productoId));
        this.resultado.set({
          importeBruto: v.importeBruto, importeProductos: v.importeProductos, subtotal: v.subtotal,
          descuentosAplicados: v.descuentosAplicados, totalDescuento: v.descuento,
          impuestosAplicados: v.impuestosAplicados, totalImpuesto: v.impuesto,
          impuestoIncluido: v.impuestosAplicados.filter((i) => i.incluidoEnPrecio).reduce((a, i) => a + i.monto, 0),
          impuestoAdicional: v.impuestosAplicados.filter((i) => !i.incluidoEnPrecio).reduce((a, i) => a + i.monto, 0),
          costoEnvioId: v.costoEnvioId, costoEnvioNombre: v.costoEnvioNombre, costoEnvio: v.costoEnvio,
          envioExonerado: v.envioExonerado, motivoExoneracionEnvio: v.motivoExoneracionEnvio, total: v.total
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private hidratarProductosReferenciados(productoIds: number[]): void {
    [...new Set(productoIds)].forEach((productoId) => {
      this.productoService.getById(productoId).subscribe({
        next: (res) => {
          const actuales = this.productos().filter((producto) => producto.id !== productoId);
          this.productos.set([...actuales, res.data].sort((a, b) => a.nombre.localeCompare(b.nombre)));
        },
        error: () => this.errorMessage.set('No se pudo cargar la información actual de uno de los productos del borrador.')
      });
    });
  }

  agregarDetalle(productoId: number | null = null, productoVarianteId: number | null = null, cantidad = 1, precioUnitario = 0): void {
    this.detalles.push(this.fb.group({
      productoId: [productoId, Validators.required],
      productoVarianteId: [productoVarianteId],
      cantidad: [cantidad, [Validators.required, Validators.min(1)]],
      precioUnitario: [precioUnitario, [Validators.required, Validators.min(0.01)]]
    }));
  }

  onProductoSeleccionado(index: number, productoId: number): void {
    const producto = this.productos().find((p) => p.id === productoId);
    if (!producto) return;
    const activas = (producto.variantes ?? []).filter((v) => v.activo);
    if (activas.length === 1) {
      this.detalles.at(index).patchValue({ productoVarianteId: activas[0].id, precioUnitario: activas[0].precio });
      this.errorMessage.set(null);
    } else {
      this.detalles.at(index).patchValue({ productoVarianteId: null, precioUnitario: producto.precio });
      if (producto.usaVariantes && activas.length === 0) {
        this.errorMessage.set(`El producto '${producto.nombre}' no tiene variantes activas disponibles.`);
      }
    }
  }

  onVarianteSeleccionada(index: number, varianteId: number): void {
    const grupo = this.detalles.at(index);
    const variante = this.variantesDisponibles(grupo).find((v) => v.id === varianteId);
    if (variante) {
      grupo.patchValue({ precioUnitario: variante.precio });
      this.errorMessage.set(null);
    }
  }

  requiereVariante(group: AbstractControl): boolean {
    return (this.productoSeleccionado(group)?.variantes ?? []).length > 0;
  }

  variantesDisponibles(group: AbstractControl): ProductoVariante[] {
    return (this.productoSeleccionado(group)?.variantes ?? []).filter((v) => v.activo);
  }

  productoSeleccionado(group: AbstractControl): Producto | undefined {
    const productoId = Number(group.value.productoId);
    return this.productos().find((producto) => producto.id === productoId);
  }

  quitarDetalle(index: number): void { this.detalles.removeAt(index); this.recalcular(); }
  subtotalDetalle(group: AbstractControl): number { return (group.value.cantidad || 0) * (group.value.precioUnitario || 0); }

  recalcular(): void {
    const detallesValidos = this.detalles.controls.map((g) => g.value)
      .filter((d) => d.productoId && d.cantidad > 0 && d.precioUnitario >= 0);
    if (detallesValidos.length === 0) { this.resultado.set(null); return; }
    if (this.detalles.controls.some((g) => this.requiereVariante(g) && !g.value.productoVarianteId)) {
      this.resultado.set(null); return;
    }

    const exonerado = this.form.value.envioExonerado === true;
    const motivo = this.form.value.motivoExoneracionEnvio || null;
    if (exonerado && !motivo?.trim()) { this.resultado.set(null); return; }

    this.calculando.set(true);
    this.ventaService.calcular(
      this.clienteId, this.form.value.codigoPromocional || null, detallesValidos,
      this.form.value.costoEnvioId, exonerado, motivo
    ).subscribe({
      next: (res) => { this.resultado.set(res.data); this.calculando.set(false); this.errorMessage.set(null); },
      error: (err) => { this.calculando.set(false); this.resultado.set(null); this.errorMessage.set(err.error?.message ?? 'No se pudo calcular el total de la venta.'); }
    });
  }

  submit(): void {
    if (this.form.invalid || this.detalles.length === 0 || !this.resultado()) return;
    this.saving.set(true); this.errorMessage.set(null);
    const raw = this.form.getRawValue();
    const value = { ...raw, descuento: 0, impuesto: 0 } as any;
    const request$ = this.isEdit() ? this.ventaService.update(this.ventaId!, value) : this.ventaService.create(value);
    request$.subscribe({
      next: (res) => { this.saving.set(false); this.router.navigate(['/ventas', res.data.id]); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la venta.'); }
    });
  }
}
