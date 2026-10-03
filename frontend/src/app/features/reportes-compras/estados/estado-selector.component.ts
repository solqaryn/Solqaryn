import { Component, EventEmitter, Input, Output, OnInit, OnChanges, SimpleChanges, ChangeDetectionStrategy } from '@angular/core';

import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

export type TipoEstadoCompra = 'EstadoOrden' | 'EstadoFactura' | 'EstadoRecepcion' | 'EstadoDevolucion';

@Component({
  selector: 'app-estado-selector',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatSelectModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <mat-form-field appearance="outline" class="w-full">
      <mat-label>{{ label }}</mat-label>
      <mat-select [formControl]="control" (selectionChange)="onSelectionChange($event.value)">
        <mat-option [value]="null">Todos</mat-option>
        @for (estado of opciones; track estado) {
          <mat-option [value]="estado.value">
            {{ estado.label }}
          </mat-option>
        }
      </mat-select>
    </mat-form-field>
    `
})
export class EstadoSelectorComponent implements OnInit, OnChanges {
  @Input() tipoEstado: TipoEstadoCompra | null = null;
  @Input() label: string = 'Estado';
  @Input() control: FormControl = new FormControl(null);
  
  @Output() selectionChange = new EventEmitter<any>();

  opciones: { label: string, value: any }[] = [];

  ngOnInit() {
    this.cargarOpciones();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['tipoEstado']) {
      this.cargarOpciones();
    }
  }

  private cargarOpciones() {
    if (this.tipoEstado === 'EstadoOrden') {
      this.opciones = [
        { label: 'Borrador', value: 1 },
        { label: 'Pendiente Aprobación', value: 2 },
        { label: 'Aprobada', value: 3 },
        { label: 'Cancelada', value: 4 },
      ];
    } else if (this.tipoEstado === 'EstadoFactura') {
      this.opciones = [
        { label: 'Borrador', value: 1 },
        { label: 'Registrada', value: 2 },
        { label: 'Anulada', value: 3 },
      ];
    } else if (this.tipoEstado === 'EstadoRecepcion') {
      this.opciones = [
        { label: 'Borrador', value: 1 },
        { label: 'Recibida', value: 2 },
        { label: 'Anulada', value: 3 },
      ];
    } else if (this.tipoEstado === 'EstadoDevolucion') {
      this.opciones = [
        { label: 'Borrador', value: 1 },
        { label: 'Confirmada', value: 2 },
        { label: 'Anulada', value: 3 },
      ];
    } else {
        this.opciones = [];
    }
  }

  onSelectionChange(value: any) {
    this.selectionChange.emit(value);
  }
}
