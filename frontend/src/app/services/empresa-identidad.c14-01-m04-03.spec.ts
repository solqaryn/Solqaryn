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

describe('C14-01.M05.01 - boundaries and authorization-neutral tenant branding rules', () => {
  const response = (empresaId: number, nombre: string, logoUrl: string | null = '/marca.svg') => ({
    success: true,
    data: { empresaId, nombre, logoUrl, rtn: 'RTN-PRIVATE', correoRemitente: 'private@example.invalid', impuestosJson: '{"private":true}' }
  });

  it('rejects nonpositive, unsafe, fractional and nonfinite IDs without invoking private API', () => {
    let calls = 0;
    const provider = { getTenant: () => { calls++; return of(response(1, 'Empresa A')); } } as unknown as EmpresaConfiguracionService;
    const identity = new EmpresaIdentidadService(provider);
    for (const invalid of [0, -1, NaN, Infinity, -Infinity, 1.5, Number.MAX_SAFE_INTEGER + 1]) {
      identity.cargarTenant(invalid).subscribe();
      expect(identity.nombreSistema()).toBe('SOLQARYN');
    }
    expect(calls).toBe(0);
    expect(identity.logoUrl()).toBe('');
  });

  it('clears prior A branding synchronously while authorized B is still pending', () => {
    const pendingB = new Subject<unknown>();
    const provider = {
      getTenant: (id: number) => id === 1 ? of(response(1, 'Empresa A', '/a.svg')) : pendingB.asObservable()
    } as unknown as EmpresaConfiguracionService;
    const identity = new EmpresaIdentidadService(provider);
    identity.cargarTenant(1).subscribe();
    expect(identity.nombreSistema()).toBe('Empresa A');
    identity.cargarTenant(2).subscribe();
    expect(identity.nombreSistema()).toBe('SOLQARYN');
    expect(identity.logoUrl()).toBe('');
    pendingB.next(response(2, 'Empresa B', '/b.svg'));
    pendingB.complete();
    expect(identity.nombreSistema()).toBe('Empresa B');
    expect(identity.logoUrl()).toBe('/b.svg');
  });

  it('does not permit an older public HTTP response to overwrite a verified private tenant', () => {
    const pendingPublic = new Subject<unknown>();
    const provider = {
      getPublica: () => pendingPublic.asObservable(),
      getWhatsAppPublico: () => of({ disponible: false, numeroTelefonoE164: null }),
      getTenant: () => of(response(2, 'Empresa B', '/b.svg'))
    } as unknown as EmpresaConfiguracionService;
    const identity = new EmpresaIdentidadService(provider);
    identity.cargar().subscribe();
    identity.cargarTenant(2).subscribe();
    pendingPublic.next({ success: true, data: { nombreVisibleSistema: 'Empresa A', logoUrl: '/a.svg' } });
    pendingPublic.complete();
    expect(identity.nombreSistema()).toBe('Empresa B');
    expect(identity.logoUrl()).toBe('/b.svg');
  });

  it('rejects crossed-tenant provider replies and malicious markup in the name', () => {
    let crossed = true;
    const provider = {
      getTenant: () => of(crossed ? response(2, 'Empresa B') : response(1, '<img src=x onerror=alert(1)>'))
    } as unknown as EmpresaConfiguracionService;
    const identity = new EmpresaIdentidadService(provider);
    identity.cargarTenant(1).subscribe();
    expect(identity.nombreSistema()).toBe('SOLQARYN');
    crossed = false;
    identity.cargarTenant(1).subscribe();
    expect(identity.nombreSistema()).toBe('SOLQARYN');
    expect(identity.logoUrl()).toBe('');
  });

  it('copies only safe presentation fields, never fiscal or mail data, and blocks credentialed logo URLs', () => {
    const provider = {
      getTenant: () => of(response(1, 'Empresa A', 'https://user:password@example.invalid/logo.svg'))
    } as unknown as EmpresaConfiguracionService;
    const identity = new EmpresaIdentidadService(provider);
    identity.cargarTenant(1).subscribe();
    expect(identity.nombreSistema()).toBe('Empresa A');
    expect(identity.logoUrl()).toBe('');
    const config = identity.config();
    expect('rtn' in config).toBe(false);
    expect('correoRemitente' in config).toBe(false);
    expect('impuestosJson' in config).toBe(false);
  });

  it('a rejected private read remains neutral and permits fresh server-side revalidation', () => {
    let calls = 0;
    const provider = {
      getTenant: () => {
        calls++;
        return calls === 1 ? throwError(() => new Error('403 forbidden')) : of(response(1, 'Empresa A'));
      }
    } as unknown as EmpresaConfiguracionService;
    const identity = new EmpresaIdentidadService(provider);
    identity.cargarTenant(1).subscribe();
    expect(identity.nombreSistema()).toBe('SOLQARYN');
    identity.cargarTenant(1).subscribe();
    expect(calls).toBe(2);
    expect(identity.nombreSistema()).toBe('Empresa A');
  });

  it('a denied B request cannot resurrect a stale A response after switching tenants', () => {
    const pendingA = new Subject<unknown>();
    const provider = {
      getTenant: (id: number) => id === 1 ? pendingA.asObservable() : throwError(() => new Error('403 B denied'))
    } as unknown as EmpresaConfiguracionService;
    const identity = new EmpresaIdentidadService(provider);
    identity.cargarTenant(1).subscribe();
    identity.cargarTenant(2).subscribe();
    pendingA.next(response(1, 'Empresa A', '/a.svg'));
    pendingA.complete();
    expect(identity.nombreSistema()).toBe('SOLQARYN');
    expect(identity.logoUrl()).toBe('');
  });
});
