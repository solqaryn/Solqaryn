import { Subject, of, throwError } from 'rxjs';
import { EmpresaIdentidadService } from './empresa-identidad.service';
import { EmpresaConfiguracionService } from './empresa-configuracion.service';
import type { EmpresaConfiguracion } from '../core/models/empresa-configuracion.model';

/** C14-01.M04.03: solo pruebas de contrato del branding, sin backend ni tenants live. */
describe('C14-01.M04.03 - identidad y branding dinamico seguro', () => {
  const config = (nombre: string, logo: string) => ({
    nombreVisibleSistema: nombre, nombreComercial: nombre, logoUrl: logo,
    mensajeLogin: 'Accede a '+nombre, descripcionSistema: 'Servicio '+nombre,
    whatsApp: '+50422220000', mostrarCopyright: true,
    copyright: '© 2024 '+nombre, usarAnioAutomaticoCopyright: true
  });
  const response = (nombre: string, logo: string) => ({success:true,data:config(nombre,logo)});

  it('valores por defecto y reset a plataforma no conservan logo/identidad empresarial', () => {
    const provider = {
      getPublica: () => of(response('Empresa A','https://static.invalid/a.svg')),
      getWhatsAppPublico: () => of({ disponible:false, numeroTelefonoE164:null })
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(provider);
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
    identidad.cargar().subscribe();
    expect(identidad.nombreSistema()).toBe('Empresa A');
    expect(identidad.logoUrl()).toBe('https://static.invalid/a.svg');
    identidad.usarPlataforma();
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
    expect(identidad.mensajeLogin()).toBe('Inicia sesión en SOLQARYN');
  });

  it('propaga branding autorizado: nombre, logo, login, descripcion y copyright por año', () => {
    const provider = {
      getPublica: () => of(response('Empresa B','https://static.invalid/b.svg')),
      getWhatsAppPublico: () => of({ disponible:false, numeroTelefonoE164:null })
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(provider);
    identidad.cargar().subscribe();
    expect(identidad.nombreSistema()).toBe('Empresa B');
    expect(identidad.logoUrl()).toBe('https://static.invalid/b.svg');
    expect(identidad.mensajeLogin()).toBe('Accede a Empresa B');
    expect(identidad.descripcionSistema()).toBe('Servicio Empresa B');
    expect(identidad.copyright()).toContain(String(new Date().getFullYear()));
    expect(identidad.mostrarCopyright()).toBe(true);
  });

  it('una respuesta A retrasada nunca reescribe branding B, logo ni texto', () => {
    const a = new Subject<unknown>(), b = new Subject<unknown>();
    let n=0;
    const provider={
      getPublica: () => (n++===0?a:b).asObservable(),
      getWhatsAppPublico: () => of({ disponible:false, numeroTelefonoE164:null })
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(provider);
    identidad.cargar().subscribe();
    identidad.usarPlataforma();
    identidad.cargar().subscribe();
    b.next(response('Empresa B','https://static.invalid/b.svg')); b.complete();
    a.next(response('Empresa A','https://static.invalid/a.svg')); a.complete();
    expect(n).toBe(2);
    expect(identidad.nombreSistema()).toBe('Empresa B');
    expect(identidad.logoUrl()).toBe('https://static.invalid/b.svg');
    expect(identidad.mensajeLogin()).toBe('Accede a Empresa B');
  });

  it('fallo del proveedor publico preserva el branding neutro, sin datos del tenant', () => {
    const provider={
      getPublica: () => throwError(()=>new Error('tenant_A_private_error')),
      getWhatsAppPublico: () => of({ disponible:false, numeroTelefonoE164:null })
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(provider);
    let completed=false;
    identidad.cargar().subscribe({next:()=>{ completed=true; }});
    expect(completed).toBe(true);
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
    expect(identidad.mensajeLogin()).toBe('Inicia sesión en SOLQARYN');
  });

  it('configuracion guardada con copyright desactivado no se mezcla con respuesta anterior', () => {
    const pending=new Subject<unknown>();
    const provider={
      getPublica: () => pending.asObservable(),
      getWhatsAppPublico: () => of({ disponible:false, numeroTelefonoE164:null })
    } as unknown as EmpresaConfiguracionService;
    const identidad=new EmpresaIdentidadService(provider);
    identidad.cargar().subscribe();
    identidad.refrescarDespuesDeGuardar({
      ...config('Empresa B','https://static.invalid/b.svg'),
      id:2, mostrarCopyright:false, usarAnioAutomaticoCopyright:false,
      eslogan:'Empresa B', encabezadoActivo:true, piePaginaActivo:true,
      moneda:'HNL', zonaHoraria:'America/Tegucigalpa', formatoFecha:'dd/MM/yyyy'
    } as EmpresaConfiguracion);
    pending.next(response('Empresa A','https://static.invalid/a.svg')); pending.complete();
    expect(identidad.nombreSistema()).toBe('Empresa B');
    expect(identidad.logoUrl()).toBe('https://static.invalid/b.svg');
    expect(identidad.mostrarCopyright()).toBe(false);
    expect(identidad.copyright()).toBe('© 2024 Empresa B');
  });
  it('V22 carga nombre y logo tenant solo mediante API privada con ID verificado', () => {
    let reads = 0;
    const provider = {
      getTenant: (id: number) => {
        reads++;
        expect(id).toBe(1);
        return of({ success: true, data: {
          empresaId: 1, nombre: 'Empresa A', logoUrl: 'https://assets.example.invalid/a.svg'
        } });
      }
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(provider);
    identidad.cargarTenant(1).subscribe();
    identidad.cargarTenant(1).subscribe();
    expect(reads).toBe(1);
    expect(identidad.nombreSistema()).toBe('Empresa A');
    expect(identidad.logoUrl()).toBe('https://assets.example.invalid/a.svg');
    expect(identidad.mensajeLogin()).toBe('Inicia sesión para administrar Empresa A');
  });

  it('V22 rechaza respuesta de tenant B para solicitud A y logos inseguros', () => {
    const provider = {
      getTenant: () => of({ success: true, data: {
        empresaId: 2, nombre: 'Empresa B', logoUrl: 'javascript:alert(1)'
      } })
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(provider);
    identidad.cargarTenant(1).subscribe();
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
    identidad.cargarTenant(0).subscribe();
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
  });

  it('V22 una respuesta A atrasada nunca restaura marca A despues de B', () => {
    const a = new Subject<unknown>();
    const b = new Subject<unknown>();
    const provider = {
      getTenant: (id: number) => (id === 1 ? a : b).asObservable()
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(provider);
    identidad.cargarTenant(1).subscribe();
    identidad.cargarTenant(2).subscribe();
    b.next({ success: true, data: { empresaId: 2, nombre: 'Empresa B', logoUrl: '/b.svg' } });
    b.complete();
    a.next({ success: true, data: { empresaId: 1, nombre: 'Empresa A', logoUrl: '/a.svg' } });
    a.complete();
    expect(identidad.nombreSistema()).toBe('Empresa B');
    expect(identidad.logoUrl()).toBe('/b.svg');
    identidad.usarPlataforma();
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
  });

  it('V22 falla cerrado ante denegacion HTTP y no conserva datos del tenant', () => {
    const provider = {
      getTenant: () => throwError(() => new Error('403 forbidden'))
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(provider);
    identidad.cargarTenant(1).subscribe();
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
  });

});
