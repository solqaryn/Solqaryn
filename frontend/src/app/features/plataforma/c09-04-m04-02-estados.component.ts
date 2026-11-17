import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/**
 * C09-04.M04.02 - estados de presentacion del panel multicanal.
 * No consulta proveedores ni autoriza acciones de backend. Un guard de ruta
 * y los controles HTTP siguen siendo imprescindibles en el consumidor.
 */
export type EstadoMensajeriaV19 = 'cargando' | 'vacio' | 'error' | 'exito';

@Component({
  selector: 'app-c09-04-m04-02-estados',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="estado-mensajeria" aria-label="Estado del panel de mensajeria multicanal"
             [attr.aria-busy]="estadoVisible() === 'cargando' ? 'true' : 'false'">
      @switch (estadoVisible()) {
        @case ('sin_permiso') {
          <p role="alert" data-testid="m0402-denegado">Sin permiso. El panel privado permanece oculto.</p>
        }
        @case ('cargando') {
          <p role="status" data-testid="m0402-cargando">Preparando la vista. Ningun proveedor ha sido consultado.</p>
        }
        @case ('vacio') {
          <p role="status" data-testid="m0402-vacio">Sin mensajes verificados. Los canales no estan conectados.</p>
        }
        @case ('exito') {
          <p role="status" data-testid="m0402-exito">Vista local disponible. No acredita envios ni entregas.</p>
        }
        @default {
          <p role="alert" data-testid="m0402-error">No se pudo verificar el estado de la vista.</p>
        }
      }
    </section>
  `,
  styles: [`
    :host { display: block; min-width: 0; }
    .estado-mensajeria {
      box-sizing: border-box;
      max-width: 100%;
      min-height: 3rem;
      padding: .875rem 1rem;
      border: 1px solid var(--mat-sys-outline-variant, #94a3b8);
      border-radius: .5rem;
      background: var(--mat-sys-surface-container-low, #f8fafc);
      color: var(--mat-sys-on-surface, #17212d);
      overflow-wrap: anywhere;
    }
    p { margin: 0; line-height: 1.5; }
  `]
})
export class C0904M0402EstadosComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly estado = input<EstadoMensajeriaV19>('vacio');

  readonly estadoVisible = computed((): EstadoMensajeriaV19 | 'sin_permiso' => {
    try {
      const sesion = this.plataforma.sesion();
      if (sesion?.ambito !== 'Plataforma' || sesion.esAdministrador !== true) {
        return 'sin_permiso';
      }
      const solicitado: unknown = this.estado();
      if (solicitado === 'cargando' || solicitado === 'vacio' ||
          solicitado === 'error' || solicitado === 'exito') {
        return solicitado;
      }
      return 'error';
    } catch {
      return 'sin_permiso';
    }
  });
}
