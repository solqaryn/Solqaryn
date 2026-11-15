# SOLQARYN V5 FINAL — C14-01.M02.06: dependencias, bloqueos y contrato de salida de aceptación funcional

**Responsable:** PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO institucionales.

## Propósito y alcance autorizado

Establecer el **contrato documental de salida** de M02 para aceptación funcional de operaciones públicas, privadas y administrativas de SOLQARYN, con separación de tenant A/B, sucursales y efectos comerciales. Ninguna ruta, DTO, migración MySQL, fixture ejecutable, trabajo de CI, webhook, worker, proveedor de cobro o bandera de frontend se crea mediante esta tarea. Las pruebas positivas y negativas de esta página son **FUTURAS_NO_EJECUTADAS**; no prueban disponibilidad de endpoints, WCAG, cero defectos P0/P1, restauración ni aislamiento runtime. Nunca habilitan QA, main, PROD, DNS, pagos, secretos o despliegue.

Fuentes: Google Doc V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, Skill canónica y contratos `docs/v5/c14-01-*`. El stack vigente se conserva: ASP.NET Core 10, Angular 22.2.1, MySQL 8.4, EF 10, JWT y RBAC backend.

## Dependencias institucionales y evidencia original

El DAG exige `C14-01.M01.01–06+A5` y `C14-01.M02.01–05+A5`; las once unidades predecesoras deben contar con LISTO y revisión A5 por Task distinta. En el preflight, esta unidad estaba PENDIENTE sin owner con COLA G/O=SI y DAG F=VALIDADO/G=SI. Los cinco contratos M02 predecesores son:

| Microtarea | Objeto que debe preservarse | Evidencia y límite |
| --- | --- | --- |
| M02.01 | 16 entidades conceptuales, 16 invariantes, oráculos positivos y 12 negativos | Documento, no esquema/servicio real |
| M02.02 | 12 rutas API candidatas, 14 validaciones y 12 errores propuestos | Corrección previa de Scope Lock acreditada; no endpoints nuevos |
| M02.03 | 12 agregados, 8 índices, 12 fixtures, 6 pasos reversibles propuestos | Fuente tuvo fallo Scope Lock por un documento ajeno; evidencia del HEAD correctivo y blob sin cambios; sin DDL |
| M02.04 | 16 claves de configuración tipada, 12 validaciones y flags seguras propuestas | Defaults fail-closed, sin configuración desplegada |
| M02.05 | 14 señales futuras, 10 invariantes de idempotencia, 12 límites | Ningún mensaje emitido ni dispatcher/Task creado |

Autor de M02.01–05: PRIMARY DEV 06; revisión técnica independiente por PRIMARY DEV 01. A5 sólo se acredita con SHA, blob, diff, controles pertinentes, CAS institucional y evento BITACORA, no por cadena histórica `A5_PENDIENTE`. No se reinterpreta un job `CANCELLED`, `SKIPPED` o `FAILED` como PASS; el origen erróneo M02.03 se valida causalmente mediante blob conservado y gobernanza exitosa en el HEAD corregido.

## Condiciones de salida y bloqueo AF01–AF16

| ID | Condición exigible a implementación futura | Denegación o bloqueo |
| --- | --- | --- |
| AF01 | JWT firmado y vigente, autenticación backend | Ausente/revocado/expirado: 401 y sin datos privados |
| AF02 | Membresía actual y RBAC efectivo por acción/recurso | Frontend o rol en DTO no concede acceso |
| AF03 | Tenant A/B y propiedad persistida verificada | Consulta cruzada: 403/404 sin filtración |
| AF04 | Sucursal válida con grant del tenant | ID de sucursal en ruta no es autoridad |
| AF05 | Ownership histórico NULL clasificado | NULL se deniega, sin herencia de tenant |
| AF06 | Compatibilidad de rutas/DTO versionadas y consumidores reales | Documento M02 no demuestra API implementada |
| AF07 | Idempotencia durable por acto comercial y payload | Reintento no crea dos pedidos/facturas/cobros |
| AF08 | Version/ETag y transiciones válidas | Concurrencia obsoleta: 409 sin mutación |
| AF09 | Moneda, importe, fiscalidad y PSP aprobados | Credencial/permiso faltante: sin cobro |
| AF10 | Positivos y negativos reales con fixtures A/B sintéticos | Oráculo futuro no se contabiliza como test |
| AF11 | Browser responsive, teclado, foco, errores y contraste | Checklist documental no es WCAG/Playwright |
| AF12 | Auditoría redactada, PII protegida y rate limiting | Tokens, credenciales y datos reales no se registran |
| AF13 | Integración .NET 10/Angular/MySQL efímero según impacto | Prohibido migrar base o cambiar auth desde M02 |
| AF14 | CI pertinente completed/success y SHA exacto | Queued, cancelled, skipped, failure nunca PASS |
| AF15 | Scope Lock STRICT, CAS owner, BITACORA append-only | Sin exclusividad o readback no hay cierre |
| AF16 | A5 independiente y permiso por ambiente separado | LISTO_DEV no autoriza QA, main ni PROD |

