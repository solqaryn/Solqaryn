# SOLQARYN V5 — C05-01.M02.03: persistencia e índices conceptuales, migración segura y fixtures de sistema de diseño

**Autor:** PRIMARY DEV 03 — JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15. **Clasificación:** R2 por frontera de persistencia/tenancy, con **diff de alcance exclusivamente documental**; no altera EF/MySQL ni instala migraciones.

## 1. Autoridad, antecedente y decisión de no ejecución

La tarea `C05-01.M02.03` solicita «Definir persistencia, índices, migraciones y datos de prueba» en el catálogo de design tokens/branding. El plan autorizado es el **STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL** en Google Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Al reclamar: PLAN H730=PENDIENTE, I730 vacío, COLA G730/O730=SI y DAG A691 F=VALIDADO/G=SI; `C05-01.M01.01–06` LISTO con A5 independiente. Las notas `C05-01.M02.01` y `C05-01.M02.02` documentan, respectivamente, DS01–DS14/INV01–18 y API01–08/DTO/errores, sin instalar modelos persistidos.

**Alcance exacto:** propuesta de modelo de almacenamiento, índices, migraciones y fixtures que debe someterse a análisis y CI posteriores. Este Markdown **NO crea ni altera** tablas, índices, entidades EF, `AppDbContext`, `StorageTenantContext`, migraciones versionadas, scripts SQL, contratos API o configuraciones. No consulta ni escribe Aiven ni ejecuta un MySQL 8.4 efímero; una prueba/rollback descrito aquí es **FUTURO_NO_EJECUTADO**. Conservar el diseño vigente Angular 22.2.1/ASP.NET Core 10/EF Core 10.0.12/Oracle MySql.EntityFrameworkCore 10.0.9 hasta autorización específica de código. Ninguna decisión documental autoriza DB dedicada, producción ni cambio de proveedor.

## 2. Familias de persistencia PROPUESTAS (DB01–DB12), no creadas

| ID | Tabla/lógica candidata | Claves propuestas, ownership e invariantes |
| --- | --- | --- |
| DB01 | `DesignTokenDefinition` | PK `TokenId`; nombre único semántico global, tipo/allowlist, sin valor ejecutable de CSS |
| DB02 | `DesignTokenRevision` | PK `TokenId, Revision`; FK DB01, inmutable/publicado, revisión monotónica |
| DB03 | `ComponentPattern` | PK `PatternId, Version`; manifiesto global; estados/roles visuales, sin conceder RBAC |
| DB04 | `ComponentTokenBinding` | PK `PatternId, PatternVersion, TokenId`; FKs DB02/03, impedir referencias sin versión |
| DB05 | `ThemePreset` | PK `PresetId, Version`; catálogo global aprobado, estado y manifiesto de accesibilidad |
| DB06 | `TenantThemeAssignment` | PK `AssignmentId`, índice único lógico `EmpresaId, ActiveEpoch` cuando activo; control CAS de versión |
| DB07 | `TenantThemeOverride` | PK `EmpresaId, AssignmentId, TokenId`; FK DB06 y DB01, tipo/valor allowlist |
| DB08 | `TenantBrandAsset` | PK `EmpresaId, AssetId`; referencia opaca y verificación servidor de ownership, nunca DSN/URL libre |
| DB09 | `ThemeActivationIntent` | PK `EmpresaId, IntentId`; `ExpectedRevision`, actor, idempotency key y transición validada |
| DB10 | `ThemeAuditTrail` | PK `EmpresaId, EventId`; append-only, actor y correlación saneados, sin tokens ni PII |
| DB11 | `AccessibilityEvidence` | PK `ComponentVersion, PresetVersion, Viewport, EvidenceSha`; resultado sólo si hubo test browser real |
| DB12 | `ThemeSessionEpoch` | PK conceptual `EmpresaId, SessionEpoch`; invalidación/TTL segura para respuesta A→B; nunca concesión de sesión |

