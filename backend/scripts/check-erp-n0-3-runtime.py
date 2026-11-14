#!/usr/bin/env python3
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[2]
errors=[]
def require(rel,text):
    if text not in (root/rel).read_text(encoding='utf-8'): errors.append(f'{rel}: falta {text}')
def forbid(rel,text):
    if text in (root/rel).read_text(encoding='utf-8'): errors.append(f'{rel}: dependencia legacy prohibida {text}')
require('backend/src/Application/Services/ProductoVarianteService.cs','SincronizarProyeccionCompatibilidadAsync')
require('backend/src/Application/Services/ProductoVarianteService.cs','public async Task<ProductoVarianteDto> SincronizarTecnicaAsync')
require('backend/src/Application/Services/VentaService.cs','SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado)')
require('backend/src/Application/Services/CompraService.cs','SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado)')
require('backend/src/Infrastructure/Services/InventarioConcurrencyService.cs','demandasLegacySinVariante')
require('backend/src/Infrastructure/Repositories/ProductoRepository.cs','ProductoVariantes')
forbid('backend/src/API/Controllers/ProductosController.cs','AplicarProyeccionLegacy(dto)')
forbid('backend/src/Application/Services/ProductoService.cs','producto.Cantidad = dto.Cantidad')
forbid('backend/src/Application/Services/ProductoService.cs','producto.Costo = dto.Costo')
forbid('backend/src/Application/Services/ProductoService.cs','producto.Precio = dto.Precio')
forbid('backend/src/Infrastructure/Repositories/ProductoRepository.cs','v.Costo ?? v.Producto.Costo')
forbid('backend/src/Infrastructure/Repositories/ProductoRepository.cs','v.Precio ?? v.Producto.Precio')
forbid('backend/src/Application/Services/ProductoEscanerService.cs','variante.Producto.Costo')
forbid('backend/src/Application/Services/ProductoEscanerService.cs','variante.Producto.Precio')
forbid('backend/src/Application/Services/VentaService.cs','producto.Cantidad < input.Cantidad')
forbid('backend/src/Application/Services/VentaService.cs','variante?.Costo ?? producto.Costo')
forbid('backend/src/Application/Services/VentaService.cs','ProductoVarianteId = variante?.Id')
require('backend/src/Application/Services/VentaService.cs','ProductoVarianteId = variante.Id')
require('backend/src/Application/Services/VentaService.cs','variante.Cantidad < input.Cantidad')
forbid('backend/src/Application/Services/CompraService.cs','ProductoVarianteId = variante?.Id')
require('backend/src/Application/Services/CompraService.cs','ProductoVarianteId = variante.Id')
forbid('backend/src/Infrastructure/Services/CargaMasivaService.cs','producto.Costo = Decimal(fila, "Costo")')
forbid('backend/src/Infrastructure/Services/CargaMasivaService.cs','producto.Precio = Decimal(fila, "Precio")')
forbid('backend/src/Infrastructure/Services/CargaMasivaService.cs','producto.UmbralStockBajo = Entero(fila, "UmbralStockBajo")')
require('backend/src/Infrastructure/Services/CargaMasivaService.cs','variante.Costo = Decimal(fila, "Costo")')
require('backend/src/Infrastructure/Services/CargaMasivaService.cs','variante.Precio = Decimal(fila, "Precio")')
require('backend/src/Infrastructure/Services/CargaMasivaService.cs','PRODUCTO_REQUIERE_VARIANTES')
require('backend/src/Infrastructure/Services/CargaMasivaService.cs','conserva stock en su variante técnica')
require('backend/src/Infrastructure/Services/CargaMasivaService.cs','tecnica.Eliminado = true')
if errors:
    print('N0.3 FAIL:\n'+'\n'.join(errors),file=sys.stderr); sys.exit(1)
print('N0.3 runtime guard: ProductoVariante es autoridad; Producto queda solo como proyección de compatibilidad.')
