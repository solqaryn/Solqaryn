import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface RolEmpresaMvp {
  id: number;
  nombre: string;
  esAdministrador: boolean;
  empresaId: number | null;
}

export interface UsuarioIdentidadMvp {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  activo: boolean;
  bloqueado: boolean;
}

export interface MembresiaEmpresaMvp {
  usuarioId: number;
  nombreUsuario: string;
  nombreCompleto: string;
  usuarioActivo: boolean;
  rolId: number;
  rolNombre: string;
  esAdministrador: boolean;
  activa: boolean;
}

@Injectable({ providedIn: 'root' })
export class PlataformaGestionMvpService {
  private readonly root = environment.apiUrl + '/plataforma/gestion';
  constructor(private readonly http: HttpClient) {}

  private verificar<T>(r: ApiResponse<T>): T {
    if (r?.success !== true || r.data === null || r.data === undefined)
      throw new Error('La API no confirmó la operación.');
    return r.data;
  }

  roles(empresaId = 0): Observable<RolEmpresaMvp[]> {
    const params = new HttpParams().set('empresaId', empresaId);
    return this.http.get<ApiResponse<RolEmpresaMvp[]>>(
      this.root + '/roles', { params }).pipe(map(r => {
      const d = this.verificar(r);
      if (!Array.isArray(d) ||
          d.some(x => !Number.isSafeInteger(x?.id) || x.id <= 0 ||
            typeof x.nombre !== 'string' || typeof x.esAdministrador !== 'boolean' ||
            (x.empresaId !== null && (!Number.isSafeInteger(x.empresaId) || x.empresaId <= 0))))
        throw new Error('Roles empresariales inválidos.');
      return d;
    }));
  }

  buscarUsuario(nombreUsuario: string): Observable<UsuarioIdentidadMvp | null> {
    const params = new HttpParams().set('nombreUsuario', nombreUsuario);
    return this.http.get<ApiResponse<UsuarioIdentidadMvp | null>>(
      this.root + '/usuarios/buscar', { params }).pipe(map(r => {
        if (r?.success !== true)
          throw new Error('No se confirmó la búsqueda.');
        if (r.data === null) return null;
        if (!Number.isSafeInteger(r.data?.id) || r.data.id <= 0 ||
            typeof r.data?.nombreUsuario !== 'string' ||
            typeof r.data?.activo !== 'boolean' ||
            typeof r.data?.bloqueado !== 'boolean')
          throw new Error('Identidad inválida.');
        return r.data;
    }));
  }

  miembros(empresaId: number): Observable<MembresiaEmpresaMvp[]> {
    return this.http.get<ApiResponse<MembresiaEmpresaMvp[]>>(
      this.root + '/empresas/' + empresaId + '/membresias').pipe(map(r => {
        const d = this.verificar(r);
        if (!Array.isArray(d) || d.length > 200 ||
            d.some(x => !Number.isSafeInteger(x?.usuarioId) || x.usuarioId <= 0 ||
              !Number.isSafeInteger(x?.rolId) || x.rolId <= 0 ||
              typeof x?.nombreUsuario !== 'string' ||
              typeof x?.usuarioActivo !== 'boolean' ||
              typeof x?.activa !== 'boolean'))
          throw new Error('Membresías inválidas.');
        return d;
    }));
  }

  crearUsuario(nombreUsuario: string, nombreCompleto: string, password: string) {
    return this.http.post<ApiResponse<{ id: number; nombreUsuario: string }>>(
      this.root + '/usuarios', { nombreUsuario, nombreCompleto, password })
      .pipe(map(r => this.verificar(r)));
  }

  asignar(empresaId: number, usuarioId: number, rolId: number) {
    return this.http.post<ApiResponse<unknown>>(
      this.root + '/empresas/' + empresaId + '/membresias',
      { usuarioId, rolId }).pipe(map(r => this.verificar(r)));
  }

  cambiarRol(empresaId: number, usuarioId: number, rolId: number) {
    return this.http.put<ApiResponse<unknown>>(
      this.root + '/empresas/' + empresaId + '/membresias/' + usuarioId + '/rol',
      { rolId }).pipe(map(r => this.verificar(r)));
  }

  cambiarMembresia(empresaId: number, usuarioId: number, activa: boolean) {
    return this.http.put<ApiResponse<unknown>>(
      this.root + '/empresas/' + empresaId + '/membresias/' + usuarioId + '/estado',
      { activa }).pipe(map(r => this.verificar(r)));
  }

  cambiarNombreEmpresa(empresaId: number, nombre: string) {
    return this.http.put<ApiResponse<unknown>>(
      this.root + '/empresas/' + empresaId + '/nombre', { nombre })
      .pipe(map(r => this.verificar(r)));
  }

  cambiarEstadoEmpresa(empresaId: number, activa: boolean) {
    return this.http.put<ApiResponse<unknown>>(
      this.root + '/empresas/' + empresaId + '/estado', { activa })
      .pipe(map(r => this.verificar(r)));
  }

  altaControlada(nombre: string, administradorUsuarioId: number) {
    // Esta operación solo existe con los dos controles backend certificados.
    // La UI nunca modifica esas flags ni sustituye la verificación del servidor.
    return this.http.post<{ empresaId: number; administradorUsuarioId: number }>(
      environment.apiUrl + '/plataforma/empresas/alta-controlada',
      { nombre, administradorUsuarioId }).pipe(map(r => {
        if (!Number.isSafeInteger(r?.empresaId) || r.empresaId <= 0 ||
            r.administradorUsuarioId !== administradorUsuarioId)
          throw new Error('El alta no fue confirmada.');
        return r;
    }));
  }
}
