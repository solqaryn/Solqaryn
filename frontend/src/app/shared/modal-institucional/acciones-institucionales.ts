import { DestroyRef, Injector, Type, effect, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { firstValueFrom } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { AppAlertData, AppAlertService } from '../alerts/app-alert.service';
import { modalInstitucional, TamanoModal } from './modal-institucional';

/** Control por consumidor, no singleton: evita mezclar formularios o permisos entre listas. */
export function crearAccionesInstitucionales(modulo: string) {
  const injector = inject(Injector);
  const destroyRef = inject(DestroyRef);
  const pendiente = signal(false);
  const contexto = () => injector.get(TenantContextService).contextoVerificado();
  const autorizado = (accion: string) =>
    !destroyRef.destroyed && injector.get(PermisosRuntimeService).puede(modulo, accion);

  async function confirmar(accion: string, data: AppAlertData): Promise<boolean> {
    const inicial = contexto();
    if (!inicial || !autorizado(accion) || pendiente()) return false;
    pendiente.set(true);
    try {
      const respuesta = await injector.get(AppAlertService).confirmar(data);
      return respuesta === true && inicial === contexto() && autorizado(accion);
    } catch {
      return false;
    } finally {
      pendiente.set(false);
    }
  }

  async function solicitarTexto(accion: string, data: AppAlertData): Promise<string | null> {
    const inicial = contexto();
    if (!inicial || !autorizado(accion) || pendiente()) return null;
    pendiente.set(true);
    try {
      const respuesta = await injector.get(AppAlertService).solicitarTexto(data);
      return typeof respuesta === 'string' && !!respuesta.trim() && inicial === contexto() && autorizado(accion) ? respuesta.trim() : null;
    } catch { return null; }
    finally { pendiente.set(false); }
  }

  async function abrir(
    cargar: () => Promise<Type<unknown>>, tamano: TamanoModal, accion: 'Crear' | 'Editar' | 'Ver',
    id?: number
  ): Promise<boolean> {
    const inicial = contexto();
    if (!inicial || !autorizado(accion) || pendiente()) return false;
    if (accion !== 'Crear' && (!Number.isInteger(id) || (id ?? 0) <= 0)) return false;
    pendiente.set(true);
    try {
      const componente = await cargar();
      if (inicial !== contexto() || !autorizado(accion)) return false;
      const resultado = await firstValueFrom(injector.get(MatDialog).open(
        componente, modalInstitucional(tamano, { data: { id }, ariaLabel: accion + ' registro' })
      ).afterClosed());
      return resultado === true && inicial === contexto() && !destroyRef.destroyed;
    } catch {
      if (!destroyRef.destroyed) injector.get(MatSnackBar).open('No se pudo abrir el registro.', 'Cerrar', { duration: 5000 });
      return false;
    } finally {
      pendiente.set(false);
    }
  }

  return { confirmar, solicitarTexto, abrir, contexto, vigente: (inicial: ReturnType<typeof contexto>, accion: string) => !!inicial && inicial === contexto() && autorizado(accion), pendiente: pendiente.asReadonly() };
}

/** Adaptador de formulario para navegación directa protegida o diálogo de la lista. */
export function crearEditorInstitucional(modulo: string) {
  const acciones = crearAccionesInstitucionales(modulo);
  const tenant = inject(TenantContextService);
  const permisos = inject(PermisosRuntimeService);
  const destroyRef = inject(DestroyRef);
  const ref = inject(MatDialogRef<unknown>, { optional: true });
  const data = inject<{ id?: number }>(MAT_DIALOG_DATA, { optional: true });
  const inicial = tenant.contextoVerificado();
  const valido = (accion: string) => !destroyRef.destroyed && !!inicial &&
    inicial === tenant.contextoVerificado() && permisos.puede(modulo, accion);
  if (ref) effect(() => {
    if (!inicial || inicial !== tenant.contextoVerificado()) ref.close(false);
  });
  return {
    ref,
    id: data?.id,
    destroyRef,
    valido,
    async confirmar(accion: string, data: AppAlertData): Promise<boolean> {
      return valido(accion) && await acciones.confirmar(accion, data) && valido(accion);
    },
    pendiente: acciones.pendiente,
    async solicitarTexto(accion: string, datos: AppAlertData): Promise<string | null> {
      if (!valido(accion)) return null;
      const texto = await acciones.solicitarTexto(accion, datos);
      return valido(accion) ? texto : null;
    },
    async abrir(cargar: () => Promise<Type<unknown>>, tamano: TamanoModal, accion: 'Crear' | 'Editar' | 'Ver', id?: number): Promise<boolean> {
      return valido(accion) && await acciones.abrir(cargar, tamano, accion, id) && valido(accion);
    },
    cerrar(guardado: boolean): void { ref?.close(guardado); }
  };
}
