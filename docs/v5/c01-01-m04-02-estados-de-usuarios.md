# SOLQARYN V5 — C01-01.M04.02 estados de usuarios

**Alcance:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 05, TASK_ID 6adc2e55f47c81919f6374e561687076. **Fuente:** Standard V5 FINAL y PLAN_MAESTRO fila 165, DAG VALIDADO/SI.

## Entrega ejecutable
Nuevo componente Angular 22.2.1 standalone OnPush conectado a los signals ya existentes de /usuarios: loading, errorMessage, usuarios y puedeCrear. Muestra carga, vacío, error, éxito por cantidad visible de la página y estado de solo lectura sin permiso para crear. Un error de carga nunca se confunde con ausencia de datos, y el componente no ejecuta escrituras.

## Impacto y seguridad
No se modifican Domain, Application, API, datos, SQL, rutas, JWT, RBAC, tenant A/B, guards ni secretos. Permanece la autoridad de backend y el contexto verificado; no se toma identidad desde URL, localStorage ni cabeceras. Sin HTML sin sanitizar. No cambia contratos ni migraciones.

## Auditoría A0–A5
- A0: inspección de diff UI+spec+workflow+este documento+CHANGELOG, alcance seguro y rollback.
- A1: seis pruebas Angular/Vitest positivas y negativas para estados, prioridad y revocación.
- A2: superficie de consulta sin autorización nueva; modo solo lectura solo informativo, backend conserva JWT/RBAC/tenant.
- A3: integración con Signals ya existentes; no nuevas API ni endpoints.
- A4: exige CI real GitHub-hosted exact-SHA (Vitest, lint, build, Scope Lock y V5), readback Git/Sheet; queued/cancelled/skipped no son PASS.
- A5: requiere otra Task Javier distinta de la autora, sin QA/main/PROD.

Rollback normal: git revert del commit de esta unidad en dev, sin force-push.
