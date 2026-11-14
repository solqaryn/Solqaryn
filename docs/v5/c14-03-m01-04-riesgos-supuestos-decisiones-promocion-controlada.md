# SOLQARYN V5 FINAL — C14-03.M01.04: riesgos, supuestos y decisiones de promoción controlada

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER, `6af16bb9492c8191b8e8c7d606f11507`. **Alcance exclusivo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** preparación documental M01; no se implementa ni autoriza un release.

Fuentes: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.01 objetivo](./c14-03-m01-01-alcance-promocion-controlada.md), [M01.02 actores e interfaces](./c14-03-m01-02-actores-interfaces-promocion-controlada.md) y [M01.03 fuentes Git](./c14-03-m01-03-inventario-fuentes-evidencia-promocion-controlada.md). Prefiltro PLAN fila 2093 PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI, claim condicional a EN_PROGRESO y lectura de owner.

## 1. Alcance semántico y límites

Los riesgos son **hipótesis potenciales**, no vulnerabilidades confirmadas; los supuestos requieren evidencia y las decisiones abiertas no implican permiso de actuación. El objetivo C14-03 es preparar una promoción futura trazable por SHA, artefacto, tests, configuración, aprobación por entorno y rollback separado. No se han medido paridad, salud de QA/main/PROD, tokens, MySQL de producción, RTO/RPO ni cumplimiento normativo. No se registran hallazgos efectivos P0/P1 sin causa probada.

## 2. Riesgos potenciales R01–R14

| ID | Riesgo POTENCIAL, no confirmado | Comprobación futura requerida |
| --- | --- | --- |
| R01 | SHA y artefacto divergente | Digest inmutable y build causal |
| R02 | Bypass de branch protection | Comprobar reglas y permisos reales |
| R03 | CI skipped/cancelled tratada como PASS | Checks completados y logs exact SHA |
| R04 | A5/LISTO_DEV interpretado permiso PROD | Aprobación separada por entorno |
| R05 | Tokens o secretos expuestos en CI | Matriz sin valores y escaneo artefactos |
| R06 | Tenant A/B con fuga de datos | HTTP positivo y negativo por empresa |
| R07 | Revocación JWT/RBAC incorrecta | Pruebas sesión caducada y rol inactivo |
| R08 | DDL MySQL irreversible | Backup y restore en servicio efímero |
| R09 | Saldo o pago duplicado | Pruebas de idempotencia/rollback |
| R10 | DTO/API incompatible | Pruebas contractuales de consumidores |
| R11 | Ausencia de health/alertas | Métricas y readiness causal |
| R12 | Rollback destructivo | Ensayo de revert normal sin perder datos |
| R13 | E2E accesibilidad rota | Playwright, foco y captura por viewport |
| R14 | BITACORA modificada o doble owner | CAS, idempotencia y readback |

Un archivo de workflow o un test fuente demuestra que existe código, **no** que una ejecución específica haya pasado ni que el sistema sea seguro. Los checks obligatorios deben estar `completed/success` sobre `head_sha` exacto; `skipped`, `cancelled`, `failed`, `queued`, `in_progress` no son PASS.

## 3. Supuestos S01–S08

| ID | Hipótesis a comprobar | Estado |
| --- | --- | --- |
| S01 | QA/PROD tienen autorizador físico vigente | NO_VERIFICADO |
| S02 | Workflow de paridad corre en todo release | NO_VERIFICADO |
| S03 | Artefacto reproducible entre entornos | NO_VERIFICADO |
| S04 | Existe backup y restore MySQL ensayado | NO_VERIFICADO |
| S05 | API valida propiedad por recurso | NO_VERIFICADO |
| S06 | Fixtures sintéticos A/B suficientes | NO_VERIFICADO |
| S07 | Observabilidad cubre deploy/rollback | NO_VERIFICADO |
| S08 | Rollback compatible con transacciones recientes | NO_VERIFICADO |

Cada hipótesis permanece NO_VERIFICADO hasta readback de autoridad y pruebas causales; no se consultan secretos ni servicios productivos para preparar el documento.

## 4. Decisiones D01–D08

| ID | Decisión que requiere autoridad o diseño futuro | Estado |
| --- | --- | --- |
| D01 | Autoridad y permisos de QA | ABIERTA |
| D02 | Autoridad y permisos de main/PROD | ABIERTA |
| D03 | Jobs obligatorios por tipo de cambio | ABIERTA |
| D04 | Divergencias aceptables entre configuraciones | ABIERTA |
| D05 | Firma y almacenamiento del artifact digest | ABIERTA |
| D06 | SLO y umbral de abortar el release | ABIERTA |
| D07 | Fixtures/migraciones efímeras en CI | ABIERTA |
| D08 | Evidencia de accesibilidad y smoke | ABIERTA |

No se pueden crear ramas, PR, Tasks, modificar schedules o autorizar QA/main/PROD para resolverlas implícitamente.

## 5. Ocho parejas de aceptación futura

| Caso | Positivo FUTURO | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Artefacto y SHA publicado coinciden con lo probado | Digest distinto bloquea |
| P02/N02 | CI exact-SHA requerida completada success | Skipped/queued/cancelled no son PASS |
| P03/N03 | A5 ajena y permiso explícito para destino | Autoaprobación o herencia de DEV bloquea |
| P04/N04 | JWT/RBAC y tenant A/B rechazan propiedad ajena | Header/ID spoofed nunca autoriza |
| P05/N05 | MySQL efímero y rollback conservan negocio | DDL destructivo o saldo inconsistente bloquea |
| P06/N06 | Playwright y health reales reproducen UX | Fuente .spec no es ejecución |
| P07/N07 | Audit trail tiene owner único e idempotencia | Doble claim o edición histórica bloquea |
| P08/N08 | Revert normal preserva transacciones | Force-push/reset destructivo bloquea |

Todos los escenarios tienen estado `FUTURO_NO_EJECUTADO`. La lista preserva los controles JWT, RBAC real del servidor, tenant A/B, datos comerciales, MySQL, accesibilidad y secretos. No se atribuyen pruebas HTTP, Playwright, migraciones, backups o despliegue.

## 6. Validaciones de la unidad documental

- **A0**: presencia y coherencia de 14 riesgos potenciales, 8 supuestos no verificados y 8 decisiones abiertas.
- **A1**: ocho parejas positivas/negativas documentales coherentes, sin runtime.
- **A2**: invariantes JWT/RBAC/tenant A/B, secretos, privacidad y consistencia financiera sin cambiar API/DB.
- **A3**: trazabilidad al gobierno y M01.01–03. Compilaciones y suites de runtime son `NO_APLICA` al diff solo Markdown.
- **A4**: publicación fast-forward/CAS en dev solo dos Markdown, checks Scope Lock y V5 provenance `completed/success` de SHA publicado, readback PLAN/COLA/BITACORA.
- **A5**: revisión independiente futura de otra Task Javier, sin autoaprobación ni autorización QA/main/PROD.

**Rollback de documento:** revert normal y auditado del commit en dev, sin reset, force-push, escritura retroactiva de BITACORA, intervención en proveedores, migraciones ni dispatcher/eventos.
