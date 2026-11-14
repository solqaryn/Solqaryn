# C00-01.M02.01 — Entidades, ownership, relaciones e invariantes

**Fuente ejecutable:** V5 FINAL, `C00-01.M02.01`, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`.

## Alcance y frontera de autoridad

Esta unidad **define** el contrato documental de ownership de las entidades ya existentes; no introduce entidades, columnas, filtros EF, DTO, migraciones, API, ejecución de base de datos ni una autorización nueva. Las referencias se inspeccionaron en el HEAD de entrada `af19ef8676da0c6c0cff3b605b06a2a078ce308b`, con lectura directa del código original. Toda decisión de acceso sigue siendo server-side por membresía verificada. Los `EmpresaId` históricos nulos están en cuarentena: **no conceder acceso por nulidad, slug, host, header, JWT no validado ni EmpresaId enviado por cliente**. Los campos `Rol` legacy no sustituyen la autoridad relacional.

## Inventario trazable e invariantes

| Entidad o contrato | Ruta fuente vigente | Relación / ownership observable | Invariante y condición negativa |
| --- | --- | --- | --- |
| `Empresa` | `backend/src/Domain/Entities/Empresa.cs` (blob `c44940df7c30983a4c23b93338e0cb9367962069`) | Raíz de identidad comercial, `AuditableEntity`; nombre, RTN y estado activa/inactiva | `CambiarNombre` rechaza vacío; `Desactivar` no equivale a soft-delete; ninguna empresa es seleccionable por datos no autenticados |
| `Usuario` | `backend/src/Domain/Entities/Usuario.cs` (blob `cfe319bb61e405379fa632ed18d3183cd2df1a68`) | `RolId` y `RolEntidad` relacionales; contraseña en `PasswordHash` | `Rol` histórico `[NotMapped]` no autoriza; no registrar hashes ni PII en trazas |
| `ContextoTenantActual` | `backend/src/Domain/Security/ContextoTenantActual.cs` (blob `7377d2d752985cc9baf31d08280f38ad94a6a114`) | Fábrica `DesdeMembresia(UsuarioEmpresa, usuarioId, empresaId)`; `RolId` efectivo por membresía | Constructor privado y `ExigirEmpresa` deben rechazar empresa ajena y valor no positivo; tenant A no ve tenant B |
| `Producto` | `backend/src/Domain/Entities/Producto.cs` (blob `19632ce1aebe6a9cbc4379523367531852ee3e7e`) | `EmpresaId?`, `Variantes`, `Imagenes`, `Categoria` | `EmpresaId` nulo histórico NO da autoridad; preservar snapshots de marca/modelo y compatibilidad sin borrar datos |
| `ProductoVariante` | `backend/src/Domain/Entities/ProductoVariante.cs` (blob `de0dce026a147bb52ad46c952a2e9231015c195f`) | `ProductoId` requerido, FK a `Producto`; `EmpresaId?`; trazabilidad de lote/serie/vencimiento | Vencimiento exige lote; días de alerta no negativos y sólo válidos con vencimiento; no mezclar ownership de producto/variante |
| `MovimientoInventario` | `backend/src/Domain/Entities/MovimientoInventario.cs` (blob `a1093f36aea78e6851fe4b364e4eb710832a7e61`) | `ProductoId`, variante, almacén y ubicación; FKs tipadas Compra/Venta/Consumo/Ajuste/Transferencia/Recepción | Correlación durable en nuevos movimientos; máximo un origen tipado; stock anterior/nuevo y auditoría sin pérdida; null legacy no se reinterpreta como permiso |
| `Factura` | `backend/src/Domain/Entities/Factura.cs` (blob `8c59b9a939cc9439efa5fd09777d9b0dfb19de66`) | `EmpresaId?`, `VentaId`, detalles y pagos; snapshots fiscales y de cliente | Empresa histórica nula no implica acceso cruzado; proteger PII y no sobrescribir snapshots fiscales por datos del navegador |
| `BaseEntity` / `AuditableEntity` | `backend/src/Domain/Common/BaseEntity.cs` / `AuditableEntity.cs` (blobs `9bd01cc49b2976bfb6cd3cd892b117888912ce31`, `7fb8c0ed8f52cf6402ae63ae6149fa41e8cac83f`) | `Id`, fechas UTC, autores de creación/actualización | Auditoría no reemplaza tenant membership ni autoriza omitir `ExigirEmpresa` |

## Reglas comprobables de aceptación de esta definición

1. **Positivo documental:** cada fila identifica código, vínculo de ownership y una invariante observable; la matriz se limita a contratos existentes y a la versión SHA/blobs declarados.
2. **Negativo tenancy:** una petición tenant A sobre recurso tenant B debe denegarse desde backend, aunque frontend y campos opcionales sean manipulados. Este documento NO demuestra la denegación HTTP real; esa prueba corresponde a una implementación posterior.
3. **Negativo identidad:** ni `Usuario.Rol` legacy ni un `EmpresaId? == null` autorizan acciones. `ContextoTenantActual` exige membresía efectiva y valor positivo.
4. **Negativo inventario:** vencimiento sin lote, alerta negativa o múltiples orígenes de movimiento deben fallar cerrado; no hacer cambios de stock por documentar invariantes.
5. **Negativo despliegue:** no ejecutar migración MySQL, Cloudflare/R2, Hostinger, QA, main ni PROD. Imágenes históricas inexistentes: migración histórica `NO_APLICA`.
6. **Entrega:** probar este contrato documental con Node 22 (`node --check` y `node --test` dirigido), revisar secretos y diff, hacer commit directo fast-forward en `dev`, A4 con SHA exacto y readback PLAN/COLA/BITACORA; registrar `A5_PENDIENTE` para **otro TASK_ID**. A5_PASS independiente es requisito previo a promoción, nunca autopromoción.

## Límites de prueba y reversión

A0–A3 de esta unidad cubren sólo documentación y contraste estático de fuentes originales; **no prueban .NET en ejecución, JWT HTTP, BCrypt, MySQL, EF Core, Angular/Playwright, tenant A/B live ni integraciones de proveedores**. El contrato evita atribuir esos PASS inexistentes a la aplicación. Un conflicto de SHA, dependencia, colisión o prueba obliga a mantener `VALIDANDO` sin inventar `LISTO`. Rollback únicamente mediante `git revert` aditivo tras revalidar HEAD y preservar trabajos ajenos; BITACORA es apendicular, nunca borrar historia. `PLAN_MAESTRO!H` es autoridad del estado, `COLA!E/G/O` se consulta por fórmula.
