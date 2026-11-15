import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TenantContextService } from '../../core/auth/tenant-context.service';

/** C01-01.M04.01: superficie read-only dentro de /usuarios con guards existentes. */
@Component({
  selector: 'app-identidad-sesion-verificada',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './identidad-sesion-verificada.component.html',
  styleUrl: './identidad-sesion-verificada.component.scss'
})
export class IdentidadSesionVerificadaComponent {
  readonly tenant = inject(TenantContextService);
}
