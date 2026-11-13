# C00-04.M01.01 — Alcance y no-regresión del gobierno de cambios

**Proyecto:** SOLQARYN / V5 FINAL / C00-04 Gobierno de cambios y decisiones / M01.01.
**Naturaleza:** preparación documental, sin cambios ejecutables ni aprobación de despliegue.
**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); seguimiento: Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.
**Autoridad técnica:** `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_AUTHORITY.md`, `docs/AUTOMATION_V13.md` y Skill canónica en `dev` del mismo repositorio.

## Objetivo verificable

Fijar qué modificaciones de SOLQARYN requieren autorización expresa, cuáles son reversibles y cuáles **nunca** deben ejecutarse automáticamente a partir de una fila habilitada. Entregar un contrato documental de decisiones, riesgos y gates sin confundir análisis preliminar con implementación completa, aprobación de cambios productivos o certificación del bloque C00.

Esta entrega es una sola microtarea de las 2.340 V5. El estado mutable se conserva exclusivamente en `PLAN_MAESTRO!H`; `COLA!E` es espejo por fórmula, `COLA!G/O` son puertas, y `DAG_TECNICO` habilita únicamente los IDs revisados como `VALIDADO` cuando sus prerrequisitos reales están LISTO. Un objetivo de reserva no crea permiso de ejecución.

## Clasificación de cambios y autoridad

| Clase | Ejemplos | Autorización y evidencia | Reversibilidad |
| --- | --- | --- | --- |
| Documental sin runtime (TIPO 1) | Inventario, contratos, matrices, aclaraciones | TASK_ID autorizado, fuente V5, diff, integridad, readback, Scope Lock exact-SHA cuando hay commit, A0–A4 proporcionales y A5 independiente posterior | Git revert ordinario del commit o nota aditiva en Sheet; preservar historial |
| Código .NET / API (TIPO 2) | Servicio, controlador, DTO, seguridad de excepción | Auditorías A0–A3 dirigidas y CI .NET real del SHA publicado; A4 readback; controles JWT/RBAC/tenant donde corresponda | Reversión trazable y pruebas de compatibilidad; no tocar datos productivos |
| UI Angular / Node (TIPO 3) | Rutas, formularios, navegación | npm lint/build/tests; Playwright/accesibilidad si afecta interacción; Scope Lock, seguridad y contrato HTTP proporcionales | Revertir commit y comprobar navegación |
| Persistencia o infraestructura (TIPO 4/5) | EF/MySQL, Docker, workers, secrets, dependencias, proveedores | Pruebas de integración reales y controles afectados; prohibido tomar `SKIPPED` por PASS, migrar PROD o usar credenciales no autorizadas | Requiere plan probado, backups y rollback; operaciones irreversibles NO autorizadas |
| Promoción de entorno | `qa`, `main`, producción, dominio, DNS, TLS, proveedor de archivos | A5_PASS independiente **y** autorización adicional expresa del propietario con controles de despliegue vigentes | No se ejecuta desde esta microtarea, aunque la CI de `dev` sea verde |

## No-alcance y prohibiciones

- No crear ni modificar microtareas fuera de V5, cambiar responsables ajenos, tomar una fila ocupada ni deducir permiso de una meta de capacidad.
- No editar `COLA!A:Q` manualmente, reinterpretar `VALIDANDO` como `PASS`, aprobar A5 propia ni usar evidencia de otro SHA para certificar el candidato.
- No introducir migraciones, endpoints, nuevos frameworks, tablas, flags de producción, nuevos secretos o cambios de autenticación desde este documento.
- No crear PR, ramas, rulesets, Tasks ni reactivar mecanismos VAEP/Jules retirados; trabajo comercial sólo en `dev`, sin force-push.
- No aceptar ni saltar aprobaciones humanas del proveedor; Javier no administra las Tasks de Alex y una Task no suple credenciales de otra cuenta.
- No activar VPS, Cloudflare, R2, Hostinger, DNS, dominio o migración real: la arquitectura objetivo del plan no constituye autorización de gasto ni despliegue.

## Registro inicial de decisiones, riesgos y gates

