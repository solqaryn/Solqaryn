import { describe, expect, it } from 'vitest';
import {
  ejecutarFacturaWhatsAppHandoff,
  WHATSAPP_AUDIT_FAILURE_MESSAGE
} from './whatsapp-share.service';

function escenario(aperturaAceptada: boolean, auditoriaFalla = false) {
  const auditoria: Array<{ error: () => void }> = [];
  const llamadas: unknown[][] = [];
  const avisos: string[] = [];
  const resultado = ejecutarFacturaWhatsAppHandoff({
    numero: '+504 9999-9999',
    mensaje: 'Factura FAC-3\nTotal: L. 200 & listo',
    construirEnlace: () => 'https://wa.me/50499999999?text=Factura',
    abrir: (...args) => { llamadas.push(args); return aperturaAceptada; },
    registrarAuditoria: (payload) => ({ subscribe: (handlers: { error: () => void }) => {
      if (auditoriaFalla) auditoria.push(handlers);
    } }),
    onAuditError: () => avisos.push(WHATSAPP_AUDIT_FAILURE_MESSAGE)
  });
  if (auditoriaFalla) auditoria[0]?.error();
  return { resultado, llamadas, avisos };
}

describe('Factura → FacturaService WhatsApp handoff contract', () => {
  it('sends normalized phone to audit when popup is accepted', () => {
    const caso = escenario(true);
    expect(caso.resultado?.aperturaAceptada).toBe(true);
    expect(caso.resultado?.payload).toEqual({
      canal: 'WhatsApp', destinatario: '50499999999', resultado: 'WHATSAPP_CLIENT_OPEN_REQUESTED'
    });
    expect(caso.resultado?.payload.destinatario).not.toContain('*');
    expect(caso.llamadas).toHaveLength(1);
  });

  it('keeps the safe fallback when the popup is blocked', () => {
    const caso = escenario(false);
    expect(caso.resultado?.aperturaAceptada).toBe(false);
    expect(caso.resultado?.url).toContain('https://wa.me/');
  });

  it('keeps wording neutral when audit fails after a blocked popup', () => {
    const caso = escenario(false, true);
    expect(caso.resultado?.aperturaAceptada).toBe(false);
    expect(caso.avisos).toEqual([WHATSAPP_AUDIT_FAILURE_MESSAGE]);
    expect(caso.avisos.join(' ')).not.toMatch(/WhatsApp se abrió|enviado|entregado|leído/i);
  });

  it('does not change accepted handoff when audit fails', () => {
    const caso = escenario(true, true);
    expect(caso.resultado?.aperturaAceptada).toBe(true);
    expect(caso.avisos[0]).toBe(WHATSAPP_AUDIT_FAILURE_MESSAGE);
  });
});
