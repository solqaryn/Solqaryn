# FASE M0 — Auditoría y mapa de impacto

Fecha de auditoría: 2026-08-08  
Repositorio: `jmejia31/Solqaryn`  
Rama auditada y única autorizada: `Desarrollo`  
Baseline auditado: `47b4ad51a1be130e7072c3191f61d89095270145`  
PR oficial: `#2 — Desarrollo -> main`

## 1. Propósito y límites

M0 determina qué existe realmente antes de ejecutar M1–M13. Esta fase no reconstruye 2A–2G ni introduce funcionalidad de negocio nueva. Producción permanece congelada, `main` no se modifica, no se crean ramas, no se realiza merge ni auto-merge y no se ejecutan cambios destructivos contra recursos productivos.

Los bloques 2A–2G continúan como baseline. No se detectó durante esta auditoría evidencia que justifique reconstruirlos.

## 2. Baseline Git/GitHub verificado

- `Desarrollo`: `47b4ad51a1be130e7072c3191f61d89095270145` al iniciar M0.
- `main`: `85b4e02814823e9671803c23798a6ff0bf05c8f6`, sin modificación.
- PR #2: abierto, Draft, `Desarrollo -> main`, no fusionado.
- Último commit previo a M0: `docs(plan): definir ciclo maestro de mejoras empresariales por fases [ChatGPT]`.
- El documento `docs/FASE2C_A_2G_CIERRE_FINAL.md` sigue siendo el cierre del baseline 2C–2G.
- El documento `docs/PLAN_MAESTRO_MEJORAS_EMPRESARIALES_2026.md` define el ciclo M0–M13 y no equivale a implementación.

### CI del baseline auditado

Sobre `47b4ad51...` se verificaron ejecuciones reales:

- `Desarrollo - aceptación funcional integral`: SUCCESS — run `31256980796`.
- `Desarrollo - Compilación y pruebas`: SUCCESS — run `31256980799`.
- `Fase 2 - Auditoría de configuración y dependencias`: SUCCESS.
- `Bloque 2C.1 - Variante técnica y migración`: SUCCESS.
- `Fase 8 - Validación completa automatizada`: SUCCESS — run `31256980839`.
- `Solqaryn CI`: SKIPPED; no se contabiliza como aprobado.

El repositorio posee además workflows dedicados para catálogos, CI general, Desarrollo, auditoría, variante técnica, escáner y validación integral.

## 3. Arquitectura real encontrada

### Backend

La solución está organizada en:

- `backend/src/API`
- `backend/src/Application`
- `backend/src/Domain`
- `backend/src/Infrastructure`

Hay separación real entre controladores, DTOs/servicios/interfaces, entidades/enums y persistencia/repositorios/servicios de infraestructura.

### Frontend

Angular está organizado en:

- `core`
- `features`
- `services`
- `shared`
- rutas lazy/standalone protegidas por autenticación y permisos.

### Persistencia

`AppDbContext` contiene modelos para productos, variantes, imágenes, categorías, catálogos, clientes/tipos, proveedores, usuarios/roles/permisos, compras, ventas, facturación/pagos, inventario, consumos administrativos, finanzas, descuentos, impuestos, costos de envío, auditoría, temas y cargas masivas.

Existe un historial amplio de migraciones y `ModelSnapshot`; el modelo actual no parte de cero.

## 4. Matriz maestra M1–M13

