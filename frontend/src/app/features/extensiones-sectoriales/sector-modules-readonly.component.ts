import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

interface SectorCapabilityPreview {
  readonly key: 'reservas' | 'membresias' | 'catalogo' | 'sitios';
  readonly title: string;
  readonly purpose: string;
}

/** C08-04.M04.01: protected, static read-only surface, never a permission grant. */
@Component({
  selector: 'app-sector-modules-readonly',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sector-modules-readonly.component.html',
  styleUrl: './sector-modules-readonly.component.scss'
})
export class SectorModulesReadonlyComponent {
  readonly capabilities = signal<readonly SectorCapabilityPreview[]>([
    { key: 'reservas', title: 'Reservas', purpose: 'Requiere recursos y sucursales verificados por el servidor.' },
    { key: 'membresias', title: 'Membresías', purpose: 'No sustituye la pertenencia de usuario a la empresa ni concede roles.' },
    { key: 'catalogo', title: 'Catálogo', purpose: 'Preserva la propiedad de productos y no crea entidades duplicadas.' },
    { key: 'sitios', title: 'Sitios por rubro', purpose: 'Un host o una URL nunca acredita permisos privados.' }
  ]);
}
