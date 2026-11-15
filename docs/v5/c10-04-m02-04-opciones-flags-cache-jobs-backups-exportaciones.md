# SOLQARYN V5 FINAL — C10-04.M02.04: opciones tipadas, ámbitos y flags de caché, jobs, backups y exportaciones

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Proyecto:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** contrato M02 exclusivamente documental; las opciones de esta página NO se añadieron a `.NET`, archivos de configuración, Angular, EF/MySQL ni servicios activos.

## 1. Base real y fronteras de responsabilidad

- `backend/src/Application/Interfaces/IPublicStoreCache.cs`: interfaz existente con `GetOrCreateAsync`, `InvalidateAll` e `InvalidateTenant`. Se conserva su ámbito público; no concede acceso al ERP privado.
- `backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs`: caché segmentada por tenant con generaciones y parámetros; su existencia NO demuestra invalidación distribuida entre instancias.
- `backend/src/Application/Services/OutboxRetryProcessor.cs`: ya existe lógica de lotes/reintentos, pero NO autoriza cron nuevo, worker universal ni cambios en orígenes de eventos.
- `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs`: interfaz tenant-aware con límites de soporte; la URL o publicId no son permiso de acceso.

Se preservan contratos actuales y los gates B01–B14 de `C10-04.M01.06` LISTO+A5 independiente. M02.01 es diseño conceptual preparado por otra Task y no es dependencia técnica indispensable de esta M02.04: las fuentes se contrastan sin atribuirle A5 ni implementación. La única habilitación efectiva se comprueba en PLAN/COLA/DAG por ID.

## 2. Catálogo de opciones propuestas — NO COMPILADAS

| Clave tipada propuesta | Tipo y valor inicial de diseño | Ámbito / protección |
| --- | --- | --- |
| `PrivateCache.Enabled` | bool; `false` | global del backend; jamás activación por cliente ni para cache pública existente |
| `PrivateCache.MaxTtlSeconds` | int; 60, rango 1–300 si se autoriza | global con techo estricto; identidad de tenant y permisos siempre en clave |
| `PrivateCache.RequireTenantPartition` | bool; `true` obligatorio | entorno/tenant verificado en servidor; no desactivable |
| `PrivateCache.InvalidateOnRevocation` | bool; `true` obligatorio | cada cambio de membresía/permiso; impedir servir snapshot anterior |
| `Jobs.NewDispatcherEnabled` | bool; `false` | únicamente feature plane backend futura, no scheduler implementado |
| `Jobs.MaxAttempts` | int; 3, rango 1–5 | política backend; no aceptar payload como fuente |
| `Jobs.MaxConcurrentPerTenant` | int; 1, rango 1–2 | cuota por empresa; no sustituye lease/CAS real |
| `Jobs.LeaseTtlSeconds` | int; 60, rango 15–120 | lease servidor monotónico/versionado, no reloj navegador |
| `Exports.PrivateEnabled` | bool; `false` | feature backend futura, RBAC + ownership y auditoría obligatorios |
| `Exports.MaxRows` | int; 1000, rango 1–1000 | cuota por actor y empresa, nunca consulta sin tenant |
| `Exports.MaxBytes` | long; 10485760, rango 1–10485760 | limitar tamaño y sanear fórmula CSV/PII antes de emitir |
| `Backups.ExternalProviderEnabled` | bool; `false` | no habilita R2/Hostinger ni secretos nuevos |
| `Backups.RestoreEnabled` | bool; `false` | sólo futura integración efímera autorizada, no producción |
| `Telemetry.IncludeSensitivePayloads` | bool; `false` inmutable | logs/auditoría sin JWT, OTP, DSN, PII ni URLs firmadas |

Los valores son **propuestas de diseño conservadoras**, no valores leídos de `appsettings`, cuotas existentes ni política comercial aprobada. Nunca sustituyen una validación de contrato live, mediciones RPO/RTO o pruebas de backup. En particular `false` no deshabilita el caché público vigente; sólo representaría una funcionalidad privada nueva cuando se implemente con permiso.

## 3. Precedencia de configuración y tenencia segura

La autoridad propuesta sigue este orden: política global segura del servidor y autorización explícita -> configuración ambiente revisada -> overrides por tenant validados y limitados al techo global. La configuración por tenant puede **restringir** cuotas, TTL y capacidades, nunca aumentar privilegios ni encender un flag global apagado. Rechazar query, header `X-Empresa-Id`, host/slug, `localStorage` y campos del DTO como fuente de permiso, provider, tenant o TTL. El backend debe resolver empresa autenticada, dueño persistido, acción, membresía y versión antes de cachear, reintentar, exportar o generar un respaldo.

