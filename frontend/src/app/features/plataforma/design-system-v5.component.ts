import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
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

  /** C05-01.M04.03: branding de demostración local sin datos ni permisos de tenant. */
  readonly presets = [
    { id: 'institucional', nombre: 'Institucional', descripcion: 'Componentes con jerarquía y contraste institucional.' },
    { id: 'contraste', nombre: 'Contraste alto', descripcion: 'Componentes con contraste reforzado sin alterar la marca guardada.' }
  ] as const;
  readonly presetSeleccionado = signal<(typeof this.presets)[number]['id']>('institucional');
  readonly presetVista = computed(() =>
    this.accesoVerificado()
      ? this.presets.find(preset => preset.id === this.presetSeleccionado()) ?? this.presets[0]
      : null
  );

  seleccionarPreset(id: string): boolean {
    if (!this.accesoVerificado()) return false;
    const permitido = this.presets.find(preset => preset.id === id);
    if (!permitido) return false;
    // C05-01.M04.04: una segunda activación no cambia el Signal de nuevo.
    if (this.presetSeleccionado() === permitido.id) return false;
    this.presetSeleccionado.set(permitido.id);
    return true;
  }

  readonly tokens = [
    { nombre: 'Primario', css: '--color-primary', clase: 'swatch-primary' },
    { nombre: 'Acento', css: '--color-accent', clase: 'swatch-accent' },
    { nombre: 'Éxito', css: '--color-success', clase: 'swatch-success' },
    { nombre: 'Error', css: '--color-danger', clase: 'swatch-danger' }
  ] as const;
}