| Fase | Clasificación M0 | Resultado de auditoría |
|---|---|---|
| M1 Catálogos maestros | PARCIAL | Color, Talla, Marca y Modelo ya comparten `CatalogoProducto`; Categoría y TipoCliente tienen mantenimientos propios. No debe recrearse esta arquitectura. Métodos de pago siguen como enum y no existe un catálogo general de tipos/zonas de envío. |
| M2 Variantes 2.0 | PARCIAL | `ProductoVariante` y variante técnica existen. La variante comercial actual está centrada en Color; Talla permanece a nivel Producto. Las imágenes pertenecen a Producto, no a Variante. |
| M3 ISV/ISC persistente | EXISTE | `Impuesto` es entidad persistente, con CRUD, vigencia, alcance, histórico/snapshots. `SeedFiscalService` crea `ISV15`/`ISC5` solo si no existen y no reactiva ni modifica decisiones posteriores. M3 debe convertirse principalmente en certificación/regresión. |
| M4 Filtros/navegación persistente | PARCIAL | Existen filtros, búsqueda, paginación, ordenamiento y `Limpiar filtros` en Productos; otras listas tienen paginación/búsqueda. No existe persistencia general mediante query params + sessionStorage al navegar y regresar. |
| M5 Clientes/segmentación | PARCIAL | `TipoCliente`, predeterminado único y `SIN_CLASIFICAR` ya existen. Cliente expone tipo, total de ventas y total vendido. Faltan segmentación operativa, filtros por tipo, estadísticas/reportes/exportaciones orientados a clientes. |
| M6 Inventarios/gastos | PARCIAL | `TipoInventario` separa Mercadería/Insumo; existen ConsumoInsumo y Finanzas/GastoOperativo. Backend impide vender/facturar insumos administrativos. Falta completar experiencia frontend y reporting/valoración separados. |
| M7 Envíos profesionales | PARCIAL | `CostoEnvio` ya soporta monto, vigencia, prioridad, predeterminado, activo/soft delete; Venta guarda snapshots del nombre/monto y exoneración. Faltan geografía/modalidad e integridad concurrente del predeterminado a nivel BD. |
| M8 Búsqueda/rendimiento | PARCIAL | Productos buscan por nombre, marca, modelo, color, talla, SKU y código de barras; escáner/autocomplete y filtro de rendimiento existen. Faltan cobertura transversal completa y agregación/medición formal p50/p95. |
| M9 Cargas masivas | PARCIAL AVANZADO | Existen preview/validación, historial, conteos, errores descargables CSV/XLSX, plantillas, hash, reuso de validación, confirmación transaccional y lock de concurrencia; soporta Clientes, Proveedores, Colores, Productos y VariantesInventario. Falta evolución para atributos M2, progreso real por lotes/versionado/cancelación operativa si se aprueba. |
| M10 UI empresarial | PARCIAL | Existe `TemaVisual`, Angular Material, componentes compartidos y E2E responsive. Falta normalización transversal de tokens/componentes y cierre WCAG/teclado/foco/estados. |
| M11 Backups/restauración | NO EXISTE COMO MÓDULO | No se encontró entidad/controlador/servicio de aplicación dedicado a backup/restore. Debe diseñarse solo para Desarrollo/entornos descartables durante este ciclo. |
| M12 Automatización transversal | PARCIAL | Ya existen automatizaciones: variante técnica, cálculos, movimientos financieros automáticos, tipo de cliente por defecto, seeds idempotentes y flujos de confirmación. Falta auditoría transversal de fricción y automatizaciones adicionales sin perder trazabilidad. |
| M13 Auditoría/certificación | PENDIENTE | Es la puerta final después de M1–M12. |

## 5. Mapa de catálogos vs enums técnicos

### Catálogos/entidades administrables ya existentes

- Categoría.
- `CatalogoProducto`: Color, Talla, Marca, Modelo.
- TipoCliente.
- Rol y Permiso.
- Descuento.
- Impuesto.
- CostoEnvio.
- EmpresaConfiguracion.
- TemaVisual.

`CatalogoProducto` ya implementa CRUD, activar/desactivar, soft delete, auditoría, orden y relación Marca -> Modelo. Por tanto, M1 no debe crear entidades separadas Color/Talla/Marca/Modelo.

### Enums técnicos que deben conservarse como invariantes salvo requisito explícito

