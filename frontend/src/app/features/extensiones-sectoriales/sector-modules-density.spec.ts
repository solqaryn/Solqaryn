import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SectorModulesReadonlyComponent } from './sector-modules-readonly.component';
describe('C08-04.M04.06 estados progresivos sin permiso operativo', () => {
 async function mount() {
  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({imports:[SectorModulesReadonlyComponent],providers:[provideRouter([])]}).compileComponents();
  const fixture=TestBed.createComponent(SectorModulesReadonlyComponent); fixture.detectChanges(); return fixture;
 }
 it('inicio veraz vacio y cuatro tarjetas sin recursos pesados ni controles',async()=>{
  const f=await mount(),root:HTMLElement=f.nativeElement;
  expect(f.componentInstance.estadoPresentacion()).toBe('vacio');
  expect(root.querySelector('[aria-label="Estado de las capacidades"]')?.getAttribute('aria-busy')).toBe('false');
  expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
  expect(root.querySelectorAll('img,video,iframe,form,input,button')).toHaveLength(0);
 });
 it('cargando quita tarjetas y aria-busy true; vacio restaura 4',async()=>{
  const f=await mount(),root:HTMLElement=f.nativeElement;
  f.componentInstance.estadoPresentacion.set('cargando');f.detectChanges();
  expect(root.querySelector('[aria-label="Estado de las capacidades"]')?.getAttribute('aria-busy')).toBe('true');
  expect(root.textContent).toContain('Preparando la vista informativa');
  expect(root.querySelectorAll('.sector-card')).toHaveLength(0);
  f.componentInstance.estadoPresentacion.set('vacio');f.detectChanges();
  expect(root.querySelector('[aria-label="Estado de las capacidades"]')?.getAttribute('aria-busy')).toBe('false');
  expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
 });
 it('exito no concede permiso; error y rechazo ocultan estado anterior',async()=>{
  const f=await mount(),root:HTMLElement=f.nativeElement;
  f.componentInstance.estadoPresentacion.set('exito');f.detectChanges();
  expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
  for(const state of ['sin_permiso','error'] as const){
   f.componentInstance.estadoPresentacion.set(state);f.detectChanges();
   expect(root.querySelectorAll('.sector-card')).toHaveLength(0);
   expect(root.querySelectorAll('form,input,button')).toHaveLength(0);
   expect(root.textContent).not.toContain('SECRET_SYNTHETIC_DSN');
  }
 });
});