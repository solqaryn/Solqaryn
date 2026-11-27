import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { Component, OnInit, OnDestroy, DestroyRef, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { catchError, forkJoin, of, switchMap, tap, throwError } from 'rxjs';
import { CategoriaService } from '../../services/categoria.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { MAX_MB_FOTO_CATEGORIA, MAX_BYTES_FOTO_OPTIMIZADA_CATEGORIA, esFotoCategoriaValida } from './categoria-galeria-limites';

interface ImagenCategoriaPreview {
  id?: string;
  url: string;
  archivo?: File;
}

@Component({
  selector: 'app-categoria-form',
  standalone: true,
  imports: [MatDialogModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  templateUrl: './categoria-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './categoria-form.component.scss'
})
export class CategoriaFormComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly tenant = inject(TenantContextService);
  readonly modalRef = inject(MatDialogRef<CategoriaFormComponent>, { optional: true });
  private readonly modalData = inject<{ id?: number }>(MAT_DIALOG_DATA, { optional: true });
  private readonly alerts = inject(AppAlertService);
  private readonly contextoModal = this.tenant.contextoVerificado();
  cancelarModal(): void { if (!this.saving()) this.modalRef?.close(false); }
  private readonly destroyRef = inject(DestroyRef);
  private empresaId: number | null = null;
  private destruido = false;
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly loading = signal(false);
  readonly preparando = signal(false);
  readonly galeriaDisponible = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly errorGaleria = signal<string | null>(null);
  readonly imagenes = signal<ImagenCategoriaPreview[]>([]);
  readonly maxImagenes = 5;
  readonly maxMbPorFoto = MAX_MB_FOTO_CATEGORIA;
  private categoriaId: number | null = null;
  private activa = true;
  private version = '';
  private galeriaModificada = false;

  form = this.fb.group({
    nombre: ['', Validators.required],
    descripcion: ['']
  });

  constructor(
    private categoriaService: CategoriaService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    if (this.modalRef) effect(() => { if (!this.contextoModal || this.tenant.contextoVerificado() !== this.contextoModal) this.modalRef?.close(false); });
  }

  ngOnInit(): void {
    if (this.modalRef && (!this.contextoModal || !this.permisos.puede('Categorias', this.modalData?.id ? 'Editar' : 'Crear'))) { this.modalRef.close(false); return; }
    this.empresaId = this.tenant.empresaIdVerificada();
    const idParam = this.modalRef ? this.modalData?.id : this.route.snapshot.paramMap.get('id');
    const permiteGaleria = this.permisos.puede('Categorias', 'Editar');
    this.galeriaDisponible.set(permiteGaleria);
    if (!idParam) return;
    this.isEdit.set(true);
    this.categoriaId = Number(idParam);
    this.loading.set(true);
    const galeria$ = permiteGaleria
      ? this.categoriaService.obtenerGaleria(this.categoriaId).pipe(catchError(() => {
          this.galeriaDisponible.set(false);
          this.errorGaleria.set('No pudimos cargar la galería. Puedes editar los datos sin modificar las fotografías.');
          return of(null);
        }))
      : of(null);
    forkJoin({ categoria: this.categoriaService.getById(this.categoriaId), galeria: galeria$ })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ categoria, galeria }) => {
          this.loading.set(false);
          if (!this.contextoValido()) return;
          this.activa = categoria.data.activa;
          this.form.patchValue({ nombre: categoria.data.nombre, descripcion: categoria.data.descripcion });
          if (galeria?.data) {
            this.version = galeria.data.version;
            this.imagenes.set(galeria.data.imagenes.map(imagen => ({ id: imagen.id, url: imagen.url })));
          }
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set('No pudimos cargar la categoría. Vuelve al listado e intenta nuevamente.');
          this.form.disable();
        }
      });
  }

  private contextoValido(): boolean {
    if (!this.empresaId || this.tenant.empresaIdVerificada() !== this.empresaId) {
      this.errorMessage.set('La empresa activa cambió. Vuelve a abrir el formulario.');
      return false;
    }
    return true;
  }

  async seleccionarImagenes(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivos = Array.from(input.files ?? []);
    input.value = '';
    if (!archivos.length || this.saving() || this.preparando() || !this.galeriaDisponible() || !this.contextoValido()) return;
    if (this.imagenes().length + archivos.length > this.maxImagenes) {
      this.errorGaleria.set('Puedes guardar hasta cinco imágenes por categoría.');
      return;
    }
    this.errorGaleria.set(null);
    this.preparando.set(true);
    const nuevas: ImagenCategoriaPreview[] = [];
    try {
      for (const archivo of archivos) {
        if (!esFotoCategoriaValida(archivo)) {
          throw new Error(`Selecciona imágenes JPG, PNG o WebP de hasta ${this.maxMbPorFoto} MB cada una.`);
        }
        const optimizada = await this.optimizar(archivo);
        if (this.destruido) return;
        nuevas.push({ archivo: optimizada, url: URL.createObjectURL(optimizada) });
      }
      if (!this.contextoValido()) return;
      this.imagenes.update(actuales => [...actuales, ...nuevas]);
      this.galeriaModificada = true;
      nuevas.length = 0;
    } catch (error) {
      this.errorGaleria.set(error instanceof Error ? error.message : 'No pudimos preparar las imágenes.');
    } finally {
      nuevas.forEach(imagen => URL.revokeObjectURL(imagen.url));
      this.preparando.set(false);
    }
  }

  private async optimizar(archivo: File): Promise<File> {
    const limite = MAX_BYTES_FOTO_OPTIMIZADA_CATEGORIA;
    if (archivo.size <= limite) return archivo;
    const bitmap = await createImageBitmap(archivo);
    try {
      const escala = Math.min(1, 800 / Math.max(bitmap.width, bitmap.height));
      const canvas = document.createElement('canvas');
      canvas.width = Math.max(1, Math.round(bitmap.width * escala));
      canvas.height = Math.max(1, Math.round(bitmap.height * escala));
      const contexto = canvas.getContext('2d');
      if (!contexto) throw new Error('No pudimos preparar la imagen.');
      contexto.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
      const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/webp', .82));
      if (!blob || blob.type !== 'image/webp' || blob.size > limite) {
        throw new Error('No pudimos optimizar la imagen. Selecciona una fotografía más pequeña.');
      }
      return new File([blob], archivo.name.replace(/\.[^.]+$/, '') + '.webp', { type: 'image/webp' });
    } finally {
      bitmap.close();
    }
  }

  portada(indice: number): void {
    if (this.saving() || this.preparando() || indice <= 0 || indice >= this.imagenes().length) return;
    const imagenes = [...this.imagenes()];
    imagenes.unshift(imagenes.splice(indice, 1)[0]);
    this.imagenes.set(imagenes);
    this.galeriaModificada = true;
    this.errorGaleria.set(null);
  }

  quitar(indice: number): void {
    if (this.saving() || this.preparando() || indice < 0 || indice >= this.imagenes().length) return;
    const imagenes = [...this.imagenes()];
    const imagen = imagenes.splice(indice, 1)[0];
    if (imagen.archivo) URL.revokeObjectURL(imagen.url);
    this.imagenes.set(imagenes);
    this.galeriaModificada = true;
    this.errorGaleria.set(null);
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.form.disabled || this.loading() || this.saving() || this.preparando() || !this.contextoValido()) return;
    this.saving.set(true);
    const contexto = this.tenant.contextoVerificado();
    const confirmado = await this.alerts.confirmar({ titulo: this.isEdit() ? 'Confirmar actualización' : 'Confirmar creación', mensaje: '¿Deseas guardar los datos y fotografías de la categoría?', confirmarTexto: 'Guardar' }).catch(() => false);
    if (!confirmado || this.destruido || !contexto || contexto !== this.tenant.contextoVerificado() || !this.contextoValido() || !this.permisos.puede('Categorias', this.isEdit() ? 'Editar' : 'Crear')) { this.saving.set(false); return; }
    this.errorMessage.set(null);
    this.errorGaleria.set(null);
    const value = { nombre: this.form.value.nombre!, descripcion: this.form.value.descripcion || undefined, activa: this.activa };
    const request$ = this.isEdit()
      ? this.categoriaService.update(this.categoriaId!, value)
      : this.categoriaService.create(value);
    let datosGuardados = false;
    request$.pipe(
      tap(res => {
        this.categoriaId = res.data.id;
        this.isEdit.set(true);
        datosGuardados = true;
      }),
      switchMap(() => {
        if (!this.contextoValido()) return throwError(() => new Error('La empresa activa cambió.'));
        if (!this.galeriaModificada) return of(null);
        const archivos: File[] = [];
        const orden = this.imagenes().map(imagen => imagen.archivo
          ? 'n:' + (archivos.push(imagen.archivo) - 1)
          : 'e:' + imagen.id);
        return this.categoriaService.guardarGaleria(this.categoriaId!, this.version, orden, archivos);
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: () => {
        this.saving.set(false);
        if (this.modalRef) this.modalRef.close(true);
        else void this.router.navigate(['/categorias']);
      },
      error: err => {
        this.saving.set(false);
        const mensaje = err.error?.message ?? err.message ?? 'Intenta nuevamente.';
        this.errorMessage.set(datosGuardados
          ? 'Los datos de la categoría se guardaron; las imágenes siguen pendientes. ' + mensaje
          : 'No se pudo guardar la categoría. ' + mensaje);
      }
    });
  }

  ngOnDestroy(): void {
    this.destruido = true;
    this.imagenes().forEach(imagen => { if (imagen.archivo) URL.revokeObjectURL(imagen.url); });
  }
}
