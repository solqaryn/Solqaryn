import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardService } from '../../services/dashboard.service';
import { AutomatizacionService } from '../../services/automatizacion.service';
import { DashboardKpiConfiguracion, DashboardKpiResuelto, DashboardResumen } from '../../core/models/dashboard.model';
import { AutomatizacionResumen } from '../../core/models/automatizacion.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly resumen = signal<DashboardResumen | null>(null);
  readonly automatizacion = signal<AutomatizacionResumen | null>(null);
  readonly kpiConfiguracion = signal<DashboardKpiConfiguracion[]>([]);
  readonly kpisResueltos = signal<DashboardKpiResuelto[]>([]);
  readonly loading = signal(true);
  readonly loadingAutomatizacion = signal(true);
  readonly loadingKpis = signal(true);
  readonly errorKpis = signal(false);
  readonly savingKpis = signal(false);
  readonly esAdministrador = this.permisosRuntime.esAdministrador;
  readonly puedeVerVentas = signal(false);
  readonly puedeVerProductos = signal(false);

  constructor(
    private dashboardService: DashboardService,
    private automatizacionService: AutomatizacionService
  ) {}

  ngOnInit(): void {
    this.puedeVerVentas.set(this.permisosRuntime.puede('Ventas', 'Ver'));
    this.puedeVerProductos.set(this.permisosRuntime.puede('Productos', 'Ver'));

    this.dashboardService.getResumen().subscribe({
      next: (res) => {
        this.resumen.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.cargarKpis();

    this.automatizacionService.getSugerencias().subscribe({
      next: (res) => {
        this.automatizacion.set(res.data);
        this.loadingAutomatizacion.set(false);
      },
      error: () => this.loadingAutomatizacion.set(false)
    });
  }

  cargarKpis(): void {
    this.loadingKpis.set(true);
    this.errorKpis.set(false);
    this.dashboardService.getKpiConfiguracion().subscribe({
      next: (configuracion) => {
        this.kpiConfiguracion.set([...configuracion].sort((a, b) => a.orden - b.orden || a.metricKey.localeCompare(b.metricKey)));
        this.dashboardService.getKpisResueltos().subscribe({
          next: (kpis) => {
            this.kpisResueltos.set([...kpis].sort((a, b) => a.orden - b.orden || a.metricKey.localeCompare(b.metricKey)));
            this.loadingKpis.set(false);
          },
          error: () => {
            this.errorKpis.set(true);
            this.loadingKpis.set(false);
          }
        });
      },
      error: () => {
        this.errorKpis.set(true);
        this.loadingKpis.set(false);
      }
    });
  }

  actualizarVisibilidad(metricKey: string, habilitado: boolean): void {
    this.persistirConfiguracion(this.kpiConfiguracion().map((item) =>
      item.metricKey === metricKey ? { ...item, habilitado } : item
    ));
  }

  actualizarEtiqueta(metricKey: string, etiquetaVisible: string): void {
    this.persistirConfiguracion(this.kpiConfiguracion().map((item) =>
      item.metricKey === metricKey ? { ...item, etiquetaVisible: etiquetaVisible.trim() || null } : item
    ));
  }

  moverKpi(metricKey: string, delta: -1 | 1): void {
    const items = [...this.kpiConfiguracion()].sort((a, b) => a.orden - b.orden || a.metricKey.localeCompare(b.metricKey));
    const index = items.findIndex((item) => item.metricKey === metricKey);
    const target = index + delta;
    if (index < 0 || target < 0 || target >= items.length) return;
    [items[index], items[target]] = [items[target], items[index]];
    this.persistirConfiguracion(items.map((item, orden) => ({ ...item, orden })));
  }

  etiquetaKpi(item: DashboardKpiResuelto): string {
    if (item.etiquetaVisible?.trim()) return item.etiquetaVisible.trim();
    return this.etiquetaKpiConfiguracion(item.metricKey);
  }

  etiquetaKpiConfiguracion(metricKey: string): string {
    return ({
      INGRESOS_MES: 'Ingresos del mes', VENTAS_MES: 'Ventas del mes', COMPRAS_MES: 'Compras del mes',
      TOTAL_PRODUCTOS: 'Productos registrados', TOTAL_UNIDADES: 'Unidades en inventario',
      VALOR_INVENTARIO: 'Valor de inventario', UTILIDAD_BRUTA: 'Utilidad bruta',
      BALANCE_OPERATIVO: 'Balance operativo', CUENTAS_POR_COBRAR: 'Cuentas por cobrar',
      CUENTAS_POR_PAGAR: 'Cuentas por pagar', PRODUCTOS_STOCK_BAJO: 'Productos con stock bajo'
    } as Record<string, string>)[metricKey] ?? metricKey;
  }

  iconoKpi(metricKey: string): string {
    return ({
      INGRESOS_MES: 'payments', VENTAS_MES: 'point_of_sale', COMPRAS_MES: 'shopping_cart',
      TOTAL_PRODUCTOS: 'warehouse', TOTAL_UNIDADES: 'warehouse', VALOR_INVENTARIO: 'account_balance_wallet',
      UTILIDAD_BRUTA: 'trending_up', BALANCE_OPERATIVO: 'monitoring', CUENTAS_POR_COBRAR: 'request_quote',
      CUENTAS_POR_PAGAR: 'receipt_long', PRODUCTOS_STOCK_BAJO: 'warning_amber'
    } as Record<string, string>)[metricKey] ?? 'analytics';
  }

  private persistirConfiguracion(configuracion: DashboardKpiConfiguracion[]): void {
    if (this.savingKpis()) return;
    this.savingKpis.set(true);
    this.errorKpis.set(false);
    this.dashboardService.guardarKpiConfiguracion(configuracion).subscribe({
      next: (guardada) => {
        this.kpiConfiguracion.set([...guardada].sort((a, b) => a.orden - b.orden || a.metricKey.localeCompare(b.metricKey)));
        this.savingKpis.set(false);
        this.cargarKpis();
      },
      error: () => {
        this.errorKpis.set(true);
        this.savingKpis.set(false);
      }
    });
  }

  iconoSugerencia(modulo: string): string {
    return ({
      Inventario: 'warehouse', Productos: 'sell', Compras: 'shopping_cart', Ventas: 'point_of_sale',
      Clientes: 'groups', Facturación: 'receipt_long', Finanzas: 'account_balance_wallet',
      Cargas: 'upload_file', Configuración: 'settings'
    } as Record<string, string>)[modulo] ?? 'tips_and_updates';
  }

  maximoActividad(r: DashboardResumen): number {
    const valores = [
      ...r.ultimasVentas.map((v) => v.total),
      ...(this.esAdministrador() ? r.ultimasCompras.map((c) => c.total) : [])
    ];
    return Math.max(1, ...valores);
  }

  barraPorcentaje(valor: number, r: DashboardResumen): number {
    return Math.max(8, Math.round((valor / this.maximoActividad(r)) * 100));
  }

  ventasRecientes(r: DashboardResumen) { return r.ultimasVentas.slice(0, 7); }
  comprasRecientes(r: DashboardResumen) { return this.esAdministrador() ? r.ultimasCompras.slice(0, 7) : []; }

  totalOperativo(r: DashboardResumen): number {
    const compras = this.esAdministrador() ? r.comprasDelMes : 0;
    return Math.max(1, r.ventasDelMes + compras + r.productosStockBajo.length);
  }

  porcentaje(valor: number, total: number): number { return Math.round((valor / Math.max(1, total)) * 100); }

  donutBackground(r: DashboardResumen): string {
    const total = this.totalOperativo(r);
    const ventas = this.porcentaje(r.ventasDelMes, total);
    const compras = this.esAdministrador() ? this.porcentaje(r.comprasDelMes, total) : 0;
    return `conic-gradient(var(--color-primary) 0 ${ventas}%, var(--color-success) ${ventas}% ${ventas + compras}%, var(--color-danger) ${ventas + compras}% 100%)`;
  }
}
