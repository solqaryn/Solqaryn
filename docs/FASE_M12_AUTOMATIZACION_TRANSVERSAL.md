# FASE M12 — Automatización transversal

Fecha de cierre: 2026-08-10  
Rama exclusiva: `Desarrollo`  
HEAD funcional certificado: `76214a93564c2e91c8f4e4f82e83846a63db8a43`  
Producción: **FUERA DE ALCANCE**  
Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

## 1. Objetivo

Reducir captura duplicada y trabajo operativo repetitivo mediante reglas empresariales deterministas, configurables y auditables, sin introducir escrituras automáticas peligrosas sobre inventario, finanzas, facturación o cargas masivas.

M12 cubre los nueve dominios definidos por el Plan Maestro: Productos, Compras, Ventas, Inventario, Clientes, Facturación, Finanzas, Cargas y Configuración.

## 2. Defaults administrables

Se incorporó una configuración singleton persistente `AutomatizacionConfiguraciones` con clave primaria obligatoria y restricciones `CHECK`, compatible con MySQL 8.4 administrado y `sql_require_primary_key=ON`.

Contrato versionado: `M12.1`.

Defaults iniciales:

- ventas en borrador: 2 días;
- compras en borrador: 7 días;
- cargas pendientes/con error: 1 día;
- movimientos financieros pendientes: 7 días;
- máximo de sugerencias: 20;
- máximo de resultados de autocompletado: 10;
- recordatorios de Dashboard: activos.

Los rangos se validan tanto en backend como mediante constraints de base de datos. Cada modificación registra fecha UTC y usuario responsable.

## 3. Motor de sugerencias y recordatorios

El motor publica recordatorios deterministas con código, módulo, severidad, cantidad, detalle y ruta. No utiliza IA generativa ni heurísticas opacas para decisiones financieras o de inventario.

Reglas cubiertas:

- Inventario: variantes activas cuya cantidad alcanzó/bajó de `UmbralStockBajo`;
- Productos: productos activos sin variante activa;
- Compras: borradores antiguos según umbral;
- Ventas: borradores antiguos según umbral;
- Clientes: activos sin teléfono ni correo;
- Facturación: ventas confirmadas sin factura asociada;
- Finanzas: movimientos pendientes antiguos;
- Cargas: pendientes de validación o con errores que superaron el umbral;
- Configuración: identidad empresarial activa incompleta.

Las sugerencias se ordenan determinísticamente por severidad, cantidad y código estable.

## 4. Autocompletado contextual

Endpoint común:

`GET /automatizaciones/autocompletar`

Contextos admitidos:

- `productos` / `inventario`: nombre, SKU o código de barras sobre ProductoVariante;
- `clientes`: nombre o identidad/RTN;
- `proveedores` / `compras`: nombre o documento.

Características:

- requiere mínimo 2 caracteres;
- límite administrable;
- orden estable;
- contextos desconocidos fallan cerrados;
- no modifica ninguna entidad.

## 5. Acciones masivas seguras

M12 no ejecuta automáticamente mutaciones masivas sobre inventario, clientes ni cargas.

El endpoint:

`POST /automatizaciones/acciones-masivas/previsualizar`

solo calcula una vista previa y siempre devuelve:

- `SoloVistaPrevia = true`;
- `RequiereConfirmacion = true`;
- IDs aplicables;
- omitidos;
- advertencias.

Acciones iniciales:

- revisar variantes con stock bajo;
- revisar clientes sin contacto;
- revisar cargas con error.

Cualquier acción no incluida en allowlist se rechaza fail-closed. Las operaciones reales continúan en sus servicios transaccionales especializados.

## 6. UI/UX

### Dashboard

Se agregó `Asistente operativo` con:

- versión M12 visible;
- recordatorios por módulo;
- severidad y cantidad;
- acceso directo a la ruta responsable;
- estado vacío positivo;
- mensaje explícito de que ninguna sugerencia modifica datos automáticamente.

### Configuración

Se incorporó una tarjeta `Automatización transversal` dentro de Configuración con:

- todos los umbrales editables;
- límites de sugerencias/autocompletado;
- activación/desactivación de recordatorios en Dashboard;
- validación de rangos;
- modo solo lectura cuando falta `Configuración: Editar`;
- guardado protegido por RBAC.

## 7. Seguridad y auditoría

