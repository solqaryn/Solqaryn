# SOLQARYN V5 — C05-04.M02.06: dependencias, contrato de salida y rollback del modelo de ligereza UI

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO/COLA/DAG_TECNICO, Google Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** cierre documental de contratos M02, no certificación de rendimiento ni implementación M03.

## 1. Evidencias y condiciones de entrada

El DAG `C05-04.M02.06` exige `C05-04.M01.01-06+A5 ; C05-04.M02.01-05+A5`. En el preflight institucional se verificaron las predecesoras `LISTO` con `A5_PASS` de Task Javier independiente, `PLAN_MAESTRO!H841=PENDIENTE/I841` vacío, `COLA!G841/O841=SI` y `DAG_TECNICO!F802=VALIDADO/G802=SI`. Esto habilita **únicamente el documento de salida de M02**; no autoriza por sí mismo código, migraciones, rutas, pruebas QA/main/PROD ni la liberación de un sucesor. Los estados y SHA deben releerse después de publicar.

| Antecedente verificable | Contrato de M02 que permanece en dev | Límite de evidencia |
| --- | --- | --- |
| M01.06, documento `c05-04-m01-06-gates-rollback-ligereza-percepcion.md` | G01–G10, P06/N14 y retroceso documental futuro | A5 independiente, no Core Web Vitals medidos |
| M02.01, `c05-04-m02-01-entidades-ownership-presupuesto-ui.md` blob `4e8841ec195b7a8837e8c903936f50434cea9873` | E01–E08, INV01–INV14, 5 positivos/10 negativos de regresión | Entidades lógicas, no tablas ni telemetría ejecutada |
| M02.02, blob `dcc8be8650f408efc7e9dc9536583ab99598810e` | OP01–OP08, seis DTO conceptuales y doce errores | Ningún endpoint ni DTO compilado |
| M02.03, blob `8c203a4c8f1eba192320ff0fd5122de0cf012c77` | Opciones S01–S04, run/evidence por SHA y siete migraciones propuestas | No se creó base, índice, DDL ni fixture en MySQL |
| M02.04, blob `9ce6243e7964cb557cc9878550820ca42b2eec58` | Doce claves tipadas, precedencia segura y flags de performance | Valores propuestos NO_COMPILADOS ni habilitados |
| M02.05, blob `7a8f5cc67892d17ca3ab60905d5f116fc69a8320` | Ocho señales futuras, idempotencia, cuotas y saneamiento | Ningún evento fue emitido, no hay dispatcher/webhook |

Las decisiones de esos documentos permanecen propuestas sujetas a implementación y pruebas. El cierre M02 no convierte promesas de rendimiento, accesibilidad, aislamiento entre tenants ni fallas de seguridad hipotéticas en evidencia real.

## 2. Contrato de salida M02: diez puertas verificables

| Gate | Condición documental de cierre | Evidencia exigible durante la futura implementación |
| --- | --- | --- |
| G01 | Componentes y rutas afectados identificados sin duplicar contratos | Diff Angular y matriz de consumidores/rutas |
| G02 | Presupuesto de bundle identificado y no relajado | `frontend/angular.json`: initial warning **650kb**, error **750kb**; estilos warning **16kb**, error **32kb**; build CI exact-SHA |
| G03 | Métricas CWV definidas sin cifras de mejora fabricadas | LCP, INP, CLS con dispositivo, red, muestra, ruta, baseline, SHA y artefacto |
| G04 | Duplicación HTTP delimitada por actor, sesión, tenant y filtros | Test Playwright/API; una dedup inválida no omite 401/403/errores |
| G05 | Carga progresiva y navegación preservadas | Pruebas de lazy loading, foco/restauración, teclado, lector y reduced motion |
| G06 | Seguridad UI/backend claramente separada | JWT/RBAC/recurso/tenant A/B en API; esconder menú no concede permiso |
| G07 | Invalidación de caché por tenant y revocación documentada | A→B, B→A, token expirado, permiso revocado y respuesta atrasada |
| G08 | Estados loading/empty/error/retry tienen oráculos accesibles | Pruebas 320px, 390px, 768px y escritorio con fallos simulados |
| G09 | Datos e instrumentos de medición minimizados | Sin JWT, cookie, PII, URL firmada, payload privado ni telemetría no consentida |
| G10 | Publicación, reversión y dependencias identificadas | SHA exacto, CI causal, A0–A4, A5 independiente y rollback normal |

