# SOLQARYN V16 — C10-03.M02.06: salida documental de archivos e inventario condicionado

**Microtarea:** C10-03.M02.06 (PLAN_MAESTRO fila 1525). **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Riesgo V15:** R0 documental, solo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA, PLAN/COLA/DAG_TECNICO y docs/QUALITY_RISK_AUDIT_V15.md.

**Objetivo:** fijar dependencias técnicas, aceptación y rollback de M02 para archivos privados, retención, idempotencia, manifiestos y futuro proveedor de almacenamiento. **No implementa endpoints, migraciones, workers, outbox, API, integración, proveedores, Cloudinary/R2, DNS o costos**.

## A0: precedentes y estado comprobable

- C10-03.M01.01–06 y C10-03.M02.01–05 requieren LISTO y A5 causal independiente según DAG. Al reclamar esta unidad, PLAN H1525=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI; reclamo solo una unidad con CAS H1525. No inferir autorización para sucesoras a partir de este texto.
- M02.01 documenta 13 conceptos, 14 invariantes de identidad/propiedad y negativos; M02.02 define OP/DTO/error sin endpoints; M02.03 propone P12, IDX12, MIG7 y fixtures sin MySQL real; M02.04 define CFG01–CFG18, cuatro ámbitos, R01–R12 y futuros escenarios; M02.05 define EVT01–EVT12, IR01–IR14, idempotencia y correlación sin emitir eventos. Ninguna de esas especificaciones demuestra inventario real.
- **Inventario histórico NO_VERIFICADO. NO equivale a cero.** Cero requiere consulta completa autenticada de todas las fuentes aprobadas, reconciliación por tenant y readback. Si se verifica CERO, debe **omitirse** exportación/copia/checksum/carga de históricos sin crear pipelines vacíos. Si se verifica POSITIVO, preparar manifiesto completo por tenant, recurso, versión, hash y retención; no migrar sin aprobación. Si hay diferencias, NO_GO solo a cutover y preservar origen.
- JWT/RBAC, roles revocados, ownership y tenant A/B se comprueban server-side desde ASP.NET Core 10 y persistencia autorizada. Header, host, slug, URL firmada, prefijo o identificadores aportados por cliente no conceden permiso. Angular 22.2.1 no autoriza operaciones.
- Datos, evidencias, cuotas, TTL, proveedor/cuenta comercial, costo y número de archivos: **NO_VERIFICADO** o **NO_MEDIDO** según corresponda. No usar Aiven o Cloudinary en vivo. Cualquier implementación de storage, EF Core/MySQL, API o migración requiere alcance R2, permisos, pruebas de integración reales y rollback probado.

## G01–G16: gates de salida para futuros cambios ejecutables

| Gate | Evidencia requerida futura | Faltante implica |
| --- | --- | --- |
| G01 | Fuente V5, Scope Lock dev y permiso de cada operación externa | NO_GO externo |
| G02 | Conteo autenticado completo y reconciliado entre fuentes | NO_VERIFICADO, jamás cero |
| G03 | Si cero verdadero, decisión explícita de omitir históricos | No lanzar pipeline vacío |
| G04 | Si positivo, manifiesto por tenant/objeto/versión/hash | No migrar/copiar |
| G05 | Discrepancias DB/proveedor resueltas sin perder históricos | Bloqueo de cutover |
| G06 | JWT firmado, permiso/RBAC vigente y revocación verificados | Deny-by-default |
| G07 | Owner y tenant A/B persistidos e independientes del cliente | Denegar cross-tenant |
| G08 | MIME por bytes, límites de tamaño/stream y cuarentena | No publicar archivo |
| G09 | Hold legal, retención, backup y auditoría versionados | No eliminar ni sobrescribir |
| G10 | Firma de actor/objeto/verbo/TTL/replay autorizada | No emitir URL privada |
| G11 | Idempotencia scoped y CAS de versión transaccional | Conflicto/no duplicación |
| G12 | Evento/outbox futuro atómico y sin PII, tokens ni URLs | No emitir señales |
| G13 | Cuota, costo, proveedor y aprobación comercial opt-in | No pagar ni activar proveedor |
| G14 | MySQL 8.4 EF efímero con Category=Integration ejecutada si cambia DB | Build verde insuficiente |
| G15 | Tests pertinentes, Scope Lock/V5 y SHA causal SUCCESS | No aprobar A4 |
| G16 | Readback PLAN/COLA/DAG/BITACORA y A5 Task Javier independiente | No liberar etapa siguiente |

