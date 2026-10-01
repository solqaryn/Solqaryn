import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, map, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { mapearProducto } from './storefront.catalog';
import { StorefrontCarritoService } from './storefront-carrito.service';
import { StorefrontCuentaService } from './storefront-cuenta.service';
import { StorefrontHeaderComponent } from './storefront-header.component';
import { EstadoConsultaPublica, TiendaDireccionCliente, TiendaNotificacionPedido, TiendaPedidoCuenta, ProductoTienda } from './storefront.models';
import { STOREFRONT_PATHS } from './storefront.paths';
import { StorefrontService } from './storefront.service';

@Component({
  selector: 'app-storefront-cuenta',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, StorefrontHeaderComponent],
  templateUrl: './storefront-cuenta.component.html',
  styleUrl: './storefront-cuenta.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontCuentaComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly tienda = inject(StorefrontService);
  private readonly carrito = inject(StorefrontCarritoService);

  readonly identidad = inject(StorefrontIdentidadService);
  readonly cuenta = inject(StorefrontCuentaService);

  readonly modo = signal<'login' | 'registro'>('login');
  readonly cargando = signal(true);
  readonly procesando = signal(false);
  readonly error = signal('');
  readonly aviso = signal('');
  readonly direcciones = signal<TiendaDireccionCliente[]>([]);
  readonly favoritos = signal<number[]>([]);
  readonly pedidos = signal<TiendaPedidoCuenta[]>([]);
  readonly notificaciones = signal<TiendaNotificacionPedido[]>([]);
  readonly catalogo = signal<ProductoTienda[]>([]);
  readonly estado = computed<EstadoConsultaPublica>(() => {
    if (this.cargando()) return 'loading';
    if (this.error()) return 'error';
    return this.cuenta.autenticado() ? 'success' : 'empty';
  });

  readonly productosFavoritos = computed(() => {
    const ids = new Set(this.favoritos());
    return this.catalogo().filter(producto => ids.has(producto.id));
  });

  readonly loginForm = new FormGroup({
    correo: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(150)] }),
    clave: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(10), Validators.maxLength(128)] })
  });

  readonly registroForm = new FormGroup({
    nombre: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(120)] }),
    correo: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(150)] }),
    clave: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(10), Validators.maxLength(128)] })
  });

  readonly direccionForm = new FormGroup({
    alias: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(60)] }),
    recibe: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(120)] }),
    telefono: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(30)] }),
    direccion: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
    predeterminada: new FormControl(false, { nonNullable: true })
  });

  readonly enlaces = {
    inicio: STOREFRONT_PATHS.inicio,
    productos: STOREFRONT_PATHS.productos,
    carrito: STOREFRONT_PATHS.carrito
  } as const;

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.cuenta.restaurar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(perfil => {
        this.cargando.set(false);
        if (perfil) this.cargarPanel();
      });
    });
  }

  cambiarModo(modo: 'login' | 'registro'): void {
    this.modo.set(modo);
    this.error.set('');
    this.aviso.set('');
  }

  login(): void {
    this.loginForm.markAllAsTouched();
    if (this.loginForm.invalid || this.procesando()) return;
    this.procesando.set(true);
    this.error.set('');
    const { correo, clave } = this.loginForm.getRawValue();
    this.cuenta.login(correo, clave).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.procesando.set(false);
        this.aviso.set('Sesión iniciada. Tu catálogo y checkout siguen disponibles como siempre.');
        this.cargarPanel();
      },
      error: error => this.fallar(error)
    });
  }

  registrar(): void {
    this.registroForm.markAllAsTouched();
    if (this.registroForm.invalid || this.procesando()) return;
    this.procesando.set(true);
    this.error.set('');
    const { nombre, correo, clave } = this.registroForm.getRawValue();
    this.cuenta.registrar(nombre, correo, clave).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.procesando.set(false);
        this.aviso.set('Cuenta creada. Comprar como invitado sigue siendo opcional.');
        this.cargarPanel();
      },
      error: error => this.fallar(error)
    });
  }

  cerrarSesion(): void {
    this.cuenta.logout().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.direcciones.set([]);
      this.favoritos.set([]);
      this.pedidos.set([]);
      this.notificaciones.set([]);
      this.catalogo.set([]);
      this.aviso.set('Sesión cerrada.');
      this.error.set('');
    });
  }

  guardarDireccion(): void {
    this.direccionForm.markAllAsTouched();
    if (this.direccionForm.invalid || this.procesando()) return;
    this.procesando.set(true);
    this.cuenta.guardarDireccion(this.direccionForm.getRawValue()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.procesando.set(false);
        this.direccionForm.reset({ alias: '', recibe: '', telefono: '', direccion: '', predeterminada: false });
        this.aviso.set('Dirección guardada.');
        this.recargarDirecciones();
      },
      error: error => this.fallar(error)
    });
  }

  eliminarDireccion(id: number): void {
    if (this.procesando()) return;
    this.procesando.set(true);
    this.cuenta.eliminarDireccion(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.procesando.set(false);
        this.recargarDirecciones();
      },
      error: error => this.fallar(error)
    });
  }

  quitarFavorito(productoId: number): void {
    this.cuenta.quitarFavorito(productoId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.favoritos.update(ids => ids.filter(id => id !== productoId)),
      error: error => this.fallar(error)
    });
  }

  recompra(pedido: TiendaPedidoCuenta): void {
    const productos = this.catalogo();
    if (!productos.length) {
      this.error.set('No pudimos cargar el catálogo vigente para la recompra.');
      return;
    }

    this.carrito.hidratar(productos, this.identidad.config().id, true);
    let agregadas = 0;
    let omitidas = 0;
    for (const linea of pedido.lineas) {
      const producto = productos.find(item => item.id === linea.productoId);
      if (!producto) {
        omitidas += 1;
        continue;
      }
      const modelo = linea.productoVarianteId
        ? producto.modelos.find(item => item.productoVarianteId === linea.productoVarianteId)
        : linea.modelo?.trim()
          ? producto.modelos.find(item => item.nombre.trim() === linea.modelo!.trim())
          : producto.modelos.length === 1
            ? producto.modelos[0]
            : undefined;
      const unidades = Math.max(1, Math.floor(Number(linea.cantidad) || 1));
      if (!modelo || !modelo.disponible) {
        omitidas += 1;
        continue;
      }
      const agregadasLinea = this.carrito.agregar(producto, modelo, unidades);
      agregadas += agregadasLinea;
      if (agregadasLinea < unidades) omitidas += 1;
    }

    if (agregadas === 0) {
      this.error.set('Los productos de ese pedido ya no están disponibles para recompra.');
      return;
    }

    this.aviso.set(omitidas > 0
      ? 'Agregamos lo disponible al carrito. Algunos productos o cantidades cambiaron.'
      : 'Pedido agregado al carrito con precios y existencias vigentes.');
    void this.router.navigateByUrl(STOREFRONT_PATHS.carrito);
  }

  moneda(valor: number): string {
    try {
      return new Intl.NumberFormat('es-HN', {
        style: 'currency',
        currency: this.identidad.config().moneda || 'HNL'
      }).format(valor);
    } catch {
      return `L ${valor.toFixed(2)}`;
    }
  }

  private cargarPanel(): void {
    this.cargando.set(true);
    forkJoin({
      direcciones: this.cuenta.direcciones(),
      favoritos: this.cuenta.favoritos(),
      pedidos: this.cuenta.pedidos(),
      notificaciones: this.cuenta.notificaciones()
    }).pipe(
      switchMap(data => {
        const productoIds = [
          ...data.favoritos,
          ...data.pedidos.flatMap(pedido => pedido.lineas.map(linea => linea.productoId))
        ];
        return this.tienda.obtenerProductosContexto(productoIds).pipe(
          map(catalogo => ({ ...data, catalogo }))
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: data => {
        this.direcciones.set(data.direcciones);
        this.favoritos.set(data.favoritos);
        this.pedidos.set(data.pedidos);
        this.notificaciones.set(data.notificaciones);
        this.catalogo.set(data.catalogo.map(mapearProducto));
        this.cargando.set(false);
      },
      error: error => {
        this.cargando.set(false);
        this.fallar(error);
      }
    });
  }

  private recargarDirecciones(): void {
    this.cuenta.direcciones().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => {
        this.direcciones.set(items);
        this.procesando.set(false);
      },
      error: error => this.fallar(error)
    });
  }

  private fallar(error: unknown): void {
    this.procesando.set(false);
    this.cargando.set(false);
    const posible = error as { error?: { message?: string }; message?: string };
    this.error.set(posible?.error?.message?.trim() || posible?.message?.trim() || 'No pudimos completar la acción.');
  }
}