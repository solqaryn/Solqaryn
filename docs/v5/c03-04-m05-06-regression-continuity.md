# C03-04.M05.06 — regresión y coste acotado de continuidad de incidentes

Estado de entrega: EN_PROGRESO. Rama: `dev`. Ejecutora solicitada: DEV03 Javier, por `Automations.run_now` inline; no inventar RUN_ID nativo. `PROJECT_SCOPE_LOCK=STRICT`. Autor histórico de esta microtarea: DEV03 tras claim V22 verificado en PLAN_MAESTRO!H/I571 y BITACORA.

## A0 — alcance y dependencias

La salida atómica exige ejecutar regresión, rendimiento y accesibilidad proporcionales al alcance de incidentes/continuidad. El changeset crea pruebas y workflow de CI; NO modifica API, Domain, Application, EF/MySQL productivo, workers, permisos, IAM, JWT, Angular, UI ni deployment. Reutiliza el contrato Domain `V5ContinuityIncidentDomainRules` y la integración real y efímera MySQL 8.4 de `C03-04.M05.03` sin tomar permisos de restauración.

Impacto: regresión de transición Open -> Contained -> Resolved en un contrato *de preflight*, idempotencia/versionado, revocación, tenant A/B, sucursal, sandbox-restore negado y concurrencia pura. Las pruebas no ejecutan contención ni restauran backups reales; tampoco autentican JWT firmado/HTTP de incidentes.

## A1 y A2 — oráculos

- Variaciones tenant 11/22: cada lectura legítima sigue exigiendo un enforcer confiable; actor de empresa distinta se niega siempre, incluso en 512 iteraciones.
- 512 iteraciones de contención/resolución y conflictos de versión/replay; nunca se autoriza mutación ni backup.
- Revocación, evidencia sin sanitizar y sucursal ajena: siempre denegados.
- 256 evaluaciones concurrentes de tenants cruzados: sin fuga de autoridad entre invocaciones ni permisos compartidos.
- Intento explícito de restore aunque se declare un flag confiable: siempre `RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER`.
- 20.000 invocaciones de regla de dominio con guardia de humo de 15 segundos; no se atribuye un SLO productivo ni una optimización demostrada.

## A3 — integración y proporcionalidad

Workflow GitHub-hosted .NET 10 con MySQL 8.4 real **en contenedor efímero**, el test `C0304M0503IncidentMySqlPersistenceIntegrationTests` y el nuevo `C0304M0506IncidentContinuityRegressionTests`. Comprobar TRX con al menos 10 casos ejecutados/aprobados, cero fallos/omitidos y evidencia descargable. El test MySQL acredita únicamente el alcance de fixture aislada, lectura tenant A/B, revocación y rollback sin efectos financieros ni datos productivos.

**Accesibilidad NO_APLICA al changeset**: no se altera pantalla, DOM, HTML/CSS, navegación ni UI. No convertir esta exención en certificación de accesibilidad global o de interfaz de incidentes; si se cambia UI en otra microtarea, exigir Chromium, teclado, responsive y WCAG según su alcance.

## A4, A5 y límites

Hasta ejecutar el workflow en el SHA publicado, Scope Lock, self-test de skill, CI y pruebas dirigidas son **PENDIENTES**, nunca PASS por presunción. Al terminar verificar todos los checks obligatorios relevantes al SHA, readback PLAN/COLA/DAG/BITACORA y evidencia causal. Transición EN_PROGRESO -> VALIDANDO -> LISTO solo cuando A0–A4 aplicables estén acreditadas, con V17/CAS y propietario intacto. A5 requiere una Task Javier distinta de DEV03 y revisión real; ningún autorevisor. Un job SKIPPED/FAILURE/CANCELLED/IN_PROGRESS requerido no es PASS.

No se autoriza restore real, host remoto, secretos, QA, main, PROD ni migración. Rollback mediante `git revert` ordinario del changeset después de revalidar HEAD, sin force-push.