**Riesgo de cardinalidad:** la restricción «una asignación activa por empresa» debe diseñarse con el proveedor Oracle concreto y una transición transaccional de `ActiveEpoch`; un índice parcial estilo SQL Server no se supone disponible en MySQL. En MySQL se debe verificar una alternativa soportada, lock/fence o índice funcional/columna calculada si el proveedor lo permite. No usar `UNIQUE(EmpresaId, ActiveEpoch)` como prueba de exclusividad si cada epoch diferente pudiera seguir activo. El contrato exige **a lo sumo una ACTIVA real** con CAS y pruebas de carrera, no propone DDL engañoso.

## 3. Índices propuestos IDX01–IDX12 (sin DDL)

| ID | Acceso futuro | Condición de seguridad y rendimiento a comprobar |
| --- | --- | --- |
| IDX01 | DB01 por nombre y versión | Unicidad global de clave semántica normalizada sin colisión |
| IDX02 | DB02 por TokenId/Revision descendente | Lectura de revisión vigente con consistencia/inmutabilidad |
| IDX03 | DB03 por PatternId/Version | Recuperación determinística de componente, sin payload ajeno |
| IDX04 | DB04 por PatternId/TokenId | Detectar binding huérfano y alias circular antes de resolver |
| IDX05 | DB05 por PresetId/Version | Tema global seguro, sin fallback de otro tenant |
| IDX06 | DB06 por EmpresaId/estado/revisión | Buscar tema propio; exclusividad ACTIVA requiere transacción y constraint verificables |
| IDX07 | DB07 por EmpresaId/AssignmentId | Filtrar en DB y Application, denegar lectura cruzada |
| IDX08 | DB08 por EmpresaId/AssetId | Owner tenant y asset autorizado, sin URL libre |
| IDX09 | DB09 por EmpresaId/IntentId | Idempotencia, pertenencia y revisión en una operación |
| IDX10 | DB09 por EmpresaId/IdempotencyKey | Reintento mismo tenant/operación, conflicto payload distinto |
| IDX11 | DB10 por EmpresaId/Fecha/EventId | Auditoría tenant, paginación estable y retención sin secreto |
| IDX12 | DB11 por SHA/ThemeVersion/Viewport | Trazabilidad de test real y descarte tras cambio de código |

No se afirma selección de índices por el optimizador. Antes de crear cualquier índice se requiere carga representativa aislada, estadísticas, EXPLAIN pertinente y test real de aislamiento A/B en CI MySQL 8.4; ninguna operación física de este tipo se realizó en M02.03.

## 4. Secuencia de migración condicional MIG01–MIG12 (NO EJECUTADA)

1. **MIG01:** inventariar `AppDbContext`, mapeos y migraciones actuales sobre el SHA aprobado; no duplicar tablas o tenant filters.
2. **MIG02:** obtener aprobación de alcance para una migración versionada aditiva, compatible con esquema y Oracle EF vigente.
3. **MIG03:** preparar DDL aditivo con claves/FK, longitudes acotadas, índices y constraints efectivamente soportados por MySQL 8.4.
4. **MIG04:** preparar datos semilla sintéticos de catálogo global sin marcas/PII de clientes.
5. **MIG05:** ejecutar en MySQL 8.4 **efímero**, con reset entre escenarios y el proveedor Oracle real, no InMemory como sustituto.
6. **MIG06:** comprobar migración forward, FKs y unicidad de assignment ACTIVA mediante dos transacciones concurrentes.
7. **MIG07:** demostrar que SELECT/UPDATE/DELETE de empresa B no acceden a filas ni assets de A con JWT/RBAC/tenant autorizados.
8. **MIG08:** probar revocación de membresía, rol y firma/sesión vencidas antes de cualquier cambio de tema.
9. **MIG09:** simular replay de intent idempotente y colisión de payload, sin duplicación de eventos o versiones.
10. **MIG10:** verificar rollback hacia esquema compatible, incluyendo snapshot/restore **realmente ejecutado** en base efímera.
11. **MIG11:** guardar logs de job, filtros `Category=Integration`, conteo PASS/FAIL/SKIP, SHA y blobs de la solución.
12. **MIG12:** sólo después de A0–A4 exact-SHA y A5 independiente, evaluar la autorización de otro entorno separadamente; nunca desplegar desde este contrato.

