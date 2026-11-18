import { expect, test } from '@playwright/test';

/** C14-01.M04.05: Chromium real sin mocks HTTP ni JWT/tenant simulados. */
test.describe('C14-01.M04.05 - responsive, teclado, foco, labels y contraste', () => {
  test.describe.configure({ retries: 0 });

  test('320, 375, 768 y 1280: login sin overflow, labels y envio bloqueado', async ({ page }) => {
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto('/login');
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
      await expect(page.getByLabel('Nombre de usuario')).toBeVisible();
      await expect(page.getByLabel('Contraseña', { exact: true })).toBeVisible();
      await expect(page.getByRole('button', { name: 'Ingresar al sistema' })).toBeDisabled();
      const extra = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(extra, 'horizontal overflow ' + width).toBeLessThanOrEqual(2);
    }
  });

  test('Tab real desde inicio, foco visible, 44px, contraseña reversible y cero POST', async ({ page }) => {
    const writes: string[] = [];
    page.on('request', r => { if (['POST','PATCH','PUT','DELETE'].includes(r.method())) writes.push(r.method()); });
    await page.goto('/login');
    const user = page.getByLabel('Nombre de usuario');
    const pass = page.getByLabel('Contraseña', { exact: true });
    const show = page.getByRole('button', { name: 'Mostrar contraseña' });
    let found = false;
    for (let n=0; n<50; n++) {
      await page.keyboard.press('Tab');
      if (await user.evaluate(e => document.activeElement === e)) { found=true; break; }
    }
    expect(found, 'reachable by Tab').toBe(true);
    await expect(user).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(pass).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(show).toBeFocused();
    expect(await show.evaluate(e => e.matches(':focus-visible'))).toBe(true);
    const rect = await show.boundingBox();
    expect(rect?.width ?? 0).toBeGreaterThanOrEqual(44);
    expect(rect?.height ?? 0).toBeGreaterThanOrEqual(44);
    await page.keyboard.press('Space');
    await expect(pass).toHaveAttribute('type', 'text');
    await expect(page.getByRole('button', { name: 'Ocultar contraseña' })).toHaveAttribute('aria-pressed', 'true');
    // Angular Material puede recolocar el foco tras cambiar el tipo del input.
    // Recuperarlo solo con Tab real (nunca .focus()) antes de exigir reversibilidad.
    const hide = page.getByRole('button', { name: 'Ocultar contraseña' });
    let reachedHideByKeyboard = false;
    for (let n = 0; n < 50; n++) {
      if (await hide.evaluate(e => document.activeElement === e)) { reachedHideByKeyboard = true; break; }
      await page.keyboard.press('Tab');
    }
    expect(reachedHideByKeyboard, 'ocultar alcanzable de nuevo con Tab').toBe(true);
    await expect(hide).toBeFocused();
    await page.keyboard.press('Space');
    await expect(pass).toHaveAttribute('type', 'password');
    await expect(page.getByRole('button', { name: 'Mostrar contraseña' })).toHaveAttribute('aria-pressed', 'false');
    await expect(page.getByRole('button', { name: 'Ingresar al sistema' })).toBeDisabled();
    expect(writes).toEqual([]);
  });

  test('titulo de login cumple AA contraste conservador incluso con alpha', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    const ratio = await page.evaluate(() => {
      type Color = {r:number;g:number;b:number;a:number};
      const parse = (v:string):Color => {
        const m = /^rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)(?:\s*[,/]\s*([\d.]+))?\s*\)$/.exec(v);
        if (m) return {r:+m[1],g:+m[2],b:+m[3],a:m[4] ? +m[4] : 1};
        const s = /^color\(srgb\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?\s*\)$/.exec(v);
        if (s) return {r:+s[1]*255,g:+s[2]*255,b:+s[3]*255,a:s[4] ? +s[4] : 1};
        throw new Error('CSS_COLOR_UNSUPPORTED '+v);
      };
      const h = document.querySelector<HTMLElement>('.login-card .header h2');
      const card = document.querySelector<HTMLElement>('.login-card');
      if (!h || !card) throw new Error('LOGIN_SURFACE_MISSING');
      const fg=parse(getComputedStyle(h).color),bg=parse(getComputedStyle(card).backgroundColor);
      if(fg.a!==1) throw new Error('FOREGROUND_ALPHA_REQUIRES_PIXEL_AUDIT');
      const lum=(c:{r:number;g:number;b:number})=>{
        const f=(n:number)=>{const x=n/255;return x<=.04045?x/12.92:((x+.055)/1.055)**2.4;};
        return .2126*f(c.r)+.7152*f(c.g)+.0722*f(c.b);
      };
      const ratioOf=(a:{r:number;g:number;b:number},b:{r:number;g:number;b:number})=>{
        const x=lum(a),y=lum(b);return (Math.max(x,y)+.05)/(Math.min(x,y)+.05);
      };
      const composite=(under:number)=>({r:bg.r*bg.a+under*(1-bg.a),g:bg.g*bg.a+under*(1-bg.a),b:bg.b*bg.a+under*(1-bg.a)});
      return Math.min(ratioOf(fg,composite(0)),ratioOf(fg,composite(255)));
    });
    expect(ratio, 'WCAG AA >=4.5 under any backdrop').toBeGreaterThanOrEqual(4.5);
  });

  test('anonimo: QA privado redirige a login sin revelar panel', async ({ page }) => {
    await page.goto('/plataforma/qa-global-v19');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByTestId('c1401-qa-global')).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(page.getByLabel('Nombre de usuario')).toBeVisible();
    await expect(page.getByLabel('Contraseña', { exact: true })).toBeVisible();
  });
});
