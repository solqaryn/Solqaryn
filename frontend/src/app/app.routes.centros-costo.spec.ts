import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { appConfig } from './app.config';

describe('N4.11.G CentroCosto application route registration', () => {
  it('registers the protected centros-costo route in the application router', () => {
    TestBed.configureTestingModule({ providers: appConfig.providers });

    const router = TestBed.inject(Router);
    const route = router.config.find(candidate => candidate.path === 'centros-costo');

    expect(route).toBeDefined();
    expect(route?.canActivate).toHaveLength(2);
    expect(route?.data).toEqual({ modulo: 'Finanzas', accion: 'Ver' });
    expect(route?.loadComponent).toBeTypeOf('function');
  });
});
