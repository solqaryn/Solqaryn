import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { ConsultaRegistroDialog, ConsultaRegistroDirective, mostrarValorConsulta } from './consulta-registro';

@Component({
  standalone: true,
  imports: [ConsultaRegistroDirective],
  template: '<button type="button" [appConsultaRegistro]="registro">Ver</button>'
})
class ConsultaHost {
  registro = { titulo: 'Registro de prueba', campos: { Nombre: 'Ana', Activo: true, Cantidad: 0 } };
}

describe('Consulta de registros', () => {
  it('representa valores ausentes, booleanos, cero y listas sin perder información', () => {
    expect(mostrarValorConsulta(null)).toBe('—');
    expect(mostrarValorConsulta(false)).toBe('No');
    expect(mostrarValorConsulta(true)).toBe('Sí');
    expect(mostrarValorConsulta(0)).toBe('0');
    expect(mostrarValorConsulta(['A', 'B'])).toBe('A, B');
  });

  it('abre únicamente con empresa verificada y transmite una copia de los campos declarados', () => {
    let autorizado = false;
    const open = vi.fn();
    TestBed.configureTestingModule({
      imports: [ConsultaHost],
      providers: [
        { provide: TenantContextService, useValue: { tieneContextoVerificado: () => autorizado } },
        { provide: MatDialog, useValue: { open } }
      ]
    });
    const fixture = TestBed.createComponent(ConsultaHost);
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();
    expect(open).not.toHaveBeenCalled();
    autorizado = true;
    button.click();
    expect(open).toHaveBeenCalledTimes(1);
    expect(open).toHaveBeenCalledWith(ConsultaRegistroDialog, expect.objectContaining({
      data: fixture.componentInstance.registro, restoreFocus: true, closeOnNavigation: true
    }));
    expect(open.mock.calls[0][1].data.campos).not.toBe(fixture.componentInstance.registro.campos);
  });
});
