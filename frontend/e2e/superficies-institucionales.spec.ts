import { expect, test } from './tenant-aware-test';
import type { Page } from '@playwright/test';
const API = 'http://localhost:5005';
const ok = (data: unknown) => ({ success: true, message: 'OK', errors: [], data });
const vacio = { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };

async function contexto(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'e2e-superficies');
    localStorage.setItem('solqaryn_user', 'qa_visual');
    localStorage.setItem('solqaryn_nombre_completo', 'QA Visual');
    localStorage.setItem('solqaryn_rol', 'Administrador');
    localStorage.setItem('solqaryn_expira_en', '2099-12-31T23:59:59Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
  });
  await page.route(API+'/permisos/mis-permisos/empresa/*', r => r.fulfill({json:ok({esAdministrador:true,permisos:[]})}));
  await page.route(url => url.origin === API && ['/categorias','/marcas','/modelos','/colores','/tallas','/almacenes/activos','/proveedores/activos','/cuentas-por-cobrar'].includes(url.pathname), r=>r.fulfill({json:ok([])}));
  await page.route(url => url.origin === API && ['/productos','/transferencias-inventario','/reservas-inventario','/conteos-inventario','/inventario/ajustes','/existencias-variante','/ordenes-compra','/recepciones-compra','/periodos-contables'].includes(url.pathname), r=>r.fulfill({json:ok(vacio)}));
}

// Anchos CSS aprobados para móvil, tableta, escritorio y pantallas amplias.
const viewports = [
  {width:320,height:568}, {width:360,height:800}, {width:390,height:844},
  {width:414,height:896}, {width:480,height:854}, {width:768,height:1024},
  {width:1024,height:768}, {width:1280,height:800}, {width:1440,height:900},
  {width:1920,height:1080}, {width:2048,height:1152}, {width:2400,height:1350},
  {width:2560,height:1440}
] as const;

for (const viewport of viewports) {
  test('QA visual: filtros de productos compactos ' + viewport.width, async ({page},testInfo) => {
    await page.setViewportSize(viewport);
    await contexto(page);
    await page.goto('/productos');
    const toolbar=page.locator('.products-card .app-table-filters');
    await expect(toolbar.locator('mat-form-field')).toHaveCount(7);
    await expect(toolbar.getByRole('combobox',{name:'Marca',exact:true})).toBeEnabled();
    await expect(toolbar.getByRole('combobox',{name:'Modelo',exact:true})).toBeDisabled();
    const geometry=await toolbar.evaluate(el => {
      const bounds=el.getBoundingClientRect();
      const fields=[...el.querySelectorAll('.mat-mdc-text-field-wrapper')].map(f=>f.getBoundingClientRect());
      return {
        contained: fields.every(f=>f.left>=bounds.left-1 && f.right<=bounds.right+1),
        compact: fields.every(f=>f.height>=32 && f.height<=72),
        sharedRow: fields.some((f,i)=>fields.slice(i+1).some(other=>Math.abs(other.top-f.top)<2 && other.left>f.left)),
        overflow: document.documentElement.scrollWidth>window.innerWidth+1,
      };
    });
    expect(geometry.contained).toBe(true);
    expect(geometry.compact).toBe(true);
    expect(geometry.overflow).toBe(false);
    if(viewport.width>=1000) expect(geometry.sharedRow).toBe(true);
    await toolbar.getByRole('combobox',{name:'Estado',exact:true}).click();
    await page.getByRole('option',{name:'Agotados',exact:true}).click();
    await expect(toolbar.getByRole('combobox',{name:'Estado',exact:true})).toContainText('Agotados');
    await toolbar.getByRole('button',{name:'Limpiar filtros'}).click();
    await expect(toolbar.getByRole('combobox',{name:'Estado',exact:true})).toContainText('Todos');
    await page.screenshot({path:testInfo.outputPath('productos-filtros.png'),fullPage:true});
  });
  test('QA visual: tarjetas de inventario vacias ' + viewport.width, async ({page},testInfo) => {
    await page.setViewportSize(viewport);
    await contexto(page);
    for(const [ruta,texto] of [['transferencias','transferencias'],['reservas','reservas'],['conteos','conteos'],['ajustes','ajustes']]) {
      await page.goto('/inventario/'+ruta);
      const surface=page.locator('.card.app-list-surface');
      await expect(surface.getByText('No se encontraron '+texto,{exact:true})).toBeVisible();
      await expect(surface.locator('.app-table-filters')).toBeVisible();
      const geometry=await surface.evaluate(el=>{
        const css=getComputedStyle(el), rect=el.getBoundingClientRect();
        const empty=el.querySelector('.app-list-empty')!.getBoundingClientRect();
        const canvas=getComputedStyle(document.documentElement).getPropertyValue('--color-bg').trim();
        const probe=document.createElement('span');probe.style.backgroundColor=canvas;document.body.append(probe);
        const pageColor=getComputedStyle(probe).backgroundColor;probe.remove();
        return {different:css.backgroundColor!==pageColor, border:parseFloat(css.borderTopWidth)>=1 && css.borderTopStyle!=='none', contained:empty.left>=rect.left && empty.right<=rect.right+1, overflow:document.documentElement.scrollWidth>window.innerWidth+1};
      });
      expect(geometry.different).toBe(true);
      expect(geometry.border).toBe(true);
      expect(geometry.contained).toBe(true);
      expect(geometry.overflow).toBe(false);
      await page.screenshot({path:testInfo.outputPath(ruta+'.png'),fullPage:true});
    }
  });
}

