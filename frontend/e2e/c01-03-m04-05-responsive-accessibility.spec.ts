import { expect, test, type Page } from '@playwright/test';

// Real .NET10 backend, signed JWT, Angular and ephemeral MySQL8.4. Never mock authorization.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('C01-03.M04.05 requires GitHub-hosted real API/JWT/MySQL/Chromium');
}
const target = '/configuracion/dinamica-v5';
const widths = [320, 390, 768, 1280, 1440] as const;

async function login(page: Page, username: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r => r.url().endsWith('/auth/login') &&
    r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const res = await pending;
  expect(res.status(), 'actual authenticated JWT login').toBe(200);
  const jwt: unknown = (await res.json())?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}

async function verifyLayout(page: Page, width: number, phase: string): Promise<void> {
  const data = await page.evaluate(() => {
    const root = document.documentElement;
    const targets = ['h1', 'app-empresa-configuracion-tenant-card', '.surface-content'];
    const boxes = targets.map(name => {
      const node = document.querySelector(name);
      if (!(node instanceof HTMLElement)) return { name, exists: false, left: 0, right: 0 };
      const rect = node.getBoundingClientRect();
      return { name, exists: true, left: rect.left, right: rect.right };
    });
    return { scroll: root.scrollWidth, width: root.clientWidth, boxes };
  });
  expect(data.scroll, 'document overflow at '+phase+' width '+width).toBeLessThanOrEqual(data.width + 2);
  for (const box of data.boxes) {
    expect(box.exists, box.name+' present').toBe(true);
    expect(box.left, box.name+' left clipping').toBeGreaterThanOrEqual(-2);
    expect(box.right, box.name+' right clipping').toBeLessThanOrEqual(data.width + 2);
  }
}

// V24: regression from real 390px CI PNGs at 100% and 200% root text size.
// The prior zero-gap header/form boundary crowded "Recargar" and "Datos de empresa".
// Measure actual DOM geometry, not screenshots or CSS declarations alone.
async function verifyCardHeaderSeparation(page: Page, width: number, mode: string): Promise<void> {
  const position = await page.evaluate(() => {
    const header = document.querySelector('app-empresa-configuracion-tenant-card .header-row');
    const form = document.querySelector('app-empresa-configuracion-tenant-card .tenant-form');
    if (!(header instanceof HTMLElement) || !(form instanceof HTMLElement)) return null;
    return { space: form.getBoundingClientRect().top - header.getBoundingClientRect().bottom,
      margin: Number.parseFloat(getComputedStyle(form).marginTop) };
  });
  expect(position, 'real tenant card header and form rendered').not.toBeNull();
  expect(position!.margin, 'computed form margin at '+width+' '+mode).toBeGreaterThanOrEqual(12);
  expect(position!.space, 'Recargar/form no collision at '+width+' '+mode).toBeGreaterThanOrEqual(12);
}

// Visual regression from actual GitHub-hosted 320px/200%-root-text PNG:
 // heading "Configuración" previously fragmented into "Configuraci" + "ón".
 // Range.getClientRects reads browser-rendered glyph lines, not CSS declarations.
async function verifyHeadingWholeWords(page: Page, width: number, phase: string): Promise<void> {
  const measured = await page.evaluate(() => {
    return ['.settings-surface h1', '.settings-surface nav a.back-link'].map(selector => {
      const element = document.querySelector(selector);
      const node = [...(element?.childNodes ?? [])].find(n =>
        n.nodeType === Node.TEXT_NODE && (n.textContent ?? '').includes('Configuración'));
      if (!node?.textContent) return { selector, present: false, segments: 0, left: 0, right: 0, viewport: 0 };
      const start = node.textContent.indexOf('Configuración');
      const range = document.createRange();
      range.setStart(node,start);
      range.setEnd(node,start+'Configuración'.length);
      const rects = [...range.getClientRects()].filter(rect => rect.width > 0 && rect.height > 0);
      return {
        selector, present: true, segments: rects.length,
        left: Math.min(...rects.map(rect => rect.left)),
        right: Math.max(...rects.map(rect => rect.right)),
        viewport: document.documentElement.clientWidth
      };
    });
  });
  for (const item of measured) {
    expect(item.present, item.selector+' exists at '+width+' '+phase).toBe(true);
    expect(item.segments, 'no mid-word wrap for Configuración at '+width+' '+phase).toBe(1);
    expect(item.left, 'word left visible at '+width+' '+phase).toBeGreaterThanOrEqual(-2);
    expect(item.right, 'word right visible at '+width+' '+phase).toBeLessThanOrEqual(item.viewport+2);
  }
}