- `EstadoDocumento`.
- `EstadoFactura`.
- `EstadoPago`.
- `EstadoCargaMasiva`.
- `EstadoConsumoInsumo`.
- `EstadoMovimientoFinanciero`.
- `EstadoRevisionFinanciera`.
- `TipoMovimientoInventario`.
- `CausaMovimientoInventario`.
- `TipoMovimientoFinanciero`.
- `TipoInventario`.
- `TipoImpuesto`, `AlcanceImpuesto`, `OperacionImpuesto`.
- tipos/alcances técnicos de descuentos.
- módulos y acciones de permisos.

### Caso especial: MetodoPago

Actualmente es un enum persistido (`Efectivo`, `Transferencia`, `Tarjeta`, `Otro`) usado por ventas, facturas y finanzas. Es negocio-visible, por lo que M1 puede evaluar metadatos/configuración administrable, pero no debe reemplazarse por una FK de forma destructiva ni romper históricos. Cualquier evolución deberá conservar un código técnico estable y migración compatible.

## 6. Variantes, tallas, colores e imágenes

### Existe

- `ProductoVariante`.
- SKU y código de barras.
- stock/costo/precio/umbral por variante.
- estado activo/soft delete.
- variante técnica para producto simple.
- Color por variante.
- Talla/Marca/Modelo como catálogos.
- galería de múltiples imágenes por Producto con principal y orden.
- integración existente con compras/ventas/inventario/escáner/autocomplete.

### Brecha M2

- `ProductoVariante` no tiene `TallaId`.
- la combinación Color + Talla no es la identidad comercial actual de la variante.
- Talla está asociada actualmente a Producto.
- `ProductoImagen` no tiene `ProductoVarianteId`.
- no hay galería independiente por variante.

### Dirección M2

Evolucionar `ProductoVariante`; nunca crear un segundo sistema. La migración debe preservar la variante técnica y los datos históricos. Las imágenes existentes deben seguir siendo válidas como imágenes generales de producto y la asociación a variante debe ser compatible/nullable durante la transición.

## 7. ISV / ISC

M3 cambia de naturaleza tras M0.

Evidencia del código actual:

- `Impuesto` persiste configuración fiscal en MySQL.
- soporta porcentaje/monto fijo, vigencia, prioridad, alcance y operaciones.
- ventas/compras conservan snapshots de impuestos aplicados.
- edición de impuestos afecta operaciones futuras, no reescribe históricos.
- `SeedFiscalService` incluye `ISV15` e `ISC5` y consulta existencia por código antes de insertar.
- el seed no reactiva un impuesto existente ni restaura su tasa/estado.

Conclusión: no se justifica crear una nueva entidad `ConfiguracionISVISC`. M3 debe validar reinicio de API, migraciones descartables y regresión frontend/backend; solo se modificará código si esa certificación detecta una falla real.

## 8. Filtros y navegación

Productos ya dispone de:

- búsqueda con debounce;
- categoría;
- color;
- talla;
- marca/modelo dependiente;
- estado;
- página/pageSize;
- ordenamiento;
- botón `Limpiar filtros`.

Sin embargo esos valores viven en el componente. Ventas muestra el mismo patrón de estado local para búsqueda y paginación. No se detectó un patrón compartido que restaure automáticamente filtros desde query params/sessionStorage al volver desde detalle/edición.

M4 debe introducir una solución reutilizable, gradual y explícita, empezando por Productos, Ventas, Compras, Clientes, Inventario y Finanzas.

## 9. Clientes

`TipoCliente` no debe recrearse. La arquitectura actual resuelve el tipo predeterminado y el DTO de cliente ya incluye `TipoClienteId`, nombre/color del tipo, total de ventas y total vendido.

M5 queda limitado a:

- filtros por TipoCliente;
- KPIs/estadísticas segmentadas;
- reportes y exportación orientados a clientes;
- integración de segmentación en Dashboard donde aporte valor;
- extensión futura sin convertir el módulo en un CRM fuera de alcance.

## 10. Inventario administrativo y finanzas

La separación de dominio ya existe:

- `TipoInventario.MercaderiaVenta`.
- `TipoInventario.InsumoAdministrativo`.
- `ConsumoInsumo` / detalles.
- `MovimientoFinanciero` con `CategoriaMovimientoFinanciero.GastoOperativo`.

