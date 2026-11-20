import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { C0404PaymentLinkPreviewComponent } from './c04-04-m04-01-payment-link-preview.component';

describe('C04-04.M04.01 - ruta desconectada de enlaces de cobro', () => {
  const admin: PlataformaSesion = { nombreUsuario:'QA',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true };
  async function montar(initial: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(initial);
    await TestBed.configureTestingModule({
      imports: [C0404PaymentLinkPreviewComponent],
      providers: [provideRouter([]), {provide:PlataformaSesionService,useValue:{sesion}}]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0404PaymentLinkPreviewComponent);
    fixture.detectChanges();
    return {fixture,sesion,root:fixture.nativeElement as HTMLElement};
  }
  it('exige una sola ruta lazy con plataformaGuard', async () => {
    const matching = routes.filter(r=>r.path==='plataforma/enlaces-cobro-simulacion-v22');
    expect(matching).toHaveLength(1);
    expect(matching[0].canActivate).toEqual([plataformaGuard]);
    expect(await matching[0].loadComponent?.()).toBe(C0404PaymentLinkPreviewComponent);
  });
  it('muestra 3 etapas no autoritativas, sin acciones financieras', async () => {
    const {root} = await montar();
    expect(root.querySelectorAll('[data-testid^="c0404-step-"]')).toHaveLength(3);
    expect(root.textContent).toContain('SIN_CONEXION');
    expect(root.textContent).toContain('no contacta proveedores');
    expect(root.querySelectorAll('form,input,button,textarea,select,iframe')).toHaveLength(0);
  });
  it('falla cerrado para nulo, ambito Empresa y revocacion', async () => {
    const {root,fixture,sesion}=await montar(null);
    expect(root.querySelector('[data-testid="c0404-payment-private"]')).toBeNull();
    sesion.set({...admin,ambito:'Empresa'} as PlataformaSesion);fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')).toBeNull();
    sesion.set(admin);fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid^="c0404-step-"]')).toHaveLength(3);
    sesion.set(null);fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')).toBeNull();
  });
  it('no confia en flag administrador falso ni HTML hostil', async () => {
    const {root}=await montar({...admin,esAdministrador:false,nombreUsuario:'<img src=x onerror=alert(1)>'});
    expect(root.querySelector('[data-testid="c0404-payment-private"]')).toBeNull();
    expect(root.querySelector('img')).toBeNull();
    expect(root.innerHTML).not.toContain('onerror');
  });
});
