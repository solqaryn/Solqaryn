import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { modalInstitucional } from '../../shared/modal-institucional/modal-institucional';
import { crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';
import { TenantContextoVerificado } from '../../core/auth/tenant-context.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, OnInit, inject, signal, computed, ChangeDetectionStrategy, TemplateRef, ViewChild, effect, DestroyRef } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, FormControl, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule, MatSelectChange } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsuarioService, UsuarioEmpresa } from '../../services/usuario.service';
import { RolService } from '../../services/rol.service';
import { EmpresaService, EmpresaResumen } from '../../services/empresa.service';
import { Usuario } from '../../core/models/usuario.model';
import { Rol } from '../../core/models/rol.model';
import { PagedResult } from '../../core/models/api-response.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AuthService } from '../../core/auth/auth.service';

const USUARIO_VALIDO = /^[a-zA-Z0-9._-]{3,50}$/;
const PASSWORD_SEGURA = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{10,128}$/;

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule
],
  templateUrl: './usuarios.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './usuarios.component.scss'
})
export class UsuariosComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly acciones = crearAccionesInstitucionales('Usuarios');
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  @ViewChild('crearUsuarioModal') private crearUsuarioModal?: TemplateRef<unknown>;
  @ViewChild('gestionEmpresasModal') private gestionEmpresasModal?: TemplateRef<unknown>;
  private crearRef?: MatDialogRef<unknown>;
  private empresasRef?: MatDialogRef<unknown>;
  private contextoEditor: TenantContextoVerificado | null = null;
  private contextoEmpresas: TenantContextoVerificado | null = null;
  readonly cambiandoEstado = signal(false);
  readonly estadoFiltro = signal<'todos' | 'activos' | 'inactivos' | 'bloqueados'>('todos');
  readonly usuariosVisibles = computed(() => this.usuarios().filter(u => this.estadoFiltro() === 'todos' ||
    (this.estadoFiltro() === 'bloqueados' ? u.bloqueado : u.activo === (this.estadoFiltro() === 'activos'))));
  limpiarFiltros(): void { this.estadoFiltro.set('todos'); this.buscador.setValue(''); }

  abrirCreacion(): void {
    const contexto = this.acciones.contexto();
    if (!this.crearUsuarioModal || this.crearRef || !this.acciones.vigente(contexto, 'Crear') || !this.permisosRuntime.puede('Usuarios', 'AsignarRol')) return;
    this.contextoEditor = contexto;
    this.form.reset();
    this.errorMessage.set(null);
    this.mostrarFormulario.set(true);
    const ref = this.dialog.open(this.crearUsuarioModal, modalInstitucional('l', { ariaLabel: 'Nuevo usuario' }));
    this.crearRef = ref;
    ref.afterClosed().subscribe(() => { if (this.crearRef !== ref) return; this.crearRef = undefined; this.mostrarFormulario.set(false); this.form.reset(); });
  }

  cancelarCreacion(): void { if (!this.saving()) this.crearRef?.close(); }

  async abrirUsuario(id: number, modo: 'Ver' | 'Editar'): Promise<void> {
    if (modo === 'Ver') {
      await this.acciones.abrir(() => import('./usuario-detail.component').then(m => m.UsuarioDetailComponent), 'm', modo, id);
    } else if (await this.acciones.abrir(() => import('./usuario-form.component').then(m => m.UsuarioFormComponent), 'l', modo, id)) this.cargar();
  }


  readonly usuarios = signal<Usuario[]>([]);
  readonly roles = signal<Rol[]>([]);
  readonly empresas = signal<EmpresaResumen[]>([]);
  readonly membresias = signal<UsuarioEmpresa[]>([]);
  readonly usuarioGestionado = signal<Usuario | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly loadingMembresias = signal(false);
  readonly savingMembresia = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly mensajeMembresias = signal<string | null>(null);
  readonly mostrarFormulario = signal(false);
  readonly buscador = new FormControl('');

  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeAsignarRol = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeVerEmpresas = signal(false);
  readonly puedeVerRoles = signal(false);

  readonly form = this.fb.group({
    nombreUsuario: ['', [Validators.required, Validators.pattern(USUARIO_VALIDO)]],
    nombreCompleto: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    password: ['', [Validators.required, Validators.pattern(PASSWORD_SEGURA)]],
    rolId: [null as number | null, Validators.required]
  });

  readonly empresaForm = this.fb.group({
    empresaId: [null as number | null, Validators.required],
    rolId: [null as number | null, Validators.required]
  });

  constructor(
    private usuarioService: UsuarioService,
    private rolService: RolService,
    private empresaService: EmpresaService,
    private permisosRuntime: PermisosRuntimeService,
    private auth: AuthService,
    private snackBar: MatSnackBar
  ) {
    effect(() => {
      const contexto = this.acciones.contexto();
      if (this.mostrarFormulario() && contexto !== this.contextoEditor) this.crearRef?.close();
      if (this.usuarioGestionado() && contexto !== this.contextoEmpresas) this.empresasRef?.close();
    });
  }

  ngOnInit(): void {
    this.puedeAsignarRol.set(this.permisosRuntime.puede('Usuarios', 'AsignarRol'));
    this.puedeCrear.set(
      this.permisosRuntime.puede('Usuarios', 'Crear') && this.puedeAsignarRol()
    );
    this.puedeEditar.set(this.permisosRuntime.puede('Usuarios', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Usuarios', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Usuarios', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Usuarios', 'EliminarLogico'));
    this.puedeVerEmpresas.set(this.permisosRuntime.puede('Configuracion', 'Ver'));
    this.puedeVerRoles.set(this.permisosRuntime.puede('Roles', 'Ver'));

    this.cargar();

    if (this.puedeCrear()) {
      this.cargarRoles();
    }

    this.buscador.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.cargar());
  }

  esUsuarioActual(u: Usuario): boolean {
    return this.auth.nombreUsuario()?.toLowerCase() === u.nombreUsuario.toLowerCase();
  }

  puedeCambiarEstado(usuario: Usuario): boolean {
    if (this.esUsuarioActual(usuario)) return false;
    return usuario.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  puedeBloquearUsuario(usuario: Usuario): boolean {
    if (this.esUsuarioActual(usuario)) return false;
    return usuario.bloqueado ? this.puedeActivar() : this.puedeDesactivar();
  }

  puedeGestionarEmpresas(): boolean {
    return this.puedeAsignarRol();
  }

  puedeCambiarEstadoMembresia(membresia: UsuarioEmpresa): boolean {
    return membresia.activa ? this.puedeDesactivar() : this.puedeActivar();
  }

  cargar(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.usuarioService.getPaged({ page: 1, pageSize: 100, search: this.buscador.value || undefined }).subscribe({
      next: (res) => {
        const usuarios = this.extraerUsuarios(res.data);
        if (usuarios.length > 0 || this.buscador.value) {
          this.usuarios.set(usuarios);
          this.loading.set(false);
          return;
        }
        this.cargarListadoLegado();
      },
      error: () => this.cargarListadoLegado()
    });
  }

  private cargarListadoLegado(): void {
    this.usuarioService.getAll().subscribe({
      next: (res) => {
        const termino = (this.buscador.value || '').trim().toLowerCase();
        const usuarios = this.extraerUsuarios(res.data).filter((u) => {
          if (!termino) return true;
          return u.nombreUsuario.toLowerCase().includes(termino) || u.nombreCompleto.toLowerCase().includes(termino);
        });

        this.usuarios.set(usuarios);
        this.loading.set(false);
      },
      error: (err) => {
        this.usuarios.set([]);
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar la lista de usuarios.');
        this.snackBar.open(this.errorMessage()!, 'Cerrar', { duration: 6000 });
      }
    });
  }

  private extraerUsuarios(data: PagedResult<Usuario> | Usuario[] | null | undefined): Usuario[] {
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.items)) return data.items;
    return [];
  }

  private cargarRoles(): void {
    this.rolService.getAll().subscribe({
      next: (res) => this.roles.set(res.data.filter((r) => r.activo)),
      error: () => this.roles.set([])
    });
  }

  private cargarEmpresas(): void {
    if (!this.puedeVerEmpresas()) return;
    this.empresaService.getAll().subscribe({
      next: (res) => this.empresas.set(res.data),
      error: () => {
        this.empresas.set([]);
        this.mensajeMembresias.set('No se pudo cargar el catálogo de empresas.');
      }
    });
  }

  abrirGestionEmpresas(usuario: Usuario): void {
    const contexto = this.acciones.contexto();
    if (!this.gestionEmpresasModal || this.empresasRef || !this.puedeGestionarEmpresas() || !this.acciones.vigente(contexto, 'AsignarRol')) return;
    this.contextoEmpresas = contexto;
    this.usuarioGestionado.set(usuario);
    const ref = this.dialog.open(this.gestionEmpresasModal, modalInstitucional('xl', { ariaLabel: 'Empresas y roles del usuario' }));
    this.empresasRef = ref;
    ref.afterClosed().subscribe(() => {
      if (this.empresasRef !== ref) return;
      this.empresasRef = undefined;
      this.usuarioGestionado.set(null); this.membresias.set([]); this.empresaForm.reset(); this.mensajeMembresias.set(null);
    });
    this.empresaForm.reset();
    this.mensajeMembresias.set(null);

    if (this.puedeVerEmpresas() && this.empresas().length === 0) {
      this.cargarEmpresas();
    }
    if (this.puedeVerRoles() && this.roles().length === 0) {
      this.cargarRoles();
    }

    this.cargarMembresias(usuario.id);
  }

  cerrarGestionEmpresas(): void { if (!this.savingMembresia()) this.empresasRef?.close(); }

  private cargarMembresias(usuarioId: number): void {
    this.loadingMembresias.set(true);
    this.usuarioService.getEmpresas(usuarioId).subscribe({
      next: (res) => {
        if (this.usuarioGestionado()?.id !== usuarioId || !this.acciones.vigente(this.contextoEmpresas, 'AsignarRol')) return;
        this.membresias.set(res.data ?? []);
        this.loadingMembresias.set(false);
      },
      error: (err) => {
        this.membresias.set([]);
        this.loadingMembresias.set(false);
        this.mensajeMembresias.set(err.error?.message ?? 'No se pudieron cargar las empresas del usuario.');
      }
    });
  }

  empresaYaAsignada(empresaId: number): boolean {
    return this.membresias().some((m) => m.empresaId === empresaId);
  }

  nombreEmpresa(empresaId: number): string {
    return this.empresas().find((e) => e.id === empresaId)?.nombre ?? `Empresa #${empresaId}`;
  }

  nombreRol(rolId: number): string {
    return this.roles().find((r) => r.id === rolId)?.nombre ?? `Rol #${rolId}`;
  }

  async asignarEmpresa(): Promise<void> {
    const usuario = this.usuarioGestionado();
    if (this.savingMembresia() || !usuario || !this.puedeGestionarEmpresas() || !this.puedeVerEmpresas() || !this.puedeVerRoles()) return;
    if (this.empresaForm.invalid) {
      this.empresaForm.markAllAsTouched();
      return;
    }

    const { empresaId, rolId } = this.empresaForm.getRawValue();
    if (empresaId === null || rolId === null || this.empresaYaAsignada(empresaId)) return;

    this.savingMembresia.set(true);
    if (!await this.acciones.confirmar('AsignarRol', { titulo: 'Asignar empresa', mensaje: '¿Deseas asignar la empresa y el rol seleccionados a este usuario?', confirmarTexto: 'Asignar' }) || !this.acciones.vigente(this.contextoEmpresas, 'AsignarRol')) { this.savingMembresia.set(false); return; }
    this.mensajeMembresias.set(null);
    this.usuarioService.asignarEmpresa(usuario.id, empresaId, rolId).subscribe({
      next: () => {
        this.savingMembresia.set(false);
        this.empresaForm.reset();
        this.mensajeMembresias.set('Empresa asignada correctamente.');
        this.cargarMembresias(usuario.id);
      },
      error: (err) => {
        this.savingMembresia.set(false);
        this.mensajeMembresias.set(err.error?.message ?? 'No se pudo asignar la empresa.');
      }
    });
  }

  async cambiarRolMembresia(membresia: UsuarioEmpresa, evento: MatSelectChange): Promise<void> {
    const rolId = Number(evento.value);
    evento.source.value = membresia.rolId;
    const usuario = this.usuarioGestionado();
    if (!usuario || this.savingMembresia() || !this.puedeAsignarRol() || rolId === membresia.rolId) return;

    this.savingMembresia.set(true);
    if (!await this.acciones.confirmar('AsignarRol', { titulo: 'Confirmar cambio de rol', mensaje: '¿Deseas asignar «' + this.nombreRol(rolId) + '» en esta empresa?', confirmarTexto: 'Cambiar rol' }) || !this.acciones.vigente(this.contextoEmpresas, 'AsignarRol')) { this.savingMembresia.set(false); return; }
    this.mensajeMembresias.set(null);
    this.usuarioService.cambiarRolEmpresa(usuario.id, membresia.empresaId, rolId).subscribe({
      next: () => {
        this.savingMembresia.set(false);
        this.mensajeMembresias.set('Rol empresarial actualizado correctamente.');
        this.cargarMembresias(usuario.id);
      },
      error: (err) => {
        this.savingMembresia.set(false);
        this.mensajeMembresias.set(err.error?.message ?? 'No se pudo actualizar el rol empresarial.');
        this.cargarMembresias(usuario.id);
      }
    });
  }

  async toggleMembresia(membresia: UsuarioEmpresa): Promise<void> {
    const usuario = this.usuarioGestionado();
    if (!usuario || this.savingMembresia() || !this.puedeCambiarEstadoMembresia(membresia)) return;

    const activar = !membresia.activa;
    this.savingMembresia.set(true);
    const confirmado = await this.acciones.confirmar(activar ? 'Activar' : 'Desactivar', {
      titulo: activar ? 'Restaurar empresa' : 'Quitar empresa',
      mensaje: `${activar ? 'Se restaurará' : 'Se quitará'} ${this.nombreEmpresa(membresia.empresaId)} para ${usuario.nombreCompleto}.`,
      detalle: activar ? 'La membresía volverá a estar activa.' : 'La membresía quedará inactiva y podrá restaurarse después.',
      tipo: activar ? 'info' : 'advertencia',
      confirmarTexto: activar ? 'Restaurar' : 'Quitar'
    });
    if (!confirmado || !this.acciones.vigente(this.contextoEmpresas, 'AsignarRol')) { this.savingMembresia.set(false); return; }
    this.mensajeMembresias.set(null);
    this.usuarioService.cambiarEstadoEmpresa(usuario.id, membresia.empresaId, activar).subscribe({
      next: () => {
        this.savingMembresia.set(false);
        this.mensajeMembresias.set(activar ? 'Membresía restaurada.' : 'Empresa quitada del usuario.');
        this.cargarMembresias(usuario.id);
      },
      error: (err) => {
        this.savingMembresia.set(false);
        this.mensajeMembresias.set(err.error?.message ?? 'No se pudo cambiar el estado de la membresía.');
      }
    });
  }

  async crear(): Promise<void> {
    if (this.saving() || !this.puedeCrear() || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    const valor = this.form.getRawValue();
    if (!await this.acciones.confirmar('Crear', { titulo: 'Confirmar nuevo usuario', mensaje: '¿Deseas crear este usuario con el rol seleccionado?', confirmarTexto: 'Crear usuario' }) || !this.acciones.vigente(this.contextoEditor, 'Crear') || !this.permisosRuntime.puede('Usuarios', 'AsignarRol')) { this.saving.set(false); return; }
    this.usuarioService.create({
      nombreUsuario: valor.nombreUsuario!.trim(),
      nombreCompleto: valor.nombreCompleto!.trim(),
      password: valor.password!,
      rol: 'Vendedor',
      rolId: valor.rolId!
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.crearRef?.close(true);
        this.mostrarFormulario.set(false);
        this.form.reset();
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo crear el usuario.');
      }
    });
  }

  async toggleEstado(usuario: Usuario, evento: MatSlideToggleChange): Promise<void> {
    evento.source.checked = usuario.activo;
    if (!this.puedeCambiarEstado(usuario) || this.cambiandoEstado()) return;
    this.cambiandoEstado.set(true);

    const activar = !usuario.activo;
    const confirmado = await this.acciones.confirmar(activar ? 'Activar' : 'Desactivar', {
      titulo: activar ? 'Activar usuario' : 'Desactivar usuario',
      mensaje: `Se ${activar ? 'habilitará' : 'deshabilitará'} el acceso de "${usuario.nombreCompleto}".`,
      detalle: activar ? 'Podrá ingresar según los permisos de su rol.' : 'No podrá iniciar nuevas sesiones.',
      tipo: activar ? 'info' : 'advertencia',
      confirmarTexto: activar ? 'Activar' : 'Desactivar'
    });
    if (!confirmado || this.esUsuarioActual(usuario)) { this.cambiandoEstado.set(false); return; }

    this.usuarioService.updateEstado(usuario.id, activar).subscribe({
      next: () => { this.cambiandoEstado.set(false); this.cargar(); },
      error: (err) => { this.cambiandoEstado.set(false); this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 }); }
    });
  }

  async bloquear(usuario: Usuario): Promise<void> {
    if (!this.puedeBloquearUsuario(usuario) || usuario.bloqueado) return;

    const motivo = await this.acciones.solicitarTexto('Desactivar', {
      titulo: 'Bloquear usuario',
      mensaje: `Indica por qué se bloqueará a "${usuario.nombreCompleto}".`,
      tipo: 'advertencia',
      confirmarTexto: 'Bloquear',
      entrada: { etiqueta: 'Motivo del bloqueo', requerida: true }
    });
    if (!motivo) return;

    this.usuarioService.bloquear(usuario.id, motivo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo bloquear al usuario.', 'Cerrar', { duration: 5000 })
    });
  }

  async desbloquear(usuario: Usuario): Promise<void> {
    if (!this.puedeBloquearUsuario(usuario) || !usuario.bloqueado) return;

    const confirmado = await this.acciones.confirmar('Activar', {
      titulo: 'Desbloquear usuario',
      mensaje: `Se restablecerá el acceso de "${usuario.nombreCompleto}".`,
      confirmarTexto: 'Desbloquear'
    });
    if (!confirmado) return;

    this.usuarioService.desbloquear(usuario.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo desbloquear al usuario.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(usuario: Usuario): Promise<void> {
    if (!this.puedeEliminar() || this.esUsuarioActual(usuario)) return;

    const confirmado = await this.acciones.confirmar('EliminarLogico', {
      titulo: 'Eliminar usuario',
      mensaje: `Se eliminará lógicamente a "${usuario.nombreCompleto}".`,
      detalle: 'Sus registros históricos se conservarán.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar usuario'
    });
    if (!confirmado) return;

    this.usuarioService.eliminar(usuario.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar al usuario.', 'Cerrar', { duration: 5000 })
    });
  }
}
