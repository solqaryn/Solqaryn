import { TestBed } from '@angular/core/testing';
import { UsuariosEstadoListadoComponent } from './usuarios-estado-listado.component';

describe('C01-01.M04.02 estados de Usuarios', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [UsuariosEstadoListadoComponent] }).compileComponents();
  });
  function mostrar(cargando: boolean, tieneError: boolean, cantidad: number, puedeCrear: boolean) {
    const fixture = TestBed.createComponent(UsuariosEstadoListadoComponent);
    fixture.componentRef.setInput('cargando', cargando);
    fixture.componentRef.setInput('tieneError', tieneError);
    fixture.componentRef.setInput('cantidad', cantidad);
    fixture.componentRef.setInput('puedeCrear', puedeCrear);
    fixture.detectChanges();
    return fixture;
  }
  it('prioriza carga frente a datos antiguos y fallos', () => {
    const f = mostrar(true, true, 5, true);
    expect(f.nativeElement.textContent).toContain('Cargando usuarios');
    expect(f.nativeElement.textContent).not.toContain('5 usuarios visibles');
    expect(f.nativeElement.querySelector('[role="status"]')).not.toBeNull();
  });
  it('presenta vacio sin confundir error', () => {
    const f = mostrar(false, false, 0, true);
    expect(f.nativeElement.textContent).toContain('No hay usuarios para mostrar');
    expect(f.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });
  it('presenta error y no certifica vacio', () => {
    const f = mostrar(false, true, 0, true);
    expect(f.nativeElement.textContent).toContain('No se pudo actualizar');
    expect(f.nativeElement.textContent).not.toContain('No hay usuarios para mostrar');
    expect(f.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });
  it('cuenta solamente los usuarios visibles de esta pagina', () => {
    const f = mostrar(false, false, 3, true);
    expect(f.nativeElement.textContent).toContain('3 usuarios visibles en esta página');
    expect(f.nativeElement.textContent).not.toContain('No se pudo actualizar');
  });
  it('muestra solo lectura cuando no puede crear', () => {
    const f = mostrar(false, false, 2, false);
    expect(f.nativeElement.textContent).toContain('No tienes permiso para crear usuarios');
    expect(f.nativeElement.textContent).toContain('2 usuarios visibles');
    expect(f.nativeElement.querySelector('button')).toBeNull();
  });
  it('revoca el estado anterior al terminar la carga', () => {
    const f = mostrar(true, false, 0, true);
    f.componentRef.setInput('cargando', false);
    f.componentRef.setInput('cantidad', 1);
    f.detectChanges();
    expect(f.nativeElement.textContent).toContain('1 usuario visible');
    expect(f.nativeElement.textContent).not.toContain('Cargando usuarios');
  });
});
