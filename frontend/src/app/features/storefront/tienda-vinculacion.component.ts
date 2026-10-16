import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';

/**
 * Puerta pública de Microfase 1. No carga el storefront legacy hasta que
 * identidad, enlaces, carrito y sesiones estén aislados (Microfases 2/3).
 * El slug público nunca concede permiso por sí solo: Render verifica
 * la firma del proxy y el registro Host+StoreKey antes de responder.
 */
@Component({
  selector: 'app-tienda-vinculacion',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="store-entry" aria-labelledby="store-title">
      <section class="store-card">
        <div class="brand">SOLQARYN</div>
        <h1 id="store-title">Tienda independiente</h1>
        @if (estado() === 'verificando') {
          <p role="status">Verificando la identidad y disponibilidad de la tienda…</p>
        } @else if (estado() === 'verificada') {
          <p role="status">La dirección está vinculada a su empresa de forma segura.</p>
          <p>Los productos, pedidos y cuentas se habilitarán cuando termine la certificación de aislamiento de tienda.</p>
        } @else {
          <p role="alert">La tienda solicitada no está disponible o no está autorizada.</p>
        }
      </section>
    </main>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: #f6f8fa; color: #203042; }
    .store-entry { min-height: 100vh; display: grid; place-items: center; padding: 24px; box-sizing: border-box; }
    .store-card { max-width: 560px; width: 100%; background: #fff; padding: clamp(24px, 5vw, 44px); box-sizing: border-box; border: 1px solid #dbe2e8; border-radius: 18px; }
    .brand { font-weight: 800; letter-spacing: .08em; color: #194d6c; }
    h1 { font-size: clamp(1.5rem, 3vw, 2.1rem); margin: 15px 0; }
    p { line-height: 1.6; }
  `]
})
export class TiendaVinculacionComponent {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly estado = signal<'verificando' | 'verificada' | 'rechazada'>('verificando');

  constructor() {
    const key = this.route.snapshot.paramMap.get('storeKey') || '';
    if (!/^[a-z][a-z0-9-]{0,46}[a-z0-9]$/.test(key)) {
      this.estado.set('rechazada');
      return;
    }

    this.http.get<ApiResponse<{ empresaId: number; verificada: boolean }>>(
      `${environment.apiUrl}/tiendas/${encodeURIComponent(key)}/tienda/binding`,
      { headers: { 'Cache-Control': 'no-store' } }
    ).subscribe({
      next: respuesta => {
        const ok = respuesta?.success === true &&
          respuesta.data?.verificada === true &&
          Number.isSafeInteger(respuesta.data?.empresaId) &&
          (respuesta.data?.empresaId ?? 0) > 0;
        this.estado.set(ok ? 'verificada' : 'rechazada');
      },
      error: () => this.estado.set('rechazada')
    });
  }
}
