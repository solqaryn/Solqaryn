# SOLQARYN V5 FINAL — C02-04.M01.06: bloqueo, rollback y salida de consola global auditada

**Microtarea exclusiva:** `C02-04.M01.06`, contrato de preparación documental M01, **no** implementación de consola, IAM o MFA. **Responsable:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14.

**Fuentes únicas:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Los 2.340 IDs son el universo autorizado. Este cierre no habilita eventos ni automatismos nuevos.

## 1. Resultado objetivo y fronteras explícitas

**Objetivo FUTURO de C02-04:** una consola global auditada separada del ERP y de sus permisos empresariales. Una operación excepcional deberá exigir autoridad global server-side vigente, MFA y reautenticación reciente y ligada a la acción, motivo validado, scope limitado a operación/recurso/empresa, TTL corto revocable, auditoría crítica durable e idempotencia. Un rol de administrador ERP, JWT aislado, `EmpresaId`, host, header, sesión antigua o UI no son grants de plataforma.

**Entrega real de M01.06:** documento de bloqueos, rollback y condiciones de salida M01. No construye MFA, grant temporal, API, autorización nueva, token, outbox, worker, auditoría append-only, migración MySQL/EF, frontend ni endpoint privilegiado. El inventario M01.03 observa código existente de `PlataformaEmpresasController`, `PlataformaScopeService` y `PlatformSupportPolicy`, pero no certifica un flujo de elevación excepcional. Toda implementación y test real pertenece a sus propios IDs M02–M06.

## 2. Dependencias, procedencia y gate de esta microtarea

Al preflight se verificaron **40/40** preparaciones `C01-01.M01.01–05`, `C01-02.M01.01–05`, `C01-03.M01.01–05`, `C01-04.M01.01–05`, `C02-01.M01.01–05`, `C02-02.M01.01–05`, `C02-03.M01.01–05` y `C02-04.M01.01–05` en `LISTO`, con evidencia A5 de revisores de las **seis Tasks Javier habilitadas**, sin usar revisores históricos retirados como autoridad. La entrada individual de `DAG_TECNICO` exige esas dependencias y figura `F=VALIDADO/G=SI`; `COLA!G403/O403=SI`, `PLAN!H403=PENDIENTE/I403` vacío antes del claim. No inferir por ello que M02 o el acceso privilegiado quedan liberados.

| Fuente documental verificada | Blob SHA en `dev` | Propósito específico |
| --- | --- | --- |
| `docs/v5/c02-04-m01-01-objetivo-no-regresion-consola-global.md` | `851f59e3ff2870f182fe8be3bf57ae606448c557` | Objetivo, exclusión del runtime, no regresión de privilegios |
| `docs/v5/c02-04-m01-02-actores-entradas-salidas-consola-global.md` | `523001b7d82a71d180891e400478278174ab2838` | Actores, identidad de plataforma, auditor independiente y ERP aislado |
| `docs/v5/c02-04-m01-03-inventario-fuentes-contratos-consola-global.md` | `2459fc9e9c7437fb4dc485ad6a82553178d8b9e7` | Fuentes de código existentes y límites observables |
| `docs/v5/c02-04-m01-04-riesgos-supuestos-decisiones-consola-global.md` | `4ea99399da5db5ce20d849893cd75972fb67e8b4` | Riesgos R01–R15, supuestos y decisiones aún pendientes |
| `docs/v5/c02-04-m01-05-aceptacion-consola-global-auditada.md` | `73ba92d91716749b5ac62fa9e4b4a79915be7b92` | Criterios futuros AC01–AC24, NO pruebas ejecutadas |

Tras el claim exclusivo, `PLAN!H403=EN_PROGRESO`, `I403=6adc2e4bd8a48191ac7d8641ed95a1a3` y `COLA!O403=NO` por fórmula. Antes de LISTO, pasar por VALIDANDO, comprobar A0–A4 proporcionales y readbacks PLAN/COLA/BITACORA A:Z de 26 campos. A5 requiere otro TASK_ID Javier y no es condición suplida por la revisión del propio autor.

## 3. Gates de seguridad, denegación y reversión

