
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  signal,
  ChangeDetectionStrategy
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CodigoScannerDialogComponent } from '../codigo-scanner-dialog/codigo-scanner-dialog.component';

@Component({
  selector: 'app-codigo-scanner-input',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
],
  templateUrl: './codigo-scanner-input.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './codigo-scanner-input.component.scss'
})
export class CodigoScannerInputComponent implements AfterViewInit {
  @Input() procesando = false;
  @Input() mensaje: string | null = null;
  @Input() error = false;
  @Input() etiqueta = 'Escanear o escribir SKU / código de barras';
  @Output() readonly codigoLeido = new EventEmitter<string>();

  @ViewChild('codigoInput') private codigoInput?: ElementRef<HTMLInputElement>;

  readonly activo = signal(false);
  readonly codigo = new FormControl('', { nonNullable: true });

  constructor(private readonly dialog: MatDialog) {}

  ngAfterViewInit(): void {
    if (this.activo()) this.enfocar();
  }

  alternarModo(): void {
    this.activo.update((valor) => !valor);
    this.codigo.setValue('', { emitEvent: false });
    if (this.activo()) queueMicrotask(() => this.enfocar());
  }

  abrirCamara(): void {
    const referencia = this.dialog.open(CodigoScannerDialogComponent, {
      width: 'min(94vw, 680px)',
      maxWidth: '680px',
      disableClose: true,
      autoFocus: false,
      restoreFocus: true
    });

    referencia.afterClosed().subscribe((codigo: string | undefined) => {
      const normalizado = codigo?.trim();
      if (normalizado) this.codigoLeido.emit(normalizado);
    });
  }

  procesar(): void {
    if (!this.activo() || this.procesando) return;
    const valor = this.codigo.value.trim();
    if (!valor) {
      this.enfocar();
      return;
    }

    this.codigoLeido.emit(valor);
    this.codigo.setValue('', { emitEvent: false });
  }

  reenfocar(): void {
    if (this.activo()) queueMicrotask(() => this.enfocar());
  }

  private enfocar(): void {
    this.codigoInput?.nativeElement.focus({ preventScroll: true });
  }
}
