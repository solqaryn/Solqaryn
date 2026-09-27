const PRODUCTION_HOST = 'solqaryn-prod.vercel.app';
const PRODUCTION_ORIGIN = 'https://solqaryn-prod.vercel.app';
const PROD_API = 'https://solqaryn-api-prod.onrender.com';
const DEV_API = 'https://solqaryn-api-dev-fxx8.onrender.com';

function hostFromRequest(req) {
  return String(req.headers['x-forwarded-host'] || req.headers.host || '').split(':')[0].toLowerCase();
}

function isIndexableHost(req) {
  return hostFromRequest(req) === PRODUCTION_HOST;
}

function publicOrigin(_req) {
  return PRODUCTION_ORIGIN;
}

function apiBase(req) {
  return isIndexableHost(req) ? PROD_API : DEV_API;
}

async function fetchJson(url, timeoutMs = 6000) {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(url, {
      headers: { accept: 'application/json', 'user-agent': 'SOLQARYN-SEO/1.0' },
      signal: controller.signal
    });
    if (!response.ok) {
      const error = new Error(`HTTP ${response.status}`);
      error.status = response.status;
      throw error;
    }
    return await response.json();
  } finally {
    clearTimeout(timer);
  }
}

async function loadBrand(req) {
  try {
    const payload = await fetchJson(`${apiBase(req)}/empresa-configuracion/publica`, 3500);
    const data = payload && payload.success ? payload.data : null;
    const rawName = String(data?.nombreComercial || '').trim();
    const name = rawName && !/administrativ[oa]/i.test(rawName) ? rawName : 'Tienda';
    return {
      name,
      slogan: String(data?.eslogan || '').trim(),
      logo: absoluteUrl(String(data?.logoUrl || ''), publicOrigin(req)),
      currency: /^[A-Z]{3}$/.test(String(data?.moneda || '').trim().toUpperCase())
        ? String(data.moneda).trim().toUpperCase()
        : 'HNL'
    };
  } catch {
    return {
      name: 'Tienda',
      slogan: '',
      logo: '',
      currency: 'HNL'
    };
  }
}

function absoluteUrl(value, origin) {
  const raw = String(value || '').trim();
  if (!raw) return '';
  if (/^https?:\/\//i.test(raw)) return raw;
  try { return new URL(raw, `${origin}/`).toString(); } catch { return ''; }
}

function escapeHtml(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function escapeXml(value) {
  return escapeHtml(value);
}

function cleanText(value, fallback = '', max = 160) {
  const text = String(value || '').replace(/\s+/g, ' ').trim() || fallback;
  if (text.length <= max) return text;
  return `${text.slice(0, Math.max(1, max - 1)).trimEnd()}…`;
}

function safeSlug(value) {
  const slug = String(value || '').trim();
  return slug.length > 0
    && slug.length <= 180
    && /^[a-zA-Z0-9áéíóúüñÁÉÍÓÚÜÑ-]+$/.test(slug)
    ? slug
    : '';
}

function sendNoIndex(res, status, message) {
  res.statusCode = status;
  res.setHeader('Content-Type', 'text/html; charset=utf-8');
  res.setHeader('X-Robots-Tag', 'noindex,nofollow,noarchive');
  res.setHeader('Cache-Control', 'no-store');
  res.end(`<!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="robots" content="noindex,nofollow"><title>Tienda</title></head><body><p>${escapeHtml(message)}</p></body></html>`);
}

module.exports = {
  PRODUCTION_HOST,
  PRODUCTION_ORIGIN,
  apiBase,
  publicOrigin,
  isIndexableHost,
  fetchJson,
  loadBrand,
  absoluteUrl,
  escapeHtml,
  escapeXml,
  cleanText,
  safeSlug,
  sendNoIndex
};
