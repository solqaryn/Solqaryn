# SOLQARYN V17 — C11-04.M05.02 contrato tipado Application y errores seguros probados

**Autor:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). **Repositorio:** `solqaryn/Solqaryn@dev`. **Gobierno:** PROJECT_SCOPE_LOCK=STRICT. **PLAN:** fila 1719, candado V17 `SQV17_C11_04_M05_02` ID 2000001719, COLA G/O=SI, DAG F=VALIDADO/G=SI, reclamo exclusivo previo. **Riesgo:** R2 por prueba de autoridad, multitenancy, información sensible y errores.

## A0 — contrato y alcance

El documento anterior `c11-04-m02-02-contratos-api-dto-errores-validaciones-observabilidad.md` define futuros OP01–OP08, DTO01–DTO08, ERR01–ERR12, aún NO IMPLEMENTADOS como endpoints. Existe código .NET 10 real `C1104ObservabilityApplicationService` con `Request`, `VerifiedEvidence` y `Result`, pero es un **preflight in-process** cuyo éxito sólo exige un adaptador autenticado futuro; no permite acceso ni devuelve datos privados. Esta microtarea añade únicamente xUnit ejecutables del contrato tipado y de sus códigos de error reales, sin agregar controller, proveedor, DB o cambiar lógica productiva. Se ejecuta sobre `Category!=Integration` en GitHub-hosted backend Release. No confundir estos códigos lógicos con 401/403 HTTP reales ni con DTO futuros.

## A1–A3 — asserts para casos que pueden fallar

- Positivo: solicitud acotada con evidencias válidas produce únicamente `TRUSTED_PRIVATE_OBSERVABILITY_ADAPTER_REQUIRED`, jamás acceso, datos, exporter ni SLO PASS.
- Negativos: señal ausente/no permitida, correlación vacía o CRLF, cadena de 65 caracteres y SHA corto se rechazan **antes** de leer evidencia.
- Seguridad: tenant A frente a B, pertenencia revocada, permiso faltante y SHA de fuente diferente producen códigos opacos y ningún permiso.
- Privacidad: etiqueta `user_email` con valor sensible retorna `LABEL_NOT_ALLOWLISTED`, sin filtrar el valor.
- Frontera: denominador cero retorna `NO_MEDIDO`, nunca certificación SLO; cancelación evita cualquier lectura.
- A2 tiene asserts negativos y positivos de autoridad **sintética in-process**, no prueba de JWT HTTP real o aislamiento físico MySQL. A3 prueba .NET real con xUnit en GitHub-hosted. API HTTP/DTO compilados nuevos, MySQL 8.4 EF, Playwright, provider externo = NO_APLICA porque este diff sólo contiene tests y documentos.

## A4 y A5 — evidencia causal requerida

Requeridos GitHub Actions backend Release/xUnit REAL, seguridad y autorización pertinentes, Scope Lock, V5/procedencia en SHA exacto, P0/P1 en cero para este alcance, y readback PLAN/COLA/DAG/BITACORA. `IN_PROGRESS/QUEUED/FAILED/CANCELLED/SKIPPED` NO PASS. No se inventan run ni resultados hasta ejecución externa. A5 revisor Javier distinto del autor DEV05. Rollback `git revert` normal sin force-push, solo dev, QA/main/PROD fuera de alcance.
