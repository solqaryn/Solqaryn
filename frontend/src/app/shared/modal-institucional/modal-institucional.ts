import { MatDialogConfig } from '@angular/material/dialog';

/** Única escala institucional: el viewport prevalece sobre el ancho nominal. */
export const ANCHOS_MODAL = { s: '440px', m: '640px', l: '960px', xl: '1200px' } as const;
export type TamanoModal = keyof typeof ANCHOS_MODAL;

export const MODAL_DEFAULTS: MatDialogConfig = {
  ...new MatDialogConfig(),
  disableClose: true,
  maxWidth: 'calc(100vw - 24px)',
  maxHeight: 'calc(100dvh - 24px)',
  restoreFocus: true,
  closeOnNavigation: true
};

export function modalInstitucional<D = unknown>(
  tamano: TamanoModal, opciones: MatDialogConfig<D> = {}
): MatDialogConfig<D> {
  const clases = typeof opciones.panelClass === 'string' ? [opciones.panelClass] : (opciones.panelClass ?? []);
  return {
    ...MODAL_DEFAULTS,
    ...opciones,
    width: ANCHOS_MODAL[tamano],
    maxWidth: MODAL_DEFAULTS.maxWidth,
    maxHeight: MODAL_DEFAULTS.maxHeight,
    panelClass: [...clases, 'solqaryn-modal', 'solqaryn-modal--' + tamano],
    disableClose: true,
    restoreFocus: true,
    closeOnNavigation: true
  } as MatDialogConfig<D>;
}
