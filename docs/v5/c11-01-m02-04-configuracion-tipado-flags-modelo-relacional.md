# SOLQARYN V17 — C11-01.M02.04: configuración tipada, ámbitos y feature flags del modelo relacional

**Unidad:** `C11-01.M02.04` · PLAN fila 1595, COLA 1595, DAG_TECNICO 1556. **Autor técnico:** PRIMARY DEV01 Javier, Task `6adbb9fff1548191a1bd5b30d9e9984a`. **Riesgo:** R0 documental; `PROJECT_SCOPE_LOCK=STRICT`, `solqaryn/Solqaryn@dev`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL en Google Drive, PLAN/COLA/DAG vivos, Skill canónica y política V15–V17.

## A0 — Propósito, contrato y NO ACTIVACIÓN

Definir un catálogo **conceptual** de opciones tipadas y precedencia segura para una futura evolución del modelo relacional de Empresa, Sucursal, UsuarioEmpresa, roles, Cliente, Producto, Venta, Factura, inventario y auditoría. **No crea clases de opciones, variables de entorno, feature flags ejecutables, columnas, migraciones, filtros EF, endpoints, secretos ni operaciones de proveedor.** No cambia la arquitectura vigente Angular 22.2.1 / ASP.NET Core 10 / EF Core 10.0.12 / Oracle MySql.EntityFrameworkCore 10.0.9 / MySQL 8.4, ni concede permiso para QA/main/PROD, Aiven real o compras.

Fuentes técnicas ya versionadas: `docs/v5/c11-01-m02-01-entidades-ownership-relaciones-invariantes.md` (blob `d1b1a09a096fdfbaa2f2acacb33e53f05bff39fc`, E01–E16/I01–I16); `docs/v5/c11-01-m02-02-api-dto-errores-validacion-compatibilidad.md` (blob `9f0d574d4ed01d4a3d3aed142a7cb67db1b9d402`, OP01–OP08/D01–D12/ER01–ER16). M01.06 conserva su compuerta documental A5. M02.03, migraciones reales, índices físicos, implementaciones R2 y supuestos de compatibilidad siguen siendo trabajos separados; no afirmar que hayan aprobado por este catálogo.

**Frontera autorizante:** el backend obtiene `EmpresaId` de identidad y membresía vigentes; la UI y los DTO no conceden acceso. `UsuarioEmpresa` y el rol deben verificarse en cada acción. `Cliente.EmpresaId` o `Sucursal.EmpresaId` NULL históricos exigen cuarentena y reconciliación, no asignación automática. Toda opción desconocida, mal tipada, vencida, sin fuente confiable o sin ownership comprobable se interpreta como DENY. Las opciones no sustituyen JWT/RBAC, la validación por recurso ni la atomicidad transaccional.

## Doce opciones tipadas propuestas (F01–F12, ninguna existe por este documento)

| Código y campo conceptual | Tipo / default seguro propuesto | Ámbito y protección |
| --- | --- | --- |
| F01 RelationalModelReadVNext | bool=false | plataforma+empresa, read-only primero y guard backend |
| F02 RequireVerifiedEmpresaOwnership | bool=true, inmutable por tenant | rechazar nulos y cross-tenant al autorizar |
| F03 RejectLegacyNullOwnerWrites | bool=true, inmutable por tenant | no asignar Cliente/Sucursal NULL por inferencia |
| F04 MembershipRevalidationEnabled | bool=true, inmutable por tenant | consultar UsuarioEmpresa y revocaciones |
| F05 CompositeTenantKeyDraftEnabled | bool=false | plataforma, solo diseño de futuras FK compuestas |
| F06 ControlledLegacyBackfillEnabled | bool=false | ambiente DEV aislado y evidencia de decisión por fila |
| F07 SafeReadOnlySupportEnabled | bool=false | soporte temporal; cero SaveChanges y cero SQL de escritura |
| F08 TypedFiltersOnlyEnabled | bool=true, inmutable por tenant | tipos/filtros allowlist, sin SQL/JS arbitrario |
| F09 ScopedIndexesExperimentEnabled | bool=false | solo MySQL 8.4 efímero, EXPLAIN y fixture sintético |
| F10 VersionedUnitOfWorkEnabled | bool=false | mutaciones futuras con revisión/CAS y transacción |
| F11 AuditRedactionStrictEnabled | bool=true, inmutable por tenant | sin token, connection string, SQL sensible ni PII cruda |
| F12 SchemaMigrationExecutionEnabled | bool=false | migración real requiere permiso, backup/rollback y CI R2 |

**Ningún valor F01–F12 fue configurado ni activado.** Los valores true de F02/F03/F04/F08/F11 son requisitos de seguridad futuros, no pruebas de mecanismos activos ni conmutadores que un administrador de tenant pueda apagar. F06/F09/F12 no otorgan permiso para cambiar datos Aiven o esquemas por tener default false. `F10=true` tampoco reemplazaría un rollback real de Venta/Stock/Factura/Pago.

## Contrato tipado, precedencia y concurrencia propuestos

`RelationalSafetyConfigSnapshot` conceptual: `SchemaVersion`, `Revision`, `Source` confiable, `Environment`, `EvaluatedAtUtc`, `ExpiresAtUtc`, `TenantScope` derivado exclusivamente del backend, `PolicyVersion`, los doce valores tipados y `DecisionReason` opaco. El snapshot no expone secretos, empresa de terceros, claves de acceso, SQL, registros de clientes ni datos de pagos. No se define una clase C# ni un DTO HTTP por esta entrega.

