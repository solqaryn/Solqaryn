import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { DirectorioPlataforma } from '../../core/auth/plataforma-empresas.service';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import {
  MembresiaEmpresaMvp, PlataformaGestionMvpService, RolEmpresaMvp,
  UsuarioIdentidadMvp
} from '../../core/auth/plataforma-gestion-mvp.service';

/**
 * MVP comercial: gestión básica y explícita de identidades/membresías.
 * La UI no autoriza: cada operación verifica el rol global en MySQL.
 * Nunca reutiliza RBAC ERP ni agrega X-Empresa-Id como autoridad.
 */
@Component({
  selector: 'app-plataforma-gestion-mvp',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatCardModule],
  template: `
    <mat-card class="gestion-card" aria-labelledby="gestion-title">
      <mat-card-header>
        <mat-card-title id="gestion-title">Operación básica de empresas y usuarios</mat-card-title>
        <mat-card-subtitle>Administración global SOLQARYN · versión comercial inicial</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <p>Selecciona una empresa para administrar sus responsables. Sus datos comerciales permanecen aislados.</p>

        <div class="form-grid">
          <div class="field">
            <label for="empresa-gestion">Empresa</label>
            <select id="empresa-gestion" [value]="empresaId()" (change)="seleccionarEmpresa($any($event.target).value)">
              <option value="0">Seleccionar empresa</option>
              @if (directorio(); as datos) {
                @for (e of datos.items; track e.id) {
                  <option [value]="e.id">{{ e.nombre }} — {{ e.activa ? 'Activa' : 'Inactiva' }}</option>
                }
              }
            </select>
          </div>
          <div class="field">
            <label for="usuario-buscar">Buscar cuenta existente (nombre exacto)</label>
            <div class="inline-input">
              <input id="usuario-buscar" type="text" maxlength="50" autocomplete="off"
                [value]="busqueda()" (input)="busqueda.set($any($event.target).value)" />
              <button mat-stroked-button type="button" (click)="buscar()" [disabled]="ocupado()">Buscar</button>
            </div>
          </div>
        </div>

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
        @if (exito()) {
          <p class="success" role="status">{{ exito() }}</p>
        }
        @if (buscado(); as cuenta) {
          <div class="identity">
            <strong>{{ cuenta.nombreUsuario }}</strong> · {{ cuenta.nombreCompleto }}
            @if ((!cuenta.activo && !cuenta.pendienteAsignacion) || cuenta.bloqueado) {
              <span class="warning">Cuenta inactiva o bloqueada — no se puede asignar.</span>
            } @else {
              <span class="success-text">{{ cuenta.pendienteAsignacion ? 'Cuenta pendiente: se activará únicamente al asignarse a una empresa.' : 'Cuenta disponible para asignación.' }}</span>
            }
          </div>
        }

        <section class="subsection" aria-labelledby="crear-usuario">
          <h3 id="crear-usuario">Crear una cuenta empresarial</h3>
          <p>La cuenta nueva no recibe permisos globales ni acceso a empresas hasta asignar una membresía.</p>
          <div class="form-grid">
            <div class="field">
              <label for="nuevo-usuario">Nombre de usuario</label>
              <input id="nuevo-usuario" maxlength="50" autocomplete="off"
                [value]="usuarioNuevo()" (input)="usuarioNuevo.set($any($event.target).value)" />
            </div>
            <div class="field">
              <label for="nuevo-nombre">Nombre completo</label>
              <input id="nuevo-nombre" maxlength="150" autocomplete="name"
                [value]="nombreCompletoNuevo()" (input)="nombreCompletoNuevo.set($any($event.target).value)" />
            </div>
            <div class="field">
              <label for="nueva-clave">Contraseña inicial (10–128 caracteres, mayúscula, minúscula, cifra y símbolo)</label>
              <input id="nueva-clave" type="password" minlength="10" maxlength="128"
                autocomplete="new-password" [value]="claveNueva()" (input)="claveNueva.set($any($event.target).value)" />
            </div>
          </div>
          <button mat-stroked-button type="button" [disabled]="ocupado()"
            (click)="crearUsuario()">Crear cuenta sin privilegios</button>
        </section>

        <section class="subsection" aria-labelledby="alta-empresa">
          <h3 id="alta-empresa">Registrar una nueva empresa</h3>
          <p>Requiere una cuenta activa como primer administrador y los dos controles de certificación activados en el servidor.</p>
          <div class="form-grid">
            <div class="field">
              <label for="empresa-nueva">Nombre de empresa</label>
              <input id="empresa-nueva" maxlength="200" [value]="empresaNueva()"
                (input)="empresaNueva.set($any($event.target).value)" />
            </div>
            <div class="field">
              <label for="admin-empresa">ID de cuenta administradora verificada</label>
              <input id="admin-empresa" type="number" [value]="buscado()?.id ?? 0" readonly
                aria-describedby="admin-empresa-aviso" />
              <small id="admin-empresa-aviso">La cuenta se selecciona mediante la búsqueda exacta anterior; no se admite un ID arbitrario.</small>
            </div>
          </div>
          <button mat-flat-button type="button" (click)="crearEmpresa()"
            [disabled]="ocupado() || directorio()?.puedeCrear !== true || (buscado()?.activo !== true && buscado()?.pendienteAsignacion !== true) || buscado()?.bloqueado === true">
            Crear empresa con administrador
          </button>
          <button mat-stroked-button type="button" (click)="crearBorrador()"
            [disabled]="ocupado() || empresaNueva().trim().length < 2">
            Guardar empresa como borrador inactivo
          </button>
          @if (directorio()?.puedeCrear !== true) {
            <p class="warning" role="status">La creación comercial activa sigue protegida. Puedes preparar empresas inactivas y asignarles administradores sin publicar tiendas.</p>
          }
        </section>

        @if (empresaId() > 0) {
          <section class="subsection" aria-labelledby="public-store-registry-title">
            <h3 id="public-store-registry-title">Vinculación pública de la tienda</h3>
            <p>El registro se guarda en MySQL. Una empresa suspendida no puede publicar tiendas.
              Esta operación no habilita catálogo, carrito ni checkout secundarios.</p>
            <div class="field">
              <label for="public-store-key">Clave pública única (2–48 caracteres)</label>
              <input id="public-store-key" maxlength="48" autocomplete="off"
                [value]="nuevaStoreKey()" (input)="nuevaStoreKey.set($any($event.target).value)" />
              <small>Host público actual: {{ hostPublico() }}</small>
            </div>
            <button mat-stroked-button type="button" [disabled]="ocupado()"
              (click)="registrarTienda(false)">Guardar vinculación inactiva</button>
            @if (empresaActiva()) {
              <button mat-flat-button type="button" [disabled]="ocupado()"
                (click)="registrarTienda(true)">Publicar identidad de tienda</button>
            }
            @if (tiendasRegistradas().length) {
              <ul>
                @for (tienda of tiendasRegistradas(); track tienda.id) {
                  <li>{{ tienda.host }}/tiendas/{{ tienda.storeKey }} — {{ tienda.activa ? 'Vinculada' : 'Inactiva' }}</li>
                }
              </ul>
            }
          </section>

          <section class="subsection" aria-labelledby="identidad-empresa">
            <h3 id="identidad-empresa">Identidad y estado de la empresa seleccionada</h3>
            <div class="inline-input">
              <div class="field grow">
                <label for="renombrar-empresa">Nombre comercial</label>
                <input id="renombrar-empresa" maxlength="200" [value]="nombreEmpresa()"
                  (input)="nombreEmpresa.set($any($event.target).value)" />
              </div>
              <button mat-stroked-button type="button" [disabled]="ocupado()" (click)="renombrarEmpresa()">Guardar nombre</button>
            </div>
            @if (confirmarEstado() === null) {
              <button mat-stroked-button type="button" [disabled]="ocupado()"
                (click)="pedirConfirmacionEstado()">
                {{ empresaActiva() ? 'Suspender empresa' : 'Reactivar empresa' }}
              </button>
            } @else {
              <div class="confirm-box" role="group" aria-label="Confirmación de estado empresarial">
                <p>Confirma el cambio de estado de esta empresa. Puede afectar inmediatamente a sus usuarios y tienda.</p>
                <button mat-flat-button type="button" (click)="actualizarEstadoEmpresa()" [disabled]="ocupado()">
                  Confirmar {{ confirmarEstado() ? 'reactivación' : 'suspensión' }}
                </button>
                <button mat-button type="button" (click)="confirmarEstado.set(null)">Cancelar</button>
              </div>
            }
          </section>

          <section class="subsection" aria-labelledby="asignacion-mvp">
            <h3 id="asignacion-mvp">Asignación de usuarios y administradores</h3>
            <p>Los roles enumerados pertenecen únicamente a esta empresa o son plantillas empresariales válidas. Nunca confieren autoridad de plataforma.</p>
            <div class="inline-input">
              <div class="field grow">
                <label for="rol-asignar">Rol empresarial</label>
                <select id="rol-asignar" [value]="rolAsignacion()" (change)="rolAsignacion.set(+$any($event.target).value)">
                  <option value="0">Seleccionar rol</option>
                  @for (rol of roles(); track rol.id) {
                    <option [value]="rol.id">{{ rol.nombre }}{{ rol.esAdministrador ? ' — Administrador' : '' }}</option>
                  }
                </select>
              </div>
              <button mat-flat-button type="button" [disabled]="ocupado() || (buscado()?.activo !== true && buscado()?.pendienteAsignacion !== true) || buscado()?.bloqueado === true || rolAsignacion() <= 0"
                (click)="asignarBuscado()">Asignar cuenta seleccionada</button>
            </div>

            @if (cargandoEmpresa()) {
              <p role="status">Validando roles y membresías contra el servidor…</p>
            } @else if (miembros().length === 0) {
              <p>No hay membresías registradas o todavía no se pudieron consultar.</p>
            } @else {
              <div class="table-scroll">
                <table aria-label="Membresías por empresa">
                  <thead><tr><th scope="col">Cuenta</th><th scope="col">Estado</th><th scope="col">Rol</th><th scope="col">Acciones</th></tr></thead>
                  <tbody>
                    @for (m of miembros(); track m.usuarioId) {
                      <tr>
                        <td><strong>{{ m.nombreUsuario }}</strong><span class="secondary">{{ m.nombreCompleto }}</span></td>
                        <td>{{ m.activa && m.usuarioActivo ? 'Vigente' : 'Sin acceso' }}</td>
                        <td>
                          <label class="sr-only" [for]="'miembro-rol-' + m.usuarioId">Rol de {{ m.nombreUsuario }}</label>
                          <select [id]="'miembro-rol-' + m.usuarioId" [value]="rolEditado(m)"
                            (change)="cambiarRolBorrador(m.usuarioId, +$any($event.target).value)">
                            @for (rol of roles(); track rol.id) {
                              <option [value]="rol.id">{{ rol.nombre }}{{ rol.esAdministrador ? ' (Admin)' : '' }}</option>
                            }
                          </select>
                        </td>
                        <td>
                          <div class="table-actions">
                            <button mat-stroked-button type="button" [disabled]="ocupado() || rolEditado(m) === m.rolId"
                              (click)="guardarRol(m)">Guardar rol</button>
                            @if (confirmarMembresia() === m.usuarioId) {
                              <span class="warning">¿Retirar acceso a esta empresa?</span>
                              <button mat-flat-button type="button" [disabled]="ocupado()" (click)="cambiarEstadoMembresia(m)">Confirmar retiro</button>
                              <button mat-button type="button" (click)="confirmarMembresia.set(null)">Cancelar</button>
                            } @else {
                              <button mat-stroked-button type="button" [disabled]="ocupado()"
                                (click)="pedirCambioMembresia(m)">{{ m.activa ? 'Retirar acceso' : 'Reactivar acceso' }}</button>
                            }
                          </div>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </section>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    :host { display: block; }
    .gestion-card { margin-top: 20px; }
    mat-card-content { padding: 20px 24px 28px; }
    h3 { margin: 0 0 12px; font-size: 1.15rem; }
    p { line-height: 1.5; }
    .subsection { margin-top: 28px; border-top: 1px solid #dce3ed; padding-top: 22px; }
    .form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; margin: 15px 0; }
    .field { display: flex; flex-direction: column; gap: 6px; min-width: 0; }
    .field.grow { flex: 1; }
    .field label { font-size: .87rem; font-weight: 600; }
    .field input, .field select, table select { width: 100%; min-height: 44px; padding: 8px 10px; border-radius: 7px; border: 1px solid #8995a7; background: var(--color-background, #fff); color: var(--color-text, #182435); }
    .inline-input { display: flex; align-items: end; gap: 12px; flex-wrap: wrap; }
    .inline-input button { min-height: 44px; }
    .identity, .confirm-box { border: 1px solid #c7d2df; padding: 12px; border-radius: 9px; margin-top: 12px; }
    .error { color: #a8231d; }
    .success, .success-text { color: #256340; }
    .success { border-left: 3px solid #256340; padding-left: 12px; }
    .warning { color: #945500; }
    .table-scroll { max-width: 100%; overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; border-bottom: 1px solid #dce3ed; padding: 11px 9px; vertical-align: middle; }
    .table-actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .secondary { display: block; opacity: .74; font-size: .85rem; }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; }
    @media (max-width: 710px) { .form-grid { grid-template-columns: 1fr; } .inline-input { align-items: stretch; flex-direction: column; } .inline-input button { width: 100%; } }
  `]
})
export class PlataformaGestionMvpComponent {
  private readonly api = inject(PlataformaGestionMvpService);
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly router = inject(Router);