async function verifyTab(page: Page): Promise<void> {
  // V24 R2: actual Tab must reach route controls, not a random visible app-shell link.
  // Never call .focus() or bypass browser keyboard navigation.
  let linkReached = false;
  let reloadReached = false;
  for (let n = 0; n < 120 && !(linkReached && reloadReached); n++) {
    await page.keyboard.press('Tab');
    const current = await page.evaluate(() => {
      const element = document.activeElement;
      if (!(element instanceof HTMLElement)) {
        return { link: false, reload: false, keyboard: false, outline: false };
      }
      const rect = element.getBoundingClientRect();
      const style = getComputedStyle(element);
      const visible = rect.width > 0 && rect.height > 0 &&
        style.visibility !== 'hidden' && style.display !== 'none';
      const within = visible && element.closest('.settings-surface') !== null;
      return {
        link: within && element.matches('a.secondary-link'),
        reload: within && element.matches('button') && /Recargar/i.test(element.textContent ?? ''),
        keyboard: element.matches(':focus-visible'),
        outline: style.outlineStyle !== 'none' && Number.parseFloat(style.outlineWidth) > 0
      };
    });
    if (current.link || current.reload) {
      expect(current.keyboard, 'route control has keyboard-visible focus').toBe(true);
    }
    if (current.link) {
      expect(current.outline, 'secondary link exposes a visible focus outline').toBe(true);
      linkReached = true;
    }
    if (current.reload) reloadReached = true;
  }
  expect(linkReached, 'Tab must reach settings secondary link, not just shell controls').toBe(true);
  expect(reloadReached, 'Tab must reach real tenant reload button').toBe(true);
}


/**
 * V24 R2: computa WCAG 2.x contrast desde estilos REALES del DOM.
 * Resuelve fondos rgba a través de ancestros; no acepta texto inventado ni mocks.
 * La prueba negativa comprueba que un par idéntico falla el umbral 4.5:1.
 */
