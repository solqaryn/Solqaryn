# SOLQARYN V5 FINAL — C04-01.M01.06: bloqueo, rollback y condición de liberación del catálogo de planes SaaS

**Alcance:** cierre de preparación documental M01 de `C04-01 | Catálogo de planes y límites`, no motor de precios, pasarela de pagos, suscripciones runtime nuevas, índices MySQL, jobs, webhooks o cobros. **Autor:** SOLQARYN PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuentes: [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Salida exacta:** contrato de dependencias, barreras de seguridad y reversión de M01. Las pruebas P/N aquí son **FUTUROS_NO_EJECUTADOS**; M01 no certifica nuevas funcionalidades, facturación ni precios. A5 será otra Task Javier.

## 1. Predecesores revisados y evidencia de entrada

Se leyó el estado `LISTO` con A5 independiente de cada `C04-01.M01.01–05`. Antes del claim, `PLAN_MAESTRO H583=PENDIENTE / I583 vacío`, `COLA G/O=SI`, `DAG_TECNICO F=VALIDADO/G=SI` y Scope Lock SUCCESS de HEAD `dev`. Este diagnóstico es por ID y se revalida tras cada commit.

| ID | Artefacto vigente | Blob Git | Resultado documental |
|---|---|---|---|
| `C04-01.M01.01` | `docs/v5/c04-01-m01-01-objetivo-no-alcance-catalogo-planes.md` | `f93af73db219672e8ce34d38ae80e0b5c90a730b` | objetivos, exclusiones y no regresión |
| `C04-01.M01.02` | `docs/v5/c04-01-m01-02-entradas-salidas-actores-superficies.md` | `0e88d1bfe60c1577c7cfc7f9f1e016c8ee44b503` | actores, contratos API/Angular y fronteras |
| `C04-01.M01.03` | `docs/v5/c04-01-m01-03-inventario-codigo-contratos-evidencia.md` | `1d68214ba00a23842d88a9c9549cecb25ab69e11` | 12 fuentes código/CI y límites |
| `C04-01.M01.04` | `docs/v5/c04-01-m01-04-riesgos-supuestos-decisiones-catalogo-planes.md` | `d26ba392f3462e92f1c4629389f11da7a899f774` | 12 riesgos y decisiones comerciales pendientes |
| `C04-01.M01.05` | `docs/v5/c04-01-m01-05-criterios-aceptacion-catalogo-planes.md` | `128b3d2b6e413d080fd00ed986a5728833d2bf78` | 14 criterios AC positivos/negativos futuros |

**Código existente verificado desde el inventario:** `SuscripcionesSaaS.cs` blob `d9b5dbe19fb728293b00b748f10ac10336debd2d` define `Plan`, `PlanLimite` y `Suscripcion`; `SuscripcionesSaaSService.cs` blob `bb501285f11cd4b32709a58a2c51efa19c3664ea` resuelve tenant y replay; `SuscripcionesSaaSController.cs` blob `cab95db5a33c6a15448e2437481eaaac25ffc4fe` expone rutas de API protegidas; `SuscripcionesSaaSRepository.cs` blob `f3340ca687867d6dc2d99dfff5452bbe5c9bbb79` consulta suscripciones/planes; `suscripcion-saas.service.ts` blob `b3883c10afce06a39bdf4d0dec356a553f8aa32f` integra UI. **La existencia de contratos no demuestra** catálogo comercial versionado, aprobación de tarifas, pasarela, cobro real, migración aplicada ni pruebas E2E causales.

## 2. Puertas y bloqueos NO_GO para fases siguientes

| Frente | Requisito específico para M02/M03 | NO_GO si no se demuestra |
|---|---|---|
| Identidad del tenant | Resolver empresa efectiva y membresía activa del usuario en backend, sin confiar en empresa de URL/body/header/Angular | Consulta/modificación de contrato B con JWT A, usuario revocado, ausencia de membresía |
| Autorización | `[Authorize]` y `[RequierePermiso]` sobre acción real; entitlement de plan solo restringe acceso, nunca crea `RolPermiso` | `EsAdministrador`, plan habilitado o feature flag concede operación sin grant empresarial |
| Modelo de plan y versión | Identidad de catálogo, plan inmutable/revisión, vigencia UTC, límites normalizados y asignación versionada al tenant | Reprecio retroactivo, plan inválido, límite 0/negativo, `null` ilimitado ambiguo o datos históricos alterados |
| Tarifa, moneda e impuestos | Decisión comercial aprobada sobre precios/moneda/periodicidad, prorrateo, cálculo e impuestos antes de cualquier operación monetaria | Cifras inventadas, cliente elige importe final, cobro antes de política/consentimiento/contrato |
| Idempotencia y concurrencia | Llave de replay aislada por tenant y operación; mismo fingerprint => resultado único, diferente payload => conflicto; ensayar concurrencia MySQL efímera | Doble alta/cargo, cambio de plan repetido, replay cross-tenant o política permisiva por timeout |
| Persistencia/rollback | EF Core 10.0.12/MySql.EntityFrameworkCore 10.0.9 con MySQL 8.4 efímero, índices/FK, versionado aditivo, backup y revert ensayado | ALTER/DROP en Aiven/PROD, cascadas destructivas, pérdida de suscripciones/ledger o migración no probada |
| API y clientes | Mantener semántica actual de onboard/consulta/límites/entitlement y DTO, paginación, errores; nuevas rutas solo en M03 autorizado | Romper cliente Angular existente, endpoint administrativo global sin RBAC explícito |
| UI y accesibilidad | Plan, periodo, límites y estado visibles y accesibles con origen servidor; manejo carga/error, teclado, contraste | UI sustituye autorización backend, presenta precio no aprobado o filtración de tenant |
| Auditoría/PII | Registro de intentos, decisión, tenant y versión con correlación opaca; sin JWT, secretos, datos de tarjeta o campos B | Exponer credenciales, logs con PII ajena, auditoría no idempotente |
| Integraciones externas | **NO IMPLEMENTAR** pasarela, webhook, worker, dispatcher, plan de pagos ni compra; no se presume autorización por documento | Evento de proveedor activa plan, cobra o da grants sin nuevo permiso/ciclo completo |
| CI y gobierno | `verify-solqaryn-project-scope` y check V5 exact SHA, .NET/Angular/MySQL/Playwright dirigidos cuando haya cambios runtime; A5 independiente | Reutilizar checks antiguos, marcar `queued/skipped/cancelled/in_progress/failed` como PASS requerido |
| Entornos | Solo `dev` con commits fast-forward serializados, CAS de owner/HEAD, BITACORA append-only | PR/ramas/Tasks nuevos, secretos, QA/main/PROD/Hostinger/Cloudflare real sin autorización |

**Punto de decisión pendiente:** no se halló evidencia de aprobación de nuevos precios, monedas, tarifas, impuestos ni procesador de cobro para este alcance. Es un bloqueo de *implementación comercial*, no de la documentación de preflight. No comprar, cobrar ni asociar plan nuevo a datos reales.

## 3. Dependencias posteriores y release seguro

**M02 – contrato:** definir `PlanVersion`/vigencia, catálogo, DTOs compatibilidad, claves/rangos de límites, política de upgrades/downgrades, garantía transaccional de idempotencia y error codes. Cada unidad `C04-01.M02.*` requiere lectura individual `PLAN/COLA/DAG`, autoría exclusiva y A0–A5; no se habilita por este documento ni por cerrar M01.

**M03 – implementación real:** primero identificar interfaces y consumidores en Domain, Application, Infrastructure, API y Angular. Cambios de JWT/RBAC/tenant exigen HTTP positivos/negativos de A/B; cambios EF/ledger/migraciones exigen MySQL 8.4 efímero, transacciones, FK y rollback; Angular requiere build/tests accesibles y Playwright cuando se justifique. No inferir que una prueba xUnit de origen histórico pasó por estar almacenada en repo. Ningún paso libera cobros, pasarela, webhooks ni QA/main/PROD.

**M04/M05/M06 – verificaciones y cierre:** pruebas causales por SHA exacto, estados completados con éxito y trazabilidad institucional. Tests SKIPPED/CANCELLED/FAIL/QUEUED/IN_PROGRESS requeridos **no** autorizan LISTO_DEV. Un A5 de mismo TASK_ID que autor está prohibido. La última etapa exige aprobación de entorno separada, integridad y preservación de historial.

## 4. Oráculos documentales P/N (NO EJECUTADOS)

| Caso | Signo | Entrada de prueba futura | Resultado requerido |
|---|---|---|---|
| P01 | Positivo | Membresía A + grant vigente para leer contrato A + plan efectivo | Ver solo plan, límites y suscripción A |
| P02 | Positivo | Onboarding autorizado en A, clave idempotente y plan válido | Una sola alta lógica; reintento igual no duplica |
| P03 | Positivo | Cambio de versión aprobado y registrado con vigencia | Historia preexistente intacta, regla de transición auditable |
| P04 | Positivo | UI accede a plan propio con permiso, estado y límites | Datos consistentes con API, estados accesibles y sin auth local |
| N01 | Negativo | JWT A solicita `/saas/tenants/B` o claim B | 403/404 seguro, sin exposición B |
| N02 | Negativo | Entitlement válido y grant RBAC ausente | DENY; ninguna acción administrativa |
| N03 | Negativo | Plan, módulo o cuota inválida, código duplicado, negativo o ambiguo | Rechazo determinista, no activar plan |
| N04 | Negativo | Mismo idempotency key con payload distinto | Conflicto, cero alta/cargo adicional |
| N05 | Negativo | Reintento concurrente del mismo key, tenant A/B | Un ganador lógico, ningún replay cross-tenant |
| N06 | Negativo | Precio/moneda/impuesto no aprobado o manipulados desde cliente | NO_GO cobro, cero efecto económico |
| N07 | Negativo | Upgrade/downgrade borra historial, altera precio retroactivo | Bloqueo y rollback no destructivo |
| N08 | Negativo | Membresía revocada durante lectura de suscripción | Backend DENY sin cache antiguo |
| N09 | Negativo | MySQL o servicio externo falla durante transacción | No mutación parcial ni entitlement por fallback |
| N10 | Negativo | Soporte o admin tenant intenta editar catálogo global no autorizado | DENY, auditoría segura |
| N11 | Negativo | PDF/log/DTO contiene JWT, PII B, credencial o dato de pago | Sanitizar, rechazar publicación |
| N12 | Negativo | UI otorga permisos o exhibe datos B por manipulación de estado | Backend bloquea, tests E2E aislados fallan |
| N13 | Negativo | Pasarela/webhook simulado repetido o falsificado intenta activar plan | NO IMPLEMENTAR, sin handler/cobro nuevo |
| N14 | Negativo | Se quiere certificar M03/PROD por diseño documental o CI pending | NO_GO, nueva autorización y pruebas por SHA |

Los cuatro positivos y catorce negativos son **criterios escritos de implementación futura**. No se ejecutaron xUnit, HTTP, MySQL, Playwright, pagos ni pruebas de carga por esta microtarea documental.

## 5. Auditoría A0–A5 y reversión

- **A0:** contrastar cinco documentos M01.01–05 con sus blobs y estado A5; verificar diff sólo documento actual y `CHANGELOG_AI.md`, encabezados y viñetas contiguas, sin secretos.
- **A1:** revisar P01–P04/N01–N14 como oráculos claros con salida observable, no tests runtime.
- **A2:** JWT/RBAC/tenant A/B, idempotencia, secretos/PII, no cobros ni autoridad implícita, fallos seguros.
- **A3:** continuidad con Plan/Suscripcion/DTO/API/EF/Angular existentes; builds .NET/Node/MySQL/Playwright `NO_APLICA_CAUSAL` para diff exclusivamente Markdown, no contarlos PASS.
- **A4:** publicación serial fast-forward con comprobación HEAD, `verify-solqaryn-project-scope` y V5 CHANGELOG `completed/success` del SHA exacto; PLAN/COLA/BITACORA readback.
- **A5:** pendiente de otra Task Javier `!= 6adbb9fff1548191a1bd5b30d9e9984a`. No autoriza promoción QA/main/PROD.
- **Rollback de este documento:** `git revert` normal con readback y evento compensatorio aditivo; no reescribir historia. La reversión de DB/dinero requiere respaldo y ensayo independiente.