Las condiciones AF01–AF16 son requisitos técnicos posteriores; no están certificadas por la aprobación documental de M02.06.

## Doce pares positivos/negativos — FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo futuro / rechazo obligatorio |
| --- | --- | --- |
| PN01 | Anónimo ve catálogo público permitido | Anónimo intenta pedido privado |
| PN02 | Miembro A ve recurso A con grant backend | Usuario B pide mismo identificador A |
| PN03 | Sucursal del tenant autorizada responde | Sucursal ajena o no vinculada |
| PN04 | Membresía vigente permite acción | Membresía revocada con JWT aún firmado |
| PN05 | Pedido idempotente produce efecto único | Reintento paralelo duplicaría factura |
| PN06 | Versión vigente permite confirmación | ETag obsoleto pretende confirmar |
| PN07 | Emisor y moneda válidos generan borrador | Importe/fiscalidad incompatible |
| PN08 | Evidencia saneada vincula SHA y job real | CI cancelled/skipped se anuncia PASS |
| PN09 | Tenant A y B conservan fixtures separados | Encabezado o DTO intenta cambiar tenant |
| PN10 | Ownership conocido autoriza operación | Owner NULL obtiene permiso por fallback |
| PN11 | UI 320/390/768/1280 conserva foco/acción | Diálogo atrapa teclado o esconde error |
| PN12 | Restore efímero acredita hash y conteo | Revert de texto se hace pasar por restore |

Resultados de esta microtarea: **0 pruebas runtime ejecutadas**. Para instrumentarlas después se requerirán fixture/tenant/rol, runner, status HTTP o evento observable, SHAs y jobs de CI pertinentes, logs saneados y rollback del entorno efímero.

## Ocho rutas de recuperación R01–R08

- **R01:** CI FAIL: clasificar el fallo y corregir causa en el componente autorizado; no eliminar tests ni analizadores.
- **R02:** CI queued/in_progress: mantener únicamente ese ID VALIDANDO; no asumir PASS, avanzar sólo scope independiente.
- **R03:** CI CANCELLED por commit posterior: comparar blobs y verificar checks causales exitosos del SHA aplicable.
- **R04:** Scope Lock FAILED: reparar token/código/documento y revalidar HEAD sin degradar verificador.
- **R05:** Owner activo o lease incierto: no tomar scope; relevo sólo con ausencia demostrada y CAS exclusivo.
- **R06:** A5_FAIL: conservar historial y registrar nueva reparación/decisión independiente con evidencias.
- **R07:** JWT/RBAC/tenant/sucursal negativo: denegación por backend y diagnóstico con fixture sintético.
- **R08:** rollback de esta entrega: `git revert` normal preservando BITACORA; no revierte MySQL ni pagos.

## Auditorías A0–A5 y cierre

**A0:** diff limitado a este Markdown y sección autocontenida de `CHANGELOG_AI.md`, sin secretos, SQL, rutas funcionales ni cambios de auth. **A1:** inspección de dieciséis gates, doce pares positivos/negativos y ocho rutas de recuperación, sin atribuir tests runtime. **A2:** validación estática de JWT/RBAC/tenant A/B/PII y denegaciones futuras; sin sustituir enforcement real. **A3:** predecesores M01.01–06/M02.01–05 LISTO+A5, documentos conceptuales y DAG vigente, sin inventar integración backend. **A4:** commit fast-forward/CAS a dev, diff/blob/HEAD exacto, verificador Scope Lock y check V5 `completed/success`, readbacks PLAN/COLA/DAG/BITACORA. Compilaciones .NET, Angular, MySQL y Playwright son NO_APLICA **a este diff Markdown**, no PASS ficticio; si falla un control de gobernanza obligatorio, conservar VALIDANDO. **A5:** Task Javier distinta de la autora debe revisar evidencia y SHA; A5 queda PENDIENTE tras LISTO_DEV, sin autoaprobación ni promoción de ambiente.

**Salida permitida:** contrato M02 documental terminado, con requisitos de implementación futura explícitos. Toda M03 requiere código real y CI causal, sin heredar autorización de este documento.
