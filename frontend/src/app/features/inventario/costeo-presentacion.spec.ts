import { MetodoCosteoInventario } from './costeo-inventario.model';
import { presentarMotivoCosteo } from './costeo-presentacion';

describe('Presentación comercial de Costeo', () => {
  it('explica la referencia exacta de la migración sin alterar el motivo de origen', () => {
    const original = 'Cutover ERP-N1.10 — Promedio Ponderado compatible';
    expect(presentarMotivoCosteo(original, MetodoCosteoInventario.PromedioPonderado)).toBe('Se adoptó el promedio ponderado como política inicial de valoración de existencias.');
    expect(presentarMotivoCosteo(original, MetodoCosteoInventario.FIFO)).toBe(original);
  });
  it('preserva íntegros los motivos registrados por las empresas', () => {
    const motivo = 'Cambio aprobado por cierre contable. Referencia ERP-N1.10.';
    expect(presentarMotivoCosteo(motivo, MetodoCosteoInventario.PromedioPonderado)).toBe(motivo);
    expect(presentarMotivoCosteo('  Motivo de prueba  ', MetodoCosteoInventario.FIFO)).toBe('  Motivo de prueba  ');
    expect(presentarMotivoCosteo(' ', MetodoCosteoInventario.PromedioPonderado)).toBe('Sin motivo registrado.');
    expect(presentarMotivoCosteo(null, MetodoCosteoInventario.PromedioPonderado)).toBe('Sin motivo registrado.');
  });
});