  readonly directorio = input<DirectorioPlataforma | null>(null);
  readonly actualizarDirectorio = output<void>();
  readonly empresaId = signal(0);
  readonly empresaActiva = signal(false);
  readonly nombreEmpresa = signal('');
  readonly roles = signal<RolEmpresaMvp[]>([]);
  readonly miembros = signal<MembresiaEmpresaMvp[]>([]);
  readonly cambiosRoles = signal<Record<number, number>>({});
  readonly cargandoEmpresa = signal(false);
  readonly ocupado = signal(false);
  readonly error = signal<string | null>(null);
  readonly exito = signal<string | null>(null);
  readonly busqueda = signal('');
  readonly buscado = signal<UsuarioIdentidadMvp | null>(null);
  readonly rolAsignacion = signal(0);
  readonly usuarioNuevo = signal('');
  readonly nombreCompletoNuevo = signal('');
  readonly claveNueva = signal('');
  readonly empresaNueva = signal('');
  readonly nuevaStoreKey = signal('');
  readonly tiendasRegistradas = signal<Array<{id: number; host: string; storeKey: string; activa: boolean}>>([]);
  readonly hostPublico = signal(typeof window !== 'undefined' ? window.location.hostname : '');

  readonly confirmarEstado = signal<boolean | null>(null);
  readonly confirmarMembresia = signal<number | null>(null);

