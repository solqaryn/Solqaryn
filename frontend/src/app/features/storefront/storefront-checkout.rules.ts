import { CheckoutLineaValidada, DatosCompradorCheckout } from './storefront.models';
import { normalizarTelefonoWhatsApp } from '../../core/services/whatsapp-share.service';

export function urlCheckoutPermitida(url: string, origenesPermitidos: readonly string[]): string {
  try {
    const destino = new URL(url);
    const permitidos = new Set(origenesPermitidos.map(origen => {
      try { return new URL(origen).origin; } catch { return ''; }
    }).filter(Boolean));
    return destino.protocol === 'https:' && permitidos.has(destino.origin) ? destino.toString() : '';
  } catch {
    return '';
  }
}

export function normalizarDatosComprador(datos: DatosCompradorCheckout): DatosCompradorCheckout {
  return {
    nombre: datos.nombre.trim().replace(/\s+/g, ' ').slice(0, 120),
    telefono: normalizarTelefonoWhatsApp(datos.telefono) || datos.telefono.replace(/[^0-9+]/g, '').slice(0, 24),
    correo: datos.correo?.trim().toLowerCase().slice(0, 160) || undefined,
    notas: datos.notas?.trim().slice(0, 600) || undefined
  };
}

export interface ProductoMensajeWhatsApp {
  nombre: string;
  modelo?: string;
  sku?: string;
  unidades: number;
  precioUnitario: number;
  subtotal: number;
  enlace?: string;
}

export function mensajeWhatsappCompraDirecta(
  comercio: string,
  moneda: string,
  producto: ProductoMensajeWhatsApp
): string {
  const formato = new Intl.NumberFormat('es-HN', { style: 'currency', currency: moneda || 'HNL' });
  const marca = limpiarTextoWhatsapp(comercio || 'Tienda');
  const nombre = limpiarTextoWhatsapp(producto.nombre);
  const modelo = producto.modelo ? limpiarTextoWhatsapp(producto.modelo) : '';
  const sku = producto.sku ? limpiarTextoWhatsapp(producto.sku) : '';
  const enlace = enlaceProductoSeguro(producto.enlace);

  return [
    `🛍️ *Solicitud de compra — ${marca}*`,
    '',
    `📦 *${nombre}*`,
    modelo ? `Modelo: ${modelo}` : '',
    sku ? `SKU: ${sku}` : '',
    enlace ? `🔗 *Ver producto:* ${enlace}` : '',
    '',
    `Cantidad: *${producto.unidades}*`,
    `Precio unitario: ${formato.format(producto.precioUnitario)}`,
    `💰 *TOTAL: ${formato.format(producto.subtotal)}*`,
    '',
    `✅ Solicitud generada desde *${marca}*.`,
    'La tienda confirmará disponibilidad, entrega y condiciones.'
  ].filter((linea, indice, lineas) => linea !== '' || (indice > 0 && lineas[indice - 1] !== '')).join('\n').trim();
}

export function mensajeWhatsappCheckout(
  comercio: string,
  comprador: DatosCompradorCheckout,
  referencia: string,
  moneda: string,
  lineas: readonly CheckoutLineaValidada[],
  total: number,
  enlacesProducto: Readonly<Record<number, string>> = {}
): string {
  const formato = new Intl.NumberFormat('es-HN', { style: 'currency', currency: moneda || 'HNL' });
  const marca = limpiarTextoWhatsapp(comercio || 'Tienda');
  const nombreCliente = limpiarTextoWhatsapp(comprador.nombre);
  const referenciaLimpia = limpiarTextoWhatsapp(referencia);
  const bloquesProductos = lineas.map((linea, indice) => {
    const nombre = limpiarTextoWhatsapp(linea.nombre);
    const modelo = linea.modelo ? `\nModelo: ${limpiarTextoWhatsapp(linea.modelo)}` : '';
    const sku = linea.sku ? `\nSKU: ${limpiarTextoWhatsapp(linea.sku)}` : '';
    const enlace = enlaceProductoSeguro(enlacesProducto[linea.productoId]);
    return [
      `📦 *Producto ${indice + 1}*`,
      `*${nombre}*${modelo}${sku}`,
      enlace ? `🔗 Ver producto: ${enlace}` : '',
      `Cantidad: ${linea.unidades}`,
      `Precio unitario: ${formato.format(linea.precioUnitario)}`,
      `Subtotal: *${formato.format(linea.total)}*`
    ].filter(Boolean).join('\n');
  }).join('\n\n');

  const contacto = comprador.telefono
    ? `📱 *Teléfono:* ${limpiarTextoWhatsapp(comprador.telefono)}`
    : '';
  const correo = comprador.correo
    ? `✉️ *Correo:* ${limpiarTextoWhatsapp(comprador.correo)}`
    : '';
  const notas = comprador.notas
    ? `\n\n📝 *Nota del cliente*\n${limpiarTextoWhatsapp(comprador.notas)}`
    : '';

  return [
    `🛍️ *Nueva solicitud de compra — ${marca}*`,
    '',
    `🔖 *Referencia:* ${referenciaLimpia}`,
    '',
    '👤 *Cliente*',
    nombreCliente,
    contacto,
    correo,
    '',
    '────────────',
    '*Detalle del pedido*',
    '',
    bloquesProductos,
    '',
    '────────────',
    `💰 *TOTAL: ${formato.format(total)}*`,
    notas,
    '',
    `✅ Solicitud generada desde *${marca}*.`,
    'La tienda confirmará disponibilidad, entrega y condiciones antes de finalizar la compra.'
  ].filter(linea => linea !== '').join('\n');
}

function limpiarTextoWhatsapp(valor: string): string {
  return valor
    .replace(/[\u0000-\u001F\u007F]/g, ' ')
    .replace(/[*_~`]/g, '')
    .replace(/\s+/g, ' ')
    .trim();
}

function enlaceProductoSeguro(valor: string | undefined): string {
  if (!valor) return '';
  try {
    const url = new URL(valor);
    const loopbackHttp = url.protocol === 'http:' && (url.hostname === 'localhost' || url.hostname === '127.0.0.1');
    return url.protocol === 'https:' || loopbackHttp ? url.toString() : '';
  } catch {
    return '';
  }
}