| ID | Decisión / riesgo | Control verificable | Estado de esta entrega |
| --- | --- | --- | --- |
| D01 | Plan único y trazabilidad | ID V5 único, `PLAN_MAESTRO!H`, `COLA!E`, `BITACORA!A:Z` (26 campos) | Contrato definido; no acredita fase completa |
| D02 | Separación DEV / QA / PROD | Commit directo fast-forward sólo `dev`; A5 de Task distinta antes de promover y nueva orden del propietario | Prohibido desplegar |
| R01 | Suplantación o doble claim | Releer propietario, TASK_ID, DAG, G/O, estado y HEAD; abortar al encontrar colisión | Fail-closed |
| R02 | Falso verde de CI | Vincular checks obligatorios a `head_sha` exacto, `completed/success`; `SKIPPED` o antiguo no satisface prueba requerida | Fail-closed |
| R03 | Truncamiento de auditoría | BITACORA A:Z, `EVENT_ID` A y `IDEMPOTENCY_KEY` R distintos; 26 valores por evento y readback | 21–25 campos no son completos |
| R04 | Cruce de tenant o filtración | Backend JWT/RBAC/EmpresaId, secretos/PII y negativas proporcionadas; UI no sustituye autorización | Sin modificación de seguridad |
| R05 | Operación irreversible | Backups, test de restauración, permiso explícito y trazabilidad antes de tocar datos/servicios reales | No ejecutable aquí |
| R06 | Dependencia documental prematura | DAG sólo libera preparación M01.01–05, revalidar al cerrar C00 y antes de M01.06/M02 | No libera implementación |

## Criterios de aceptación y pruebas documentales reproducibles

**Caso positivo:** una corrección documental a `docs/v5/`, con autor autenticado, diferencia acotada, Scope Lock SUCCESS para SHA real cuando aplique, eventos A:Z completos, PLAN/COLA coherentes y sin cambio de runtime, puede alcanzar `LISTO_DEV_A0_A4_PASS;A5_PENDIENTE`. La revisión A5 de otro TASK_ID no otorga autorización de QA/PROD.

**Caso negativo A — permisos:** si el cambio implica PROD, pagos, dominios, secretos o modificar `qa/main` sin autorización adicional, el resultado es `NO_GO`, aunque la cola indique `SI` o la CI de `dev` sea verde.

**Caso negativo B — concurrencia:** si el ID tiene responsable distinto, una Task nueva no lo reclama ni reemplaza el autor. Si aparece un evento con `EVENT_ID` o `IDEMPOTENCY_KEY` ya usado, no se inserta duplicado.

**Caso negativo C — CI:** si un job realmente obligatorio está `pending`, `failed`, `skipped` o corresponde a SHA anterior, el control permanece pendiente; un job irrelevante se identifica `NO_APLICA` con justificación y no bloquea un documento simple.

**Caso negativo D — integridad:** un evento de BITACORA con 21, 22, 23, 24 o 25 campos no cumple el contrato físico A:Z y no puede habilitar `LISTO`.

**Pruebas aplicables a esta microtarea:** A0 inspección Markdown, diff, enlaces y secretos; A1 objetivos/no-alcance, tabla de decisiones y cuatro negativas; A2 separación de cuentas, auth y restricciones QA/PROD; A3 concordancia con V5, V13, DAG y esquema real; A4 diff y checks del SHA publicado, readback PLAN/COLA/BITACORA y reversión. Compilación .NET/Angular, MySQL, Docker, E2E y pruebas de proveedor vivo: **NO_APLICA** porque no se modifica código ejecutable ni infraestructura.

## Dependencias, seguimiento y rollback

La excepción documental `DAG_TECNICO` revisada para C00-04.M01.01 exige `C00-01.M02.01–06` y `C00-03.M01.01–05` terminadas: once prerequisitos; fueron comprobados LISTO antes del claim y se deben revalidar si cambia la fuente. No inferir cierre total de C00: revalidar frente a C00-01 terminal antes de nuevas fases y de M01.06.

Registro institucional: conservar `PLAN_MAESTRO!H/I`, evidencias en J:L y cada evento en `BITACORA!A:Z` con claves únicas y readback. A5 independiente queda `PENDIENTE` al cerrar DEV, sin borrar fallos A5 históricos.

**Rollback:** `git revert` normal del commit documental cuando corresponda, sin reset ni force-push; cualquier nota institucional correctiva es aditiva y auditada, no elimina eventos.
