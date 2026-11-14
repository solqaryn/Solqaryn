import { describe, expect, it } from 'vitest';
import { normalizarNombreDescarga } from './descarga-segura';

describe('normalizarNombreDescarga', () => {
  it('elimina rutas y caracteres reservados de nombres provistos por el servidor', () => {
    expect(normalizarNombreDescarga('../../factura:<septiembre>|*.pdf')).toBe('-..-factura--septiembre---.pdf');
  });

  it('elimina caracteres de control y normaliza espacios', () => {
    expect(normalizarNombreDescarga('  comprobante\u0000   proveedor.pdf  ')).toBe('comprobante proveedor.pdf');
  });

  it('usa un respaldo cuando el nombre no contiene caracteres utilizables', () => {
    expect(normalizarNombreDescarga('... ', 'comprobante')).toBe('comprobante');
  });

  it('limita nombres excesivos para evitar descargas no portables', () => {
    expect(normalizarNombreDescarga('a'.repeat(220))).toHaveLength(180);
  });
});
