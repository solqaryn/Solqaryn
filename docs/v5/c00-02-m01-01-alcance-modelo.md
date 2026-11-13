# C00-02.M01.01 — Objetivo, no-alcance y no-regresión del modelo de trabajo

**Programa:** SOLQARYN / V5 FINAL / bloque C00-02 — Modelo de trabajo para automatizaciones.
**Microtarea:** C00-02.M01.01; fase M01, alcance y preparación.
**Fuentes vigentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), hoja institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_AUTHORITY.md` y `docs/AUTOMATION_V13.md` del HEAD de `dev`.

## Objetivo verificable

Definir un contrato mínimo, trazable y sin duplicados para tramitar las **2.340 microtareas V5**, desde la elección de una fila elegible hasta su cierre en desarrollo. Las identidades de tarea, dependencias, evidencias y propietario se conservan a lo largo de cada transición. Este documento fija los límites de la fase M01; **no** ejecuta ni certifica otras microtareas.

El modelo debe permitir que las Tasks autorizadas trabajen en distintos IDs sin coedición, que el estado editable de `PLAN_MAESTRO!H` se refleje en `COLA!E` por fórmula, y que cada cambio de estado auditado tenga un `EVENT_ID` único en `BITACORA!A:U`.

## No-alcance

- No agregar, eliminar, renumerar ni reasignar microtareas del plan V5; no convertir los 2.340 IDs en cuota de ejecución.
- No escribir valores manuales, timestamps, responsables ni evidencias en `COLA!A:Q`: es una vista derivada, con dependencias computadas.
- No cambiar la política V13 A0–A5, la Skill canónica, Scope Lock, reglas de autenticación, límites multitenant, JWT/RBAC, secretos o versiones de Angular/.NET/MySQL.
- No crear ramas, PR ni mecanismos VAEP/Jules, CAS, leases o reservas permanentes para simular progreso.
- No administrar Tasks de otra cuenta, reprogramarlas, ni desplegar a `qa`, `main` o producción.
- No asumir `PASS` cuando una CI requerida figura `PENDING`, `SKIPPED`, `FAILURE` o pertenece a otro SHA; no sustituir una prueba real por una declaración.

## Contrato de no-regresión

| Invariante | Comprobación observable |
| --- | --- |
| Única autoridad | El ID y criterio proceden del Google Doc V5; `PLAN_MAESTRO!H` es el estado editable; `COLA!E` conserva fórmula espejo. |
| Elegibilidad | Antes del claim: `PENDIENTE`, propietario libre, `COLA!G=SI`, `COLA!O=SI` y dependencias cumplidas. |
| Exclusividad | Un `TASK_ID` auténtico por ID en trabajo; no pisar autoría ni evidencias de otro actor. |
| Transiciones | `PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO` sólo con trabajo, auditoría y readback reales. |
| Calidad proporcional | A0 calidad estática, A1 funcional, A2 seguridad y A3 integración sobre candidato real; A4 sobre publicación/resultado exacto; A5 independiente para promoción. Marcar `NO_APLICA` con motivo cuando no hay runtime. |
| Idempotencia | Cada evento de `BITACORA!A:U` tiene `EVENT_ID` único y las 21 columnas pertinentes; relectura evita duplicados. |
| Seguridad | Ni un cierre documental ni un `LISTO` DEV conceden permiso de QA/main/PROD. |
| Concurrencia | Releer HEAD de `dev`, estado, custodio y fórmulas antes de publicar; sólo fast-forward sin force-push. |

## Casos de aceptación (contrato, no ejecución simulada)

**Positivo:** una PENDIENTE elegible y libre se reclama por una Task realmente autorizada; el propietario queda visible en PLAN, COLA refleja por fórmula y BITACORA registra un único evento. Tras candidato completo y A0–A4 aplicables con evidencia, LISTO conserva el autor y A5 queda identificado como pendiente si no hubo revisión independiente.

**Negativo 1:** si la fila ya tiene custodio o `COLA!O` no es `SI`, no se realiza claim, no cambia el estado ni se crea un evento falso.

**Negativo 2:** si falla seguridad, falta prueba causal o cambia el SHA de forma relevante, la unidad permanece `VALIDANDO` con responsable y siguiente acción; ningún resultado ajeno se convierte en `PASS`.

**Negativo 3:** si ya existe el mismo `EVENT_ID`, se verifica su readback en vez de añadir otro registro.

## Evidencia y condición de salida de M01.01

La entrega de esta microtarea es **documental**. A0 verifica alcance, markdown, diff, duplicación, enlaces y ausencia de secretos; A1 verifica objetivos, no-alcance, contratos positivo/negativo y criterios observables; A2 verifica prohibiciones de suplantación, fuga de datos y bypass de seguridad; A3 revisa concordancia con las fuentes V5 y V13 sin exigir Node/Angular/.NET/MySQL/Playwright que no se modifican. A4 verifica el SHA publicado, `CHANGELOG_AI.md`, Scope Lock exact-SHA cuando se genere y readback de PLAN/COLA/BITACORA. Cualquier gate requerido pendiente mantiene `VALIDANDO`.

**Rollback:** revertir únicamente el commit documental mediante una reversión ordinaria; nunca reset/force-push. Si existieran anotaciones institucionales, reconciliarlas mediante nuevos eventos auditados, no borrar historial.

Este contrato de límites no certifica la implementación completa de C00-02 ni autoriza promociones.
