import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { C0904MensajeriaPrivadaComponent, CANALES_DECLARADOS_C0904 } from './c09-04-m04-01-mensajeria-privada.component';

describe('C09-04.M04.01 superficie multicanal privada R2', () => {
  const admin: PlataformaSesion = {
    nombreUsuario:'Operador', rolNombre:'Administrador', ambito:'Plataforma', esAdministrador:true
  };
  async function montar(estado: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(estado);
    await TestBed.configureTestingModule({
      imports:[C0904MensajeriaPrivadaComponent],
      providers:[provideRouter([]),{provide:PlataformaSesionService,useValue:{sesion:sesion.asReadonly()}}]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0904MensajeriaPrivadaComponent);
    fixture.detectChanges();
    return {fixture,sesion,root:fixture.nativeElement as HTMLElement};
  }
  it('ruta exacta única lazy y plataformaGuard, sin acceso ERP empresarial', async () => {
    const r = routes.filter(r=>r.path==='plataforma/mensajeria-multicanal-v19');
    expect(r).toHaveLength(1);
    expect(r[0].canActivate).toEqual([plataformaGuard]);
    expect(await r[0].loadComponent?.()).toBe(C0904MensajeriaPrivadaComponent);
  });
  it('admin vigente ve tres estados literales no conectados y ninguna acción de envío', async () => {
    const {root}=await montar();
    expect(root.querySelectorAll('[data-testid=c0904-canales-privados] li')).toHaveLength(3);
    expect(root.textContent).toContain('NO OPERATIVO');
    expect(root.textContent).toContain('NO_CONECTADO');
    expect(root.querySelectorAll('form,input,button,textarea')).toHaveLength(0);
    expect([...root.querySelectorAll('a')].every(a=>a.getAttribute('href')==='/plataforma')).toBe(true);
  });
  it('sin sesión no muestra catálogo privado', async () => {
    const {root}=await montar(null);
    expect(root.querySelector('[data-testid=c0904-canales-privados]')).toBeNull();
    expect(root.querySelector('[role=alert]')).not.toBeNull();
  });
  it('tenant empresarial y no-admin jamás reciben panel', async () => {
    for (const cambio of [{ambito:'Empresa'},{esAdministrador:false}]) {
      const {root}=await montar({...admin,...cambio} as unknown as PlataformaSesion);
      expect(root.querySelector('[data-testid=c0904-canales-privados]')).toBeNull();
    }
  });
  it('revocación reactiva oculta catálogo sin permisos residuales', async () => {
    const {fixture,sesion,root}=await montar();
    sesion.set(null); fixture.detectChanges();
    expect(root.querySelector('[data-testid=c0904-canales-privados]')).toBeNull();
    expect(root.querySelectorAll('form,input,button,textarea')).toHaveLength(0);
  });
  it('contrato cerrado no acepta canales, destinatarios o URLs ajenos', () => {
    expect(CANALES_DECLARADOS_C0904.map(c=>c.id)).toEqual(['email','whatsapp','otros']);
    expect(CANALES_DECLARADOS_C0904.every(c=>c.estado==='NO_CONECTADO')).toBe(true);
    expect(Object.isFrozen(CANALES_DECLARADOS_C0904)).toBe(true);
    expect(JSON.stringify(CANALES_DECLARADOS_C0904)).not.toContain('https://');
  });
});