Todos los gates son condiciones **futuras**, no 16 tests ya ejecutados. Evidencia futura incluye SHA, autor, UTC, muestra, numerador/denominador cuando aplique, permisos y plan de reversión. Cualquier dato desconocido se mantiene NO_VERIFICADO, jamás número inventado.

## Estados y transiciones contractuales (sin persistencia real)

Estado conceptual UNKNOWN puede pasar a ZERO_VERIFIED únicamente con conteos completos autorizados, o a POSITIVE_VERIFIED solo con manifiesto y reconciliación; INCONSISTENT bloquea migración sin borrar fuentes. Estados no autorizan por sí solos lecturas, cargas, descargas, backup, copias o pagos. Tenant, actor, rol, recurso y versión se revalidan al momento de cada operación. Correlación no es un token de acceso; replay y firma expirada deben fallar cerrados. Las señales EVT01–EVT12 del M02.05 son inertes, no mensajes emitidos.

## A1–A3: T01–T12 futuros positivos/negativos FUTUROS_NO_EJECUTADOS (0/12)

| Test | Positivo futuro | Negativo / frontera / error futuro |
| --- | --- | --- |
| T01 | Cero auténtico permite omitir carga histórica | UNKNOWN equivale falsamente a cero |
| T02 | Positivo produce manifiesto íntegro | Conteo parcial se presenta como completo |
| T03 | INCONSISTENT bloquea solo cutover | Diferencia borra el origen |
| T04 | Tenant A autorizado accede recurso privado A | Tenant B/rol revocado accede A |
| T05 | MIME por bytes y tamaño límite aceptados | Extensión falsa o límite+1 pasan |
| T06 | Hold conserva archivo y auditoría | Lifecycle elimina recurso protegido |
| T07 | URL privada autoriza verbo/actor/TTL válidos | Replay caducado o verbo ajeno se acepta |
| T08 | Misma key/payload evita duplicados | Key idéntica con payload distinto confirma |
| T09 | Cache particionada por tenant y versión | Archivo privado A servido por B |
| T10 | Error de proveedor saneado, retry acotado | JWT/HMAC/URL secreta en logs o retry infinito |
| T11 | EF/MySQL Category=Integration real y 0 FAIL | Job que omitió integración se declara PASS |
| T12 | SHA/Scope Lock/V5 exactos y rollback verificado | CANCELLED/SKIPPED/IN_PROGRESS se cuenta PASS |

**Se ejecutaron 0/12.** Estos son criterios, no resultados reales de MySQL, HTTP, storage, CI de código, xUnit, Playwright, backup o proveedor. A1: revisar coherencia de 12 oráculos futuros. A2: control estático de JWT/RBAC/tenant A/B, MIME, retención/hold, idempotencia y privacidad. A3: backend/EF/MySQL/browser/Cloudinary/API **NO_APLICA** al diff exclusivamente documental R0.

## RB01–RB08: contención y reversión futura

- RB01: fijar SHA, manifiesto, scope, owner, permiso y versión antes de operar.
- RB02: usar backup/restore en MySQL efímero con datos sintéticos A/B si existiese cambio DB.
- RB03: verificar fuentes y estado UNKNOWN, cero real, positivo o inconsistente antes de copiar.
- RB04: congelar migración nueva ante fallo sin impedir acceso legal a datos originales.
- RB05: desactivar proveedor/flag nuevo sin fallback a URL pública, preservar hold.
- RB06: revertir migración expand-contract probada en entorno efímero, no producción.
- RB07: reconciliar checksums/conteos/caché y permisos A/B tras rollback, registrar pérdidas.
- RB08: git revert normal en dev después de revalidar HEAD; conservar BITACORA e historial de A5.

## A4–A5: salida del contrato R0

A0: revisión de fuentes, 6 antecedentes M01/5 M02 y diff solo este Markdown + CHANGELOG_AI.md autocontenido. A1: 16 gates, 8 rutas de reversión, 12 oráculos futuros sin ejecución. A2: inspección estática tenant A/B, JWT/RBAC, revocación, privacidad y retención. A3: runtime NO_APLICA, no PASS implícito. A4: un commit dev fast-forward/CAS, checks verify-solqaryn-project-scope y V5 de procedencia completed/success en SHA exacto, blobs/readback PLAN/COLA/DAG/BITACORA, sin reinterpretar checks no pertinentes cancelados como aprobados. A5: revisión real por otra Task Javier distinta al autor, capaz de rechazar; LISTO_DEV no equivale a A5_PASS.

Rollback de este documento por git revert aditivo. No se ejecutaron inventario de archivos, copias, migraciones, exportación, API, test funcional, modificación de cuentas, secretos, QA/main/PROD o costos.