for (const viewport of viewports) {
  test('QA visual: tarjetas de compras y contabilidad ' + viewport.width, async ({page},testInfo) => {
    await page.setViewportSize(viewport);
    await contexto(page);
    const casos = [
      {ruta:'/ordenes-compra', mensaje:'No se encontraron órdenes', filtros:true},
      {ruta:'/recepciones-compra', mensaje:'No se encontraron recepciones', filtros:true},
      {ruta:'/inventario/existencias', mensaje:'No se encontraron existencias', filtros:true},
      {ruta:'/periodos-contables', mensaje:'No se encontraron períodos', filtros:true},
      {ruta:'/cuentas-por-cobrar', mensaje:'No hay saldos pendientes.', filtros:false}
    ];
    for (const caso of casos) {
      await page.goto(caso.ruta);
      const surface = page.locator('.card.app-list-surface');
      await expect(surface.locator('.app-list-empty').getByText(caso.mensaje,{exact:true})).toBeVisible();
      if(caso.filtros) await expect(surface.locator('.app-table-filters')).toBeVisible();
      const geometry=await surface.evaluate(el => {
        const rect=el.getBoundingClientRect();
        const empty=el.querySelector('.app-list-empty')!.getBoundingClientRect();
        const css=getComputedStyle(el);
        const probe=document.createElement('span');
        probe.style.backgroundColor=getComputedStyle(document.documentElement).getPropertyValue('--color-bg').trim();
        document.body.append(probe);const pageColor=getComputedStyle(probe).backgroundColor;probe.remove();
        return {distinct:css.backgroundColor!==pageColor, border:parseFloat(css.borderTopWidth)>=1 && css.borderTopStyle!=='none', contained:empty.left>=rect.left-1 && empty.right<=rect.right+1, overflow:document.documentElement.scrollWidth>window.innerWidth+1};
      });
      expect(geometry.distinct).toBe(true);
      expect(geometry.border).toBe(true);
      expect(geometry.contained).toBe(true);
      expect(geometry.overflow).toBe(false);
      await page.screenshot({path:testInfo.outputPath(caso.ruta.slice(1).replaceAll('/','-')+'.png'),fullPage:true});
    }
  });
}


