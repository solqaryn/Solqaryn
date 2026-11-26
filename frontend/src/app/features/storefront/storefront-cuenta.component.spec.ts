import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { StorefrontCuentaComponent } from './storefront-cuenta.component';
import { StorefrontCuentaService } from './storefront-cuenta.service';
import { StorefrontCarritoService } from './storefront-carrito.service';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { StorefrontService } from './storefront.service';
import { TiendaCuentaPerfil, TiendaDireccionCliente } from './storefront.models';

describe('QA — limpieza de cuenta al cerrar sesión', () => {
  it('cancela la carga anterior y elimina los datos del editor', () => {
    const perfil = signal<TiendaCuentaPerfil | null>({ id: 1, nombre: 'Cliente QA', correo: 'qa@example.invalid' });
    const direcciones = new Subject<TiendaDireccionCliente[]>();
    const cuenta = {
      perfil, autenticado: computed(() => perfil() !== null),
      restaurar: () => of(perfil()), logout: vi.fn(() => { perfil.set(null); return of(undefined); }),
      direcciones: () => direcciones, favoritos: () => of([11]), pedidos: () => of([]), notificaciones: () => of([])
    };
    const tienda = { obtenerProductosContexto: vi.fn(() => of([])) };
    TestBed.configureTestingModule({ providers: [
      { provide: StorefrontCuentaService, useValue: cuenta },
      { provide: StorefrontIdentidadService, useValue: { cargar: () => of(null) } },
      { provide: StorefrontCarritoService, useValue: {} },
      { provide: StorefrontService, useValue: tienda },
      { provide: Router, useValue: {} }
    ] });
    const component = TestBed.runInInjectionContext(() => new StorefrontCuentaComponent());
    component.ngOnInit();
    expect(component.cargando()).toBe(true);
    component.direccionForm.patchValue({ alias: 'Casa', direccion: 'Dirección QA' });
    component.cerrarSesion();
    direcciones.next([{ id: 1, alias: 'Casa', recibe: 'Cliente QA', telefono: '00000000', direccion: 'Dirección QA', predeterminada: true }]);
    direcciones.complete();
    expect(cuenta.logout).toHaveBeenCalledTimes(1);
    expect(component.direcciones()).toEqual([]);
    expect(component.favoritos()).toEqual([]);
    expect(component.direccionForm.controls.direccion.value).toBe('');
    expect(component.cargando()).toBe(false);
    expect(tienda.obtenerProductosContexto).not.toHaveBeenCalled();
  });
});