`AppDbContext.SaveChangesAsync` ejecuta una validación fail-closed que rechaza vender o confirmar una venta con un insumo administrativo.

M6 no debe crear un modelo paralelo. Debe completar:

- ruta/vista frontend explícita para insumos administrativos;
- consumo, búsqueda y reporting dedicados;
- valoración separada mercadería/insumos;
- UX que impida confundir gasto financiero con artículo inventariable;
- pruebas E2E de la separación ya impuesta por backend.

## 11. Costos de envío

Existe una base funcional relevante:

- mantenimiento persistente;
- monto;
- vigencia;
- prioridad;
- activo;
- predeterminado;
- soft delete;
- snapshots en Venta;
- exoneración y motivo.

Brecha de integridad detectada: el servicio desmarca otros predeterminados antes de guardar, pero `CostoEnvioConfiguration` solo crea un índice normal por `(Activo, EsPredeterminado)` y no una garantía única condicional/generada equivalente a `TipoCliente`. Dos escrituras concurrentes podrían depender únicamente de lógica de aplicación. Se clasifica P1 y debe resolverse quirúrgicamente en M7 con preflight, migración segura y prueba concurrente.

M7 también cubrirá, si el requisito sigue aprobado: zona/ciudad/departamento/modalidad y snapshots correspondientes.

## 12. Búsqueda y rendimiento

La búsqueda de Productos ya cubre:

- Nombre.
- Marca legacy y catálogo.
- Modelo legacy y catálogo.
- Color.
- Talla.
- Variante SKU.
- Variante código de barras.
- Color de variante.

Escáner/autocomplete ya están instrumentados mediante `MedirRendimientoBusquedaFilter`, sin registrar términos sensibles.

M8 deberá:

- ampliar cobertura transversal a cliente/proveedor/observaciones donde falte;
- evitar `Include`/materialización innecesaria en resultados ligeros;
- medir con datos representativos;
- consolidar p50/p95;
- crear índices únicamente después de evidencia de plan/latencia.

## 13. Cargas masivas

La implementación actual es considerablemente más avanzada que la descripción de M9:

- validación previa;
- vista previa persistida como JSON normalizado;
- hash del archivo;
- conteos de válidas/errores/advertencias/procesadas;
- historial;
- reporte de errores CSV/XLSX;
- plantillas CSV/XLSX;
- límites de archivo;
- validación XLSX;
- confirmación en transacción;
- lock de confirmación concurrente;
- soporte de VariantesInventario.

M9 no reconstruirá esta infraestructura. Se concentrará en compatibilidad M2, progreso/lotificación cuando sea necesario, cancelación segura si el procesamiento llega a ser asíncrono/largo y versionado de plantilla/esquema si aporta compatibilidad real.

## 14. Seguridad y permisos

### Controles observados

- JWT con issuer/audience/lifetime/signing key y `ClockSkew = 0`.
- validación de secreto JWT mínimo y rechazo de placeholder.
- CORS por lista configurada.
- rate limiting del login.
- security headers.
- Swagger condicionado al entorno/configuración.
- `[Authorize]` en controladores protegidos.
- `RequierePermiso(ModuloSistema, AccionPermiso)` verifica permisos en backend.
- `authGuard` + `permisoGuard` en frontend.
- módulos/acciones dinámicos y mantenimientos de Roles/Permisos.
- sesión frontend por actividad con 30 minutos de inactividad y renovación periódica mediante `/auth/renovar`.

No se detectó en M0 una regresión que obligue a reabrir el baseline de seguridad 2F/2G. M13 debe repetir el análisis integral después de todas las fases.

## 15. UX/UI y accesibilidad

Existe base de UI con Angular Material, componentes compartidos, `TemaVisual` persistente y E2E específicos de interfaz/responsive. No obstante, los estilos por feature siguen siendo numerosos y M10 debe cerrar consistencia transversal.