for (const viewport of viewports) {
  test('QA visual: costeo comercial, tabla y paginación ' + viewport.width, async ({page},testInfo) => {
    await page.setViewportSize(viewport);
    await contexto(page);
    const original = 'Cutover ERP-N1.10 — Promedio Ponderado compatible';
    const comercial = 'Se adoptó el promedio ponderado como política inicial de valoración de existencias.';
    const motivoEmpresa = 'Cambio autorizado por cierre contable: ' + 'referencia-comercial-'.repeat(8);
    const politica = {
      id:1, empresaConfiguracionId:1, metodo:1, metodoNombre:'Promedio Ponderado',
      vigenteDesdeUtc:'2026-10-02T12:00:00Z', vigenteHastaUtc:null, estaVigente:true,
      motivo:original, fechaCreacion:'2026-10-02T12:00:00Z', fechaActualizacion:'2026-10-02T12:00:00Z'
    };
    const historica = {...politica, id:2, estaVigente:false, vigenteHastaUtc:'2026-10-02T12:00:00Z', motivo:motivoEmpresa};
    await page.route(API+'/costeo-inventario/politica-vigente', r=>r.fulfill({json:ok(politica)}));
    await page.route(API+'/costeo-inventario/metodos', r=>r.fulfill({json:ok([{id:1,nombre:'Promedio Ponderado'},{id:2,nombre:'FIFO'},{id:3,nombre:'Estándar'}])}));
    await page.route(url=>url.origin===API && url.pathname==='/costeo-inventario/politicas', r=>r.fulfill({json:ok({items:[politica,historica],totalCount:2,page:1,pageSize:20,totalPages:1})}));
    await page.goto('/inventario/costeo');
    await expect(page.getByRole('heading',{name:'Política de costeo',exact:true})).toBeVisible();
    await expect(page.locator('.summary-grid').getByText(comercial,{exact:true})).toBeVisible();
    const tabla=page.getByRole('region',{name:'Historial de políticas de costeo'});
    await expect(tabla.getByText(comercial,{exact:true})).toBeVisible();
    await expect(tabla.getByText(motivoEmpresa,{exact:true})).toBeVisible();
    await expect(page.getByText(original,{exact:true})).toHaveCount(0);
    const paginador=page.getByRole('group',{name:'Paginación del historial de costeo'});
    await expect(paginador.getByText('Registros por página',{exact:true})).toBeVisible();
    await expect(paginador.getByText('1–2 de 2',{exact:true})).toBeVisible();
    const geometria=await tabla.evaluate(el=>({
      clientWidth:el.clientWidth, scrollWidth:el.scrollWidth,
      overflow:document.documentElement.scrollWidth>window.innerWidth+1,
      overflowX:getComputedStyle(el).overflowX,
      contained:el.getBoundingClientRect().right<=window.innerWidth+1
    }));
    expect(geometria.overflow).toBe(false);
    expect(geometria.contained).toBe(true);
    expect(geometria.overflowX).toBe('auto');
    if(geometria.clientWidth<818) expect(geometria.scrollWidth).toBeGreaterThan(geometria.clientWidth);
    if(viewport.width===390 || viewport.width===1440) {
      await tabla.getByRole('button',{name:'Ver política de costeo 1',exact:true}).click();
      const detalle=page.getByRole('dialog');
      await expect(detalle.getByText(comercial,{exact:true})).toBeVisible();
      await expect(detalle.getByText(original,{exact:true})).toHaveCount(0);
      await detalle.getByRole('button',{name:'Cerrar',exact:true}).click();
      await expect(detalle).toHaveCount(0);
      const form = page.locator('.change-form');
      const metodo = form.getByRole('combobox',{name:'Método',exact:true});
      const aplicar = form.getByRole('button',{name:'Aplicar política'});
      await expect(aplicar).toBeDisabled();
      const seleccionarMetodo = async (nombre: string) => {
        await expect(metodo).toHaveAttribute('aria-expanded','false');
        await expect(page.getByRole('listbox')).toHaveCount(0);
        await metodo.click();
        await expect(metodo).toHaveAttribute('aria-expanded','true');
        const panel = page.getByRole('listbox');
        await expect(panel).toBeVisible();
        await panel.getByRole('option',{name:nombre,exact:true}).click();
        await expect(metodo).toHaveAttribute('aria-expanded','false');
        await expect(panel).toHaveCount(0);
      };
      await seleccionarMetodo('FIFO');
      await expect(form.getByText(/asigna a las salidas el costo de las entradas más antiguas/)).toBeVisible();
      await expect(form.getByText(/no modifica las políticas anteriores ni recalcula costos ya registrados/)).toBeVisible();
      await expect(aplicar).toBeDisabled();
      await form.getByRole('textbox',{name:'Motivo del cambio'}).fill('Validación visual sin guardar');
      await expect(aplicar).toBeEnabled();
      await seleccionarMetodo('Estándar');
      await expect(form.getByText(/utiliza un costo de referencia establecido/)).toBeVisible();
      await seleccionarMetodo('Promedio Ponderado');
      await expect(form.locator('.method-description')).toHaveCount(0);
      await expect(aplicar).toBeDisabled();
    }
    await page.screenshot({path:testInfo.outputPath('costeo.png'),fullPage:true});
  });
}

