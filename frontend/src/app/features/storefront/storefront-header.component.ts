
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { telefonoWhatsapp } from './storefront.catalog';
import { STOREFRONT_PATHS } from './storefront.paths';
import { IconoTiendaComponent } from './storefront.visual';

@Component({
  selector: 'app-storefront-header',
  standalone: true,
  imports: [RouterLink, IconoTiendaComponent],
  templateUrl: './storefront-header.component.html',
  styleUrl: './storefront-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontHeaderComponent {
  readonly identidad = inject(StorefrontIdentidadService);

  @Input() busqueda = '';
  @Input() categorias: readonly string[] = [];
  @Input() categoriaActiva = '';
  @Input() totalUnidades: number | null = 0;
  @Input() subtotal: number | null = 0;
  @Input() permiteWhatsapp = true;
  @Input() destinoSaltar = '#catalogo';
  @Input() etiquetaSaltar = 'Saltar al catálogo';

  @Output() readonly busquedaActualizada = new EventEmitter<string>();
  @Output() readonly buscarSolicitado = new EventEmitter<void>();
  @Output() readonly categoriaSeleccionada = new EventEmitter<string>();
  @Output() readonly carritoSolicitado = new EventEmitter<void>();

  @ViewChild('menuMovil') private menuMovil?: ElementRef<HTMLDialogElement>;
  @ViewChild('botonMenu') private botonMenu?: ElementRef<HTMLButtonElement>;
  @ViewChild('primerEnlaceMovil') private primerEnlaceMovil?: ElementRef<HTMLAnchorElement>;

  readonly menuAbierto = signal(false);
  private readonly logoFallido = signal<string | null>(null);
  private restaurarFocoAlCerrar = true;
  readonly telefono = computed(() => telefonoWhatsapp(this.identidad.config().whatsApp));
  readonly mostrarLogo = computed(() => {
    const logo = this.identidad.logoUrl();
    return Boolean(logo && this.logoFallido() !== logo);
  });
  readonly enlaceWhatsapp = computed(() => this.telefono() ? `https://wa.me/${this.telefono()}` : '');

  readonly enlaces = {
    inicio: STOREFRONT_PATHS.inicio,
    productos: STOREFRONT_PATHS.productos,
    ofertas: STOREFRONT_PATHS.ofertas,
    categorias: STOREFRONT_PATHS.categorias,
    contacto: `${STOREFRONT_PATHS.inicio}#contacto`
  } as const;

  actualizarBusqueda(texto: string): void {
    this.busquedaActualizada.emit(texto);
  }

  enviarBusqueda(evento: Event): void {
    evento.preventDefault();
    this.buscarSolicitado.emit();
  }

  seleccionarCategoria(nombre: string): void {
    if (this.menuAbierto()) this.cerrarMenuMovil(false);
    this.categoriaSeleccionada.emit(nombre);
  }

  solicitarCarrito(): void {
    if (this.menuAbierto()) this.cerrarMenuMovil(false);
    this.carritoSolicitado.emit();
  }

  abrirMenuMovil(): void {
    const dialogo = this.menuMovil?.nativeElement;
    if (!dialogo || dialogo.open) return;

    this.restaurarFocoAlCerrar = true;
    dialogo.showModal();
    this.menuAbierto.set(true);
    queueMicrotask(() => this.primerEnlaceMovil?.nativeElement.focus());
  }

  cerrarMenuMovil(devolverFoco = true): void {
    const dialogo = this.menuMovil?.nativeElement;
    const estabaAbierto = Boolean(dialogo?.open || this.menuAbierto());
    if (!estabaAbierto) return;

    this.restaurarFocoAlCerrar = devolverFoco;
    if (dialogo?.open) dialogo.close();
    else this.menuAbierto.set(false);
  }

  cerrarDesdeFondo(evento: MouseEvent): void {
    if (evento.target === this.menuMovil?.nativeElement) this.cerrarMenuMovil(true);
  }

  alCerrarDialogo(): void {
    const restaurarFoco = this.restaurarFocoAlCerrar;
    this.restaurarFocoAlCerrar = true;
    this.menuAbierto.set(false);
    if (restaurarFoco) queueMicrotask(() => this.botonMenu?.nativeElement.focus());
  }

  reportarErrorLogo(url: string): void {
    this.logoFallido.set(url);
  }

  etiquetaCarrito(): string {
    return this.totalUnidades === null ? 'Abrir carrito' : `Abrir carrito con ${this.totalUnidades} unidades`;
  }

  textoSubtotalCarrito(): string {
    return this.subtotal === null ? 'Ver carrito' : this.moneda(this.subtotal);
  }

  moneda(valor: number): string {
    try {
      return new Intl.NumberFormat('es-HN', {
        style: 'currency',
        currency: this.identidad.config().moneda || 'HNL'
      }).format(valor);
    } catch {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
    }
  }
}