async function verifyWcagContrast(page: Page): Promise<void> {
  const measured = await page.evaluate(() => {
    type Color = { rgb: number[]; alpha: number };
    const parse = (value: string): Color | null => {
      const m = /^rgba?\(\s*(.*?)\s*\)$/i.exec(value);
      if (!m) return null;
      const parts = m[1]!.replaceAll(',', ' ').replace('/', ' ').trim().split(/\s+/);
      if (parts.length < 3) return null;
      const rgb = parts.slice(0, 3).map(part =>
        part.endsWith('%') ? Number.parseFloat(part) * 2.55 : Number(part));
      const alpha = parts.length >= 4 ? Number(parts[3]) : 1;
      if (!rgb.every(v => Number.isFinite(v) && v >= 0 && v <= 255) ||
          !Number.isFinite(alpha) || alpha < 0 || alpha > 1) return null;
      return { rgb, alpha };
    };
    const relativeLuminance = (rgb: number[]): number => {
      const linear = rgb.map(v => {
        const channel = v / 255;
        return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
      });
      return linear[0]! * 0.2126 + linear[1]! * 0.7152 + linear[2]! * 0.0722;
    };
    const ratio = (foreground: number[], background: number[]): number => {
      const a = relativeLuminance(foreground), b = relativeLuminance(background);
      return (Math.max(a,b) + 0.05) / (Math.min(a,b) + 0.05);
    };
    const targetSelectors = [
      '.settings-surface h1',
      '.settings-surface .lead',
      '.settings-surface .secondary-link'
    ];
    const ratios = targetSelectors.map(selector => {
      const element = document.querySelector(selector);
      if (!(element instanceof HTMLElement)) return { selector, ratio: Number.NaN };
      const foreground = parse(getComputedStyle(element).color);
      if (!foreground) return { selector, ratio: Number.NaN };
      const layers: Color[] = [];
      for (let ancestor: Element | null = element; ancestor; ancestor = ancestor.parentElement) {
        const layer = parse(getComputedStyle(ancestor).backgroundColor);
        if (layer) layers.unshift(layer);
      }
      let backdrop = [255,255,255];
      for (const layer of layers) {
        backdrop = layer.rgb.map((v,i) => v * layer.alpha +
          backdrop[i]! * (1 - layer.alpha));
      }
      const effectiveForeground = foreground.rgb.map((v,i) =>
        v * foreground.alpha + backdrop[i]! * (1 - foreground.alpha));
      return { selector, ratio: ratio(effectiveForeground, backdrop) };
    });
    return {
      ratios,
      negativeEqualColors: ratio([119,119,119],[119,119,119]),
      positiveBlackOnWhite: ratio([0,0,0],[255,255,255])
    };
  });
  expect(measured.negativeEqualColors, 'adversarial low-contrast control').toBeLessThan(4.5);
  expect(measured.positiveBlackOnWhite, 'high-contrast control').toBeGreaterThanOrEqual(21);
  for (const item of measured.ratios) {
    expect(Number.isFinite(item.ratio), 'actual computed color for '+item.selector).toBe(true);
    expect(item.ratio, 'WCAG AA 4.5:1 on '+item.selector).toBeGreaterThanOrEqual(4.5);
  }
}