Pendiente para M10:

- inventario de tokens reales frente a CSS repetido;
- botones/headers/tables/cards/dialogs compartidos;
- estados loading/error/empty/disabled consistentes;
- foco visible y navegación por teclado;
- contraste y labels accesibles;
- validación responsive por viewports representativos;
- evitar usar E2E visual como sustituto de revisión WCAG.

## 16. Backups e integraciones externas

No se encontró un módulo de aplicación de backup/restore entre entidades, controladores o servicios auditados. M11 es una capacidad nueva.

M11 debe operar exclusivamente contra Desarrollo/entornos descartables y probar:

`backup -> checksum -> restore descartable -> verificación de integridad`.

Las integraciones externas existentes incluyen Cloudinary, SMTP, Vercel/Render/Aiven según configuración/documentación. M0 no modificó ninguna de ellas.

## 17. Pruebas y cobertura existente

Backend posee pruebas unitarias/integración dedicadas, entre otras, para:

- cálculo;
- catálogos;
- clientes;
- compras;
- snapshots de valoración;
- consumo de insumos;
- aislamiento de insumos en ventas;
- finanzas;
- seguridad de imágenes;
- concurrencia de inventario/documentos;
- permisos;
- autocomplete;
- escáner;
- variantes y variante técnica;
- cargas masivas/concurrencia.

Frontend posee Playwright/E2E para catálogos, escáner, autocomplete, compatibilidad, interfaz, responsive, variantes, cargas masivas, imágenes, facturación/impresión, reportes administrativos, correo, validación integral, aislamiento, filtros de productos y sesión/inactividad.

La existencia de pruebas no implica cobertura de los requisitos nuevos M1–M12; cada fase debe agregar/ajustar las pruebas de sus brechas reales.

## 18. Issues #3 y #4 re-auditados

### Issue #3 — Gmail/POS-80

Clasificación:

- Código y automatización SMTP/PDF: YA RESUELTO/AUTOMATIZADO según baseline y CI.
- afirmación histórica de que Fase 8 estaba bloqueada: OBSOLETA; Fase 8 posteriormente ejecutó SUCCESS.
- Gmail SMTP real en Desarrollo: REALMENTE PENDIENTE, requiere credencial/infra autorizada.
- recepción real del correo/PDF: REALMENTE PENDIENTE, validación externa.
- POS-80 driver/impresión física: REALMENTE PENDIENTE.

No se cierra el issue por CI porque sus pendientes actuales son físicos/externos.

### Issue #4 — Catálogos, sesión, accesibilidad y finanzas

Reclasificación de los bloques principales:

- modelo normalizado Color/Talla/Marca/Modelo: YA RESUELTO mediante `CatalogoProducto`.
- CRUD/activar/desactivar/soft delete de esos catálogos: YA RESUELTO.
- relación Marca -> Modelo: YA RESUELTO.
- integración básica con Producto: YA RESUELTO.
- soft delete de Categoría: YA RESUELTO.
- sesión por actividad y 30 minutos de inactividad: YA RESUELTO.
- finanzas con ingresos/egresos/utilidad/márgenes/valor inventario/cuentas: YA RESUELTO en backend y módulo existente.
- aislamiento de insumos administrativos: YA RESUELTO en backend; PARCIAL en experiencia frontend.
- filtros persistentes/navegación: PARCIAL.
- accesibilidad/UI transversal: PARCIAL.
- variantes Color+Talla e imágenes por variante: REALMENTE PENDIENTE dentro de M2.
- métricas/segmentación de clientes: PARCIAL/REALMENTE PENDIENTE dentro de M5.

El issue #4 no debe usarse como checklist literal de implementación sin esta reclasificación.

## 19. Deuda técnica y brechas priorizadas

### P0

No se identificó durante M0 una regresión P0 demostrada en el código inspeccionado. Esto no sustituye la auditoría final M13 ni las validaciones externas.

### P1

