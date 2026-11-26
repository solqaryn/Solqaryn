import { MetodoCosteoInventario } from './costeo-inventario.model';
import { describirMetodoCosteo, presentarMotivoCosteo } from './costeo-presentacion';

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

describe('Descripción de los métodos de costeo', () => {
  it('explica cada método sin confundir sus criterios de valoración', () => {
    expect(describirMetodoCosteo(MetodoCosteoInventario.PromedioPonderado)).toContain('costo unitario promedio');
    expect(describirMetodoCosteo(MetodoCosteoInventario.FIFO)).toContain('entradas más antiguas');
    expect(describirMetodoCosteo(MetodoCosteoInventario.Estandar)).toContain('costo de referencia');
    expect(new Set([
      describirMetodoCosteo(MetodoCosteoInventario.PromedioPonderado),
      describirMetodoCosteo(MetodoCosteoInventario.FIFO),
      describirMetodoCosteo(MetodoCosteoInventario.Estandar)
    ]).size).toBe(3);
  });
  it('no atribuye un método desconocido o no seleccionado a una política', () => {
    expect(describirMetodoCosteo(null)).toBeNull();
    expect(describirMetodoCosteo(undefined)).toBeNull();
    expect(describirMetodoCosteo(99 as MetodoCosteoInventario)).toBeNull();
  });
});
