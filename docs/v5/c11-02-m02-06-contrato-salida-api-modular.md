# SOLQARYN V16 — C11-02.M02.06: contrato de salida API modular

**ID:** C11-02.M02.06 (PLAN_MAESTRO fila 1633). **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Riesgo:** R0 documental sin runtime. Solo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN/COLA/DAG institucionales y docs/QUALITY_RISK_AUDIT_V15.md.

**Resultado requerido:** fijar dependencias técnicas, criterios y salida de M02 de la API modular, preservando fronteras de soporte de solo lectura, tenant, DTO, errores, persistencia e idempotencia. **No implementa endpoints ni DTO runtime, alteraciones de middleware/DB/EF, workers, webhook, dispatcher, JWT, roles, migraciones, UI, QA/main/PROD o proveedor.**

## A0 — autoridad, alcance y relaciones causales

- El DAG exige C11-02.M01.01–06+A5 y C11-02.M02.01–05+A5; al reclamar esta unidad PLAN H1633=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI. La autorización se limita a documentación M02.06; no sustituye puertas adicionales M03/QA/PROD.
- M02.01 cubre entidades E01–E16, invariantes I01–I18, fallos F01–F10 y escenarios futuros. M02.02 define diez operaciones API/DTO conceptuales, dieciséis invariantes de compatibilidad y diez P/N futuros. M02.03 define PR01–PR12 proyecciones, IX01–IX08 índices NO CREADOS y MG01–MG06 posibles etapas de migración, F01–F10 fixtures no ejecutados. M02.04 propone 16 opciones tipadas, validación y flags OFF. M02.05 propone EV01–EV10, ID01–ID16, P/N01–10 y RB01–08, sin mensajes emitidos.
- El código vigente ASP.NET Core 10, EF Core 10/MySQL Oracle y Angular 22.2.1 sigue inalterado; una propuesta documental no implica que la API modular exista, que su autorización se haya ensayado o que el soporte pueda modificar datos.
- Identidad y tenant proceden del backend autenticado y propiedad persistida. Un claim, slug, host, EmpresaId, token parcial, correlation ID o DTO del cliente no otorga autoridad. Actor soporte solamente read-only con permiso explícito; sin UPDATE/DELETE/SaveChanges ni escalado de rol.
- Estado de nuevos endpoints y consumidores HTTP, performance real, costo, error budget y latencias p95/p99: **NO_IMPLEMENTADO/NO_MEDIDO**, no extrapolar datos de otros bloques. No se ejecutan migraciones ni tests de base de datos, HTTP o UI por este diff documental.

## G01–G16: puertas para futura API modular ejecutable

| Gate | Evidencia exigible por implementación futura | Falta implica |
| --- | --- | --- |
| G01 | Inventario de rutas, métodos, dueños y roles autorizados | No habilitar endpoint |
| G02 | Contrato DTO y esquema versionado con allowlist de campos | No admitir entrada arbitraria |
| G03 | Autenticación JWT validada por middleware real y política vigente | 401/denegación por defecto |
| G04 | Permiso RBAC, membresía activa, rol revocado y autoridad de soporte | 403/denegación por defecto |
| G05 | Tenant A/B de contexto backend y owner persistido, IDOR/BOLA negativos | No lectura cross-tenant |
| G06 | Soporte estrictamente read-only con tests de no-escritura | No exponer mutación |
| G07 | Error codes seguros, 400/401/403/404/409/429/500 sin fuga de SQL/PII | No publicar error inseguro |
| G08 | Límites de paginación, filtros, orden, carga, size/timeout y rate | Reject sobrelímites |
| G09 | Índices y proyecciones verificados con consultas y tenant predicates | No cambiar DB |
| G10 | Migraciones expand-contract con reversión y versión sin pérdida de tenant B | No aplicar DDL |
| G11 | Eventos futuros idempotentes, correlation opaca y ninguna autoridad del cliente | No emitir evento |
| G12 | Reintentos, conflictos de versión y errores transitorios fail-closed | No reintento infinito |
| G13 | Auditoría de actor/permiso/tenant/recurso saneada y con UTC | No operar sin trazabilidad |
| G14 | Tests positivos, negativos, frontera y errores; MySQL 8.4 efímero cuando EF cambie | Sin PASS técnico |
| G15 | Scope Lock/V5, CI backend/auth/security exact-SHA, rollback e impacto de archivos | A4 no certificado |
| G16 | A0–A4 reales, BITACORA/PLAN/COLA/DAG readback y A5 de otra Task JAVIER | No liberar sucesoras |

G01–G16 son requisitos conceptuales futuros, no checks ya ejecutados. Toda evidencia futura debe registrar commit SHA, blob, test, versión, ambiente, UTC, fuente real y, cuando corresponda, numerador/denominador. Un compilador verde no certifica JWT/HTTP/DB live.

## Interfaz conceptual, confianza y compatibilidad

