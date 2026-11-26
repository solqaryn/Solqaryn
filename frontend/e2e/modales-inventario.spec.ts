import { expect, test } from './tenant-aware-test';
const API = 'http://localhost:5005';
const ok = (data: unknown) => ({ success: true, message: 'OK', errors: [], data });
const base = { id: 41, numero: 'INV-QA-41', fechaCreacion: '2026-11-25T12:00:00Z', detalles: [], observaciones: 'QA' };
const casos = [
  { ruta: 'transferencias', api: 'transferencias-inventario', crear: 'Nueva transferencia', editar: 'Editar transferencia', ver: 'Ver transferencia 41', accion: 'Solicitar', endpoint: 'solicitar', dato: { ...base, estado: 'Borrador', almacenOrigenId: 1, almacenOrigenNombre: 'Origen', almacenDestinoId: 2, almacenDestinoNombre: 'Destino' }, cambiado: { estado: 'Solicitada' }, estado: 'Solicitada' },
  { ruta: 'reservas', api: 'reservas-inventario', crear: 'Nueva reserva', editar: 'Editar reserva', ver: 'Ver reserva', accion: 'Activar', endpoint: 'activar', dato: { ...base, estado: 'Borrador', ventaId: null, fechaExpiracion: null }, cambiado: { estado: 'Activa' }, estado: 'Activa' },
  { ruta: 'conteos', api: 'conteos-inventario', crear: 'Nuevo conteo', editar: 'Editar conteo', ver: 'Ver conteo', accion: 'Iniciar', endpoint: 'iniciar', dato: { ...base, estado: 1, estadoNombre: 'Borrador', tipo: 1, tipoNombre: 'General', almacenId: 1, almacenNombre: 'Origen', esCiego: false, cantidadLineas: 0, cantidadCapturadas: 0, cantidadConDiferencia: 0, diferenciaNeta: 0 }, cambiado: { estado: 2, estadoNombre: 'En proceso' }, estado: 'En proceso' }
];
for (const caso of casos) for (const viewport of [{width:1280,height:900},{width:390,height:844}]) {
  test('QA visual: inventario ' + caso.ruta + ' ' + viewport.width, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.addInitScript(() => {
      localStorage.setItem('solqaryn_token','e2e-modal-inventario');
      localStorage.setItem('solqaryn_user','qa_visual');
      localStorage.setItem('solqaryn_nombre_completo','QA Visual');
      localStorage.setItem('solqaryn_rol','Administrador');
      localStorage.setItem('solqaryn_expira_en','2099-12-31T23:59:59Z');
      localStorage.setItem('solqaryn_empresa_solicitada_id','1');
    });
    let registro = { ...caso.dato };
    let escrituras = 0;
    let puedeMutar = true;
    await page.route(API+'/permisos/mis-permisos/empresa/*', r=>r.fulfill({json:ok(puedeMutar ? {esAdministrador:true,permisos:[]} : {esAdministrador:false,permisos:['MovimientosInventario:Ver']})}));
    await page.route(API+'/almacenes/activos',r=>r.fulfill({json:ok([{id:1,codigo:'ALM-1',nombre:'Origen',sucursalNombre:'QA',activo:true},{id:2,codigo:'ALM-2',nombre:'Destino',sucursalNombre:'QA',activo:true}])}));
    await page.route(API+'/categorias/activas',r=>r.fulfill({json:ok([])}));
    await page.route(API+'/ubicaciones-almacen/activas**',r=>r.fulfill({json:ok([])}));
    await page.route(url=>url.origin===API&&url.pathname==='/productos',r=>r.fulfill({json:ok({items:[],totalCount:0,totalPages:1})}));
    await page.route(url=>url.origin===API&&url.pathname==='/existencias-variante',r=>r.fulfill({json:ok({items:[],totalCount:0,totalPages:1})}));
    await page.route(url=>url.origin===API&&url.pathname==='/'+caso.api,r=>{ if(r.request().method()!=='GET') escrituras++; return r.fulfill({json:ok({items:[registro],totalCount:1,page:1,pageSize:20,totalPages:1})}); });
    await page.route(API+'/'+caso.api+'/41',r=>{ if(r.request().method()!=='GET') escrituras++; return r.fulfill({json:ok(registro)}); });
    await page.route(API+'/'+caso.api+'/41/'+caso.endpoint,r=>{
      escrituras++;
      registro={...registro,...caso.cambiado} as typeof registro;
      return r.fulfill({json:ok(registro)});
    });
    await page.goto('/inventario/'+caso.ruta);
    const tabla=page.locator('table.app-data-table');
    await expect(tabla).toBeVisible();
    expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1)).toBe(true);
    await page.getByRole('button',{name:caso.crear,exact:true}).click();
    const modal=page.locator('.solqaryn-modal--xl');
    await expect(modal.getByRole('heading',{name:caso.crear,exact:true})).toBeVisible();
    await modal.getByRole('button',{name:'Cancelar',exact:true}).click();
    await expect(modal).toHaveCount(0);
    await tabla.getByRole('button',{name:caso.editar,exact:true}).click();
    await expect(modal.getByRole('heading',{name:caso.ruta==='conteos'?'Editar borrador':caso.editar,exact:true})).toBeVisible();
    await modal.getByRole('button',{name:'Cancelar',exact:true}).click();
    await expect(modal).toHaveCount(0);
    await tabla.getByRole('button',{name:caso.ver,exact:true}).click();
    await expect(modal.getByRole('heading',{name:base.numero,exact:true})).toBeVisible();
    await modal.getByRole('button',{name:caso.accion,exact:true}).click();
    const confirmacion=page.locator('.solqaryn-modal--s');
    await expect(confirmacion).toBeVisible();
    await confirmacion.getByRole('button',{name:'Cancelar',exact:true}).click();
    await expect(confirmacion).toHaveCount(0);
    expect(escrituras).toBe(0);
    const accion = modal.getByRole('button',{name:caso.accion,exact:true});
    await expect(accion).toBeEnabled();
    await accion.click();
    await expect(confirmacion).toBeVisible();
    await confirmacion.getByRole('button',{name:caso.accion,exact:true}).click();
    await expect(modal.getByText(caso.estado,{exact:true})).toBeVisible();
    expect(escrituras).toBe(1);
    await modal.getByRole('button',{name:'Cerrar detalle',exact:true}).click();
    await expect(tabla.getByText(caso.estado,{exact:true})).toBeVisible();
    expect(new URL(page.url()).pathname).toBe('/inventario/'+caso.ruta);
    puedeMutar=false;
    await page.reload();
    await expect(tabla.getByRole('button',{name:caso.ver,exact:true})).toBeVisible();
    await expect(page.getByRole('button',{name:caso.crear,exact:true})).toHaveCount(0);
    expect(escrituras).toBe(1);
  });
}
