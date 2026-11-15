# SOLQARYN V5 FINAL — C03-04.M01.06: bloqueos, rollback y liberación de continuidad

**Microtarea:** `C03-04.M01.06` · **Autor:** PRIMARY DEV 02 — JAVIER (`6af16bb9492c8191b8e8c7d606f11507`) · **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT` · **Fecha:** 2026-11-14, Tegucigalpa.

**Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Aplican la Skill canónica de SOLQARYN, `AGENTS.md`, Scope Lock estricto y revisión A5 por otra Task de las seis Javier.

**Resultado atómico:** contrato DOCUMENTAL de bloqueo, reversibilidad y salida del lote M01 de respuesta y recuperación. **No realiza** respaldo, restauración, revocación, cambio IAM, migración MySQL, activación de servicio, dispatcher ni despliegue; no certifica RPO/RTO, permisos productivos, disponibilidad de credenciales o pruebas runtime. Todos los escenarios descritos son **FUTUROS_NO_EJECUTADOS**.

## 1. Precedencia e integridad del lote

Al reclamar, `PLAN_MAESTRO!H547=PENDIENTE`, `I547` vacío, `COLA!G547=SI/O547=SI`, `DAG_TECNICO!F508=VALIDADO/G508=SI`. La reclamación se efectuó con sustitución condicional del estado y lectura posterior de propietario exclusivo. Este preflight no autoriza sucesoras: sus gates se recalculan después de cada transición.

| Antecedente LISTO + A5 independiente | Archivo Git previamente verificado | Blob de referencia |
| --- | --- | --- |
| `C03-04.M01.01` objetivo y no-regresión | `docs/v5/c03-04-m01-01-objetivo-continuidad-incidentes.md` | `1f5ff7892c6fc0e907f689a467bd3ef23e4d7f21` |
| `C03-04.M01.02` actores y límites | `docs/v5/c03-04-m01-02-actores-entradas-salidas-continuidad.md` | `92efa6ae683a90e4553497a69c0457cb0e205568` |
| `C03-04.M01.03` fuentes y brechas | `docs/v5/c03-04-m01-03-inventario-contratos-continuidad.md` | `ccc2ba4b245562af7bce35c84342fd609c41403e` |
| `C03-04.M01.04` riesgos/decisiones | `docs/v5/c03-04-m01-04-riesgos-supuestos-decisiones-continuidad.md` | `a65a5776c439cb600411fc2860f10e1299cb4ffa` |
| `C03-04.M01.05` aceptación futura | `docs/v5/c03-04-m01-05-criterios-aceptacion-continuidad.md` | `2da9cedd6ea7e098e45657a93c2432a60825f11e` |

Las cinco fuentes están LISTO y tienen constancias A5 de otras Tasks Javier en PLAN/COLA. Los antecedentes técnicos adicionales del DAG siguen siendo autoridad dinámica; no sustituirlos por esta tabla ni aceptar un A5_FAIL histórico sin reparación causal.

## 2. Capacidad observada frente a pruebas aún faltantes

| Ruta observada en HEAD dev | Blob comprobado | Lo que demuestra / lo que no demuestra |
| --- | --- | --- |
| `scripts/backup_dev.sh` | `d2e0ca8bf48dc8c21fcbfd5ab5256c770bdbdcf3` | Exige comandos, variables y allowlist dev/development/ci; no acredita backup exitoso |
| `scripts/restore_dev.sh` | `9be58822ae5c6189e7a84cf785998bf42a1b576c` | Exige bandera destructiva explícita y destino MySQL descartable local; no acredita restore |
| `.github/workflows/backup-restore-dev.yml` | `cee9cdd1e22d006c21d9a23cc40ead6223d7295c` | Define flujo con MySQL efímero; YAML existente no equivale a job aprobado |
| `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md` | `81e8b11557912fffcd741ff0f34faf69e0ed6f47` | Describe entorno DEV y custodia; no acredita secretos ni permisos |
| `docs/ROLLBACK_RUNBOOK.md` | `b7424464a5a79f0d5ff858a7ec4d2356a30355f0` | Separa rollback DEV/PROD; no acredita reversión ensayada |

Ninguna ejecución de MySQL 8.4, Docker, Playwright, JWT/HTTP ni restauración se hizo en esta unidad. Un check `SKIPPED`, `CANCELLED`, `IN_PROGRESS` o de otro SHA nunca prueba recuperación.

## 3. Bloqueos que impiden pasar de diseño a operación

| ID | Condición de bloqueo | Evidencia necesaria para levantarla |
| --- | --- | --- |
| B01 | No existe owner/autoridad de incidente verificados | Identidad y permiso comprobados; decisión auditable |
| B02 | Destino de restore fuera de CI o DEV descartable | Allowlist de entorno/host, permiso específico, prueba sin PROD |
| B03 | Copia sin manifest, checksum o cifrado verificable | Mismo artefacto origen y destino, hash íntegro y logs saneados |
| B04 | Clave ausente, vencida o expuesta | Custodia/rotación aprobadas sin introducir secretos al repo |
| B05 | Revocación de JWT, rol o membresía sin prueba efectiva | HTTP y seguridad A/B, token viejo, rol retirado, DENY |
| B06 | Operador de empresa A intenta ver o restaurar datos B | Aislamiento de recursos y PII por pruebas negativas causales |
| B07 | Reposición reescribe datos ajenos o historial confirmado | Snapshot/compare, exclusión de escritores y rollback comprobado |
| B08 | Cambio EF/DDL sin migración reversible ni ensayo | Servicio MySQL 8.4 efímero, rollback y permisos mínimos |
| B09 | RPO, RTO, retención o ventana sin aprobación/medición | Decisión del propietario y mediciones trazables; no números inferidos |
| B10 | CI obligatoria falló, continúa o fue cancelada | Nueva hipótesis si falla; `completed/success` exact-SHA |
| B11 | A5 ausente, propia o con autor ambiguo | Otra Task Javier, diff/blobs/checks y evento BITACORA |
| B12 | Gate DAG NO o HEAD/owner conflictivos | Releer PLAN/COLA/DAG/HEAD y resolver causa sin forzar SI |

Los bloqueos son reglas de prevención: no equivalen a doce defectos productivos observados. La salida M01 documenta **cómo decidir**, no elimina ni reduce estos controles.

## 4. Estrategias futuras de reversión

| ID | Operación futura | Reversión segura y evidencia exigida |
| --- | --- | --- |
| RB01 | Cambio solo documental de esta unidad | `git revert` normal del commit, sin reset/force-push; mantener BITACORA |
| RB02 | Contención de una identidad | Revaluar JWT/RBAC/membresía; nunca reactivar token sin nueva autorización |
| RB03 | Cambio de configuración de incidente | CAS de versión y registro antes/después por ámbito/tenant; rechazo 409 ante colisión |
| RB04 | Ensayo de restauración | Base descartable, comparación hashes/esquema/datos y destrucción controlada del entorno efímero |
| RB05 | Migración EF/MySQL futura | Plan versionado, ensayo reversible y prohibición de borrado destructivo sin orden |
| RB06 | Despliegue futuro | Orden separada, health/readiness y reversión de versión sin afectar PROD implícitamente |
| RB07 | Fallo de auditoría o integridad | Bloquear cierre y accesos dependientes; preservar evidencia sin PII/secretos |
| RB08 | CI interrumpida por HEAD posterior | Revalidar diff de componente y checks exactos; no reutilizar PASS ajeno |

## 5. Condiciones de liberación y consumidores

**Para cerrar esta M01.06 DOCUMENTAL:** L01 alcance inalterado; L02 cinco fuentes/blobs coherentes; L03 prerequisitos actuales del DAG satisfechos; L04 riesgos y no-autorizaciones explícitos; L05 oráculos positivos/negativos definidos como futuros; L06 JWT/RBAC/tenant A/B fail-closed; L07 no secretos/PII; L08 solo dos Markdown en diff; L09 CHANGELOG con encabezado y viñetas contiguos; L10 Scope Lock y procedencia V5 exact-SHA `completed/success`; L11 PLAN/COLA/BITACORA readback; L12 A5_PENDIENTE por otra Task Javier.

**Para M02 o cualquier implementación posterior:** verificar nuevamente la fila y los predecesores efectivos de `DAG_TECNICO`, `PLAN_MAESTRO` y `COLA` después del LISTO y del A5 independiente. A5 de M01.06 NO autoriza ensayos destructivos, producción, nuevos secrets, QA/main/PROD ni cambios operativos. C10/C12 consumen este contrato solamente como referencia de requisitos, no como certificación de continuidad.

### Oráculos futuros (no ejecutados)

| Caso | Clase | Resultado exigible |
| --- | --- | --- |
| P01 | + incidente autorizado de A | Trazabilidad saneada; ninguna PII de B |
| P02 | + backup y restore CI desde mismo artefacto | Hash, esquema, integridad y rollback medidos |
| P03 | + revocación autorizada | Nueva solicitud y token antiguo rechazados según política comprobada |
| P04 | + rollback descartable | Reversión preserva operaciones legítimas y otro tenant |
| N01 | - intento de restore en host no local | DENY antes de conexión/escritura |
| N02 | - falta bandera de restore destructivo | DENY sin mutaciones |
| N03 | - token revocado o rol inactivo | DENY incluso con bandera/plan activos |
| N04 | - solicitud tenant A a dato B | DENY y cero filtración |
| N05 | - checksum o manifest inconsistente | Abort, ningún éxito ficticio |
| N06 | - operador sin permiso excepcional | DENY con evento auditado |
| N07 | - PII/JWT/contraseña en log público | Sanitización y rechazo de evidencia |
| N08 | - CI `CANCELLED` o `SKIPPED` ofrecida como PASS | Rechazar certificación |
| N09 | - owner o versión cambiados durante operación | Rechazo CAS y relectura |
| N10 | - petición de usar este contrato para PROD | NO_GO, orden diferenciada necesaria |

## 6. Impacto, comprobaciones y cierre

**Capas afectadas:** solo `docs/v5/` y `CHANGELOG_AI.md`; cero modificaciones en Domain, Application, Infrastructure, API, Angular, EF/MySQL, integraciones, workflows y datos. Consumidores posteriores: C03-04.M02, C10/C12 y responsables de seguridad/continuidad. No hay API/DTO nuevo, migración, coste, integración externa ni cambio arquitectónico.

**A0:** revisar SHA, diff, blobs, coherencia de contratos, 12 bloqueos, 8 estrategias, 12 gates y no secretos. **A1:** inspección documental P01–P04 y N01–N10, sin simular ensayos. **A2:** límites de tenant A/B, JWT/RBAC, revocación, host/entorno, PII y autoridad humana. **A3:** conciliación de los cinco precedentes y cinco rutas Git reales; pruebas .NET/Angular/MySQL/Docker/E2E `NO_APLICA_POR_DIFF_MARKDOWN`, nunca PASS inventado. **A4:** publicación fast-forward dev con CAS de HEAD, controles Scope Lock/V5 sobre SHA real y readbacks. **A5:** pendiente de revisión independiente por otro TASK_ID Javier. Rollback de esta entrega: `git revert` normal y evento compensatorio aditivo, sin modificar BITACORA histórica.