- DTO de solicitud futura: versión de contrato, clave de recurso opaca, filtros allowlisted, paginación con límite, requestId y cabecera de versión solo como consistencia. No aceptar tenant/rol/owner enviados por cliente como autoridad.
- Salida futura: códigos estables, paginación determinista, datos mínimos permitidos, ausencia de PII en logs, trazas de error que nunca incluyan JWT/secretos/SQL crudo, y encabezados CSP/CORS/Cache-Control aplicables; se validarán con HTTP real cuando haya endpoints.
- Lecturas de soporte: auditoría de rol/justificación/membresía activa, proyección mínima, sin escritura. Ante IDOR, token revocado, recurso no perteneciente al tenant, versión obsoleta o caída de DB: negar o fallar seguro, no fallback superusuario.
- Persistencia: índices, constraints y transacciones deberán probarse en MySQL 8.4 EFÍMERO con Category=Integration ejecutada y cero fallos; un job verde filtrando Category!=Integration NO acredita esto.
- Mensajes/worker futuros: require outbox transaccional, idempotency key por tenant+operación+versión+payload; replay de hash distinto debe ser 409, no publicar salida parcial ni ejecutar en esta fase.
- Revisión y promoción: calidad R1/R2 para código auténtico, A5 Task JAVIER distinta del autor, no QA/main/PROD sin autorización separada.

## A1–A3 — T01–T12, positivos/negativos FUTUROS_NO_EJECUTADOS (0/12)

| Test | Positivo futuro | Negativo / frontera / error futuro |
| --- | --- | --- |
| T01 | Método autorizado devuelve DTO compatible | Campo/operación no allowlisted se acepta |
| T02 | Token JWT real vigente valida identidad y scope | JWT expirado/inválido se procesa |
| T03 | Usuario A obtiene solo recurso tenant A | B o IDOR accede A |
| T04 | Rol soporte vigente puede solo SELECT permitido | Soporte ejecuta UPDATE/DELETE/SaveChanges |
| T05 | Error 403/404 sana los datos de recurso ajeno | 500/403 expone SQL, JWT o PII |
| T06 | Paginación max y filtro permiten lectura acotada | pageSize límite+1 o filtro masivo |
| T07 | Versión coincide y actualización futura es CAS real | Versión vieja sobrescribe actual |
| T08 | Replay misma clave/payload es idempotente | Misma clave/carga distinta se admite |
| T09 | Tenant A y B tienen índices/rows aislados | Join omite tenant predicate |
| T10 | Tests Integration MySQL 8.4 ejecutan 0 FAIL | Build excluye Category=Integration y se declara PASS |
| T11 | CSP/CORS/Cache-Control no cachean contenido privado | Cache/host cruzados exponen datos |
| T12 | SHA, Scope Lock, V5, autoría y A5 sostienen cierre | CANCELLED/SKIPPED/IN_PROGRESS se declara SUCCESS |

**No se ejecutó ninguna de estas doce pruebas (0/12).** A1 revisa especificación positiva/negativa/frontera/error; A2 inspección estática de JWT/RBAC, tenant A/B, soporte read-only, revocación, IDOR, idempotencia, privacidad. A3 MySQL/EF/HTTP/xUnit/Playwright/CDN/Angular/worker = **NO_APLICA a documentación R0**, no PASS implícito de runtime.

## RB01–RB08: reversión y contención futura

- RB01: identificar SHA, versión de contrato, data/tenant ownership y dueño antes de publicar.
- RB02: desactivar endpoint nuevo ante fuga de roles/tenant, sin quitar middleware existente.
- RB03: restaurar política JWT/RBAC segura y revocar credenciales afectadas desde backend autorizado.
- RB04: bloquear acceso de soporte si hay intento de escritura, mantener auditoría.
- RB05: revertir API/DTO vía contrato compatible y tests clientes reales, sin migración silenciosa.
- RB06: revertir expand-contract DB en MySQL efímero primero, reconciliar owner/tenant.
- RB07: deshabilitar dispatcher/caché/eventos nuevos ante replay/PII, preservar datos.
- RB08: para este documento git revert normal aditivo en dev, sin borrar BITACORA ni force-push; A5 nuevo si cambia SHA.

## A4/A5 y efecto real de este commit

A0: fuentes, dependencias y diff único Markdown+CHANGELOG. A1: 16 gates, 8 reversión y 12 pruebas futuras escritas, 0 ejecutadas. A2: contrato estático fail-closed para tenant A/B, permisos, read-only y privacidad. A3: runtime NO_APLICA por R0. A4: publicación dev fast-forward/CAS con exact-SHA verify-solqaryn-project-scope y V5 procedencia completed/success, readback de PLAN/COLA/DAG/BITACORA. Los checks de backend cancelados/no aplicables NO son PASS. A5: revisión independiente real por otra Task JAVIER de distinto TASK_ID con poder de A5_FAIL.

El estado LISTO_DEV de este contrato **no autoriza implementaciones M03 ni QA/main/PROD**, ni certifica endpoint, pruebas HTTP, migración, secretos, infraestructura, navegación ni proveedor. Reversión de este texto: git revert normal sin reescribir historia.
