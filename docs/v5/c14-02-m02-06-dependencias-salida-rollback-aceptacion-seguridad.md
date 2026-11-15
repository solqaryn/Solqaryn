# SOLQARYN V5 — C14-02.M02.06: dependencias técnicas y contrato de salida de aceptación de seguridad

**Microtarea:** `C14-02.M02.06`. **Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT (`6adbb9fff1548191a1bd5b30d9e9984a`). **Fecha:** 2026-11-14, America/Tegucigalpa. **Repositorio:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK:** `STRICT`.

**Naturaleza y resultado:** cierre **DOCUMENTAL** del diseño M02 de aceptación de seguridad. Esta entrega fija condiciones verificables, fallos negativos, prerequisitos técnicos, evidencia exigible y rollback futuro; **NO** ejecuta pentest, explotación BOLA/IDOR, análisis dinámico, pruebas HTTP, Playwright, migraciones, pruebas MySQL, backups, restores, workers, ni evaluación real de vulnerabilidades. En especial, **NO se afirma P0/P1=0**. Que este documento esté LISTO_DEV y obtenga A5 independiente no certificará seguridad de QA/PROD ni abrirá por sí solo una ruta de despliegue.

## 1. Autoridad, entrada exacta y procedencia documental

Fuente ejecutable única: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). Estados: [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Gobernanza: Skill canónica de `dev`, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md`. Ningún plan archivado ni artefacto de otro repositorio autoriza trabajo.

En el preflight se comprobó PLAN_MAESTRO fila **2065**, ID exacto, H=PENDIENTE, I vacía; COLA fila **2065** G=SI/O=SI, DAG_TECNICO fila **2026** F=VALIDADO/G=SI. Después se reclamó exclusivamente para PRIMARY DEV 01 con evento de BITACORA y lectura posterior `EN_PROGRESO`. El DAG exige `C14-02.M01.01–06+A5` y `C14-02.M02.01–05+A5`; la elegibilidad actual no prueba seguridad runtime y deberá releerse después de cada transición.

| Predecesor | Artefacto inmutable de referencia en dev | Aporte, no certificación runtime |
| --- | --- | --- |
| `C14-02.M01.06` | `docs/v5/c14-02-m01-06-gates-bloqueo-rollback-liberacion-seguridad.md` · `6cb629af7993a6f4eb7cb8518f538f7ed196326f` | Barreras B01–B12 y bloqueo/rollback |
| `C14-02.M02.01` | `docs/v5/c14-02-m02-01-modelo-ownership-invariantes-aceptacion-seguridad.md` · `1bf7ca3f8c549212a24e10ed1afefb2fdedea972` | Modelo de ownership e invariantes |
| `C14-02.M02.02` | `docs/v5/c14-02-m02-02-api-dto-errores-validaciones-compatibilidad-seguridad.md` · `9de9d398cc2d238ce2ba66f27890c54b3317d782` | API/DTO/errores propuestos, no endpoints |
| `C14-02.M02.03` | `docs/v5/c14-02-m02-03-persistencia-indices-fixtures-seguridad.md` · `f8eeec67ea3c7165c967d9a9d6c28a6970d8909a` | Persistencia/índices y fixtures propuestos |
| `C14-02.M02.04` | `docs/v5/c14-02-m02-04-configuracion-tipada-ambitos-flags-seguridad.md` · `adcd29527f2c6faef59363e3017b1b4ba316a0ae` | CFG01–16 y VAL01–12, sin flags reales |
| `C14-02.M02.05` | `docs/v5/c14-02-m02-05-eventos-idempotencia-correlacion-limites-seguridad.md` · `41547e4c9d8e505cee5f49d4121918e20542f422` | EV01–12, ID01–10, LM01–12, sin dispatcher |

Las revisiones A5 independientes de M02.04 y M02.05 por PRIMARY DEV 01 fueron verificadas en esta misma corrida, con commits fuente `c732cf3780d632473653ec389a08974020ac2806` y `9b8bc00947e189a94b07bb1110a67c57f1334f21`, eventos individuales y readback de PLAN!K. Se preservan autorías de PRIMARY DEV 04 y la separación revisor/autor. Las A5 de documentos no sustituyen las pruebas de ejecución futuras.

## 2. Contrato de salida por amenaza (SEC01–SEC12)

Cada renglón define **obligación futura**, evidencia positiva y contraprueba negativa; la fase M02 sólo identifica el oráculo. La ausencia de evidencia deja `NOT_EVALUATED`, no `PASS`.

| Gate | Evidencia positiva mínima antes de un futuro release | Contraprueba / condición de bloqueo |
| --- | --- | --- |
| SEC01 · BOLA/IDOR | API server-side verifica recurso, rol, permiso y owner real | Actor A usa ID válido de B, recibe datos o modifica B |
| SEC02 · JWT y sesiones | Firma, issuer, audience, exp, revocación y membresía contrastadas en backend | JWT caducado, sesión revocada o claim manipulado acepta acción |
| SEC03 · RBAC/ABAC | Permiso y alcance de negocio por operación en backend, denegación por defecto | UI, flag o payload crea permiso no autorizado |
| SEC04 · Aislamiento A/B | Fixtures sintéticos A/B; filtros EF y claves foráneas referidas al tenant efectivo | Consulta, cache o escritura mezcla recursos A/B |
| SEC05 · Host y spoofing | Binding exacto autorizado de host/StoreKey/tenant, fuente confiable | Host parecido, substring o cabecera falseada resuelve otro owner |
| SEC06 · Archivos y exportación | Firma, owner, TTL, redacción y permiso efectivo comprobados | URL expirada/replay, archivo B o PII de otro usuario se descarga |
| SEC07 · Jobs y reintentos | Worker autorizado e idempotencia transaccional con permiso vigente | Duplicado o revocación genera segundo efecto o efecto cross-tenant |
| SEC08 · Caché y propagación | Namespace por tenant/permiso/versión, invalidez ante revocación | Datos de B sobreviven en caché de A o se sirven tras revocar |
| SEC09 · Backup/restore | Sandbox efímero autorizado; manifest, checksum, RPO/RTO y rollback medidos | Restauración incompleta, PII cruzada o destino productivo involuntario |
| SEC10 · Privacidad y logs | Trazas redactadas, correlación opaca, retención y acceso limitado | JWT, refresh token, email ajeno o secreto aparece en logs |
| SEC11 · Hallazgos P0/P1 | Inventario reproducible, corrección y retest sobre SHA correspondiente | P0/P1 abierto o sin inventario aparece como cero |
| SEC12 · Publicación | Scope Lock, tests causales, A5 ajena y permiso explícito por ambiente | CANCELLED/SKIPPED/queued como PASS o LISTO_DEV abre QA/PROD |

**Criterio de salida del diseño M02:** SEC01–SEC12 deben estar **especificados**, rastreables y con negativas explícitas; su ejecución real pertenece a M03–M06 y a autorizaciones de ambiente posteriores. La evidencia mínima del plan «cero P0/P1 sin mitigación aprobada» es **gate futuro obligatorio**, actualmente `NO_CERTIFICADO`; no se rebaja a una firma documental. No ejecutar ataques con datos reales ni modificar permisos de clientes.

## 3. Prerrequisitos técnicos para M03 y exclusión de efectos

- **M03.01:** dominio/validaciones server-side reales; demostrar aislamiento BOLA/IDOR, negocio y DTO con tests positivos y negativos en CI .NET 10.
- **M03.02:** Application/use case, contratos y errores sin confiar en `EmpresaId`, tenant o claim del cliente; integrar tests causales.
- **M03.03:** JWT, membresía, RBAC y autorización/tenancy A/B efectivos; se requiere test negativo de acceso cruzado. Un documento M02 no autoriza modificar IAM sin su gate.
- **M03.04:** EF Core 10/MySQL 8.4, índices, migración reversible y pruebas con servicio MySQL efímero antes de cualquier acción persistente.
- **M03.05:** worker/proveedor/cola **sólo si** alcance y permisos lo justifican; idempotencia de efecto, cuotas, locks reales y pruebas de revocación. No instalar dispatcher por este documento.
- **M03.06:** auditoría y logs estructurados sin PII/secretos; correlación por SHA, scope y actor, con cobertura y retención verificadas.

Para cada ID, primero releer PLAN!H/I/K, COLA!G/O, DAG!F/G y SHA de HEAD; el avance M02.06 en `VALIDANDO` no habilita implementación, migración ni revisión por la misma Task. Una eventual transición `LISTO_DEV` exige A0–A4 y su propia A5 independiente antes de liberar predecesoras que la requieran; M03 aún necesita todos sus gates particulares. Ninguna hoja constituye autorización de QA, main, PROD, Cloudflare, Hostinger, DNS, secretos o datos productivos.

## 4. Política de evidencias y decisión de seguridad

Definir un expediente por escenario con `microtask_id`, `source_sha`, `head_sha`, `check_run_id`, `test_case_id`, `actor_role`, `tenant_fixture`, `expected_denial`, `observed_result`, `severity`, `retest_sha`, `reviewer_task_id` y `rollback_evidence`. Este es un **esquema documental**, no una tabla o API instalada. Evidencia inválida: texto de plan sin ejecución, control de SHA diferente sin equivalencia demostrada, job requerido CANCELLED/SKIPPED/IN_PROGRESS, test sólo positivo, revisor igual al autor o permisos de producción inferidos.

| Estado futuro | Regla de evaluación |
| --- | --- |
| `NOT_EVALUATED` | Faltan prueba, permiso, ambiente, SHA o fixture; no emitir PASS |
| `IN_PROGRESS` | CI o revisión realmente activa; conservar scope VALIDANDO |
| `FAIL` | Contraprueba rompe invariante, P0/P1 abierto o requisito obligatorio no acreditado; reparar y retestar |
| `PASS_DEV` | Pruebas positivas y negativas causales y checks aplicables SUCCESS en SHA correcto, A0–A4 y readback |
| `A5_PASS_INDEPENDIENTE` | Otro TASK_ID Javier revisa diff, artefacto y evidencia original, sin autoaprobación |
| `RELEASE_AUTHORIZED` | Requiere decisión adicional explícita para entorno de destino, nunca implícita por A5 |

## 5. Recuperación y rollback RB01–RB08

| Riesgo/trigger | Contención en dev | Evidencia de retorno |
| --- | --- | --- |
| RB01 · Scope Lock FAILED | Parar sólo escritura insegura, diagnosticar regla y SHA | Check exact-SHA completed/success de corrección |
| RB02 · CI real inconclusa | Conservar VALIDANDO, seguir otra independiente | Job causal terminado SUCCESS, no CANCELLED |
| RB03 · BOLA/IDOR/tenant | Bloquear promotion y denegar acceso; reparar backend en dev | Retest positivo/negativo A/B en runner autorizado |
| RB04 · Revocación JWT/RBAC | Fallar cerrado y aislar scope afectado | Tests de sesión revocada/grant denegado y regresión |
| RB05 · Migración/backup inseguro | No tocar datos reales, revertir ensayo efímero | Checksum, integridad, RPO/RTO y manifiesto verificables |
| RB06 · Webhook/job duplicado | No habilitar efecto; controlar idempotencia en futura implementación | Un solo efecto con key/versión, prueba de colisión |
| RB07 · Conflicto Git/Sheet | Abort de intento perdedor; respetar owner activo, sin force-push | HEAD nuevo, exclusividad y lectura institucional |
| RB08 · Error documental de esta entrega | Revert normal de commit, evento compensatorio aditivo | Diff de reversión y nuevos checks Scope Lock/V5 |

La reversión de documentos nunca borra BITACORA histórica, retira un hallazgo P0/P1 ni restaura secretos o credenciales. Una solución de carrera no consiste en llamar CAS atómico a dos peticiones separadas de Google Sheets.

## 6. Auditorías A0–A5 de esta microtarea

- **A0 estática:** dos Markdown como único diff, Scope Lock STRICT, ID/owner, doce gates, ocho planes RB y predecesores/links; verificar CHANGELOG autocontenido, sin encabezados vacíos, sin secretos.
- **A1 documental positiva/negativa:** confrontar SEC01–SEC12 y las negativas; **oráculos futuros no ejecutados**, no afirmar pruebas HTTP/pentest.
- **A2 aislamiento y seguridad:** revisión de fronteras de JWT/RBAC, tenant A/B, Host, medios, logs, backups y negativa por defecto; no alterar runtime.
- **A3 integración documental:** cotejar blobs M01.06/M02.01–05, STANDARD, PLAN/COLA/DAG y compatibilidad .NET/Angular/MySQL actuales. Sin pruebas DB/browser reales por cambio Markdown.
- **A4 publicación:** fast-forward/CAS de HEAD de dev sin force, commit único de este documento y `CHANGELOG_AI.md`; verificar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` como completed/success en SHA apropiado, diff/blobs y readbacks PLAN/BITACORA.
- **A5 independiente:** `A5_PENDIENTE` hasta revisión genuina por Task Javier distinta de PRIMARY DEV 01. No certificar ni promover durante la autoría.

**Rollback documental:** `git revert` normal de este changeset, preservando historial y anotando compensación. **Resultado esperado:** contrato M02 definible/verificable; **no** ausencia real de P0/P1, no runtime implementado ni seguridad productiva certificada.
