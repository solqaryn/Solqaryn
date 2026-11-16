import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

export type EstadoPresentacionSectorial = 'cargando' | 'vacio' | 'error' | 'exito' | 'sin_permiso';

interface SectorCapabilityPreview {
  readonly key: 'reservas' | 'membresias' | 'catalogo' | 'sitios';
  readonly title: string;
  readonly purpose: string;
}

/**
 * C08-04.M04.01-.02: private, read-only sector preview with safe presentation states.
 * This signal is NOT server authorization or an authoritative capability feed.
 * No HTTP call, module activation, persisted selection or role grant is performed.
 */
@Component({
  selector: 'app-sector-modules-readonly',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sector-modules-readonly.component.html',
  styleUrl: './sector-modules-readonly.component.scss'
})
export class SectorModulesReadonlyComponent {
  // Future trusted integration must derive this from backend-verified results;
  // until then the only runtime entry state is the truthful empty preview.
  readonly estadoPresentacion = signal<EstadoPresentacionSectorial>('vacio');

  readonly capabilities = signal<readonly SectorCapabilityPreview[]>([
    { key: 'reservas', title: 'Reservas', purpose: 'Requiere recursos y sucursales verificados por el servidor.' },
    { key: 'membresias', title: 'Membresías', purpose: 'No sustituye la pertenencia de usuario a la empresa ni concede roles.' },
    { key: 'catalogo', title: 'Catálogo', purpose: 'Preserva la propiedad de productos y no crea entidades duplicadas.' },
    { key: 'sitios', title: 'Sitios por rubro', purpose: 'Un host o una URL nunca acredita permisos privados.' }
  ]);
}
