import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

export type AppToastKind = 'info' | 'success' | 'warning' | 'error';

export interface AppToastOptions {
  tipo?: AppToastKind;
  duracionMs?: number;
  accion?: string;
}

@Injectable({ providedIn: 'root' })
export class AppToastService {
  constructor(private snackBar: MatSnackBar) {}

  mostrar(mensaje: string, options: AppToastOptions = {}): void {
    const tipo = options.tipo ?? 'info';
    const duracionMs = options.duracionMs ?? (tipo === 'error' ? 6500 : 4200);

    this.snackBar.open(mensaje, options.accion ?? 'Cerrar', {
      duration: duracionMs,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      politeness: tipo === 'error' ? 'assertive' : 'polite',
      panelClass: [`app-toast`, `app-toast--${tipo}`]
    });
  }

  info(mensaje: string, options: Omit<AppToastOptions, 'tipo'> = {}): void {
    this.mostrar(mensaje, { ...options, tipo: 'info' });
  }

  success(mensaje: string, options: Omit<AppToastOptions, 'tipo'> = {}): void {
    this.mostrar(mensaje, { ...options, tipo: 'success' });
  }

  warning(mensaje: string, options: Omit<AppToastOptions, 'tipo'> = {}): void {
    this.mostrar(mensaje, { ...options, tipo: 'warning' });
  }

  error(mensaje: string, options: Omit<AppToastOptions, 'tipo'> = {}): void {
    this.mostrar(mensaje, { ...options, tipo: 'error' });
  }
}