| Gate | Condición necesaria para una implementación FUTURA | NO_GO o reversión |
| --- | --- | --- |
| Identidad y autorización | Resolver rol global persistido vigente desde backend y separar sus grants del ERP/tenant | Admin ERP A o soporte fuera de allowlist no accede a B ni opera globalmente |
| MFA y reautenticación | Segundo factor validado y reciente para **cada operación sensible**, atado al actor/operación/sesión | Sesión antigua, JWT solo, MFA ausente, replay o token revocado → DENY |
| Privilegio excepcional | Motivo no vacío, aprobación requerida, grant de mínimo privilegio con tenant/recurso/acción precisos y TTL revocable | Grant sin expiración, sin razón, de empresa ajena o reuso fuera de scope → DENY |
| Aislamiento multiempresa | Resolver tenant server-side y ownership; ningún `EmpresaId` del cliente amplía ámbito | Intento A→B, sucursal incorrecta, usuario revocado, claim falso → DENY y 0 efectos |
| Auditoría crítica | Actor, acción, motivo sanitizado, recurso, timestamp, correlación, resultado y denegaciones, sin PII/secrets | Fallo de auditoría obligatoria o registro mutable que borra historia → DENY, rollback transaccional y alerta segura |
| Idempotencia / concurrencia | Identificador de operación estable y único, control replay/cancelación antes de efecto | Doble ejecución, concurrencia sin ganador único o efecto sin auditoría → bloquear |
| Plan y code ownership | Releer H/I, G/O, DAG, permisos, exclusión de archivos y HEAD exacto antes de escribir | Colisión o CAS fallido → no tomar ID; no force-push, revert normal y trazabilidad |
| Quality gates | A0 estática, A1 prueba positiva/negativa según diff, A2 tenant y secretos, A3 integración, A4 commit fast-forward, Scope Lock + V5 exact-SHA | SKIPPED/CANCELLED/IN_PROGRESS/FAIL no son PASS de prueba requerida; no usar checks de SHA ajeno |
| A5 y promoción | Revisión por distinta Task Javier y autorización separada para QA/main/PROD | Autoaprobación, cuenta excluida, cambio de branch sin autorización → NO_GO |

**Rollback de este documento:** `git revert` ordinario sobre el commit documental, previa comprobación de HEAD y consumidores, manteniendo BITACORA histórica intacta y evento compensatorio si hubiera discrepancia. No hay revert de MySQL productivo: **no se modifican datos ni roles**. Si en futuras microtareas se agregan grants o privilegios, definir de manera independiente revoke inmediato, TTL, corte de sesiones, rollback de esquema y verificación A/B real.

## 4. Oráculos documentales: sin certificación runtime

| Caso | Resultado exigido |
| --- | --- |
| P01 (+) | Los 40 antecedentes exactos están LISTO con A5 Javier; DAG VALIDADO/SI, H/I/COLA y claim CAS de un solo propietario |
| P02 (+) | La matriz de restricciones futuras incluye plataforma≠ERP, MFA/reautenticación, motivo, TTL, scope, auditoría y reversión, sin afirmar implementación |
| P03 (+) | Dos archivos Markdown, sección CHANGELOG autocontenida, Scope Lock y procedencia V5 SUCCESS SHA exacto, BITACORA A:Z, readback; A5_PENDIENTE |
| N01 (−) | Falta una dependencia, review Javier válida, actor autorizado, DAG individual o dueño único: NO_GO |
| N02 (−) | Admin ERP A intenta acceso global o recurso empresa B: DENY |
| N03 (−) | JWT robado, MFA no reciente, reautenticación omitida, sesión revocada o replay: DENY |
| N04 (−) | Motivo vacío, grant sin TTL, scope mayor al aprobado o operación no autorizada: DENY |
| N05 (−) | Fallo del registro crítico, evento con secretos, duplicado idempotente o auditor sin permisos: DENY |
| N06 (−) | CI obligatoria pendiente/skip/cancelada/fallida o de SHA ajeno, CHANGELOG intercalado o evento A:Z incompleto: NO_PASS |
| N07 (−) | Evidencia documental interpretada como prueba de MFA/MySQL/JWT live, A5 propia o permiso de QA/main/PROD: NO_GO |

**Clasificación de esta unidad:** diff únicamente `.md` + `CHANGELOG_AI.md`; `.NET/API=NO_APLICA`, `Angular/Node runtime=NO_APLICA`, `MySQL/EF/Docker=NO_APLICA`, `Playwright=NO_APLICA`, revisión de seguridad **documental** `SI`, Scope Lock y procedencia CHANGELOG sobre SHA publicado `OBLIGATORIOS`. Los AC01–AC24 y R01–R15 son especificaciones/riesgos, no tests ejecutados aquí. En futuras entregas ejecutables, CI .NET, integración MySQL efímera, JWT/RBAC tenant A/B, auditar secretos y Playwright cuando aplique.

## 5. Condición de liberación estricta

LISTO_DEV de este documento acredita únicamente la salida **documental** M01. Para cada siguiente ID M02 se necesita preflight nuevo con DAG exacto, propietarios, dependencias, decisiones pendientes de política MFA/TTL/aprobación, código real, pruebas/rollback y A5 independiente. Jamás habilitar automatismos server-side infinitos, cuentas extra, roles cross-tenant, un séptimo ejecutor, consola global activa, IAM, pagos, migraciones, despliegues, QA/main/PROD ni releases por el estado M01.06. Un A5_FAIL causal histórico bloquea hasta reparación y evaluación independiente.
