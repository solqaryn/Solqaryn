# SOLQARYN V5 — C10-03.M02.04: configuración tipada conceptual e inventario condicionado

**ID institucional:** C10-03.M02.04 · PLAN_MAESTRO fila 1523 · DAG_TECNICO fila 1484 · **Autor:** PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Repo `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK STRICT, fecha 2026-11-15.

## Alcance y estado de realidad

Esta unidad define **opciones de configuración propuestas**, ámbito, límites y denegación por defecto para un inventario histórico todavía **NO_VERIFICADO** y almacenamiento futuro de archivos. No crea valores de configuración ejecutables ni flags runtime, no modifica Cloudinary/R2/Hostinger, DNS, proveedores, secretos, EF/MySQL, API, UI ni datos. **NO_VERIFICADO no significa cero**. Si un inventario completo autenticado acredita cero, se omitirán exportación, copia, checksum y cargas de históricos; un inventario positivo exigiría manifiesto y conciliación antes de otra autorización. Cualquier discrepancia será NO_GO solamente para la operación afectada.

**Preflight:** `C10-03.M01.01–06` tenían `LISTO` y A5 independientes; `COLA!G1523/O1523=SI`, `DAG_TECNICO!F1484=VALIDADO/G1484=SI`; PLAN H1523 PENDIENTE e I vacío antes del claim. Las tareas M02.01–03 tienen dueños propios y son preparaciones independientes: no se reutiliza evidencia de su ejecución como condición de cierre de M02.04.

## Cuatro ámbitos restrictivos propuestos

| Ámbito | Autoridad futura | Precedencia fail-closed |
| --- | --- | --- |
| PLATFORM_GOVERNANCE | Operador autorizado | Límites máximos de TTL/tamaño, retención, privacidad, auditoría, allowlist y seguridad; nunca reducibles por tenant |
| ENV_PROVIDER | Configuración de infraestructura autorizada | Cuenta, entorno, proveedor, secretos fuera del repo y modo read-only sin fallback permisivo |
| TENANT_FILES | Membresía y RBAC actual verificados server-side | Preferencias de tenant y cuotas dentro de techos globales; no cambia owner de recurso |
| RESOURCE_POLICY | Aplicación sobre recurso y versión autorizados | MIME, clasificación, publicación, hold y TTL; intersección restrictiva con los demás |

Precedencia PLATFORM_GOVERNANCE > ENV_PROVIDER > TENANT_FILES > RESOURCE_POLICY. El servidor decide tenant; header, slug, host, URL, prefijo y EmpresaId del cliente no conceden permisos.

## Dieciocho opciones conceptuales CFG01–CFG18, NO IMPLEMENTADAS

| ID | Opción propuesta | Valor seguro sin autorización |
| --- | --- | --- |
| CFG01 | HistoricalInventoryState: enum UNKNOWN/ZERO_VERIFIED/POSITIVE_VERIFIED/INCONSISTENT | UNKNOWN |
| CFG02 | HistoricalInventoryReadEnabled: bool | false |
| CFG03 | HistoricalExportEnabled: bool | false |
| CFG04 | HistoricalMigrationEnabled: bool | false |
| CFG05 | TrustedInventorySourceSet: allowlist | vacía |
| CFG06 | InventoryAsOfUtc: timestamp opcional | null |
| CFG07 | InventoryReconciliationDigest: digest | ausente hasta evidencia |
| CFG08 | ProposedObjectProvider: enum | CURRENT_ONLY |
| CFG09 | PrivateFilesByDefault: bool | true |
| CFG10 | TenantPrefixAuthority: enum | SERVER_ONLY |
| CFG11 | MaxFutureFileBytes: long | deny until ceiling approved |
| CFG12 | AllowedFileMimeBytes: set | vacía |
| CFG13 | SignedDownloadTtlSeconds: int | 0 |
| CFG14 | SignedUrlReplayPolicy: enum | DENY_UNCONFIGURED |
| CFG15 | RetentionAndHoldMode: enum | ENFORCE |
| CFG16 | CacheIsolationMode: enum | TENANT_RESOURCE_VERSION |
| CFG17 | MaxRetriesAndIdempotencyPolicy: typed | deny unconfigured |
| CFG18 | ProviderCutoverEnabled: bool | false |

Los defaults se describen como **contratos futuros**; no se aplican a `appsettings`, envvars ni secretos actuales. Un valor NULL/UNKNOWN en inventario no se redondea a 0.

## Reglas R01–R12 de validación futura

1. `ZERO_VERIFIED` únicamente con conteos autenticados y reconciliados de todas las fuentes permitidas.
2. Un cero real acreditado obliga a saltar export/copia/checksum históricos sin generar pipelines vacíos.
3. Inventario positivo requiere manifiesto por tenant/recurso/version, hash y retención antes de plan de migración.
4. Inventario inconsistente bloquea solo migración; no borra fuente ni habilita nuevo proveedor.
5. Credenciales de proveedor, claves, HMAC, URL firmada y PII nunca viven en frontend, documento ni BITACORA.
6. JWT/RBAC/membresía se verifican server-side incluso si el cliente aporta un tenant o prefijo supuestamente válido.
7. Un archivo sin owner verificable permanece privado/en cuarentena; nunca público por null legacy.
8. MIME se decide por bytes y límites de tamaño/stream; el nombre/extensión no autorizan.
9. Hold legal, retención y backups preceden a cualquier lifecycle de eliminación.
10. Firma liga actor/tenant/objeto/verbo/UTC/version y replay; TTL es techo superior no ampliable por tenant.
11. Reintentos limitados y clave de idempotencia no permiten payload distinto, publicación parcial ni fuga cross-tenant.
12. Cambio de opción requiere control versionado/CAS, aprobación aplicable, rollback y CI causal antes de habilitarla.

## Doce pares de aceptación P/N FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Conteo 0 autenticado y readback permite omitir históricos | UNKNOWN presentado como 0 |
| P02/N02 | Inventario positivo produce manifiesto | Fuente incompleta se certifica |
| P03/N03 | Inconsistencia da NO_GO migración | Diferencias se ignoran |
| P04/N04 | Tenant A lista sólo archivos A | Host o prefix da acceso a B |
| P05/N05 | Rol vigente permite recurso propio | Revocado conserva permiso |
| P06/N06 | MIME/tamaño válidos aceptados | Extensión engañosa aceptada |
| P07/N07 | URL privada válida liga TTL/verbo | Replay/expired permite lectura |
| P08/N08 | Retención/hold preservados | Lifecycle elimina protegido |
| P09/N09 | Cache aislada A/B | Cache mezcla objetos |
| P10/N10 | Idempotencia mismo digest no duplica | Key con digest distinto confirma |
| P11/N11 | Error de proveedor saneado | HMAC/PII en logs |
| P12/N12 | Configuración reversible con CAS | Versión obsoleta sobrescribe |

Todos los P/N son **escenarios futuros no ejecutados**. Ni esta tabla ni M01 prueban inventario real, latencia, R2, DNS, MySQL o HTTP.

## A0–A5 e impacto

- A0: Skill/HEAD/Scope Lock, PLAN/owner, COLA G/O y DAG F/G individuales, diff y control CAS.
- A1: dieciocho opciones, cuatro ámbitos, doce reglas y doce pares P/N revisables; sin afirmar pruebas live.
- A2: límites JWT/RBAC/tenant A/B, firma, MIME, privacidad, retención y registros sensibles documentados.
- A3: compatible conceptualmente con C10-03.M01 y contratos C10-02; .NET/EF/MySQL/Playwright/HTTP/proveedor NO_APLICA para un diff Markdown, **no PASS**.
- A4: publicar sólo documentos y CHANGELOG por fast-forward expected HEAD, checks Scope Lock/V5 `completed/success` SHA causal, readback PLAN/COLA/BITACORA.
- A5: pendiente de otra Task Javier distinta de la autora; sin liberar M03 por esta documentación.

**Rollback documental:** git revert normal del commit o rectificación aditiva. Ningún impacto sobre Cloudinary, DNS/R2, infraestructura, datos, QA/main/PROD o configuración física de automatizaciones.
