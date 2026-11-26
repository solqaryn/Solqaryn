import { Component, Directive, DestroyRef, Injector, ChangeDetectionStrategy, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { CategoriaService } from '../../services/categoria.service';
import { CategoriaImagen } from '../../core/models/categoria.model';
import { modalInstitucional } from '../modal-institucional/modal-institucional';

export type ValorConsulta = string | number | boolean | null | undefined | readonly (string | number)[];
export interface ConsultaRegistro {
  titulo: string;
  campos: Readonly<Record<string, ValorConsulta>>;
  categoriaId?: number;
}

export function mostrarValorConsulta(valor: ValorConsulta): string {
  if (valor == null || valor === '') return '—';
  if (typeof valor === 'boolean') return valor ? 'Sí' : 'No';
  if (typeof valor === 'object') return valor.length ? valor.join(', ') : 'Ninguno';
  if (typeof valor === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(valor)) {
    const fecha = new Date(valor);
    if (!Number.isNaN(fecha.getTime())) return new Intl.DateTimeFormat('es-HN', { dateStyle: 'medium', timeStyle: 'short' }).format(fecha);
  }
  return String(valor);
}

@Component({
  selector: 'app-consulta-registro-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.titulo }}</h2>
    <mat-dialog-content>
      <p class="consulta-nota"><mat-icon aria-hidden="true">visibility</mat-icon> Información de solo lectura</p>
      <dl class="consulta-campos">
        @for (campo of campos; track campo[0]) {
          <div><dt>{{ campo[0] }}</dt><dd>{{ mostrar(campo[1]) }}</dd></div>
        }
      </dl>
      @if (data.categoriaId) {
        <section class="consulta-galeria" aria-label="Imágenes de la categoría">
          <h3>Imágenes de la categoría</h3>
          @if (cargando()) {
            <div role="status"><mat-spinner diameter="24"></mat-spinner> Cargando imágenes…</div>
          } @else if (errorGaleria()) {
            <p role="alert">No fue posible consultar las imágenes. Cierra y vuelve a abrir el detalle para reintentar.</p>
          } @else {
            <div class="galeria">
              @for (imagen of imagenes(); track imagen.id; let i = $index) {
                <figure><img [src]="imagen.url" [alt]="'Imagen ' + (i + 1) + ' de la categoría'" loading="lazy"><figcaption>{{ i === 0 ? 'Portada' : 'Imagen ' + (i + 1) }}</figcaption></figure>
              } @empty { <p>No hay imágenes registradas.</p> }
            </div>
          }
        </section>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" mat-dialog-close>Cerrar</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .consulta-nota{display:flex;align-items:center;gap:8px;margin:0 0 16px;color:var(--color-text-secondary,inherit)}
    .consulta-nota mat-icon{color:var(--color-primary);flex:none}
    .consulta-campos{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:0 20px;margin:0}
    .consulta-campos div{min-width:0;padding:12px 0;border-bottom:1px solid var(--color-border,rgba(127,127,127,.25))}
    dt{font-size:.8rem;font-weight:500;color:var(--color-text-secondary,inherit);margin:0 0 5px}
    dd{margin:0;font-weight:600;overflow-wrap:anywhere;white-space:pre-wrap}
    .consulta-galeria{margin-top:22px}.consulta-galeria h3{font-size:1rem;margin:0 0 12px}
    .galeria{display:grid;grid-template-columns:repeat(auto-fit,minmax(100px,1fr));gap:12px}
    figure{margin:0;min-width:0}img{display:block;width:100%;aspect-ratio:1;object-fit:contain;border:1px solid var(--color-border,rgba(127,127,127,.25));border-radius:8px}
    figcaption{font-size:.8rem;margin-top:4px;overflow-wrap:anywhere}
    @media(max-width:480px){.consulta-campos{grid-template-columns:1fr}}
  `]
})
export class ConsultaRegistroDialog {
  readonly data = inject<ConsultaRegistro>(MAT_DIALOG_DATA);
  readonly campos = Object.entries(this.data.campos);
  readonly mostrar = mostrarValorConsulta;
  readonly imagenes = signal<CategoriaImagen[]>([]);
  readonly cargando = signal(false);
  readonly errorGaleria = signal(false);
  private readonly dialogRef = inject(MatDialogRef<ConsultaRegistroDialog>);
  private readonly tenant = inject(TenantContextService);
  private readonly categoria = inject(CategoriaService);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    const contexto = this.tenant.contextoVerificado();
    effect(() => {
      const actual = this.tenant.contextoVerificado();
      if (!actual || actual.empresaId !== contexto?.empresaId || actual.usuarioId !== contexto?.usuarioId || actual.rolId !== contexto?.rolId) this.dialogRef.close();
    });
    if (this.data.categoriaId) {
      this.cargando.set(true);
      this.categoria.obtenerGaleria(this.data.categoriaId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => {
          if (res.success && res.data) this.imagenes.set(res.data.imagenes);
          else this.errorGaleria.set(true);
          this.cargando.set(false);
        },
        error: () => { this.errorGaleria.set(true); this.cargando.set(false); }
      });
    }
  }
}

@Directive({
  selector: 'button[appConsultaRegistro]',
  standalone: true,
  host: { '(click)': 'abrir()' }
})
export class ConsultaRegistroDirective {
  readonly appConsultaRegistro = input.required<ConsultaRegistro>();
  private readonly injector = inject(Injector);

  abrir(): void {
    const tenant = this.injector.get(TenantContextService);
    if (!tenant.tieneContextoVerificado()) return;
    const registro = this.appConsultaRegistro();
    this.injector.get(MatDialog).open(ConsultaRegistroDialog, modalInstitucional(registro.categoriaId ? 'l' : 'm', {
      data: { ...registro, campos: { ...registro.campos } },
      autoFocus: 'dialog'
    }));
  }
}