- GET de configuración: `Configuración: Ver`;
- actualización: `Configuración: Editar`;
- sugerencias/autocompletado/preview: usuario autenticado con permiso de Dashboard;
- reglas versionadas `M12.1`;
- cambio de preferencias registra usuario y fecha UTC;
- no existen escrituras automáticas financieras;
- no existen ajustes automáticos de stock;
- no existen confirmaciones automáticas de ventas/compras/cargas;
- no se exponen secretos;
- Producción no fue intervenida.

## 8. Migración

Migración:

`20260810174200_M12AutomatizacionTransversal`

Fue ejecutada en el gate M12 contra MySQL 8.4 con `sql_require_primary_key=ON` y verificada en `__EFMigrationsHistory`.

## 9. Regresión permanente

Backend:

`backend/tests/Solqaryn.Tests/M12AutomatizacionTransversalTests.cs`

Resultado certificado: **6/6 aprobadas, 0 fallos, 0 errores**.

Playwright:

`frontend/e2e/m12-automatizacion-transversal.spec.ts`

Resultado certificado: **5/5 aprobadas, 0 fallos, 0 omitidas, 0 errores**.

El E2E verifica configuración versionada, persistencia y restauración de preferencias, sugerencias no mutantes, preview masivo, autocompletado fail-closed y Dashboard real.

## 10. Defecto detectado y corregido por CI

La primera ejecución dedicada detectó cuatro desacoples nominales entre el motor nuevo y el modelo existente:

- `StockMinimo` debía ser `UmbralStockBajo`;
- `MovimientoFinancieros` debía ser `MovimientosFinancieros`;
- `EmpresaConfiguracion` debía ser `EmpresaConfiguraciones`;
- `Activo` debía ser `Activa` en `EmpresaConfiguracion`.

El gate falló antes de certificar, se corrigieron los nombres contra las entidades reales y la recertificación posterior quedó verde. No se deshabilitaron pruebas ni se relajaron validaciones.

## 11. Evidencia automática

Workflow: `M12 - Automatización transversal`  
Run: **`31417673610` — SUCCESS**  
HEAD: `76214a93564c2e91c8f4e4f82e83846a63db8a43`

Todos los pasos pasaron:

- MySQL 8.4;
- `sql_require_primary_key=ON`;
- backend Release;
- 6 pruebas backend M12;
- migración real y API healthy;
- lint Angular;
- build de producción;
- Angular real;
- 5 pruebas Playwright M12;
- artifact de evidencia.

Artifact:

- nombre: `m12-automatizacion-transversal`;
- ID: `9074157123`;
- SHA-256: `4092a31a7e9d1e23f8f1bfa4bc5956b85fc85b3b48ec2c1ea8dff73ed5d13a8d`;
- retención: 14 días.

Gate transversal `Desarrollo - Compilación y pruebas`, run `31417673706`: backend Release, frontend producción, Docker, higiene y migraciones/integración MySQL **SUCCESS**.

## 12. Archivos principales

Backend:

- `backend/src/API/Controllers/AutomatizacionesController.cs`;
- `backend/src/Application/DTOs/AutomatizacionDto.cs`;
- `backend/src/Application/Interfaces/IAutomatizacionService.cs`;
- `backend/src/Infrastructure/Services/AutomatizacionService.cs`;
- `backend/src/Infrastructure/Migrations/20260810174200_M12AutomatizacionTransversal.cs`;
- `backend/tests/Solqaryn.Tests/M12AutomatizacionTransversalTests.cs`.

Frontend:

- `frontend/src/app/core/models/automatizacion.model.ts`;
- `frontend/src/app/services/automatizacion.service.ts`;
- `frontend/src/app/features/dashboard/dashboard.component.ts`;
- `frontend/src/app/features/dashboard/dashboard.component.html`;
- `frontend/src/app/features/configuracion/automatizacion-configuracion-card.component.ts`;
- integración en `configuracion.component.ts/html`;
- `frontend/e2e/m12-automatizacion-transversal.spec.ts`.

CI:

- `.github/workflows/m12-automatizacion-transversal.yml`.

## 13. Cierre

**FASE M12 — Automatización transversal: ✅ COMPLETADA Y CERTIFICADA AUTOMÁTICAMENTE.**

Siguiente fase del Plan Maestro:

**M13 — Auditoría integral, hardening y certificación final.**
