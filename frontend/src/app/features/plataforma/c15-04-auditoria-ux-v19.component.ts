import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

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
    { id: 'landing', titulo: 'Landing', objetivo: 'Lectura inicial, accesibilidad, formularios y carga visual.', ruta: '/conoce-solqaryn', acceso: 'Ruta publica.' },
    { id: 'portal', titulo: 'Portal', objetivo: 'Estados, permisos, teclado y navegación autenticada.', ruta: '/plataforma', acceso: 'Requiere autoridad global comprobada.' },
    { id: 'erp', titulo: 'ERP', objetivo: 'Tablas, densidad, navegación y respuesta en pantallas pequeñas.', ruta: '/dashboard', acceso: 'Requiere sesion empresarial y permiso Dashboard.Ver.' },
    { id: 'builder', titulo: 'Builder', objetivo: 'Controles, previsualización, foco y configuración segura.', ruta: null, acceso: 'Sin ruta unica publicada; revisar desde el modulo autorizado.' },
    { id: 'global', titulo: 'Consola global', objetivo: 'Superficies privadas, contraste, errores y carga progresiva.', ruta: '/plataforma/consola-global-v5', acceso: 'Requiere autoridad global comprobada.' }
  ] as const;
  readonly anchosObjetivo = [320, 375, 768, 1280] as const;
}
