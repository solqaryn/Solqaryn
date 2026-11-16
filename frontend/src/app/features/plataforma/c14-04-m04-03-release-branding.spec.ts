import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import {
  C1404ReleaseBrandingV17Component, normalizarC1404ReleasePresentacion
} from './c14-04-m04-03-release-branding.component';

describe('C14-04.M04.03 branding privado fail-closed', () => {
  const grant: PlataformaSesion = {
    nombreUsuario:'Revisor',rolNombre:'Admin',ambito:'Plataforma',esAdministrador:true
  };
  async function montar() {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(grant);
    await TestBed.configureTestingModule({
      imports:[C1404ReleaseBrandingV17Component],
      providers:[{provide:PlataformaSesionService,useValue:{sesion:sesion.asReadonly()}}]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1404ReleaseBrandingV17Component);
    fixture.detectChanges();
    return {fixture,sesion};
  }
  it('acepta tres presets exactos sin certificarlos como operaciones', () => {
    expect(normalizarC1404ReleasePresentacion({
      marca:'SOLQARYN DEV',densidad:'compacta',contraste:'alto'
    })).toEqual({marca:'SOLQARYN DEV',densidad:'compacta',contraste:'alto'});
    expect(normalizarC1404ReleasePresentacion(null))
      .toEqual({marca:'SOLQARYN',densidad:'comoda',contraste:'normal'});
  });
  it('rechaza HTML, CSS, URLs, arrays y campos nuevos por defecto', () => {
    expect(normalizarC1404ReleasePresentacion({
      marca:'<script>alert(1)</script>',densidad:'url(https://invalid.example)',
      contraste:'var(--secret)',href:'javascript:alert(1)',css:'position:fixed'
    })).toEqual(normalizarC1404ReleasePresentacion(null));
    expect(normalizarC1404ReleasePresentacion(['alto']))
      .toEqual(normalizarC1404ReleasePresentacion(null));
  });
  it('muestra solo dos controles cosméticos a admin vigente', async () => {
    const {fixture} = await montar();
    expect(fixture.nativeElement.querySelectorAll('.release-branding-v17')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('button[type="button"]')).toHaveLength(2);
    expect(fixture.nativeElement.querySelectorAll('form,input,textarea,a')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });
  it('Signals alternan densidad y contraste en forma estricta', async () => {
    const {fixture} = await montar();
    fixture.componentInstance.alternarDensidad();
    fixture.componentInstance.alternarContraste();
    fixture.detectChanges();
    const p: HTMLElement = fixture.nativeElement.querySelector('.release-branding-v17');
    expect(p.getAttribute('data-density')).toBe('compacta');
    expect(p.getAttribute('data-contrast')).toBe('alto');
  });
  it('sesión ausente niega vista y cualquier modificación', async () => {
    const {fixture,sesion} = await montar();
    sesion.set(null); fixture.detectChanges();
    fixture.componentInstance.alternarDensidad();
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(0);
    expect(fixture.componentInstance.presentacion().densidad).toBe('comoda');
  });
  it('revocación posterior oculta controles y bloquea mutación', async () => {
    const {fixture,sesion} = await montar();
    fixture.componentInstance.alternarContraste();
    expect(fixture.componentInstance.presentacion().contraste).toBe('alto');
    sesion.set(null); fixture.detectChanges();
    fixture.componentInstance.alternarDensidad();
    expect(fixture.componentInstance.presentacion().densidad).toBe('comoda');
    expect(fixture.nativeElement.querySelector('.release-branding-v17')).toBeNull();
  });
});
