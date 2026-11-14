# C00-04.M01.05 — Criterios de aceptación funcional y técnica observables

**Fuente ejecutable:** SOLQARYN V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Autoridad:** Skill canónica `solqaryn-project-governance`, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_V13.md`, `docs/AUTOMATION_AUTHORITY.md`.

**Resultado exigido:** fijar qué requiere autorización, qué es reversible y qué no se ejecuta automáticamente. **Salida de M01.05:** contrato de aceptación y matriz de decisiones, riesgos y gates. Es una microtarea de preparación **documental**, no código runtime, certificación global C00 ni autorización de QA/main/PROD.

## Criterios observables

| ID | Condición de aceptación | Rechazo causal |
| --- | --- | --- |
| AC01 Identidad | Cuenta Javier GitHub `jmejia31`, Drive JAVIER, TASK_ID real y slot autorizado | Suplantar Task Alex, interpretar `is_self_owned=false` como permiso o denegación definitiva |
| AC02 Estado | `PLAN_MAESTRO!H` es el único estado mutable, I propietario; `COLA!E` es espejo por fórmula | Escribir E a mano, tomar ID con propietario ajeno |
| AC03 Prerrequisitos | DAG `VALIDADO`, once dependencias de C00-04.M01.05 LISTO, COLA G/O=SI antes de claim | Reserva objetivo N o `G=SI` aislado habilitando despacho ficticio |
| AC04 Alcance | RESULTADO_EXIGIDO, artefactos, interfaces, dependencias, riesgos y rollback definidos | Dar por implementado código exigido mediante documentación |
| AC05 Transiciones | PENDIENTE → EN_PROGRESO → VALIDANDO antes de A0–A3 → LISTO solo tras A4 | Saltar estado o marcar PASS sin candidato |
| AC06 A0 | Markdown, diff, secreto, changelog aditivo, versión y huella del candidato revisados | Token/secretos o sobrescritura concurrente |
| AC07 A1 | Objetivos, matrices D01–D05, riesgos R01–R10 y casos positivos/negativos comprobables | Aceptación genérica no verificable |
| AC08 A2 | JWT/RBAC/tenant A/B, permisos, secretos y no QA/main/PROD preservados | Bypass de autorización, autoaprobación A5 o cruce de cuenta |
| AC09 A3 | Contratos V5/V13/DAG, BITACORA A:Z 26 y pruebas técnicas por superficie modificada | Usar SKIPPED o SHA viejo como PASS de .NET/MySQL/Angular requerido |
| AC10 Publicación | Commit directo fast-forward a `dev`, HEAD revalidado, no PR/ramas ni force-push | Destruir historia o pisar diff ajeno |
| AC11 A4 | Scope Lock exact-`head_sha` `completed/success`, blobs/diff, rollback y PLAN/COLA/BITACORA readback | Scope Lock pending/skipped/failure o SHA distinto |
| AC12 A5 | Evento `A5_PENDIENTE` distinto de A5_PASS; revisor independiente posterior | Autorrevisión, BITACORA truncada, inferir autorización QA/PROD desde LISTO_DEV |

## Casos positivos y negativos observables

**P1 positivo documental:** una Task verificada encuentra C00-04.M01.05 PENDIENTE, I libre, DAG VALIDADO y prerequisitos C00-01.M02.01–06 y C00-03.M01.01–05 LISTO, con G/O=SI. Tras claim, candidato y VALIDANDO, A0–A3 documentales PASS, commit FF dev, Scope Lock exact SHA PASS y A4/readback, llega LISTO_DEV con A5_PENDIENTE por otro TASK_ID, sin QA/main/PROD.

**P2 positivo técnico condicional:** para un cambio futuro C#/.NET, validar compilación/tests dotnet del SHA exacto y pruebas seguridad proporcionales. Si afecta MySQL/EF, exigir integración MySQL real. Si afecta navegación Angular, exigir lint/build y E2E/accesibilidad si aplican. Para este documento sin código ejecutable, dichas subpruebas son `NO_APLICA` motivadas, nunca PASS inventado.

**N1 identidad y autoría:** si PLAN I pertenece a otra Task, conservar autor y no reclamar, aunque una señal de cola parezca favorable. Javier no administra Alex; errores get/peek aislados no validan permisos.

**N2 DAG y concurrencia:** un prerequisito no LISTO, DAG no VALIDADO o G/O distinto de SI impide claim, sin importar objetivos de reserva 2N–3N.

**N3 BITACORA:** una fila con 21, 22, 23, 24 o 25 campos en vez de 26 A:Z, o `EVENT_ID` / `IDEMPOTENCY_KEY` duplicados, se rechaza o reconcilia sin duplicados.

**N4 CI y SHA:** check obligatorio `queued`, `skipped`, `failure` o asociado a otro `head_sha` impide A4 y LISTO. Un job técnico ajeno a documento no se espera como gate artificial.

**N5 permiso de entorno:** A5 por el mismo autor, A5_FAIL material no corregido o falta de orden expresa impiden promoción a QA/main/PROD; no bloquean otros IDs DEV independientes.

**N6 irreversibilidad:** no ejecutar migraciones productivas, DNS, VPS, Cloudflare/R2, proveedores, pagos, secretos o cambios a main a partir de un documento aprobado; requieren permiso y backup/rollback causal.

**N7 HEAD concurrente:** si el HEAD avanzó, integrar cambios ajenos sin sobrescribirlos y repetir verificaciones afectadas; prohibido force-push.

## Decisiones, riesgos y reversibilidad

| Decisión | Control/gate | Resultado |
| --- | --- | --- |
| D01 Única fuente V5 | ID único, RESULTADO_EXIGIDO y Sheet vivo | No traer planes V3/V13 antiguos |
| D02 DEV seguro | A0–A4 y Scope Lock SHA exacto | Commit FF; A5 posterior por otra Task |
| D03 Promoción | A5_PASS independiente y autorización adicional | No QA/main/PROD en esta microtarea |
| D04 Reversibilidad | Revert de commit documental, backup/restore antes de tocar datos | No equiparar git revert con reversión MySQL/infraestructura |
| D05 DAG y capacidad | 11 prerequisitos y estado VALIDADO, G/O auténticos | Objetivo 2N–3N no libera fases sin gate |

Riesgos R01–R10 de concurrencia, CI, auditoría, tenancy, secretos, roles, migraciones y reversibilidad se documentan en `docs/v5/c00-04-m01-04-riesgos-supuestos-decisiones.md` y deben revalidarse por alcance; la matriz no certifica ausencia de riesgo.

## Pruebas A0–A5 aplicables y no-alcance

**TIPO 1 documental:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI` por límites de autoridad, `requiere_scope_lock=SI` por commit documental.

A0: revisar Markdown/diff/secretos/changelog aditivo. A1: AC01–AC12, P1–P2, N1–N7. A2: separar autor/revisor, JWT/RBAC, tenant, secretos y entornos. A3: contrastar V5, Skill, DAG, PLAN/COLA y BITACORA A:Z; pruebas runtime no aplicables al documento. A4: commit FF, hash exacto, Scope Lock y readback institucional. A5: revisor con TASK_ID distinto posterior; no autopromoción.

**No-alcance:** no modificar API, DTO, frontend Angular, backend .NET, MySQL/EF, Docker, workflows, secrets, workers, datos, ramas, reglas ni servicios. Este documento no certifica la implementación C00-04 completa.

**Rollback:** revertir el commit documental por `git revert` normal; reconciliar cambios institucionales con eventos nuevos completos de 26 columnas, nunca borrar BITACORA, resetear o forzar `dev`.