Precedencia restrictiva: **Scope Lock/entorno no permitido > autenticación y membresía revocadas > propietario NULL o ambiguo > prohibición global > prohibición de empresa/sucursal/recurso > permisos RBAC insuficientes > bloqueo de seguridad > flags habilitantes**. Un `true` particular nunca anula denegaciones anteriores. Un fallo de lectura de configuración, excepción, timeout, estado corrupto, revisión no monotónica o expiración debe producir DENY sin reusar un snapshot obsoleto para escribir.

Las futuras modificaciones de configuración exigen revisión CAS/`IfMatch`, `Revision` monotónica, token de servicio con privilegio mínimo y auditoría redactada; si se reciben dos actualizaciones concurrentes, sólo la versión vigente puede sobrevivir. Al revocar una membresía, invalidar permisos efectivos y caches; el soporte read-only nunca autoriza una escritura por tracking ni SQL directo.

## Reglas P01–P12 de seguridad para una futura implementación

| Regla | Rechazo requerido |
| --- | --- |
| P01 | Ausencia de identidad Empresa autenticada deniega |
| P02 | Tenant B nunca hereda banderas, datos ni sucursales de tenant A |
| P03 | `Cliente.EmpresaId=NULL` o `Sucursal.EmpresaId=NULL` no concede acceso |
| P04 | UsuarioEmpresa revocado prevalece sobre caché, rol legacy y flag true |
| P05 | Soporte con solo lectura no realiza `SaveChanges` ni escritura SQL |
| P06 | Deshabilitar seguridad estricta por usuario/tenant se rechaza |
| P07 | Índices/FK globales no sustituyen claves tenant scoped comprobadas |
| P08 | Backfill no atribuye un huérfano por defecto o heurística |
| P09 | Operación de venta/stock/factura/pago revierte completamente ante error |
| P10 | Cambios de schema sin backup, MySQL efímero y ruta rollback = NO_GO |
| P11 | Snapshots vencidos o revisiones viejas no vuelven a habilitar writes |
| P12 | Auditoría nunca expone JWT, SQL sensible, PII ni conexión DB |

## T01–T12: criterios positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro (en prueba autorizada) | Negativo capaz de encontrar defecto |
| --- | --- | --- |
| T01 | Empresa A consulta recurso A vigente | Token A fuerza ID o filtro B |
| T02 | Rol activo permite lectura acotada | Membresía revocada o RolId legacy |
| T03 | Registro legado NULL permanece intacto | NULL se asigna a A sin conciliación |
| T04 | Soporte consulta reporte sin mutar | Tracking SaveChanges o SQL UPDATE |
| T05 | Filtro tipado devuelve páginas estables | SQL/JS arbitrario o límite excesivo |
| T06 | FK compuesta valida padre/hijo A | Padre B se vincula a hijo A |
| T07 | Índice tenant scoped admite claves paralelas A/B | Índice global bloquea injustamente B |
| T08 | Migración EF en MySQL efímero aplica/rollback | Migración destructiva o fuga de datos reales |
| T09 | Venta y factura confirman transacción completa | Fallo de stock deja pago parcial |
| T10 | Versión vigente hace CAS de configuración | Escrito stale supera revocación nueva |
| T11 | Audit log redactado conserva traceId opaco | JWT, cadena DB o PII sale en log |
| T12 | Falta de origen confiable provoca DENY | Fallback permisivo o flag de frontend concede |

**Resultados reales de esta entrega:** 0/12 pares ejecutados; no se modificó MySQL/EF, JWT runtime, API, frontend, Playwright, servicios, secretos ni permisos. Las futuras pruebas R2 deberán usar MySQL 8.4 aislado y `Category=Integration` real cuando haya cambios de persistencia, además de rechazos A/B, revocación, concurrencia y rollback. Un build verde de un diff Markdown no representa esa certificación.

## A0–A5, puertas y reversibilidad

A0: diff circunscrito a este documento y sección autocontenida en `CHANGELOG_AI.md`, fuente V5, PLAN H/I, COLA G/O, DAG F/G y V17 lock exclusivo. A1: inspección estática de doce opciones tipadas, doce políticas y doce pares futuros — distinguir contrato de tests ejecutados. A2: revisar concepto de tenant A/B, legacy NULL, roles, soporte read-only, PII, inventario/finanzas y fail-closed sin atribuir pruebas de runtime. A3: MySQL/EF/.NET/HTTP/Playwright/proveedores **NO_APLICA R0** por no modificar código o esquema; no atribuir PASS a mocks. A4: publicar por fast-forward y expected SHA, comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA publicado, verificar blobs y PLAN/COLA/BITACORA readback. A5 requiere Task Javier distinta, revisión independiente con posibilidad de rechazo; LISTO_DEV no lo sustituye.

Rollback documental: `git revert` aditivo después de releer HEAD, sin reescribir historia, eliminar BITACORA ni tocar fórmulas COLA/DAG. La implementación posterior de índices, EF, servicios, migraciones o despliegues se bloquea hasta autorización técnica real, pruebas causales y aislamiento comprobado. Ningún A5 aquí permite QA/main/PROD.