test.describe('V24 C01-03.M04.05 actual tenant UI responsive and keyboard', () => {
  test.describe.configure({ retries: 0 });

  test('anonymous users cannot access guarded configuration', async ({ page }) => {
    await page.goto(target);
    await expect(page).toHaveURL(/login/);
    await expect(page.getByRole('heading', { name: 'Configuración dinámica' })).toHaveCount(0);
  });

  for (const [username, tenant] of [['c1504_admin_a',1],['c1504_admin_b',2]] as const) {
    test(username+' real A/B at five widths and zoom-stress', async ({ page }) => {
      test.setTimeout(240_000);
      const jwt = await login(page, username);
      const verified = await page.request.get(api+'/tenant-context/'+tenant, {
        headers:{Authorization:'Bearer '+jwt}
      });
      expect(verified.status()).toBe(200);
      await page.evaluate(id => localStorage.setItem('solqaryn_empresa_solicitada_id',String(id)),tenant);
      await page.goto(target);
      await expect(page.getByRole('heading',{name:'Configuración dinámica',exact:true})).toBeVisible();
      await expect(page.getByRole('heading',{name:'Configuración del tenant activo'})).toBeVisible();
      await expect(page.getByText('Empresa #'+tenant,{exact:false})).toBeVisible();
      await expect(page.getByText('Moneda ISO',{exact:true})).toBeVisible();
      await expect(page.getByText('Zona horaria IANA',{exact:true})).toBeVisible();
      await expect(page.getByRole('button',{name:/Recargar/})).toBeVisible();
      await expect(page.getByRole('link',{name:'Ver todos los ajustes'})).toBeVisible();
      await verifyTab(page);
      await verifyWcagContrast(page);
      for (const width of widths) {
        await page.setViewportSize({width,height:900});
        await verifyLayout(page,width,'100pct');
        await verifyHeadingWholeWords(page,width,'100pct');
        await verifyCardHeaderSeparation(page,width,'100pct');
        await page.screenshot({path:'test-results/c0103-m0405-'+username+'-'+width+'-100.png',fullPage:true});
      }
      // CSS zoom is only a 200% reflow stress PROXY, NOT native Chrome browser zoom.
      for (const width of [768,1280,1440]) {
        await page.setViewportSize({width,height:900});
        await page.evaluate(() => {document.documentElement.style.zoom='2';});
        await verifyLayout(page,width,'css-zoom-stress-200');
        await page.screenshot({path:'test-results/c0103-m0405-'+username+'-'+width+'-zoom-proxy.png',fullPage:true});
        await page.evaluate(() => {document.documentElement.style.zoom='';});
      }

      // V24 real text-only 200% resizing (WCAG 1.4.4), NOT native Chrome page zoom.
      for (const width of widths) {
        await page.setViewportSize({width,height:900});
        const before = await page.evaluate(() =>
          Number.parseFloat(getComputedStyle(document.documentElement).fontSize));
        const after = await page.evaluate(() => {
          document.documentElement.style.fontSize = '200%';
          return Number.parseFloat(getComputedStyle(document.documentElement).fontSize);
        });
        try {
          expect(Number.isFinite(before) && Number.isFinite(after), 'real root font size').toBe(true);
          expect(after, 'root text resize applied').toBeGreaterThanOrEqual(before * 1.95);
          await verifyLayout(page,width,'text-only-resize-200');
          await verifyHeadingWholeWords(page,width,'text-only-resize-200');
          await verifyCardHeaderSeparation(page,width,'text-only-resize-200');
          await expect(page.getByRole('button',{name:/Recargar/})).toBeVisible();
          await expect(page.getByRole('link',{name:'Ver todos los ajustes'})).toBeVisible();
          // V24: aserciones adversariales reales a 200% de texto en los cinco viewports.
          // Tab real y contraste calculado se verifican despues del reflow; no es zoom nativo.
          await verifyTab(page);
          await verifyWcagContrast(page);
          await page.screenshot({
            path:'test-results/c0103-m0405-'+username+'-'+width+'-text-resize-200.png',
            fullPage:true
          });
        } finally {
          await page.evaluate(() => {document.documentElement.style.fontSize = '';});
        }
      }
    });
  }

  test('JWT company A cannot select B via forged header or storage',async ({page})=>{
    const jwt=await login(page,'c1504_admin_a');
    const rejected=await page.request.get(api+'/tenant-context/2',{
      headers:{Authorization:'Bearer '+jwt,'X-Empresa-Id':'2'}
    });
    expect([403,404]).toContain(rejected.status());
    await page.evaluate(()=>localStorage.setItem('solqaryn_empresa_solicitada_id','2'));
    await page.goto(target);
    // A stale/forged selection must NEVER expose tenant B. A verified fallback
    // to the already-authorized A is safe; so is fail-closed redirection to login.
    // A visible route heading alone is not evidence of a security violation.
    await expect.poll(async () => {
      if (/\\/login(?:[/?#]|$)/.test(page.url())) return 'blocked-login';
      const ownTenant = page.getByText('Empresa #1', { exact: false });
      if (await ownTenant.count() && await ownTenant.first().isVisible()) return 'verified-tenant-a';
      return 'unverified';
    }, { message: 'forged tenant B must resolve to verified A or a closed login gate', timeout: 20_000 })
      .not.toBe('unverified');
    await expect(page.getByText('Empresa #2', { exact: false })).toHaveCount(0);
    const savedId = await page.evaluate(() => localStorage.getItem('solqaryn_empresa_solicitada_id'));
    expect(savedId, 'forged tenant B must not remain selected after validation').not.toBe('2');
  });

  test('modified signed JWT rejected by real backend',async ({page})=>{
    const jwt=await login(page,'c1504_admin_a');
    const parts=jwt.split('.');
    parts[2]=(parts[2][0]==='A'?'B':'A')+parts[2].slice(1);
    const bad=await page.request.get(api+'/tenant-context/1',{
      headers:{Authorization:'Bearer '+parts.join('.')}
    });
    expect(bad.status()).toBe(401);
  });
});
