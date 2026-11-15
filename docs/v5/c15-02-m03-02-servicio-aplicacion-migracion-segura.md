# SOLQARYN V5 FINAL — C15-02.M03.02: servicio de aplicación de preflight para migración

**Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** implementación de código real .NET 10 en Application + pruebas xUnit.

## Resultado y frontera

- `V5ArchitectureMigrationApplicationService.Prepare` recibe únicamente un contexto **supuestamente ya verificado por el servidor** y una lista acotada (máximo 64) de candidatos. No confía directamente en tenant, roles o proveedor enviados por cliente.
- Reutiliza sin duplicarla `V5ArchitectureMigrationDomainRules.Evaluate`, definida en M03.01, y detiene el lote en el primer rechazo. Registra cuántos candidatos se inspeccionaron, sin escribir datos ni generar autorizaciones.
- Rechaza contexto nulo, lotes vacíos o sobredimensionados, tenants cruzados, SHA no coincidente, QA/PROD, proveedores fuera de catálogo, cambios DNS/secretos, SQL destructivo, hallazgos críticos y falta de checksum, restore aislado o plan de rollback.
- El veredicto estructural no bloqueante es `INDEPENDENT_RUNTIME_EVIDENCE_AND_A5_REQUIRED`; los cuatro indicadores de autorización/certificación del resultado son **siempre false**.
- Esta unidad **no** integra HTTP, EF/MySQL, Cloudflare, R2, Hostinger, autenticación real, firmas, backups ni restores. Los datos de entrada no son prueba de que dichos controles se ejecutaron. No hay eventos/webhooks, migraciones, despliegue ni secreto.
- Los únicos proveedores del fixture de prueba son Render, Vercel, Aiven y Cloudinary como *catálogo sintético* de entorno dev, sin aprovisionamiento real.

## Verificación causal A0–A5

- **A0:** archivos de Application, xUnit y documento añadidos; no alteran controladores, infraestructura, JWT/RBAC ni migraciones.
- **A1:** casos xUnit de flujo estructural positivo y negativos de contexto, SHA, capacidad, rollback, proveedores, crítica y tenant A/B. Las suites deben terminar en GitHub Actions con SHA exacto.
- **A2:** todas las decisiones fallan por defecto; un resultado de preflight jamás concede privilegios. Son pruebas unitarias de reglas, **no** certificación de autorización HTTP o aislamiento EF/MySQL real.
- **A3:** compilar Application en .NET 10, ejecutar xUnit y verificación de seguridad multiempresa en GitHub-hosted CI. No sustituir PASS por queued/cancelled/skipped.
- **A4:** publicar sólo mediante fast-forward con verificación de HEAD y comprobar Scope Lock y procedencia CHANGELOG exact-SHA, más CI dirigida y readback PLAN/COLA/DAG/BITACORA antes de LISTO_DEV.
- **A5:** requiere revisión auténtica de otra de las cinco Tasks Javier diferente del autor; no se aprueba automáticamente.

## Seguridad, consumidores y rollback

No se crea endpoint consumidor todavía. Un futuro adaptador de Application/API deberá resolver autoridad mediante JWT/membresía real, tenant A/B y catálogo del servidor, validar procedencia/SHAs y probar MySQL, DNS y respaldo aislado antes de cualquier operación. La reversión sería un nuevo commit `git revert` en dev, sin force-push, seguido de CI sobre el nuevo SHA. Este artefacto no autoriza QA, main ni PROD.
