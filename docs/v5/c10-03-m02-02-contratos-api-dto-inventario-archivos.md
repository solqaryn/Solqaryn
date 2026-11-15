# SOLQARYN V5 — C10-03.M02.02: contratos API, DTO, errores, validaciones y compatibilidad de archivos

**Unidad:** C10-03.M02.02 · PLAN_MAESTRO fila 1521 · DAG_TECNICO fila 1482 · **Autor:** PRIMARY DEV 01 / `6adbb9fff1548191a1bd5b30d9e9984a`. **Rama exclusiva:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). Fecha 2026-11-15.

## Alcance, dependencias y límites verificables

El lote C10-03 prepara el almacenamiento futuro de archivos condicionado por un **inventario histórico auténtico**. El estado histórico actual es `NO_VERIFICADO`: ni `CERO_VERIFICADO` ni `POSITIVO_VERIFICADO` se pueden inferir sin pruebas. Sólo un inventario autenticado y conciliado igual a cero permitirá omitir exportación, copia y checksum de históricos; un inventario positivo exigiría manifiesto e integridad antes de cualquier migración autorizada. Cualquier inconsistencia bloquea exclusivamente la operación afectada.

Se diseñan **contratos conceptuales futuros**, no rutas HTTP existentes. No se añaden endpoints, DTO C#, migraciones, tablas, workers, Cloudflare/R2, DNS, cambios a Cloudinary, secretos, permisos reales, configuración, tráfico, datos ni conexiones externas. Sin autorización separada no existen lectura de fuentes históricas, cutover ni despliegue. Mantener Cloudinary y la arquitectura .NET 10/Angular 22.2.1/MySQL Oracle EF Core declarada sin cambiarla.

**Preflight causal:** `C10-03.M01.01–06` tienen LISTO y A5 independiente en PLAN; `DAG_TECNICO!F1482=VALIDADO/G1482=SI`, `COLA!G1521=SI/O1521=SI`, `PLAN_MAESTRO!H1521=PENDIENTE/I1521` vacío antes del CLAIM exclusivo confirmado con CAS H (1 coincidencia). `C10-03.M02.01` se desarrolla de forma separada bajo otra Task: este documento no lo modifica ni lo usa como A5 propia.

## Operaciones futuras OP01–OP12, NO REGISTRADAS EN ROUTING

| Operación conceptual | Entrada propuesta mínima | Autorización/resultado seguro requerido |
| --- | --- | --- |
| OP01 Consultar estado de inventario | Alcance permitido y versión esperada | Sólo estado UNKNOWN/ZERO_VERIFIED/POSITIVE_VERIFIED/INCONSISTENT y fecha autorizada; sin inferir conteo cero |
| OP02 Solicitar conciliación de fuentes | Identificador opaco de tarea y fuentes allowlisted | JWT/RBAC de operador, permiso por fuente, estado pendiente; nunca exportar implícitamente |
| OP03 Consultar resumen conciliado | Referencia y versión | Conteos y discrepancias saneados por ámbito; sin keys, credenciales ni datos de otro tenant |
| OP04 Consultar manifiesto propuesto | Referencia de conciliación validada | Sólo metadata autorizada y digest, nunca contenido sensible ni objetos de otro tenant |
| OP05 Proponer ingreso de archivo nuevo | Propósito, tamaño, hash, MIME, idempotencyKey | Identidad/tenant servidor, cuota/bytes/MIME y versión; emisión de intención limitada, sin upload real por esta fase |
| OP06 Confirmar ingreso propuesto | Referencia opaca, digest, longitud y versión | Verificar integridad y propiedad; mismatch no produce publicación ni estado completado |
| OP07 Consultar metadata de archivo | Referencia opaca/version | Retornar sólo campos saneados propios, sin bucket/key privada ni enlace firmado |
| OP08 Solicitar capacidad de lectura | Referencia, versión, propósito, verbo | Revalidar JWT/RBAC/tenant, retención, método, TTL y política de replay antes de cualquier firma futura |
| OP09 Revocar emisión futura | Recurso/version/expectedVersion | Bloquear nuevas capacidades; no prometer invalidación de enlaces bearer ya emitidos sin mecanismo real |
| OP10 Consultar política de retención | Recurso y clasificación | Decisión restrictiva; ausencia/hold impide eliminación; sin cambiar retenciones reales |
| OP11 Previsualizar eventual traslado | Manifiesto probado y versión | Comparación inerte de estados y gaps, sin mover archivos, DNS ni proveedor |
| OP12 Consultar auditoría saneada | Referencia, rango UTC y rol | CorrelationId/eventos redactados, sin tokens, secretos, URLs firmadas ni PII |

