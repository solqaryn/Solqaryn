import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-store-icon', standalone: true, changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.65" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path [attr.d]="rutas[nombre] || rutas['bolsa']" /></svg>`,
  styles: [':host{display:inline-flex;width:1.3em;height:1.3em;flex-shrink:0;vertical-align:middle}svg{width:100%;height:100%}']
})
export class IconoTiendaComponent {
  @Input() nombre = 'bolsa';
  readonly rutas: Record<string, string> = {
    bolsa: 'M5 7h14l1 14H4L5 7ZM9 8V5a3 3 0 0 1 6 0v3',
    buscar: 'M21 21l-5-5M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0',
    flecha: 'M4 12h16m-6-6 6 6-6 6',
    volver: 'M20 12H4m6-6-6 6 6 6',
    cerrar: 'm6 6 12 12M6 18 18 6',
    mas: 'M12 5v14M5 12h14',
    menos: 'M5 12h14',
    rejilla: 'M3 3h7v7H3ZM14 3h7v7h-7ZM3 14h7v7H3ZM14 14h7v7h-7Z',
    filtro: 'M4 6h16M7 12h10M10 18h4',
    tarjeta: 'M3 5h18v14H3ZM3 10h18M6 15h3',
    chat: 'M21 11a9 9 0 0 1-13 8l-5 2 2-5A9 9 0 1 1 21 11ZM8 10h8M8 14h5',
    check: 'm5 12 4 4L19 6',
    escudo: 'M12 2 3 6v6c0 5 9 10 9 10s9-5 9-10V6l-9-4ZM8 12l3 3 5-6',
    envio: 'M2 6h12v11H2ZM14 10h4l4 4v3h-8M5 17a2 2 0 1 0 0 4 2 2 0 0 0 0-4M18 17a2 2 0 1 0 0 4 2 2 0 0 0 0-4',
    pin: 'M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 1 1 16 0ZM15 10a3 3 0 1 1-6 0 3 3 0 0 1 6 0',
    borrar: 'M3 6h18M9 6V3h6v3M6 6l1 15h10l1-15M10 10v7M14 10v7',
    sol: 'M12 2v2M12 20v2M2 12h2M20 12h2m-3-9-2 2M5 19l2-2M3 3l2 2m14 14-2-2M17 12a5 5 0 1 1-10 0 5 5 0 0 1 10 0'
  };
}

