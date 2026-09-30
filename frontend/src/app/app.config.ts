import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withPreloading } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import { ALMACENES_ROUTES } from './features/almacenes/almacenes.routes';
import { UBICACIONES_ALMACEN_ROUTES } from './features/ubicaciones-almacen/ubicaciones-almacen.routes';
import { TRANSFERENCIAS_INVENTARIO_ROUTES } from './features/inventario/transferencias.routes';
import { COTIZACIONES_ROUTES } from './features/cotizaciones/cotizaciones.routes';
import { CENTROS_COSTO_ROUTES } from './features/centros-costo/centros-costo.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { AfterRenderSelectivePreloadingStrategy } from './core/performance/after-render-selective-preloading.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter([
      ...UBICACIONES_ALMACEN_ROUTES,
      ...ALMACENES_ROUTES,
      ...TRANSFERENCIAS_INVENTARIO_ROUTES,
      ...COTIZACIONES_ROUTES,
      ...CENTROS_COSTO_ROUTES,
      ...routes
    ], withPreloading(AfterRenderSelectivePreloadingStrategy)),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
