# SOLQARYN V17 — C05-04.M03.05: cola acotada de revisión aislada de ligereza UI

**Autor:** Javier PRIMARY DEV05, TASK_ID `6adc2e55f47c81919f6374e561687076`; **rama** `solqaryn/Solqaryn@dev`; **seguridad** PROJECT_SCOPE_LOCK=STRICT. **Fuentes**: STANDARD V5 FINAL, PLAN fila 846, COLA G/O SI, DAG_TECNICO fila 807 F=VALIDADO G=SI, predecesores C05-04.M02.01-06 y M03.01-04 con A5 causal. **Exclusividad** V17 named range SQV17_C05_04_M03_05 / 2000000846.

## A0 — implementación real, arquitectura y no-alcance

Añade Infrastructure .NET 10 `V5UiLightnessBoundedReviewQueue`, cola en memoria de un único proceso, **ejecutada solamente cuando un llamador de prueba invoca `ProcessNextAsync`**; reutiliza el adaptador M03.04 y los controles Application/Domain previos. No registra HostedService, DI, endpoint, webhooks, interfaz Angular, caché real, proveedor duradero ni Worker en producción. El objeto almacena temporalmente sólo identidad de prueba y referencia a contexto autenticado sintético; no se considera almacén de identidades persistente ni autoriza a exponer datos. Ningún `Outcome` concede acceso, realiza I/O de BD o certifica Core Web Vitals.

- Cola acotada 1..32; registro idempotente de memoria con máximo 256 keys y fail-closed al agotarse, sin evicción silenciosa ni falso PASS por replay.
- Serialización del consumidor con SemaphoreSlim, protección de productor y contador con lock. `Enqueue` no ejecuta trabajos; `ProcessNextAsync` procesa a lo sumo uno.
- Scope del actor backend verificado por `ContextoTenantActual.ExigirEmpresa`, empresa y SHA de 40 hex; resource/key de caracteres seguros. Nunca se confía en EmpresaId elegido por navegador.
- Misma clave con mismo scope: `REPLAY_REQUIRES_DURABLE_IDEMPOTENCY`, jamás segundo efecto; misma key con recurso/actor distinto: `IDEMPOTENCY_KEY_CONFLICT_DENIED`.
- Cancela antes de retirar; si `EvaluateReadOnlyAsync` lanza error, deja la entrada para diagnóstico/reintento explícito, sin ACK. Aun el caso nominal retorna `EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED`, no PASS de performance.
- El registro es **temporal, no duradero ni transaccional**; una futura integración necesitará broker/outbox, persistencia de idempotencia, seguridad real y monitoreo bajo microtarea separada.

## A1/A2 — 12 pruebas xUnit ejecutables por GitHub Actions

`V5UiLightnessBoundedReviewQueueTests` tiene 12 casos [Fact] para: revisión nominal sin grants ni medición, replay pendiente, replay completado, conflicto de la misma clave distinto recurso, tenant A/B y actor extranjero, límite de cola, agotamiento de registro acotado, SHA obsoleto/IDs inseguros, cancelación que preserva entrada, snapshot con permiso revocado, cola vacía sin PASS y validación de constructor/capacidad. Un cambio que rompa los rechazos debe hacer fallar las aserciones. `ContextoTenantActual` se crea únicamente en tests desde identidad ficticia verificada. No son pruebas de JWT HTTP productivo ni de proveedor distribuido. El adaptador previo retorna la decisión real de Application/Domain.

## A3/A4/A5, rollback

**A3:** exigir CI del SHA causal con build Release y xUnit .NET10, seguridad multiempresa, Authorization y Scope Lock y procedencia V5 aplicables. MySQL 8.4 EF, HTTP, Angular/Chromium y CWV reales **NO_APLICA al diff sin cambios EF/UI/API**; siguen requeridos cuando un scope los modifique. **A4:** commit fast-forward serializado, diff únicamente Infrastructure/test/doc/CHANGELOG, check-runs `completed/success` en SHA causal; si cualquier requerido queued/in_progress/failed/cancelled, conservar VALIDANDO con RUN/SHA/owner y no acreditar LISTO. BITACORA append idempotente, PLAN y DAG readback. **A5:** revisión independiente por otra Task Javier distinta. Rollback exclusivamente `git revert` normal, sin rewrite ni eliminación BITACORA, QA/main/PROD ni servicios de pago.

**Riesgo R2:** tenant, cache, idempotencia y concurrencia. No se activa código ejecutable de producción ni se altera DI existente.
