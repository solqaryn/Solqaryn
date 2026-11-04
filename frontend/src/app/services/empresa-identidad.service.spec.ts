import { Subject, of } from 'rxjs';
import { EmpresaIdentidadService } from './empresa-identidad.service';
import { EmpresaConfiguracionService } from './empresa-configuracion.service';
import { EmpresaConfiguracion } from '../core/models/empresa-configuracion.model';

describe('P1-007.A - invalidación de identidad al cambiar empresa', () => {
  const respuesta = (nombre: string) => ({
    success: true,
    data: { nombreVisibleSistema: nombre, nombreComercial: nombre, whatsApp: '+50422220000' }
  });

  it('no rehidrata identidad A con HTTP 200 cuando ya se salió de la empresa', () => {
    const pendienteA = new Subject<any>();
    const servicio = {
      getPublica: () => pendienteA.asObservable(),
      getWhatsAppPublico: () => of({ disponible: false })
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(servicio);
    let nombreEmitido = '';

    identidad.cargar().subscribe(config => { nombreEmitido = config.nombreVisibleSistema; });
    identidad.usarPlataforma();
    pendienteA.next(respuesta('Empresa A'));
    pendienteA.complete();

    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(nombreEmitido).toBe('SOLQARYN');
    expect(identidad.config().logoUrl).toBeUndefined();
  });

  it('una respuesta tardía A no reemplaza el branding ya verificado de B', () => {
    const pendientes = [new Subject<any>(), new Subject<any>()];
    let llamadas = 0;
    const servicio = {
      getPublica: () => pendientes[llamadas++].asObservable(),
      getWhatsAppPublico: () => of({ disponible: false })
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(servicio);

    identidad.cargar().subscribe();
    identidad.usarPlataforma();
    identidad.cargar().subscribe();

    pendientes[1].next(respuesta('Empresa B'));
    pendientes[1].complete();
    pendientes[0].next(respuesta('Empresa A'));
    pendientes[0].complete();

    expect(llamadas).toBe(2);
    expect(identidad.nombreSistema()).toBe('Empresa B');
  });

  it('la respuesta anterior tampoco revierte identidad recién guardada', () => {
    const pendiente = new Subject<any>();
    const servicio = {
      getPublica: () => pendiente.asObservable(),
      getWhatsAppPublico: () => of({ disponible: false })
    } as unknown as EmpresaConfiguracionService;
    const identidad = new EmpresaIdentidadService(servicio);

    identidad.cargar().subscribe();
    identidad.refrescarDespuesDeGuardar({
      nombreVisibleSistema: 'Empresa B guardada',
      nombreComercial: 'Empresa B guardada'
    } as EmpresaConfiguracion);
    pendiente.next(respuesta('Empresa A'));
    pendiente.complete();

    expect(identidad.nombreSistema()).toBe('Empresa B guardada');
  });
});
