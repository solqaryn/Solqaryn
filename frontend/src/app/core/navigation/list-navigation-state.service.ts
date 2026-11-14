import { Injectable } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

@Injectable({ providedIn: 'root' })
export class ListNavigationStateService {
  private readonly prefix = 'solqaryn.navigation.v1';

  constructor(private router: Router, private auth: AuthService) {}

  restore<T extends object>(scope: string, route: ActivatedRoute, defaults: T): T {
    const defaultRecord = defaults as Record<string, unknown>;
    const result: Record<string, unknown> = { ...defaultRecord };
    const stored = this.read(scope);

    for (const key of Object.keys(defaultRecord)) {
      if (stored && Object.prototype.hasOwnProperty.call(stored, key)) {
        result[key] = this.coerce(stored[key], defaultRecord[key]);
      }

      const queryValue = route.snapshot.queryParamMap.get(key);
      if (queryValue !== null) {
        result[key] = this.coerce(queryValue, defaultRecord[key]);
      }
    }

    return result as T;
  }

  persist<T extends object>(scope: string, route: ActivatedRoute, state: T, defaults: T): void {
    const storage = this.storage();
    if (storage) {
      try {
        storage.setItem(this.key(scope), JSON.stringify(state));
      } catch {
        // El almacenamiento puede estar bloqueado por el navegador; la URL sigue siendo fuente navegable.
      }
    }

    const stateRecord = state as Record<string, unknown>;
    const defaultRecord = defaults as Record<string, unknown>;
    const queryParams: Params = {};

    for (const key of Object.keys(defaultRecord)) {
      const value = stateRecord[key];
      if (!Object.is(value, defaultRecord[key])) {
        queryParams[key] = value;
      }
    }

    void this.router.navigate([], {
      relativeTo: route,
      queryParams,
      replaceUrl: true
    });
  }

  clear(scope: string): void {
    const storage = this.storage();
    if (!storage) return;
    try {
      storage.removeItem(this.key(scope));
    } catch {
      // Sin almacenamiento disponible no hay estado que limpiar.
    }
  }

  private read(scope: string): Record<string, unknown> | null {
    const storage = this.storage();
    if (!storage) return null;

    try {
      const raw = storage.getItem(this.key(scope));
      if (!raw) return null;
      const parsed = JSON.parse(raw);
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
        ? parsed as Record<string, unknown>
        : null;
    } catch {
      return null;
    }
  }

  private key(scope: string): string {
    const usuario = (this.auth.nombreUsuario() || 'anonimo').trim().toLowerCase();
    return `${this.prefix}.${encodeURIComponent(usuario)}.${scope}`;
  }

  private storage(): Storage | null {
    try {
      return typeof sessionStorage === 'undefined' ? null : sessionStorage;
    } catch {
      return null;
    }
  }

  private coerce(value: unknown, fallback: unknown): unknown {
    if (typeof fallback === 'number') {
      const numberValue = Number(value);
      return Number.isFinite(numberValue) ? numberValue : fallback;
    }

    if (typeof fallback === 'boolean') {
      if (value === true || value === 'true') return true;
      if (value === false || value === 'false') return false;
      return fallback;
    }

    if (typeof fallback === 'string') {
      return typeof value === 'string' ? value : String(value ?? '');
    }

    return value ?? fallback;
  }
}
