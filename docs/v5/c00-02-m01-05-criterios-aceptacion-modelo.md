# V5 FINAL — C00-02.M01.05: aceptación funcional y técnica observable del modelo de trabajo

**Resultado:** contrato de cola, dependencias, autoría, estados y cierre sin duplicados, con pruebas positivas y negativas comprobables.  
**Autor:** PRIMARY DEV 01 — JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`).  
**Fuente ejecutable:** [V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) (2.340 IDs). Seguimiento [PLAN_MAESTRO/COLA/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Repositorio único `solqaryn/Solqaryn@dev`.
**Fuentes técnicas:** `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_V13.md`, `docs/AUTOMATION_AUTHORITY.md`, `AGENTS.md`, `.agents/skills/solqaryn-project-governance/SKILL.md`, `docs/v5/c00-02-m01-03-fuentes-contratos-evidencias.md` (blob `58f0fc4ae93079fe96124066da8089b93e27016b`). Se respetan las enmiendas V5 de elegibilidad DAG por microtarea y CI condicionada.

## Criterios de aceptación verificables

| ID | Observable positivo / condición PASS | Prueba negativa / condición FAIL | Fuente comprobable |
| --- | --- | --- | --- |
| AC-01 | La selección usa exactamente uno de los 2.340 IDs V5 y su `RESULTADO_EXIGIDO`. | ID inventado o de plan histórico -> no claim. | Google Doc V5 + PLAN!A y E |
| AC-02 | PLAN!H=PENDIENTE, COLA!E copia mediante fórmula, COLA!G=SI y O=SI, y PLAN!I sin dueño antes de reservar. | E distinto, G/O=NO o dueño ya existente -> no escribir. | PLAN H/I + fórmulas COLA E/G/O |
| AC-03 | Un único autor/TASK_ID queda en PLAN!I durante EN_PROGRESO, VALIDANDO y LISTO; otras Tasks operan sobre IDs diferentes. | Segundo claim del mismo ID o overwrite de autor -> rechazo y registro de conflicto. | PLAN I + COLA I + BITACORA |
| AC-04 | Toda transición se refleja sin intervención manual en COLA!E y conserva hora, responsable y evidencia; no crear filas ficticias. | Escribir un valor duro en COLA!E u omitir readback -> NO_GO. | PLAN H/I/M, COLA E |
| AC-05 | VALIDANDO comienza después de candidato verificable y antes de A0–A3; una prueba inactiva no es PASS. | Cambio a LISTO con A0–A4 pendientes o CI vieja -> NO_GO. | Candidate tree/SHA, logs GHA y PLAN |
| AC-06 | A0–A4 se auditan según tipo documental, C#/.NET, frontend, SQL/integraciones o seguridad, y SHA exacto. | SKIPPED o job de tecnología no ejecutada pasa como PASS -> NO_GO; pruebas ajenas sólo se marcan NO_APLICA razonado. | V13, runs/jobs/logs GitHub |
| AC-07 | A4 comprueba SHA, diff, Scope Lock requerido, readback y rollback normal; un commit directo sólo es fast-forward de dev. | Ref no fast-forward, CI obligatoria FALLIDA o Scope Lock FAIL -> NO_GO. | GitHub commit, check runs, PLAN/COLA |
| AC-08 | BITACORA registra EVENT_ID único con 21 columnas y no duplica un ID ya existente. | Repetir EVENT_ID o insertar filas vacías artificiales -> rechazo. | BITACORA A:U |
| AC-09 | LISTO_DEV conserva prueba A0–A4 y `A5_PENDIENTE` hasta revisión con TASK_ID distinto del autor. | Autoaprobar A5 o promover a QA/main/PROD -> NO_GO. | PLAN evidencia, revisión A5 y BITACORA |
| AC-10 | HEAD puede avanzar por otro Scope; revalidar archivos consumidores y base actual sin sobrescribir cambios ajenos. | Validar candidato únicamente con CI de SHA anterior sin equivalencia -> NO_GO. | diff/compare GitHub y blobs |

## Ejemplo positivo, acreditado y limitado a su alcance

La microtarea `C00-01.M05.01` recorrió **PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO_DEV** conservando autor J00. El commit `1de966000059a53ace3faddbb1db53478cf800ff` añadió protección del contexto de ruta Angular y tres pruebas. Priority4 real (3/3), M10 Chromium real (12/12) y Scope Lock exact-SHA aparecen en GitHub Actions. Sus eventos A4, A5_PENDIENTE y LISTO se registraron una vez en BITACORA con 21 campos. Este hecho acredita ese ciclo; **no** equivale a aprobación de M05 completo, ni de prod.

## Negativos de aceptación y criterios de seguridad

- **Concurrencia:** si `C00-01.M05.03` ya está en VALIDANDO con Task distinta, el segundo agente NO puede reclamarla, reescribir owner ni reutilizar el EVENT_ID de esa Task.
- **Plan/DAG:** una puerta operativa P/Q para un bloque distinto no sustituye la comprobación G/O y la precedencia explícita del ID. Si la dependencia real contradice G/O, detener únicamente la operación concreta, conservar trazabilidad y reconciliar sin borrar fórmulas.
- **CI:** un check en progreso o SKIPPED no es PASS; tampoco se espera un job ajeno cuando el cambio es sólo documental y Scope Lock obligatorio está aprobado. No afirmar seguridad multiempresa de una prueba de UI por inferencia.
- **Identidades:** sólo las seis Tasks Javier con cuenta autorizada y las dos Alex en su cuenta, nunca suplantación cruzada; sin cambios de RRULE, títulos, permisos o secretos.
- **Rollback:** revertir commit(s) concretos ordinariamente tras comprobar HEAD y dependencias, sin force-push. Compensar registros mediante nuevos eventos, no borrar BITACORA.

## Checklist de auditorías para este alcance

**TIPO 1 — únicamente documentación y CHANGELOG.** A0: comprobar diff Markdown, enlaces/IDs, ausencia de secretos o dependencias ejecutables y fuente de hashes. A1: comprobar diez criterios observables, prueba positiva y cuatro clases de negativo. A2: comprobar denegación ante claim duplicado, bypass de Auth, secretos/PII y promoción. A3: verificar lectura real de PLAN/COLA/BITACORA, precedencia DAG y concordancia V5/V13; `.NET`, Node/Angular, MySQL, Docker, E2E **NO_APLICA por no modificar runtime**. A4: commit documental directo fast-forward, SHA exacto, Scope Lock, diff final y readback institucional. A5: revisión independiente posterior. No declarar un A5_PASS ni desplegar.

**Limitaciones:** las pruebas en esta ficha son reglas y observaciones de gobierno, no una simulación de API ni certificación de rendimiento live. La autorización de QA/main/PROD no forma parte de esta microtarea.
