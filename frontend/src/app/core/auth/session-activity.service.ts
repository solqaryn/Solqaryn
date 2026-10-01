import { Injectable, NgZone } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from './auth.service';
import { PermisosRuntimeService } from './permisos-runtime.service';

const INACTIVITY_LIMIT_MS = 30 * 60 * 1000;
const TOKEN_RENEW_INTERVAL_MS = 5 * 60 * 1000;
const ACTIVITY_WRITE_THROTTLE_MS = 1_000;
const SESSION_MESSAGE_KEY = 'solqaryn_session_message';
const LAST_ACTIVITY_KEY = 'solqaryn_last_activity';
const LAST_RENEW_KEY = 'solqaryn_last_renew';

@Injectable({ providedIn: 'root' })
export class SessionActivityService {
  private iniciado = false;
  private cerrando = false;
  private renovando = false;
  private intervalId?: number;
  private routerSubscription?: Subscription;
  private ultimaEscrituraActividad = 0;
  private readonly eventos = [
    'mousemove',
    'pointerdown',
    'click',
    'keydown',
    'input',
    'change',
    'touchstart',
    'scroll'
  ];

  constructor(
    private authService: AuthService,
    private permisosRuntime: PermisosRuntimeService,
    private router: Router,
    private zone: NgZone
  ) {}

  iniciar(): void {
    if (this.iniciado || !this.authService.getToken()) return;

    const ahora = Date.now();
    const actividadGuardada = Number(localStorage.getItem(LAST_ACTIVITY_KEY) || 0);
    if (actividadGuardada > 0 && ahora - actividadGuardada >= INACTIVITY_LIMIT_MS) {
      this.cerrarSesion('Tu sesión expiró por 30 minutos de inactividad.');
      return;
    }

    this.iniciado = true;
    this.marcarActividad(true);
    if (!localStorage.getItem(LAST_RENEW_KEY)) {
      localStorage.setItem(LAST_RENEW_KEY, String(ahora));
    }

    this.routerSubscription = this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd && this.authService.getToken()) {
        this.marcarActividad(true);
      }
    });

    this.zone.runOutsideAngular(() => {
      this.eventos.forEach((evento) => window.addEventListener(evento, this.onActividad, { passive: true }));
      this.intervalId = window.setInterval(() => this.verificar(), 15_000);
    });
  }

  detener(): void {
    if (!this.iniciado) return;
    this.iniciado = false;
    this.eventos.forEach((evento) => window.removeEventListener(evento, this.onActividad));
    if (this.intervalId) window.clearInterval(this.intervalId);
    this.intervalId = undefined;
    this.routerSubscription?.unsubscribe();
    this.routerSubscription = undefined;
  }

  cerrarPor401(): void {
    this.cerrarSesion('Tu sesión expiró. Inicia sesión nuevamente.');
  }

  cerrarManual(): void {
    this.limpiarMensajePendiente();
    this.cerrarSesion(undefined);
  }

  tomarMensajePendiente(): string | null {
    // No se elimina al leerlo: durante el cambio reactivo de layout Angular puede
    // construir una instancia transitoria de Login antes de completar la
    // navegación. El mensaje se limpia únicamente al iniciar sesión con éxito o
    // al cerrar manualmente.
    return sessionStorage.getItem(SESSION_MESSAGE_KEY);
  }

  limpiarMensajePendiente(): void {
    sessionStorage.removeItem(SESSION_MESSAGE_KEY);
  }

  private readonly onActividad = () => {
    if (this.authService.getToken()) this.marcarActividad(false);
  };

  private marcarActividad(forzar: boolean): void {
    const ahora = Date.now();
    const guardada = Number(localStorage.getItem(LAST_ACTIVITY_KEY) || 0);
    const escrituraLocalReciente = ahora - this.ultimaEscrituraActividad < ACTIVITY_WRITE_THROTTLE_MS;
    const almacenamientoReciente = guardada > 0 && ahora - guardada < ACTIVITY_WRITE_THROTTLE_MS;

    // El throttle solo puede omitir una escritura cuando tanto esta pestaña como
    // el almacenamiento compartido ya reflejan actividad reciente. Si otra
    // pestaña, una suspensión del navegador o una restauración dejó un valor
    // vencido, la primera interacción real siempre prevalece y reinicia el reloj.
    if (!forzar && escrituraLocalReciente && almacenamientoReciente) return;

    this.ultimaEscrituraActividad = ahora;
    localStorage.setItem(LAST_ACTIVITY_KEY, String(ahora));
  }

  private verificar(): void {
    if (!this.authService.getToken()) return;

    const ahora = Date.now();
    const ultimaActividad = Number(localStorage.getItem(LAST_ACTIVITY_KEY) || ahora);
    const inactividad = ahora - ultimaActividad;

    if (inactividad >= INACTIVITY_LIMIT_MS) {
      this.zone.run(() => this.cerrarSesion('Tu sesión expiró por 30 minutos de inactividad.'));
      return;
    }

    if (this.authService.isTokenExpired()) {
      this.zone.run(() => this.cerrarSesion('Tu sesión expiró. Inicia sesión nuevamente.'));
      return;
    }

    const ultimaRenovacion = Number(localStorage.getItem(LAST_RENEW_KEY) || 0);
    const huboActividadReciente = inactividad < TOKEN_RENEW_INTERVAL_MS;
    if (huboActividadReciente && ahora - ultimaRenovacion >= TOKEN_RENEW_INTERVAL_MS) {
      this.renovar();
    }
  }

  private renovar(): void {
    if (this.renovando || !this.authService.getToken()) return;
    this.renovando = true;

    this.zone.run(() => {
      this.authService.renovarSesion().subscribe({
        next: () => {
          localStorage.setItem(LAST_RENEW_KEY, String(Date.now()));
          this.renovando = false;
        },
        error: (error) => {
          this.renovando = false;
          if (error?.status === 401) this.cerrarPor401();
        }
      });
    });
  }

  private cerrarSesion(mensaje?: string): void {
    if (this.cerrando) return;
    this.cerrando = true;
    this.detener();
    if (mensaje) sessionStorage.setItem(SESSION_MESSAGE_KEY, mensaje);
    this.authService.logout();
    this.permisosRuntime.limpiar();
    localStorage.removeItem(LAST_ACTIVITY_KEY);
    localStorage.removeItem(LAST_RENEW_KEY);
    this.router.navigate(['/login']).finally(() => {
      this.cerrando = false;
    });
  }
}