  private limpiarMensajes(): void {
    this.error.set(null);
    this.exito.set(null);
  }

  private fallar(accion: string, motivo?: unknown): void {
    // Backend es la única fuente de autoridad. Una revocación invalida
    // toda visualización previa de identidades, aunque siga abierto el panel.
    if (motivo instanceof HttpErrorResponse && (motivo.status === 401 || motivo.status === 403)) {
      this.roles.set([]);
      this.miembros.set([]);
      this.buscado.set(null);
      this.plataforma.limpiar();
      void this.router.navigate(['/login'], { queryParams: { plataforma: 'denegado' } });
      return;
    }
    this.error.set(accion + ' no se completó o dejó de estar autorizado. Verifica la sesión y los datos.');
  }

  seleccionarEmpresa(value: string): void {
    const id = Number(value);
    const empresa = this.directorio()?.items.find(e => e.id === id);
    this.empresaId.set(empresa?.id ?? 0);
    this.empresaActiva.set(empresa?.activa ?? false);
    this.nombreEmpresa.set(empresa?.nombre ?? '');
    this.roles.set([]);
    this.miembros.set([]);
    this.cambiosRoles.set({});
    this.rolAsignacion.set(0);
    this.confirmarEstado.set(null);
    this.confirmarMembresia.set(null);
    this.limpiarMensajes();
    this.tiendasRegistradas.set([]);
    this.nuevaStoreKey.set('');
    if (empresa) {
      this.cargarEmpresa();
      this.api.tiendas(empresa.id).subscribe({
        next: data => { if (this.empresaId() === empresa.id) this.tiendasRegistradas.set(data); },
        error: err => this.fallar('La consulta de tiendas', err)
      });
    }
  }