**Resultado documental aprobado para diseñar:** G01–G10 tienen criterios y fuentes; **resultado runtime: NO_EJECUTADO**. No registrar PASS funcional de G02/G03/G04/G05/G06/G07/G08/G09 hasta pruebas causales en el cambio de código respectivo. El presupuesto es una política configurada, no el tamaño producido ni una métrica real de usuarios.

## 3. Matriz de riesgos, precedencia y decisiones pendientes

- R01: deduplicar por URL sin Empresa efectiva mezcla A/B; rechazar claves incompletas y respuestas tardías.
- R02: precarga de módulo privado filtrada por nombre de ruta revela datos; conservar auth backend y carga selectiva autorizada.
- R03: skeleton/focus/reduced-motion mal coordinados rompen accesibilidad; exigir oráculos por interacción.
- R04: subir budget para pasar CI oculta regresión; requerir revisión de impacto y baselines, no cambiar los límites aquí.
- R05: capturar JWT, identificador cliente o ruta sensible para medir performance expone privacidad; datos sintéticos, saneados y efímeros.
- R06: flags frontend o host/slug conceden acceso; backend resuelve autorización real siempre.
- R07: `EmpresaId` legado NULL no es permiso universal; no trasladar datos de B a A ni inferir tenant del navegador.
- R08: instrumentación de eventos propuesta se interpreta como sistema instalado; no hay autorización para scheduler, dispatcher, webhook ni nueva Task.

**Decisiones sin fabricar:** baseline/objetivos cuantitativos para LCP/INP/CLS, muestreo, retención, dedup de solicitudes, política de telemetría, autorización de persistencia EF/MySQL y activación de flags siguen pendientes de un scope de código que incluya pruebas y, si corresponde, aprobación humana.

## 4. Matriz futura P/N — explícitamente NO_EJECUTADA

| ID | Positivo que deberá ejecutarse | Negativo que debe ser rechazado |
| --- | --- | --- |
| P01/N01 | Build Angular exact-SHA respeta budgets sin inflarlos | Compilación sobre límite y check fallido se marca PASS |
| P02/N02 | Métricas CWV comparables tienen valores observados y artifact | Número objetivo se reporta como medición real |
| P03/N03 | Consulta duplicada por misma identidad conserva resultado | Dedup mezcla tenant A y B o silencia HTTP 403 |
| P04/N04 | Navegación a 320px mantiene CTA, foco y lectura | Tab atrapa foco, oculta error o CTA |
| P05/N05 | Cambio autorizado A→B elimina estado y respuesta previos | Caché/response viejo muestra contenido de A en B |
| P06/N06 | Token o permiso revocado deniega endpoint privado | Guard Angular o feature flag simula autorización |
| P07/N07 | Error de red muestra estado accesible y retry acotado | Timeout se convierte en éxito/empty sin evidencia |
| P08/N08 | Revisión independiente verifica SHA y test apropiado | CI queued/cancelled/skipped/fail se considera PASS |

## 5. Handoff M03, seguridad y rollback

Las tareas M03 son **implementación real**: código Angular/.NET, tests positivos/negativos, seguridad tenancy JWT/RBAC y CI pertinente. Requieren comprobar `C05-04.M02.01-06+A5` en DAG actual, `COLA G/O=SI`, ausencia de owner y claim exclusivo; la publicación de este documento M02.06 no activa M03 ni constituye A5. No alterar `main`, QA, PROD, datos reales, cuentas ni servicios de pago. Rollback de este changeset documental: `git revert` normal en dev, preservando cambios de otros autores; no existe migración ni dato que restaurar.

**Auditoría causal de M02.06:** A0: diff `docs/v5` + CHANGELOG, sin runtime ni secretos. A1: diez gates y matriz P/N revisables documentalmente, sin tests funcionales ejecutados. A2: tenant A/B, JWT/RBAC, privacidad y autorización backend tratados como límites de diseño, no evidencia de prueba runtime. A3: correspondencia con contratos M01/M02.01–05, sin integración API/DB/CI adicional (NO_APLICA causal). A4: publicar sólo por Git fast-forward/CAS sobre HEAD vigente, comprobar `verify-solqaryn-project-scope` y check V5 de procedencia `completed/success` para ese SHA, BITACORA/PLAN/COLA readback. A5: exclusivamente otra Task Javier distinta de la autora. Ninguna prueba `queued`, `in_progress`, `skipped`, `cancelled` o `failed` se cuenta PASS requerido.