Cada operación requiere contrato de autenticación, autorización server-side y límites anti-IDOR/BOLA. Los nombres OP son **propuestas de diseño**: no constituyen promesa de endpoint, permiso ni URL desplegada.

## DTO conceptuales V1 y compatibilidad

| Contrato propuesto | Campos públicos mínimos | Regla restrictiva |
| --- | --- | --- |
| `InventarioEstadoViewV1` | state, observedAtUtc?, evidenceRef?, sourceCount? | UNKNOWN no transforma null en cero; evidenceRef opaca |
| `FuenteConciliacionIntentV1` | sourceRefs, expectedVersion, correlationRef | Fuentes allowlisted y privilegio operador; sin credenciales |
| `ConciliacionResultadoV1` | state, totalCount?, gapCount?, manifestDigest? | Conteos sólo después de autenticación y conciliación |
| `ManifestPreviewV1` | opaqueManifestRef, version, entryCount, digest | No lista rutas internas ni datos de otro tenant |
| `NuevoArchivoIntentV1` | purpose, expectedBytes, digest, mediaType, idempotencyKey | Tenant/owner se resuelven por servidor; MIME real por bytes |
| `ArchivoResultadoV1` | opaqueFileRef, version, safeState, digestRef | Nunca bucket/key, token proveedor ni URL privada |
| `ArchivoMetadataV1` | opaqueFileRef, version, length, classification, state | Acceso sólo a dueño y versión autorizados |
| `LecturaCapacidadIntentV1` | opaqueFileRef, version, purpose, method | Firma futura limita actor/tenant/objeto/método/TTL |
| `RetencionDecisionV1` | policyRef, holdState, canDelete, reasonCode | canDelete=false por defecto y ante evidencia insuficiente |
| `TrasladoVistaPreviaV1` | opaqueManifestRef, sourceDigest, gapCount, decision | Vista preliminar no equivale a autorización de traslado |
| `ErrorArchivoSeguroV1` | code, correlationRef, retryable | Sin PII, path, firma, token ni stacktrace |
| `CursorArchivosV1` | opaqueCursor, pageSize, nextCursor? | Cursor protegido y ligado a tenant/filtros/rol |

No imponer estos nombres al código existente sin revisar `ApiResponse`, DTO de archivos y contratos Cloudinary. Proponer compatibilidad aditiva: versión V1 inmutable una vez publicada, lectores viejos desconocen campos nuevos sin fuga, valores enum desconocidos deniegan, fechas UTC, tamaño integer no negativo, digest canónico, JSON acotado, paginación determinista y errores estables. Ningún `EmpresaId`, hostname, slug o prefix suministrado por cliente confiere autoridad.

## Errores futuros E01–E16: denegación por defecto

| Código | Condición y salida futura |
| --- | --- |
| E01 | JWT ausente, expirado o revocado: rechazo sin capacidad |
| E02 | Rol/capacidad insuficiente: acceso denegado sin enumeración |
| E03 | Referencia de tenant B presentada por A: mismo resultado seguro que recurso ajeno no visible |
| E04 | Intento de elevar owner mediante campo cliente: ignorar o rechazar |
| E05 | Fuente histórica fuera de allowlist: no leer, exportar ni registrar credenciales |
| E06 | UNKNOWN tratado como cero: error de precondición, cero sin certificado prohibido |
| E07 | Inventario inconsistentemente conciliado: NO_GO de migración; conservar fuentes |
| E08 | Tamaño o cuota excedidos: no publicar archivo parcial |
| E09 | MIME por extensión contradice bytes: rechazo/cuarentena, sin lectura pública |
| E10 | Digest o longitud inválidos: integridad no certificada |
| E11 | TTL, método o firma inválidos, replay no autorizado: denegar |
| E12 | Misma clave idempotente con payload distinto: conflicto y ninguna duplicación |
| E13 | Retención/hold/expiración normativa: prohibir borrado físico |
| E14 | Proveedor inaccesible o no autorizado: error controlado, sin fallback público |
| E15 | Version/CAS obsoletos: conflicto sin sobrescribir cambios simultáneos |
| E16 | Riesgo de PII, token, URL firmada o stacktrace: respuesta y telemetría redactadas |

