import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TenantContextService, TenantContextoVerificado } from '../../core/auth/tenant-context.service';
import { IdentidadSesionVerificadaComponent } from './identidad-sesion-verificada.component';

describe('C01-01.M04.01 IdentidadSesionVerificadaComponent', () => {
  const verificado = signal<TenantContextoVerificado | null>(null);

  beforeEach(() => {
    verificado.set(null);
    TestBed.configureTestingModule({
      imports: [IdentidadSesionVerificadaComponent],
      providers: [{ provide: TenantContextService, useValue: { contextoVerificado: verificado } }]
    });
  });

  it('no presenta empresa solicitada como identidad verificada', () => {
    const fixture = TestBed.createComponent(IdentidadSesionVerificadaComponent);
    fixture.detectChanges();
    const texto: string = fixture.nativeElement.textContent;
    expect(texto).toContain('No hay una empresa confirmada');
    expect(texto).not.toContain('Empresa confirmada:');
  });

  it('muestra solo contexto confirmado y limpia A antes de B', () => {
    const fixture = TestBed.createComponent(IdentidadSesionVerificadaComponent);
    verificado.set({ usuarioId: 7, empresaId: 11, rolId: 3, rolNombre: 'Operador A', esAdministrador: false });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Operador A');
    expect(fixture.nativeElement.textContent).toContain('11');
    verificado.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Operador A');
    verificado.set({ usuarioId: 7, empresaId: 22, rolId: 4, rolNombre: 'Operador B', esAdministrador: false });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Operador B');
    expect(fixture.nativeElement.textContent).not.toContain('Operador A');
  });

  it('escapa rol HTML sin ejecutar marcado arbitrario', () => {
    const fixture = TestBed.createComponent(IdentidadSesionVerificadaComponent);
    verificado.set({ usuarioId: 7, empresaId: 11, rolId: 3,
      rolNombre: '<img src=x onerror=alert(1)>', esAdministrador: false });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('<img src=x');
  });
});
