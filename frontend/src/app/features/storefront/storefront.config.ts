import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

export type ModoCarrito = 'whatsapp' | 'tarjeta' | 'ambos';

export interface StorefrontConfiguracion {
  utilizarDatosBaseDatos: boolean;
  modoCarrito: ModoCarrito;
  mostrarControlesVistaPrevia: boolean;
  /** Relative to environment.apiUrl. Leave null until a server checkout exists. */
  endpointCheckoutTarjeta: string | null;
  /** Exact HTTPS origins permitted for the server-generated checkout URL. */
  origenesCheckoutPermitidos: readonly string[];
}

/** Feature-only switches. No changes to company identity, theme or shared settings. */
export const STOREFRONT_CONFIGURACION: Readonly<StorefrontConfiguracion> = {
  utilizarDatosBaseDatos: environment.production,
  modoCarrito: 'ambos',
  mostrarControlesVistaPrevia: !environment.production,
  endpointCheckoutTarjeta: null,
  origenesCheckoutPermitidos: []
};

export const STOREFRONT_CONFIG = new InjectionToken<Readonly<StorefrontConfiguracion>>(
  'STOREFRONT_CONFIG', { providedIn: 'root', factory: () => STOREFRONT_CONFIGURACION }
);
