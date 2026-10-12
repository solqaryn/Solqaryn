import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../../core/auth/permisos-runtime.service';
import {
  CreateDevolucionCliente,
  DevolucionCliente,
  EstadoDevolucionCliente,
  TipoResolucionDevolucionCliente
} from '../../../core/models/devolucion-cliente.model';
import { DevolucionClienteService } from '../../../services/devolucion-cliente.service';

const UI = [CommonModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule];

@Component({
  selector: 'app-devoluciones-cliente-list', standalone: true, imports: [...UI, RouterLink],
  template: `
  <section class="page-shell" aria-labelledby="returns-title">
    <header class="page-header"><div><p class="eyebrow">Ventas</p><h1 id="returns-title">Devoluciones de clientes</h1><p>Consulta borradores, confirmaciones y anulaciones sin alterar el contrato de la venta original.</p></div>
      @if (puedeCrear()) {<a mat-flat-button routerLink="/devoluciones-clientes/nueva" data-testid="nueva-devolucion-cliente"><mat-icon>assignment_return</mat-icon>Nueva devolución</a>}
    </header>
    <mat-card><mat-card-content>
      <div class="filters">
        <mat-form-field appearance="outline"><mat-label>Venta ID</mat-label><input matInput type="number" min="1" [value]="ventaId ?? ''" (input)="ventaId=entero($any($event.target).value)"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Estado</mat-label><mat-select [value]="estado" (selectionChange)="estado=$event.value;aplicar()"><mat-option value="">Todos</mat-option><mat-option value="Borrador">Borrador</mat-option><mat-option value="Confirmada">Confirmada</mat-option><mat-option value="Anulada">Anulada</mat-option></mat-select></mat-form-field>
        <button mat-stroked-button type="button" (click)="aplicar()"><mat-icon>filter_alt</mat-icon>Aplicar</button>
      </div>
      @if (error()) {<div class="state error" role="alert">{{error()}} <button mat-button (click)="cargar()">Reintentar</button></div>}
      @else if (loading()) {<div class="state"><mat-spinner diameter="34"></mat-spinner>Cargando…</div>}
      @else {<div class="table-wrap"><table><thead><tr><th>ID</th><th>Venta</th><th>Factura</th><th>Estado</th><th>Monto ref.</th><th>Fecha</th><th></th></tr></thead><tbody>
        @for (item of items(); track item.id) {<tr><td>#{{item.id}}</td><td>#{{item.ventaId}}</td><td>{{item.facturaId ? '#'+item.facturaId : '—'}}</td><td><span class="status">{{estadoNombre(item.estado)}}</span></td><td>{{item.montoReferencia | number:'1.2-2'}}</td><td>{{item.fechaCreacion | date:'short'}}</td><td><a mat-icon-button [routerLink]="['/devoluciones-clientes',item.id]" [attr.aria-label]="'Ver devolución '+item.id"><mat-icon>visibility</mat-icon></a></td></tr>}
        @empty {<tr><td colspan="7" class="empty">No hay devoluciones para los filtros seleccionados.</td></tr>}
      </tbody></table></div><footer class="pager"><span>{{total()}} registro(s)</span><div><button mat-button [disabled]="page<=1" (click)="irPagina(page-1)">Anterior</button><span>Página {{page}}</span><button mat-button [disabled]="page*pageSize>=total()" (click)="irPagina(page+1)">Siguiente</button></div></footer>}
    </mat-card-content></mat-card>
  </section>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`.page-shell{display:grid;gap:1rem;max-width:1180px;margin:0 auto}.page-header{display:flex;justify-content:space-between;gap:1rem;align-items:flex-start}.eyebrow{margin:0;text-transform:uppercase;font-size:.75rem;letter-spacing:.08em;font-weight:700;opacity:.7}h1{margin:.2rem 0}.filters{display:flex;gap:.75rem;align-items:center;flex-wrap:wrap}.state{min-height:100px;display:flex;gap:.75rem;align-items:center;justify-content:center}.error{color:var(--mat-sys-error,#b3261e)}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{padding:.8rem;border-bottom:1px solid rgba(127,127,127,.2);text-align:left;white-space:nowrap}.status{padding:.2rem .55rem;border-radius:999px;background:rgba(127,127,127,.15)}.empty{text-align:center;padding:2rem}.pager,.pager div{display:flex;justify-content:space-between;gap:.5rem;align-items:center}.pager{padding-top:1rem}@media(max-width:680px){.page-header{flex-direction:column}}`]
})
export class DevolucionesClienteListComponent implements OnInit {
  private readonly service=inject(DevolucionClienteService); private readonly permisos=inject(PermisosRuntimeService);
  readonly items=signal<DevolucionCliente[]>([]); readonly loading=signal(false); readonly error=signal(''); readonly total=signal(0); readonly puedeCrear=signal(false);
  page=1;pageSize=20;ventaId:number|null=null;estado:''|'Borrador'|'Confirmada'|'Anulada'='';
  ngOnInit():void{this.puedeCrear.set(this.permisos.puede('Ventas','Crear'));this.cargar();}
  cargar():void{this.loading.set(true);this.error.set('');this.service.getPaged({page:this.page,pageSize:this.pageSize,ventaId:this.ventaId,estado:this.estado||null}).pipe(finalize(()=>this.loading.set(false))).subscribe({next:r=>{this.items.set(r.data?.items??[]);this.total.set(r.data?.totalCount??0);},error:()=>this.error.set('No fue posible cargar las devoluciones de clientes.')});}
  aplicar():void{this.page=1;this.cargar();} irPagina(v:number):void{this.page=Math.max(1,v);this.cargar();} entero(v:string):number|null{const n=Number(v);return Number.isInteger(n)&&n>0?n:null;} estadoNombre(v:EstadoDevolucionCliente):string{return v===1?'Borrador':v===2?'Confirmada':v===3?'Anulada':String(v);}
}

@Component({
  selector:'app-devolucion-cliente-form',standalone:true,imports:[...UI,ReactiveFormsModule],
  template:`<section class="page-shell"><header class="page-header"><div><p class="eyebrow">Ventas</p><h1>Nueva devolución de cliente</h1><p>Registra únicamente líneas reales de la venta. La confirmación se realiza después desde el detalle.</p></div><button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon>Volver</button></header>
  <form [formGroup]="form" (ngSubmit)="guardar()" class="form-grid" novalidate>
    <mat-form-field appearance="outline"><mat-label>Venta ID</mat-label><input matInput type="number" min="1" formControlName="ventaId" data-testid="devolucion-venta-id"><mat-error>Venta requerida.</mat-error></mat-form-field>
    <mat-form-field appearance="outline"><mat-label>Factura ID (opcional)</mat-label><input matInput type="number" min="1" formControlName="facturaId"></mat-form-field>
    <mat-form-field class="span-2" appearance="outline"><mat-label>Observaciones</mat-label><textarea matInput rows="2" maxlength="1000" formControlName="observaciones"></textarea></mat-form-field>
    <div class="span-2 toolbar"><h2>Detalle</h2><button mat-stroked-button type="button" (click)="agregarDetalle()"><mat-icon>add</mat-icon>Agregar línea</button></div>
    <div class="span-2 lines" formArrayName="detalles">@for(g of detalles.controls;track $index;let i=$index){<article class="line" [formGroupName]="i"><mat-form-field appearance="outline"><mat-label>Venta detalle ID</mat-label><input matInput type="number" min="1" formControlName="ventaDetalleId"></mat-form-field><mat-form-field appearance="outline"><mat-label>Cantidad</mat-label><input matInput type="number" min="1" formControlName="cantidad"></mat-form-field><mat-form-field appearance="outline"><mat-label>Resolución</mat-label><mat-select formControlName="resolucion"><mat-option [value]="1">Reintegro</mat-option><mat-option [value]="2">Cambio</mat-option><mat-option [value]="3">Crédito a favor</mat-option></mat-select></mat-form-field><button mat-icon-button type="button" (click)="quitarDetalle(i)" aria-label="Quitar línea"><mat-icon>delete</mat-icon></button></article>}</div>
    @if(error()){<div class="span-2 state error" role="alert">{{error()}}</div>}
    <div class="span-2 actions"><button mat-button type="button" (click)="volver()">Cancelar</button><button mat-flat-button type="submit" [disabled]="saving()||form.invalid||detalles.length===0" data-testid="guardar-devolucion-cliente">@if(saving()){<mat-spinner diameter="20"></mat-spinner>}@else{<mat-icon>save</mat-icon>}Guardar borrador</button></div>
  </form></section>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles:[`.page-shell{display:grid;gap:1rem;max-width:980px;margin:0 auto}.page-header,.toolbar,.actions,.line{display:flex;gap:1rem;align-items:center;justify-content:space-between}.page-header{align-items:flex-start}.eyebrow{margin:0;text-transform:uppercase;font-size:.75rem;letter-spacing:.08em;font-weight:700;opacity:.7}h1{margin:.2rem 0}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.span-2{grid-column:1/-1}.lines{display:grid;gap:.75rem}.line{padding:1rem;border:1px solid rgba(127,127,127,.2);border-radius:12px}.line mat-form-field{flex:1}.error{color:var(--mat-sys-error,#b3261e)}@media(max-width:760px){.form-grid{grid-template-columns:1fr}.span-2{grid-column:1}.line,.page-header{flex-direction:column;align-items:stretch}}`]
})
export class DevolucionClienteFormComponent implements OnInit {
  private readonly fb=inject(FormBuilder);private readonly service=inject(DevolucionClienteService);private readonly router=inject(Router);private readonly route=inject(ActivatedRoute);
  readonly saving=signal(false);readonly error=signal('');
  readonly form=this.fb.group({ventaId:[null as number|null,[Validators.required,Validators.min(1)]],facturaId:[null as number|null,[Validators.min(1)]],observaciones:[''],detalles:this.fb.array([])});
  get detalles():FormArray{return this.form.controls.detalles as FormArray;}
  ngOnInit():void{const ventaId=Number(this.route.snapshot.queryParamMap.get('ventaId'));if(Number.isInteger(ventaId)&&ventaId>0)this.form.controls.ventaId.setValue(ventaId);this.agregarDetalle();}
  agregarDetalle():void{this.detalles.push(this.fb.group({ventaDetalleId:[null,[Validators.required,Validators.min(1)]],cantidad:[1,[Validators.required,Validators.min(1)]],resolucion:[1,[Validators.required]]}));}
  quitarDetalle(i:number):void{if(this.detalles.length>1)this.detalles.removeAt(i);}
  guardar():void{if(this.form.invalid||!this.detalles.length){this.form.markAllAsTouched();return;}const raw=this.form.getRawValue();const value:CreateDevolucionCliente={ventaId:Number(raw.ventaId),facturaId:raw.facturaId?Number(raw.facturaId):null,observaciones:raw.observaciones?.trim()||null,detalles:(raw.detalles??[]).map((d:any)=>({ventaDetalleId:Number(d.ventaDetalleId),cantidad:Number(d.cantidad),resolucion:Number(d.resolucion) as TipoResolucionDevolucionCliente}))};this.saving.set(true);this.error.set('');this.service.create(value).pipe(finalize(()=>this.saving.set(false))).subscribe({next:r=>{if(r.data?.id)this.router.navigate(['/devoluciones-clientes',r.data.id]);else this.router.navigate(['/devoluciones-clientes']);},error:()=>this.error.set('No fue posible guardar la devolución. Verifica la venta, las cantidades y vuelve a intentar.')});}
  volver():void{this.router.navigate(['/devoluciones-clientes']);}
}

@Component({
  selector:'app-devolucion-cliente-detail',standalone:true,imports:[...UI,RouterLink],
  template:`<section class="page-shell"><header class="page-header"><div><p class="eyebrow">Ventas</p><h1>Devolución #{{item()?.id}}</h1><p>Venta #{{item()?.ventaId}} · Estado {{item()?estadoNombre(item()!.estado):'—'}}</p></div><a mat-stroked-button routerLink="/devoluciones-clientes"><mat-icon>arrow_back</mat-icon>Volver</a></header>
  @if(loading()){<div class="state"><mat-spinner diameter="34"></mat-spinner>Cargando…</div>}@else if(error()){<div class="state error" role="alert">{{error()}}</div>}@else if(item();as d){<mat-card><mat-card-content><div class="summary"><div><small>Factura</small><strong>{{d.facturaId?'#'+d.facturaId:'Sin factura asociada'}}</strong></div><div><small>Monto referencia</small><strong>{{d.montoReferencia|number:'1.2-2'}}</strong></div><div><small>Creada</small><strong>{{d.fechaCreacion|date:'short'}}</strong></div></div><p>{{d.observaciones||'Sin observaciones.'}}</p><div class="table-wrap"><table><thead><tr><th>Detalle venta</th><th>Producto</th><th>Cantidad</th><th>Resolución</th><th>Monto ref.</th></tr></thead><tbody>@for(x of d.detalles;track x.id){<tr><td>#{{x.ventaDetalleId}}</td><td>{{x.productoNombreSnapshot||('Producto #'+x.productoId)}}</td><td>{{x.cantidad}}</td><td>{{resolucionNombre(x.resolucion)}}</td><td>{{x.montoReferencia|number:'1.2-2'}}</td></tr>}</tbody></table></div>
  @if((esBorrador(d.estado)&&puedeConfirmar())||(esConfirmada(d.estado)&&puedeAnular())){<div class="actions">@if(esBorrador(d.estado)&&puedeConfirmar()){<button mat-flat-button type="button" (click)="confirmar()" [disabled]="acting()" data-testid="confirmar-devolucion-cliente"><mat-icon>check_circle</mat-icon>Confirmar</button>}@if(esConfirmada(d.estado)&&puedeAnular()){<mat-form-field appearance="outline"><mat-label>Motivo de anulación</mat-label><input matInput [value]="motivo" (input)="motivo=$any($event.target).value"></mat-form-field><button mat-stroked-button type="button" (click)="anular()" [disabled]="acting()||!motivo.trim()"><mat-icon>cancel</mat-icon>Anular</button>}</div>}
  </mat-card-content></mat-card>}</section>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles:[`.page-shell{display:grid;gap:1rem;max-width:1080px;margin:0 auto}.page-header,.summary,.actions{display:flex;gap:1rem;justify-content:space-between;align-items:flex-start}.eyebrow{margin:0;text-transform:uppercase;font-size:.75rem;letter-spacing:.08em;font-weight:700;opacity:.7}h1{margin:.2rem 0}.summary{display:grid;grid-template-columns:repeat(3,1fr);margin-bottom:1rem}.summary div{display:grid}.state{min-height:120px;display:flex;gap:.75rem;align-items:center;justify-content:center}.error{color:var(--mat-sys-error,#b3261e)}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{padding:.75rem;border-bottom:1px solid rgba(127,127,127,.2);text-align:left}.actions{align-items:center;justify-content:flex-end;margin-top:1rem}@media(max-width:680px){.page-header,.actions{flex-direction:column}.summary{grid-template-columns:1fr}}`]
})
export class DevolucionClienteDetailComponent implements OnInit {
  private readonly service=inject(DevolucionClienteService);private readonly route=inject(ActivatedRoute);private readonly permisos=inject(PermisosRuntimeService);
  readonly item=signal<DevolucionCliente|null>(null);readonly loading=signal(false);readonly acting=signal(false);readonly error=signal('');readonly puedeConfirmar=signal(false);readonly puedeAnular=signal(false);motivo='';
  ngOnInit():void{this.puedeConfirmar.set(this.permisos.puede('Ventas','Confirmar'));this.puedeAnular.set(this.permisos.puede('Ventas','Anular'));this.cargar();}
  cargar():void{const id=Number(this.route.snapshot.paramMap.get('id'));if(!Number.isInteger(id)||id<1){this.error.set('Identificador de devolución inválido.');return;}this.loading.set(true);this.service.getById(id).pipe(finalize(()=>this.loading.set(false))).subscribe({next:r=>this.item.set(r.data??null),error:()=>this.error.set('No fue posible cargar la devolución.')});}
  confirmar():void{const d=this.item();if(!d||!this.puedeConfirmar()||!this.esBorrador(d.estado))return;this.acting.set(true);this.service.confirmar(d.id).pipe(finalize(()=>this.acting.set(false))).subscribe({next:r=>this.item.set(r.data??d),error:()=>this.error.set('No fue posible confirmar la devolución.')});}
  anular():void{const d=this.item();if(!d||!this.puedeAnular()||!this.esConfirmada(d.estado)||!this.motivo.trim())return;this.acting.set(true);this.service.anular(d.id,this.motivo).pipe(finalize(()=>this.acting.set(false))).subscribe({next:r=>this.item.set(r.data??d),error:()=>this.error.set('No fue posible anular la devolución.')});}
  esBorrador(v:EstadoDevolucionCliente):boolean{return v===1||v==='Borrador';} esConfirmada(v:EstadoDevolucionCliente):boolean{return v===2||v==='Confirmada';} estadoNombre(v:EstadoDevolucionCliente):string{return v===1?'Borrador':v===2?'Confirmada':v===3?'Anulada':String(v);} resolucionNombre(v:TipoResolucionDevolucionCliente):string{return v===1||v==='Reintegro'?'Reintegro':v===2||v==='Cambio'?'Cambio':'Crédito a favor';}
}