**Regla de reversión:** `git revert` de este Markdown NO restaura filas ni esquemas. Un rollback de datos necesita snapshot, restore e integridad probados con autorización, fuera de este scope.

## 5. Fixtures sintéticos F01–F10 y negativos obligatorios

| Caso | Positivo FUTURO | Negativo FUTURO que debe fallar |
| --- | --- | --- |
| F01 | Tenant A lee su Assignment y ThemePreset permitido | Tenant B lee DB06/07 de A |
| F02 | Dos revisiones históricas, una ACTIVA | Dos transacciones activan revisiones distintas simultáneamente |
| F03 | Binding DS04 referencia token y revisión existentes | FK huérfana o ciclo de alias llega a CSS |
| F04 | Override tiene tipo/color en allowlist | Payload CSS/JS/URL arbitraria se persiste o ejecuta |
| F05 | Asset opaco pertenece a A con acceso vigente | Asset/host de B se filtra a A |
| F06 | Reintento con misma clave/payload devuelve un único intent | Payload cambiado o tenant B reutiliza clave |
| F07 | Desactivar rol bloquea propuesta | JWT anterior o caché repone permiso revocado |
| F08 | A→B cambia epoch e invalida cache/respuesta A | Request tardío reaplica branding de A |
| F09 | Evidencia Chromium real guarda SHA, viewport y resultado | Documento sin browser marca DS11 como PASS |
| F10 | Migración efímera permite restore y rollback verificables | `git revert` se trata como backup de MySQL |

Fixtures deben usar sólo empresas ficticias A/B, usuarios ficticios, assets de prueba seguros y secretos efímeros. Deben ser **capaces de fallar** ante ausencia de filtro EmpresaId, error de revocación, carrera de versión o inyección. No se han generado seeds ni ejecutado tests con estos casos.

## 6. Protección de frontera y limitaciones

Los campos `EmpresaId` recibidos del cliente no son autoridad; backend JWT/RBAC, pertenencia persistida y ownership de cada fila/asset se revalidan. No se almacenan contraseñas, JWT, DSN, connection strings, HTML/JS sin saneamiento ni PII de prueba. Evitar eventos fuera de orden y respuesta de tenant anterior; el uso de un theme nunca habilita operaciones comerciales. No tocar tabla empresarial real, información de Aiven, middleware de auth, proveedor de medios, secrets, QA/main/PROD. No afirmar que el schema ya contenga DB01–DB12, que existan índices o que Category=Integration pasó.

## 7. A0–A5 reales para ESTE documento y handoff

**A0:** verificar definición de DB01–DB12, IDX01–IDX12, MIG01–MIG12, fixtures F01–F10, fuentes M01 y M02.01/02; diff Markdown+CHANGELOG contiguo únicamente. **A1:** comprobar consistencia de cardinalidades, constraints provisionales y 10 pares P/N futuros, sin atribuir ejecución. **A2:** inspección R2 estática de propiedad EmpresaId, RBAC/JWT, exclusividad bajo carrera, idempotencia, inyección, redacción y rollback: ninguna prueba de runtime real se atribuye. **A3:** compatibilidad documental con MySQL/Oracle EF Core y arquitectura actual; prueba MySQL/EF/HTTP/Chromium = `NO_APLICA` **a un diff exclusivamente Markdown**, no PASS. **A4:** verificar fast-forward/CAS en dev, SOURCE_SHA, blobs, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA exacto, readback PLAN/COLA/DAG/BITACORA; otros checks en curso no son PASS. **A5:** revisión independiente por otra Task Javier distinta de la autora, con capacidad de A5_FAIL y pruebas adicionales para nuevos cambios.

**Salida:** definición de persistencia/índices/migración/fixtures **conceptual**, sin código EF/DDL ni datos. Revertir únicamente con `git revert` normal del commit documental contra HEAD válido, preservando historial y cambios de otros autores. No introducir pruebas falsas ni promover a QA/main/PROD.