// Afirmaciones exactas de tokens y superficies, no sólo "distinto del fondo".
for (const width of [390, 1440]) {
  test('QA visual: tema de empresa en tarjetas y barra superior ' + width, async ({page}) => {
    await page.setViewportSize({width,height:900});
    await contexto(page);
    const tema = {
      colorPrimario:'#245b49',colorSecundario:'#204a3a',colorAcento:'#996b22',
      fondoPrincipal:'#f4eee4',fondoTarjetas:'#e7f0e8',menuLateral:'#182c25',
      barraSuperior:'#14251e',encabezados:'#182c25',botonesPrincipales:'#245b49',
      textoPrincipal:'#182c25',textoSecundario:'#354c40',colorExito:'#166534',
      colorAdvertencia:'#a16207',colorError:'#b91c1c',colorInformacion:'#1d4ed8'
    };
    await page.route(API+'/tema-visual', r=>r.fulfill({json:ok(tema)}));
    for (const ruta of ['/ordenes-compra','/inventario/existencias','/periodos-contables','/cuentas-por-cobrar']) {
      await page.goto(ruta);
      const surface = page.locator('.card.app-list-surface');
      await expect(surface.locator('.app-list-empty')).toBeVisible();
      await expect(surface).toHaveCSS('background-color','rgb(231, 240, 232)');
      await expect(page.locator('body')).toHaveCSS('background-color','rgb(244, 238, 228)');
      await expect(page.locator('.topbar')).toHaveCSS('background-color','rgb(20, 37, 30)');
      await expect(page.locator('.topbar .user-name')).toHaveCSS('color','rgb(255, 255, 255)');
      await expect(page.locator('.topbar .user-role')).toHaveCSS('background-color','rgb(36, 91, 73)');
      if(width===390) await expect(page.locator('#menu-toggle')).toHaveCSS('color','rgb(255, 255, 255)');
    }
    // La última ruta usa un listado vacío; recargar con una factura sintética.
    await page.route(API+'/cuentas-por-cobrar', r=>r.fulfill({json:ok([{
      id:1, ventaId:2, numeroFactura:'FAC-QA-001', numeroVentaOrigen:'VEN-QA-002',
      clienteNombre:'Cliente QA', estado:'Emitida', estadoPago:'Pendiente',
      moneda:'HNL', total:100, totalPagado:25, saldoPendiente:75,
      fechaVencimiento:'2026-10-01T00:00:00Z'
    }])}));
    await page.goto('/cuentas-por-cobrar');
    const pago = page.getByRole('link',{name:'Pagos de factura FAC-QA-001'});
    await expect(pago).toHaveCSS('background-color','rgb(36, 91, 73)');
    await expect(pago).toHaveCSS('color','rgb(255, 255, 255)');
    await expect(page.getByRole('region',{name:'Tabla de cuentas por cobrar'})).toHaveCSS('overflow-x','auto');
    expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1)).toBe(true);
  });
}
