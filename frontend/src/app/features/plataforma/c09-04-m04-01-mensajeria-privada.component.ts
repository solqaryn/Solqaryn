import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** Estado meramente declarativo. No hay adaptadores, tokens, destinatarios ni envíos en UI. */
export const CANALES_DECLARADOS_C0904 = Object.freeze([
  Object.freeze({ id: 'email', nombre: 'Correo electrónico', estado: 'NO_CONECTADO' }),
  Object.freeze({ id: 'whatsapp', nombre: 'WhatsApp', estado: 'NO_CONECTADO' }),
  Object.freeze({ id: 'otros', nombre: 'Otros proveedores', estado: 'NO_CONECTADO' })
] as const);

/** Presets locales confiables; jamás se construye CSS, HTML ni URLs desde el tenant. */
export const PRESENTACIONES_C0904 = Object.freeze([
  Object.freeze({id: 'neutro', nombre: 'Neutro'}),
  Object.freeze({id: 'alto-contraste', nombre: 'Alto contraste'}),
  Object.freeze({id: 'azul', nombre: 'Azul'})
] as const);

export const ELEMENTOS_PERMITIDOS_C0904 = Object.freeze([
  Object.freeze({id: 'tarjetas', nombre: 'Tarjetas informativas'}),
  Object.freeze({id: 'estados', nombre: 'Estados sin conexión'}),
  Object.freeze({id: 'retorno', nombre: 'Enlace de retorno'})
] as const);

type PresentacionIdC0904 = typeof PRESENTACIONES_C0904[number]['id'];

/** C09-04.M04.01: referencia privada read-only; jamás autorización de proveedor. */
@Component({
  selector: 'app-c0904-mensajeria-privada',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './c09-04-m04-01-mensajeria-privada.component.html',
  styleUrl: './c09-04-m04-01-mensajeria-privada.component.scss'
})
export class C0904MensajeriaPrivadaComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly canales = CANALES_DECLARADOS_C0904;
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly temas = PRESENTACIONES_C0904;
  readonly elementosPermitidos = ELEMENTOS_PERMITIDOS_C0904;
  private readonly temaId = signal<PresentacionIdC0904>('neutro');
  readonly presentacion = computed(() =>
    this.temas.find(tema => tema.id === this.temaId()) ?? this.temas[0]);

  /** Selección únicamente local. Nunca acepta entrada arbitraria o identidad de empresa. */
  seleccionarTema(valor: string): void {
    if (!this.autorizado()) {
      this.temaId.set('neutro');
      return;
    }
    const permitido = this.temas.find(tema => tema.id === valor);
    this.temaId.set(permitido?.id ?? 'neutro');
  }
}
