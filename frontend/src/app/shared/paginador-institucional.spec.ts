import { MatPaginatorIntl } from '@angular/material/paginator';
import { crearPaginadorEspanol, PAGINADOR_ESPANOL_PROVIDER } from './paginador-institucional';

describe('Paginación institucional en español', () => {
  it('mantiene las etiquetas y todos los límites de rango', () => {
    const intl = crearPaginadorEspanol();
    expect(intl.itemsPerPageLabel).toBe('Registros por página');
    expect(intl.nextPageLabel).toBe('Página siguiente');
    expect(intl.previousPageLabel).toBe('Página anterior');
    expect(intl.firstPageLabel).toBe('Primera página');
    expect(intl.lastPageLabel).toBe('Última página');
    expect(intl.getRangeLabel(0, 20, 0)).toBe('0 de 0');
    expect(intl.getRangeLabel(0, 20, 43)).toBe('1–20 de 43');
    expect(intl.getRangeLabel(2, 20, 43)).toBe('41–43 de 43');
    expect(intl.getRangeLabel(3, 20, 43)).toBe('0 de 43');
  });
  it('crea instancias independientes para las vistas y conserva el token Material', () => {
    expect(PAGINADOR_ESPANOL_PROVIDER.provide).toBe(MatPaginatorIntl);
    expect(PAGINADOR_ESPANOL_PROVIDER.useFactory()).not.toBe(PAGINADOR_ESPANOL_PROVIDER.useFactory());
  });
});
