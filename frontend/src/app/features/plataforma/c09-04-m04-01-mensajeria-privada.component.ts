import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** Estado meramente declarativo. No hay adaptadores, tokens, destinatarios ni envíos en UI. */
export const CANALES_DECLARADOS_C0904 = Object.freeze([
  Object.freeze({ id: 'email', nombre: 'Correo electrónico', estado: 'NO_CONECTADO' }),
  Object.freeze({ id: 'whatsapp', nombre: 'WhatsApp', estado: 'NO_CONECTADO' }),
  Object.freeze({ id: 'otros', nombre: 'Otros proveedores', estado: 'NO_CONECTADO' })
] as const);

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
}
