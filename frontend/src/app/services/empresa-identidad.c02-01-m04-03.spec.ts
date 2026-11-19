import { of, Subject } from 'rxjs';
import { EmpresaIdentidadService, c0201LogoBrandingSeguro, c0201NombreBrandingSeguro } from './empresa-identidad.service';
import { EmpresaConfiguracionService } from './empresa-configuracion.service';

function crearServicio(respuesta = of({ data: { nombreVisibleSistema: 'Empresa A', logoUrl: 'https://cdn.example.test/logo.png', whatsApp: '50499999999' } })) {
  return new EmpresaIdentidadService({
    getPublica: () => respuesta,
    getWhatsAppPublico: () => of({ disponible: false })
  } as unknown as EmpresaConfiguracionService);
}

describe('C02-01.M04.03 branding dinamico sin autoridad cliente', () => {
  it('conserva colores y nombre solo como presentacion, no cambia el contexto ni el rol', () => {
    const identidad = crearServicio();
    identidad.cargar().subscribe();
    expect(identidad.nombreSistema()).toBe('Empresa A');
    expect(identidad.logoUrl()).toBe('https://cdn.example.test/logo.png');
    expect(identidad.config().id).toBe(0);
  });

  it('limita logos a HTTPS sin credenciales o ruta relativa local, y niega esquemas hostiles', () => {
    expect(c0201LogoBrandingSeguro('/assets/marca.svg')).toBe('/assets/marca.svg');
    expect(c0201LogoBrandingSeguro('https://cdn.example.test/a.png')).toBe('https://cdn.example.test/a.png');
    for (const input of [
      'javascript:alert(1)', 'data:image/svg+xml,<svg/>',
      'http://tenant-b.example.test/imagen.png', '//tenant-b.example.test/logo.png',
      'https://user:token@tenant-b.example.test/logo.png',
      '/\\tenant-b/logo', '', null, 123
    ]) expect(c0201LogoBrandingSeguro(input)).toBe('');
  });

  it('rechaza nombres vacios, truncados o de markup, y mantiene texto visible seguro', () => {
    expect(c0201NombreBrandingSeguro(' Empresa válida ')).toBe('Empresa válida');
    for (const input of [' ', '<img src=x>', 'A'.repeat(121), 'Empresa\nX', 7, null])
      expect(c0201NombreBrandingSeguro(input)).toBeNull();
    const identidad = crearServicio();
    identidad.refrescarDespuesDeGuardar({
      ...identidad.config(),
      nombreVisibleSistema: '<svg/onload=alert(1)>',
      logoUrl: 'javascript:alert(1)',
      mensajeLogin: ' '
    });
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
    expect(identidad.mensajeLogin()).toBe('Inicia sesión para administrar SOLQARYN');
  });

  it('reinicia el branding al regresar a plataforma y no revive respuesta HTTP tardia de tenant A', () => {
    const tardia = new Subject<{ data: { nombreVisibleSistema: string; logoUrl: string; whatsApp: string } }>();
    const identidad = crearServicio(tardia.asObservable());
    identidad.cargar().subscribe();
    identidad.usarPlataforma();
    tardia.next({ data: {
      nombreVisibleSistema: 'Tenant A',
      logoUrl: 'https://tenant-a.test/logo.png',
      whatsApp: '50499999999'
    } });
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
  });
});
