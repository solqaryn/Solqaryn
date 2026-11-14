import {
  construirEnlaceWhatsApp,
  crearPayloadAuditoriaWhatsApp,
  enmascararTelefonoWhatsApp,
  normalizarTelefonoWhatsApp
} from './whatsapp-share.service';
import { describe, expect, it } from 'vitest';

describe('WhatsAppShareService policy', () => {
  it('normaliza formatos hondureños y conserva internacional', () => {
    expect(normalizarTelefonoWhatsApp('+504 9999-9999')).toBe('50499999999');
    expect(normalizarTelefonoWhatsApp('50499999999')).toBe('50499999999');
    expect(normalizarTelefonoWhatsApp('9999-9999')).toBe('50499999999');
    expect(normalizarTelefonoWhatsApp('(504) 9999 9999')).toBe('50499999999');
    expect(normalizarTelefonoWhatsApp('+521 5555 555555')).toBe('5215555555555');
  });

  it('rechaza vacío, caracteres inválidos, corto y prefijo duplicado', () => {
    expect(normalizarTelefonoWhatsApp(null)).toBe('');
    expect(normalizarTelefonoWhatsApp('abc')).toBe('');
    expect(normalizarTelefonoWhatsApp('123')).toBe('');
    expect(normalizarTelefonoWhatsApp('50450499999999')).toBe('');
  });

  it('codifica Unicode y símbolos sin incluir secretos', () => {
    const url = construirEnlaceWhatsApp('99999999', 'áéíóú ñ & ? = #\nFactura FAC-3');
    expect(url).toContain('https://wa.me/50499999999?text=');
    expect(url).toContain(encodeURIComponent('áéíóú ñ & ? = #\nFactura FAC-3'));
    expect(url).not.toContain('jwt');
  });

  it('enmascara el destinatario para auditoría', () => {
    expect(enmascararTelefonoWhatsApp('99999999')).toBe('*******9999');
  });

  it('envía al API el número normalizado y deja el enmascarado al backend', () => {
    const payload = crearPayloadAuditoriaWhatsApp('+504 9999-9999');
    expect(payload).toEqual({
      canal: 'WhatsApp', destinatario: '50499999999', resultado: 'WHATSAPP_CLIENT_OPEN_REQUESTED'
    });
    expect(payload.destinatario).not.toContain('*');
  });
});