  private cargarEmpresa(): void {
    const id = this.empresaId();
    if (!id || this.cargandoEmpresa()) return;
    this.cargandoEmpresa.set(true);
    this.roles.set([]);
    this.miembros.set([]);
    forkJoin({ roles: this.api.roles(id), miembros: this.api.miembros(id) })
      .pipe(finalize(() => this.cargandoEmpresa.set(false)))
      .subscribe({
        next: data => {
          if (this.empresaId() !== id) return;
          this.roles.set(data.roles);
          this.miembros.set(data.miembros);
          this.cambiosRoles.set({});
          this.rolAsignacion.set(data.roles[0]?.id ?? 0);
        },
        error: err => this.fallar('La consulta de roles y membresías', err)
      });
  }

  private ejecutar(accion: string, operacion: () => void): void {
    if (this.ocupado()) return;
    this.ocupado.set(true);
    this.limpiarMensajes();
    operacion();
  }

  buscar(): void {
    const nombre = this.busqueda().trim();
    if (this.ocupado() || !nombre) return;
    this.ejecutar('Buscar cuenta', () =>
      this.api.buscarUsuario(nombre).pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: cuenta => {
            this.buscado.set(cuenta);
            this.exito.set(cuenta ? 'Cuenta localizada.' : 'No existe esa cuenta.');
          },
          error: err => {
            this.buscado.set(null);
            this.fallar('La búsqueda', err);
          }
        })
    );
  }

  crearUsuario(): void {
    if (this.ocupado()) return;
    const nombre = this.usuarioNuevo().trim();
    const completo = this.nombreCompletoNuevo().trim();
    const clave = this.claveNueva();
    if (!nombre || !completo || clave.length < 10) {
      this.error.set('Completa nombre de usuario, nombre y contraseña fuerte.');
      return;
    }
    this.ejecutar('Crear cuenta', () =>
      this.api.crearUsuario(nombre, completo, clave)
        .pipe(finalize(() => { this.claveNueva.set(''); this.ocupado.set(false); }))
        .subscribe({
          next: data => {
            this.busqueda.set(data.nombreUsuario);
            this.usuarioNuevo.set('');
            this.nombreCompletoNuevo.set('');
            this.buscado.set({ id: data.id, nombreUsuario: data.nombreUsuario,
              nombreCompleto: completo, activo: false, bloqueado: false, pendienteAsignacion: true });
            this.exito.set('Cuenta creada INACTIVA. Se activará dentro de la primera asignación empresarial auditada.');
          },
          error: err => this.fallar('El alta de cuenta', err)
        })
    );
  }

  crearBorrador(): void {
    const nombre = this.empresaNueva().trim();
    if (this.ocupado() || nombre.length < 2 || nombre.length > 200) return;
    this.ejecutar('Crear borrador', () =>
      this.api.crearBorradorEmpresa(nombre)
        .pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: data => {
            this.empresaNueva.set('');
            this.exito.set('Borrador empresarial inactivo creado. ID: ' + data.empresaId +
              '. Asigna un administrador antes de solicitar su activación.');
            this.actualizarDirectorio.emit();
          },
          error: err => this.fallar('La creación del borrador empresarial', err)
        })
    );
  }

  registrarTienda(activa: boolean): void {
    const id = this.empresaId();
    const storeKey = this.nuevaStoreKey().trim();
    const host = this.hostPublico();
    if (this.ocupado() || id <= 0 || !/^[a-z][a-z0-9-]{0,46}[a-z0-9]$/.test(storeKey) ||
        !host.includes('.') || (activa && !this.empresaActiva())) {
      this.error.set('Selecciona empresa y clave pública válida.');
      return;
    }
    this.ejecutar('Registrar tienda', () =>
      this.api.vincularTienda(id, storeKey, host, activa)
        .pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: () => {
            this.exito.set(activa ? 'Identidad pública vinculada.' : 'Vinculación guardada inactiva.');
            this.api.tiendas(id).subscribe({
              next: data => { if (this.empresaId() === id) this.tiendasRegistradas.set(data); },
              error: err => this.fallar('La consulta de tiendas', err)
            });
          },
          error: err => this.fallar('La vinculación de tienda', err)
        })
    );
  }

  crearEmpresa(): void {
    const nombre = this.empresaNueva().trim();
    const id = this.buscado()?.id ?? 0;
    if (this.ocupado() || this.directorio()?.puedeCrear !== true ||
        this.buscado()?.bloqueado === true ||
        (this.buscado()?.activo !== true && this.buscado()?.pendienteAsignacion !== true))
      return;
    if (nombre.length < 2 || id <= 0) {
      this.error.set('Indica un nombre válido y un administrador existente.');
      return;
    }
    this.ejecutar('Crear empresa', () =>
      this.api.altaControlada(nombre, id).pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: data => {
            this.empresaNueva.set('');
            this.exito.set('Empresa creada con administrador inicial. ID: ' + data.empresaId);
            this.actualizarDirectorio.emit();
          },
          error: err => this.fallar('El alta empresarial', err)
        })
    );
  }

  asignarBuscado(): void {
    const e = this.empresaId();
    const cuenta = this.buscado();
    const rol = this.rolAsignacion();
    if (!e || !cuenta || (!cuenta.activo && !cuenta.pendienteAsignacion) || cuenta.bloqueado || rol <= 0 ||
        !this.roles().some(r => r.id === rol)) return;
    this.ejecutar('Asignar membresía', () =>
      this.api.asignar(e, cuenta.id, rol).pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: () => {
            this.exito.set('Membresía empresarial registrada.');
            this.buscado.set({ ...cuenta, activo: true, pendienteAsignacion: false });
            this.cargarEmpresa();
          },
          error: err => this.fallar('La asignación', err)
        })
    );
  }

  rolEditado(m: MembresiaEmpresaMvp): number {
    return this.cambiosRoles()[m.usuarioId] ?? m.rolId;
  }

  cambiarRolBorrador(usuarioId: number, rolId: number): void {
    this.cambiosRoles.update(actual => ({ ...actual, [usuarioId]: rolId }));
  }

  guardarRol(m: MembresiaEmpresaMvp): void {
    const empresaId = this.empresaId(), rolId = this.rolEditado(m);
    if (!empresaId || !this.roles().some(r => r.id === rolId) || rolId === m.rolId) return;
    this.ejecutar('Cambiar rol', () =>
      this.api.cambiarRol(empresaId, m.usuarioId, rolId)
        .pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: () => {
            this.exito.set('Rol empresarial actualizado.');
            this.cargarEmpresa();
          },
          error: err => this.fallar('El cambio de rol', err)
        })
    );
  }

  pedirCambioMembresia(m: MembresiaEmpresaMvp): void {
    if (m.activa) {
      this.confirmarMembresia.set(m.usuarioId);
      return;
    }
    this.cambiarEstadoMembresia(m);
  }

  cambiarEstadoMembresia(m: MembresiaEmpresaMvp): void {
    const id = this.empresaId();
    if (!id) return;
    this.ejecutar('Cambiar estado de membresía', () =>
      this.api.cambiarMembresia(id, m.usuarioId, !m.activa)
        .pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: () => {
            this.exito.set('Estado de membresía actualizado.');
            this.confirmarMembresia.set(null);
            this.cargarEmpresa();
          },
          error: err => {
            this.confirmarMembresia.set(null);
            this.fallar('El cambio de membresía', err);
          }
        })
    );
  }

  renombrarEmpresa(): void {
    const id = this.empresaId(), nombre = this.nombreEmpresa().trim();
    if (!id || nombre.length < 2) return;
    this.ejecutar('Actualizar nombre', () =>
      this.api.cambiarNombreEmpresa(id, nombre)
        .pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: () => {
            this.exito.set('Nombre empresarial actualizado.');
            this.actualizarDirectorio.emit();
          },
          error: err => this.fallar('La edición empresarial', err)
        })
    );
  }

  pedirConfirmacionEstado(): void {
    this.confirmarEstado.set(!this.empresaActiva());
  }

  actualizarEstadoEmpresa(): void {
    const id = this.empresaId(), activa = this.confirmarEstado();
    if (!id || activa === null) return;
    this.ejecutar('Cambiar estado empresarial', () =>
      this.api.cambiarEstadoEmpresa(id, activa)
        .pipe(finalize(() => this.ocupado.set(false)))
        .subscribe({
          next: () => {
            this.empresaActiva.set(activa);
            this.confirmarEstado.set(null);
            this.exito.set('Estado de empresa actualizado.');
            this.actualizarDirectorio.emit();
          },
          error: err => {
            this.confirmarEstado.set(null);
            this.fallar('El cambio de estado', err);
          }
        })
    );
  }
}
