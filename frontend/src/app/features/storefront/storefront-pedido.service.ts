import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { ReciboPedidoPublico } from './storefront.models';

const PREFIJO = 'storefront:pedido:v1:';
const MAX_VIGENCIA_MS = 2 * 60 * 60 * 1000;

@Injectable({ providedIn: 'root' })
export class StorefrontPedidoService {
  private readonly document = inject(DOCUMENT);

  guardar(recibo: ReciboPedidoPublico): void {
    const referencia = this.referenciaSegura(recibo.referencia);
    if (!referencia) return;
    const ahora = Date.now();
    const expira = Math.min(Date.parse(recibo.expiraUtc), ahora + MAX_VIGENCIA_MS);
    if (!Number.isFinite(expira) || expira <= ahora) return;

    const seguro: ReciboPedidoPublico = {
      referencia,
      estado: recibo.estado,
      creadoUtc: recibo.creadoUtc,
      expiraUtc: new Date(expira).toISOString(),
      total: recibo.total,
      moneda: recibo.moneda,
      lineas: recibo.lineas.map(linea => ({ ...linea }))
    };

    try {
      this.document.defaultView?.sessionStorage.setItem(`${PREFIJO}${referencia}`, JSON.stringify(seguro));
    } catch {
      // El recibo es una comodidad de UX; nunca bloquea el canal de compra.
    }
  }

  obtener(referencia: string): ReciboPedidoPublico | null {
    const segura = this.referenciaSegura(referencia);
    if (!segura) return null;
    try {
      const storage = this.document.defaultView?.sessionStorage;
      const clave = `${PREFIJO}${segura}`;
      const raw = storage?.getItem(clave);
      if (!raw) return null;
      const recibo = JSON.parse(raw) as ReciboPedidoPublico;
      if (!this.esValido(recibo, segura)) {
        storage?.removeItem(clave);
        return null;
      }
      return recibo;
    } catch {
      return null;
    }
  }

  private esValido(recibo: ReciboPedidoPublico, referencia: string): boolean {
    const expira = Date.parse(recibo?.expiraUtc);
    return recibo?.referencia === referencia
      && ['whatsapp-preparado', 'tarjeta-redirigida', 'demo'].includes(recibo.estado)
      && Number.isFinite(expira)
      && expira > Date.now()
      && Number.isFinite(recibo.total)
      && recibo.total >= 0
      && typeof recibo.moneda === 'string'
      && Array.isArray(recibo.lineas)
      && recibo.lineas.length > 0;
  }

  private referenciaSegura(referencia: string): string {
    const valor = referencia.trim();
    return /^[a-zA-Z0-9_-]{8,80}$/.test(valor) ? valor : '';
  }
}
