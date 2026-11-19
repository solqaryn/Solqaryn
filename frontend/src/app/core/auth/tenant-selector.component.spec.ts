import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { TenantSelectorComponent } from './tenant-selector.component';
import { TenantContextService } from './tenant-context.service';

describe('TenantSelectorComponent - P1-006.A', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, TenantSelectorComponent]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('muestra únicamente empresas recibidas desde JWT, sin permitir ingreso de ID arbitrario', () => {
    const fixture = TestBed.createComponent(TenantSelectorComponent);
    fixture.detectChanges();
    http.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true,
      data: [{ empresaId: 7, nombre: 'Empresa permitida' },
        { empresaId: 8, nombre: 'Empresa alternativa' }]
    });
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(fixture.nativeElement.querySelector('input#empresa-id')).toBeNull();
    const selector = fixture.nativeElement.querySelector('select#empresa-id') as HTMLSelectElement;
    expect(selector).not.toBeNull();
    expect(selector.textContent).toContain('Empresa permitida');
    expect(selector.textContent).not.toContain('Empresa ajena');
    component.empresaId = 99;
    expect(component.empresaValida()).toBe(false);

    let verificada = false;
    component.confirmado.subscribe(() => { verificada = true; });
    component.empresaId = 7;
    component.activarEmpresa();
    expect(verificada).toBe(false);
    http.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true,
      data: {
        usuarioId: 3, empresaId: 7, rolId: 4,
        rolNombre: 'Operador', esAdministrador: false
      }
    });
    expect(verificada).toBe(true);
    expect(TestBed.inject(TenantContextService).empresaIdVerificada()).toBe(7);
  });

  it('no revalida automáticamente un ID recordado que ya no pertenece al listado', () => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '999');
    const fixture = TestBed.createComponent(TenantSelectorComponent);
    fixture.detectChanges();
    http.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true,
      data: [{ empresaId: 7, nombre: 'Empresa vigente' },
        { empresaId: 8, nombre: 'Otra empresa' }]
    });
    fixture.detectChanges();
    expect(fixture.componentInstance.empresaId).toBeNull();
    expect(fixture.componentInstance.empresaValida()).toBe(false);
    http.expectNone(`${environment.apiUrl}/tenant-context/999`);
  });

  it('mantiene el gate cerrado si la consulta del listado falla', () => {
    const fixture = TestBed.createComponent(TenantSelectorComponent);
    fixture.detectChanges();
    http.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush(
      {}, { status: 503, statusText: 'Service Unavailable' }
    );
    fixture.detectChanges();
    expect(fixture.componentInstance.empresaValida()).toBe(false);
    expect(fixture.componentInstance.errorLista).toBe(true);
    const boton = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(boton.disabled).toBe(true);
  });

  it('C02-01.M04.02: cero empresas deja gate cerrado y rechazo explicito', () => {
    const fixture = TestBed.createComponent(TenantSelectorComponent);
    fixture.detectChanges();
    http.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: []
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No tienes membresías empresariales activas');
    expect(fixture.componentInstance.empresaValida()).toBe(false);
    expect((fixture.nativeElement.querySelector('select#empresa-id') as HTMLSelectElement).disabled).toBe(true);
    expect((fixture.nativeElement.querySelector('select#empresa-id') as HTMLSelectElement).hasAttribute('disabled')).toBe(true);
    expect((fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
    expect(TestBed.inject(TenantContextService).contextoVerificado()).toBeNull();
  });

  it('C02-01.M04.02: una empresa se verifica silenciosamente solo por servidor', () => {
    const fixture = TestBed.createComponent(TenantSelectorComponent);
    let confirmaciones = 0;
    fixture.componentInstance.confirmado.subscribe(() => { confirmaciones++; });
    fixture.detectChanges();
    http.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [{ empresaId: 7, nombre: 'Única empresa' }]
    });
    expect(fixture.componentInstance.empresaId).toBe(7);
    expect(fixture.componentInstance.cargando).toBe(true);
    expect(confirmaciones).toBe(0);
    expect(TestBed.inject(TenantContextService).tieneContextoVerificado()).toBe(false);
    http.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true, data: {
        usuarioId: 3, empresaId: 7, rolId: 4,
        rolNombre: 'Operador', esAdministrador: false
      }
    });
    expect(confirmaciones).toBe(1);
    expect(TestBed.inject(TenantContextService).empresaIdVerificada()).toBe(7);
  });

  it('C02-01.M04.02: preferencia ajena se descarta; 403 no concede contexto', () => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '999');
    const fixture = TestBed.createComponent(TenantSelectorComponent);
    fixture.detectChanges();
    http.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [{ empresaId: 7, nombre: 'Empresa actual' }]
    });
    http.expectNone(`${environment.apiUrl}/tenant-context/999`);
    expect(fixture.componentInstance.empresaId).toBe(7);
    http.expectOne(`${environment.apiUrl}/tenant-context/7`).flush(
      {}, { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();
    expect(TestBed.inject(TenantContextService).contextoVerificado()).toBeNull();
    expect(TestBed.inject(TenantContextService).empresaSolicitadaId()).toBeNull();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent)
      .toContain('no tiene membresía empresarial activa');
  });

  it('C02-01.M04.02: dos empresas sin preferencia requieren seleccion explicita', () => {
    const fixture = TestBed.createComponent(TenantSelectorComponent);
    fixture.detectChanges();
    http.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [
        { empresaId: 7, nombre: 'Empresa A' },
        { empresaId: 8, nombre: 'Empresa B' }
      ]
    });
    expect(fixture.componentInstance.empresaId).toBeNull();
    http.expectNone(`${environment.apiUrl}/tenant-context/7`);
    http.expectNone(`${environment.apiUrl}/tenant-context/8`);
    expect(TestBed.inject(TenantContextService).contextoVerificado()).toBeNull();
  });

  it('C02-01.M04.02: listado malformado o duplicado no habilita seleccion', () => {
    const fixture = TestBed.createComponent(TenantSelectorComponent);
    fixture.detectChanges();
    http.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [
        { empresaId: 7, nombre: 'Empresa A' },
        { empresaId: 7, nombre: 'Empresa duplicada' }
      ]
    });
    fixture.detectChanges();
    expect(fixture.componentInstance.empresas).toEqual([]);
    expect(fixture.componentInstance.errorLista).toBe(true);
    expect((fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
    expect(TestBed.inject(TenantContextService).contextoVerificado()).toBeNull();
  });

});
