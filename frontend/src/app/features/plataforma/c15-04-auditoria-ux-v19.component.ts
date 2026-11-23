import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

type FiltroAuditoriaUx = 'todas' | 'publica' | 'protegida' | 'sin-ruta';

/**
 * C15-04.M04.01 — contrato visual privado de auditoria UX.
 * No es telemetria en vivo ni una certificacion WCAG o de rendimiento.
 * El acceso lo controla plataformaGuard del router.
 */
@Component({
  selector: 'app-c1504-auditoria-ux-v19',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './c15-04-auditoria-ux-v19.component.html',
  styleUrl: './c15-04-auditoria-ux-v19.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class C1504AuditoriaUxV19Component {
  // Enlaces a rutas existentes; cada guard original sigue siendo la autoridad.
  // El builder no tiene una ruta unica publicada en app.routes.ts: nunca inventar acceso.
  readonly superficies = [
    { id: 'landing', titulo: 'Landing', objetivo: 'Lectura inicial, accesibilidad, formularios y carga visual.', ruta: '/conoce-solqaryn', acceso: 'Ruta publica.', categoria: 'publica' },
    { id: 'portal', titulo: 'Portal', objetivo: 'Estados, permisos, teclado y navegación autenticada.', ruta: '/plataforma', acceso: 'Requiere autoridad global comprobada.', categoria: 'protegida' },
    { id: 'erp', titulo: 'ERP', objetivo: 'Tablas, densidad, navegación y respuesta en pantallas pequeñas.', ruta: '/dashboard', acceso: 'Requiere sesion empresarial y permiso Dashboard.Ver.', categoria: 'protegida' },
    { id: 'builder', titulo: 'Builder', objetivo: 'Controles, previsualización, foco y configuración segura.', ruta: null, acceso: 'Sin ruta unica publicada; revisar desde el modulo autorizado.', categoria: 'sin-ruta' },
    { id: 'global', titulo: 'Consola global', objetivo: 'Superficies privadas, contraste, errores y carga progresiva.', ruta: '/plataforma/consola-global-v5', acceso: 'Requiere autoridad global comprobada.', categoria: 'protegida' }
  ] as const;
  readonly filtros = [
    { id: 'todas', texto: 'Todas' },
    { id: 'publica', texto: 'Públicas' },
    { id: 'protegida', texto: 'Protegidas' },
    { id: 'sin-ruta', texto: 'Sin ruta publicada' }
  ] as const;

  readonly filtroActivo = signal<FiltroAuditoriaUx>('todas');
  readonly superficiesVisibles = computed(() => {
    const filtro = this.filtroActivo();
    return this.superficies.filter(superficie => filtro === 'todas' || superficie.categoria === filtro);
  });

  seleccionarFiltro(filtro: FiltroAuditoriaUx): void {
    this.filtroActivo.set(filtro);
  }

  readonly anchosObjetivo = [320, 375, 768, 1280] as const;
}
