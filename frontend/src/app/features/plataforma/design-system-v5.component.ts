import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/**
 * C05-01.M04.01: catálogo de referencia visual dentro de /plataforma.
 * No expone datos de tenants, ni consulta APIs, ni permite escrituras.
 * El guard de ruta y el backend siguen siendo la autoridad de acceso.
 */
@Component({
  selector: 'app-design-system-v5',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './design-system-v5.component.html',
  styleUrl: './design-system-v5.component.scss'
})
export class DesignSystemV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly accesoVerificado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });

  // Lista estática: nunca se obtienen colores ni clases desde parámetros del usuario.

  /** C05-01.M04.02: ejemplos de estados de UI. Son datos estaticos, no respuestas de una API. */
  readonly estadosMuestra = [
    { id: 'carga', titulo: 'Cargando', detalle: 'Ejemplo de espera: los datos no se muestran antes de una respuesta confirmada.', clase: 'state-loading' },
    { id: 'vacio', titulo: 'Sin resultados', detalle: 'Ejemplo de vista vacia cuando no existen resultados para presentar.', clase: 'state-empty' },
    { id: 'error', titulo: 'Error recuperable', detalle: 'Ejemplo de error: no mostrar detalles privados del servidor.', clase: 'state-error' },
    { id: 'exito', titulo: 'Operacion completada', detalle: 'Ejemplo de confirmacion visual; esta muestra no guarda ni modifica datos.', clase: 'state-success' },
    { id: 'permiso', titulo: 'Permiso insuficiente', detalle: 'Ejemplo de denegacion: solicitar acceso por los canales autorizados.', clase: 'state-denied' }
  ] as const;

  readonly tokens = [
    { nombre: 'Primario', css: '--color-primary', clase: 'swatch-primary' },
    { nombre: 'Acento', css: '--color-accent', clase: 'swatch-accent' },
    { nombre: 'Éxito', css: '--color-success', clase: 'swatch-success' },
    { nombre: 'Error', css: '--color-danger', clase: 'swatch-danger' }
  ] as const;
}