No afirmar códigos HTTP exactos de rutas inexistentes; los futuros 401/403/404/409/422/429/503 deberán alinearse con la política existente y evitar oráculos de pertenencia entre tenants.

## Matriz P01–P12 / N01–N12 para aceptación FUTURA_NO_EJECUTADA

| Par | Positivo futuro que deberá demostrarse | Negativo obligatorio |
| --- | --- | --- |
| 01 | Cero histórico autenticado omite tareas históricas | UNKNOWN se interpreta como cero |
| 02 | Inventario positivo da manifiesto conciliado | Fuente faltante se marca completa |
| 03 | Inconsistencia mantiene origen y NO_GO | Diferencia dispara cutover |
| 04 | Operador habilitado ve sólo fuentes permitidas | Rol revocado consulta fuente |
| 05 | Tenant A obtiene metadata propia | Tenant A enumera archivo de B |
| 06 | Bytes/MIME/size coinciden en fixture seguro | Extensión engañosa se acepta |
| 07 | Clave idempotente con mismo digest no duplica | Misma clave con distinto digest se acepta |
| 08 | Firma futura limita versión, verbo y TTL | Replay o capacidad vencida funciona |
| 09 | Hold/retención impide borrado | Proceso asíncrono elimina protegido |
| 10 | CorrelationId y errores no filtran secretos | URL firmada/PII aparece en log |
| 11 | Cursor paginado filtra por tenant y permisos | Cursor de A se reutiliza en B |
| 12 | Rollback futuro conserva origen y referencias | Cutover incompleto borra fuente |

Los 24 escenarios son **especificaciones falsables**, no tests ejecutados. Cualquier prueba de DB usa MySQL 8.4 efímero y Category=Integration verificable, nunca datos reales. Sin desarrollo runtime en este changeset, dotnet, HTTP, MySQL, R2, Angular y Playwright son `NO_APLICA` como CI causal de este documento, **no PASS**.

## Seguridad, impacto, compatibilidad y recuperación

- **Capas:** sólo Markdown y CHANGELOG; Domain/Application/Infrastructure/API y Angular sin edición. Ningún contrato HTTP real, tabla, migración ni datos cambian.
- **Tenant:** derivación server-side, JWT/RBAC relacional, pertenencia tenant A/B con pruebas negativas futuras; recurso no visible no proporciona indicios cross-tenant.
- **Archivos:** MIME por bytes, límites de streaming/cuotas, hashes, refs opacas, caché privada y callbacks firmados sujetos a política futura.
- **Proveedor:** Cloudinary actual se conserva. R2/DNS/Hostinger son futuros separados y no autorizados.
- **Concurrencia:** expectedVersion/CAS por recurso, correlación única e idempotencia con digest; reintentos de proveedor no crean duplicados.
- **Observabilidad:** errores saneados y CorrelationId; nunca registrar identidad o payload sensible en BITACORA.
- **Rollback:** revert ordinario del documento y sección CHANGELOG en un commit nuevo; ningún force-push ni cambio de datos; no usar rollback ficticio de migración.
- **No alcance:** nada de QA/main/PROD, secretos, dominios o activación comercial.

## Controles A0–A5 de esta microtarea

- **A0:** contrastar Skill canónica y referencia arquitectura con HEAD; autoridad documental V5; `PLAN H1521/I1521` y `COLA G/O` + `DAG F/G`, CAS exclusivo y diff limitado.
- **A1:** revisar cobertura real de 12 operaciones, 12 DTO, 16 errores y 12 pares positivos/negativos **conceptuales**; no afirmar pruebas en ejecución.
- **A2:** revisar prohibiciones cross-tenant, JWT/RBAC, inventario UNKNOWN, firma/TTL/replay, retención, idempotencia y PII sin cambios runtime.
- **A3:** coherencia con C10-03.M01.01–06, contratos C10-02 y stack vigente; compilación/API/DB/browser `NO_APLICA` como pruebas de Markdown.
- **A4:** publicar un único commit fast-forward/CAS en `dev`, revisar blob y diff, comprobar checks exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en SUCCESS, registrar VALIDANDO antes de A1–A3 y readback institucional para LISTO.
- **A5:** `A5_PENDIENTE` y revisión independiente por otra de las cinco Tasks Javier distinta de DEV01. No declarar liberación técnica de M03 sin todos los gates reales.

Este documento es un **diseño propuesto**, no certificación de inventario, transferencia, seguridad end-to-end ni contratación de un proveedor.