Las propuestas de new dispatcher/export/restore se mantienen false hasta microtarea de implementación auténtica, análisis de impacto, tests de seguridad A/B, CI y revisión A5 ajena. Activación no equivale a autorización para proveedores externos, pagos, migraciones o QA/main/PROD.

## 4. Reglas de validación V01–V14 FUTURAS

| ID | Regla que el futuro validador debe aplicar |
| --- | --- |
| V01 | Todo provider/config/secret proviene del servidor autorizado, no del cliente |
| V02 | Flag global apagado prevalece sobre cualquier override por tenant |
| V03 | Tenant A y B tienen particiones y cuotas separadas por identidad/permiso |
| V04 | TTL/MaxAttempts/MaxConcurrency/LeaseTtl dentro de rangos enteros finitos |
| V05 | Leases necesitan CAS/version y no se infieren de un timestamp antiguo |
| V06 | Una revocación invalida respuestas cacheadas antes de la siguiente lectura |
| V07 | La misma clave idempotente con payload distinto causa conflicto, no segundo efecto |
| V08 | Backoff y límite de intentos no se elevan por retry ni actor sin permiso |
| V09 | Export sólo con RBAC, ownership, cuota, saneamiento CSV y checksum final |
| V10 | URL firmada, storage key o publicId no sustituyen autorización de descarga |
| V11 | Desactivar feature de restore NO ejecuta proceso ni abre un endpoint |
| V12 | Config real exige respaldo y restore efímero probados antes de producción |
| V13 | Telemetría mínimamente necesaria, redactada y sin datos entre tenants |
| V14 | Todo fallo de proveedor/recurso devuelve NO_GO seguro, sin fallback abierto |

## 5. Oráculos de aceptación futuros P/N — NO EJECUTADOS

| Caso | Futuro positivo | Rechazo negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Política global disabled conserva rutas existentes | Flag tenant o frontend habilita export privado |
| P02/N02 | Cache A con clave de tenant/version correcta | Consulta B recibe valor cacheado de A |
| P03/N03 | Revocación incrementa generación y niega lectura previa | Entrada stale se sirve con permiso revocado |
| P04/N04 | Retry duplicado conserva un efecto por lease CAS | Dos consumidores reclaman la misma tarea |
| P05/N05 | Cuotas/rangos del servidor se respetan | Payload altera MaxAttempts/TTL/concurrencia |
| P06/N06 | Exportación A redactada y saneada | CSV de fórmula maliciosa o datos B se descarga |
| P07/N07 | Config de backup futuro requiere evidencia cifrada | Se infiere restore PASS de una URL o nombre |
| P08/N08 | Eventos de auditoría minimalistas, sin secretos | Log contiene JWT, DSN, URL bearer o PII |
| P09/N09 | Fallo de provider deja estado recuperable | Error parcial se declara `completed/success` |
| P10/N10 | Cambio futuro tiene SHA, A0–A4, A5 ajena | Check CANCELLED/SKIPPED o aprobación propia es PASS |

No se ejecutaron xUnit/HTTP/MySQL/Playwright, no se desplegó nueva infraestructura ni se midió ninguna cuota. Estos son criterios verificables de futuro, no certificaciones.

## 6. Impacto, dependencias, pruebas y reversión

**Impacto efectivo:** este Markdown y una sección contigua de `CHANGELOG_AI.md`. No tocar `main`, QA, PROD, dominios, Aiven, secretos, R2, Cloudflare, cuentas o Tasks. **A0:** fuentes, Scope Lock, identidad, M01.06+A5, DAG F=VALIDADO/G=SI, COLA G/O=SI, owner exclusivo. **A1:** consistencia del catálogo de 14 opciones, V01–V14 y P/N01–10; sin afirmar ejecución. **A2:** tenants A/B, JWT/RBAC, revocación, privacidad, leases y restauración fail-closed como contrato. **A3:** cotejar interfaces cache/outbox/storage vigentes; .NET/Angular/SQL runtime NO_APLICA causal al diff de dos Markdown, nunca PASS funcional. **A4:** publicación fast-forward CAS, checks Scope Lock y procedencia V5 `completed/success` SHA exacto, diff y readback institucional. **A5:** otra Task Javier distinta del autor antes de liberar fases dependientes. Rollback mediante `git revert` normal del changeset documental, preservando aportaciones concurrentes.
