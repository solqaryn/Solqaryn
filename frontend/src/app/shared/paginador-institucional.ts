import { MatPaginatorIntl } from '@angular/material/paginator';

/** Etiquetas compartidas por todos los paginadores de Angular Material. */
export function crearPaginadorEspanol(): MatPaginatorIntl {
  const intl = new MatPaginatorIntl();
  intl.itemsPerPageLabel = 'Registros por página';
  intl.nextPageLabel = 'Página siguiente';
  intl.previousPageLabel = 'Página anterior';
  intl.firstPageLabel = 'Primera página';
  intl.lastPageLabel = 'Última página';
  intl.getRangeLabel = (page, pageSize, length) => {
    const total = Math.max(0, length);
    if (total === 0 || pageSize <= 0) return `0 de ${total}`;
    const inicio = Math.max(0, page) * pageSize;
    if (inicio >= total) return `0 de ${total}`;
    return `${inicio + 1}–${Math.min(inicio + pageSize, total)} de ${total}`;
  };
  return intl;
}

/** Instancia local de cada vista paginada: no importar en el bootstrap. */
export const PAGINADOR_ESPANOL_PROVIDER = { provide: MatPaginatorIntl, useFactory: crearPaginadorEspanol };
