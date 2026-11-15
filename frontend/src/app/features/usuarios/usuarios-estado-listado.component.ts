import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Estados del listado en la ruta protegida /usuarios; no concede permisos. */
@Component({
  selector: 'app-usuarios-estado-listado',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './usuarios-estado-listado.component.html',
  styleUrl: './usuarios-estado-listado.component.scss'
})
export class UsuariosEstadoListadoComponent {
  readonly cargando = input.required<boolean>();
  readonly tieneError = input.required<boolean>();
  readonly cantidad = input.required<number>();
  readonly puedeCrear = input.required<boolean>();
}