/** Local SVG illustrations for demo inventory and unavailable images. All colors inherit the system theme. */
@Component({
  selector: 'app-store-art', standalone: true, changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg viewBox="0 0 320 230" fill="none" aria-hidden="true" focusable="false">
      <ellipse cx="160" cy="203" rx="108" ry="12" class="shadow" />
      @switch (tipo) {
        @case ('laptop') {
          <rect x="56" y="38" width="208" height="139" rx="11" class="ink" />
          <rect x="64" y="46" width="192" height="121" rx="5" class="screen" />
          <circle cx="199" cy="91" r="38" class="accent" />
          <path d="M64 146 126 84l62 83H64Z" class="screen-soft" />
          <rect x="86" y="71" width="60" height="6" rx="3" class="light" />
          <rect x="86" y="84" width="37" height="4" rx="2" class="light" />
          <path d="M56 176h208l27 18c2 5-5 10-12 10H41c-7 0-14-5-12-10Z" class="metal" />
          <path d="M133 178h54l-5 10h-44Z" class="ink-soft" />
          <path d="M36 196h248" class="line" />
        }
        @case ('audio') {
          <path d="M81 137V100a79 79 0 0 1 158 0v37" class="wide-line" />
          <path d="M94 99a66 66 0 0 1 132 0" class="accent-line" />
          <rect x="69" y="110" width="49" height="82" rx="22" transform="rotate(-8 69 110)" class="ink" />
          <rect x="209" y="104" width="49" height="82" rx="22" transform="rotate(8 209 104)" class="ink" />
          <rect x="82" y="121" width="23" height="56" rx="11" class="screen" />
          <rect x="219" y="119" width="23" height="56" rx="11" class="screen" />
        }
        @case ('telefono') {
          <rect x="94" y="17" width="133" height="194" rx="22" class="ink" />
          <rect x="101" y="24" width="119" height="180" rx="17" class="screen" />
          <circle cx="185" cy="95" r="47" class="accent" />
          <path d="M101 166q48-100 119-24v45a17 17 0 0 1-17 17h-85a17 17 0 0 1-17-17Z" class="screen-soft" />
          <rect x="140" y="29" width="42" height="9" rx="5" class="ink" />
          <path d="M143 192h36" class="light-line" />
        }
        @case ('monitor') {
          <rect x="33" y="26" width="254" height="152" rx="10" class="ink" />
          <rect x="41" y="34" width="238" height="133" rx="4" class="screen" />
          <circle cx="220" cy="95" r="48" class="accent" />
          <path d="m41 146 76-81 75 102H41Z" class="screen-soft" />
          <path d="M144 178h32v22h-32Z" class="metal" /><rect x="107" y="200" width="106" height="8" rx="4" class="ink" />
          <rect x="63" y="53" width="70" height="7" rx="3" class="light" />
        }
        @case ('gaming') {
          <path d="M88 64h144c34 0 47 81 35 111-8 24-28 21-44 3l-23-24h-80l-24 24c-16 18-35 21-44-3C40 145 54 64 88 64Z" class="metal" />
          <path d="M86 91v37M68 109h37" class="wide-line" />
          <circle cx="221" cy="96" r="8" class="accent" /><circle cx="240" cy="114" r="8" class="screen" />
          <circle cx="203" cy="114" r="8" class="screen" /><circle cx="222" cy="132" r="8" class="accent" />
          <circle cx="124" cy="146" r="16" class="ink" /><circle cx="183" cy="146" r="16" class="ink" />
          <path d="M149 100h20" class="line" />
        }
        @case ('reloj') {
          <rect x="125" y="4" width="70" height="221" rx="21" class="screen" />
          <rect x="99" y="58" width="122" height="119" rx="30" class="metal" />
          <rect x="107" y="66" width="106" height="103" rx="24" class="ink" />
          <circle cx="160" cy="117" r="33" class="accent-ring" />
          <path d="M160 90v29l19 10" class="light-line" /><rect x="221" y="96" width="7" height="23" rx="3" class="ink" />
        }
        @case ('bocina') {
          <rect x="95" y="25" width="130" height="177" rx="27" class="ink" />
          <rect x="105" y="35" width="110" height="157" rx="20" class="metal" />
          <circle cx="160" cy="134" r="42" class="ink" /><circle cx="160" cy="134" r="27" class="screen" />
          <circle cx="160" cy="66" r="19" class="ink" /><circle cx="160" cy="66" r="10" class="accent" />
        }
        @case ('camara') {
          <path d="M143 139h35v43h-35Z" class="metal" /><ellipse cx="160" cy="191" rx="62" ry="13" class="metal" />
          <rect x="92" y="27" width="136" height="127" rx="46" class="metal" />
          <circle cx="160" cy="90" r="44" class="ink" /><circle cx="160" cy="90" r="28" class="screen" />
          <circle cx="168" cy="81" r="9" class="light" /><circle cx="160" cy="143" r="3" class="accent" />
        }
        @case ('teclado') {
          <rect x="23" y="58" width="274" height="123" rx="14" class="ink" />
          @for (y of [74, 101, 128]; track y) {
            @for (x of [37, 66, 95, 124, 153, 182, 211, 240, 269]; track x) {
              <rect [attr.x]="x" [attr.y]="y" width="18" height="19" rx="3" class="metal" />
            }
          }
          <rect x="97" y="155" width="116" height="13" rx="3" class="screen" />
          <path d="M42 192h236" class="accent-line" />
        }
        @default {
          <path d="m160 29 93 45v105l-93 40-93-40V74Z" class="metal" />
          <path d="m67 74 93 44 93-44M160 118v101" class="line" />
          <path d="m117 50 93 44v37l-28 12v-36L92 64Z" class="screen" />
          <path d="m88 146 33 14m-33-3 22 10" class="line" />
        }
      }
    </svg>`,
  styles: [`:host{display:block;width:100%;height:100%}svg{width:100%;height:100%;overflow:visible}
    .ink{fill:var(--color-heading)}.ink-soft{fill:color-mix(in srgb,var(--color-heading) 50%,var(--color-surface))}
    .metal{fill:color-mix(in srgb,var(--color-heading) 22%,var(--color-surface));stroke:color-mix(in srgb,var(--color-heading) 40%,var(--color-surface));stroke-width:1.5}
    .screen{fill:var(--color-primary)}.screen-soft{fill:color-mix(in srgb,var(--color-primary) 58%,var(--color-heading))}
    .accent{fill:var(--color-accent)}.light{fill:var(--color-surface);opacity:.8}
    .shadow{fill:var(--color-heading);opacity:.08}.line{stroke:var(--color-heading);stroke-width:2;opacity:.6}
    .wide-line{stroke:var(--color-heading);stroke-width:17;stroke-linecap:round}
    .accent-line{stroke:var(--color-accent);stroke-width:5;stroke-linecap:round}
    .accent-ring{stroke:var(--color-accent);stroke-width:6}.light-line{stroke:var(--color-surface);stroke-width:4;stroke-linecap:round}`]
})
export class IlustracionTiendaComponent { @Input() tipo = 'paquete'; }