1. Integridad concurrente del único `CostoEnvio` predeterminado: lógica de aplicación sin restricción DB equivalente a TipoCliente.
2. Evolución M2 debe preservar inventario/histórico al mover Talla hacia variante; una migración ingenua puede fragmentar stock.
3. M11 carece todavía de capacidad verificable de restauración; se trata como brecha planificada, no como evidencia de pérdida actual.

### P2

- persistencia de filtros/navegación incompleta;
- reporting/UX de insumos separado incompleto;
- segmentación de clientes incompleta;
- búsqueda transversal y p50/p95 incompletos;
- normalización UI/accesibilidad incompleta;
- cargas masivas sin la evolución posterior a M2.

### P3

- consolidación documental de checklists históricos;
- estandarización adicional de componentes/tokens donde no afecte funcionalidad.

## 20. Alcance corregido de M1–M13

### M1

NO recrear Color/Talla/Marca/Modelo/Categoría/TipoCliente. Auditar y completar únicamente catálogos empresariales realmente configurables que aún falten. Prioridad: decisión compatible para `MetodoPago`; metadatos de envío se coordinan con M7. Estados de documentos/movimientos permanecen enums.

### M2

Extender `ProductoVariante` con Talla/combinación Color+Talla y galería por variante compatible con imágenes generales. Integrar con compras, ventas, inventario, facturación, cargas, escáner y autocomplete.

### M3

Fase de certificación fiscal, no reconstrucción: persistencia/reinicio, seed idempotente, snapshots y frontend. Cerrar sin código si las pruebas confirman el estado auditado.

### M4

Crear mecanismo compartido de estado de lista con query params + sessionStorage auxiliar y aplicarlo gradualmente.

### M5

Agregar segmentación, filtros, KPI, reportes y exportación usando `TipoCliente` existente.

### M6

Completar UI/reportes/valoración para insumo administrativo y reforzar E2E; conservar gasto financiero separado.

### M7

Fortalecer predeterminado único a nivel DB/concurrencia y ampliar geografía/modalidad/snapshots sin alterar históricos.

### M8

Completar búsquedas transversales y medición p50/p95; optimizar solo con evidencia.

### M9

Extender infraestructura existente para M2 y mejoras de procesamiento; no reescribir el motor actual.

### M10

Normalizar UI/UX/accesibilidad sobre `TemaVisual` y componentes existentes.

### M11

Diseñar backup/restore únicamente para Desarrollo/descartables con checksum y prueba real de restore.

### M12

Auditar automatizaciones existentes y agregar solo las que reduzcan trabajo sin perder control de dinero/stock/auditoría.

### M13

Auditoría integral, regresión completa y certificación con CI real, separando automatizado de físico/externo.

## 21. Criterios de cierre de M0

M0 queda técnicamente cerrable cuando:

1. baseline Git/PR/CI ha sido verificado;
2. código backend/frontend/BD y pruebas han sido inspeccionados;
3. M1–M13 están clasificados por existencia real;
4. catálogos se distinguen de enums técnicos;
5. variantes/tallas/colores/imágenes están trazados;
6. ISV/ISC, envíos, inventarios, finanzas, cargas y búsquedas están clasificados;
7. issues #3/#4 están reconciliados contra código actual;
8. deuda P0–P3 está documentada;
9. el alcance siguiente, M1, está reducido a brechas reales;
10. no se ha tocado Producción, `main`, merge/auto-merge ni recursos externos.

## 22. Cambios realizados por M0

M0 no modifica código funcional, entidades, migraciones, configuración, secretos, infraestructura ni Producción. El único cambio previsto para el cierre es este documento de auditoría en `Desarrollo`.

## 23. Estado posterior a M0

- 2A–2G: baseline conservado.
- M0: auditoría concluida a nivel repositorio y documentada.
- Próxima fase: M1, con alcance corregido por esta auditoría.
- Producción: congelada.
- `main`: sin cambios.
- PR #2: debe permanecer Draft y sin merge.
- Validaciones físicas/externas: permanecen separadas y pendientes donde corresponda.
