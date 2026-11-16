# SOLQARYN V17/V15 — C11-04.M03.05 revisión acotada aislada de observabilidad

**ID:** C11-04.M03.05, PLAN fila 1710, autor PRIMARY DEV01 Javier TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Rama dev exclusiva, PROJECT_SCOPE_LOCK=STRICT. Candado V17 SQV17_C11_04_M03_05 / 2000001710. **Riesgo R2:** multi-tenant, telemetría sensible, JWT/RBAC, concurrencia y fuente de métricas.

## A0 — código y dependencias reales

Implementa Infrastructure .NET 10 C1104ObservabilityBoundedReadWorker, con interfaz IC1104BoundedObservabilityPreviewPort y adaptador explícito C1104ObservabilitySnapshotPreviewPort sobre C1104ObservabilityIsolatedSnapshotAdapter (M03.04), que compone gate Application M03.03, Service M03.02 y Domain M03.01. Se valida lote completo antes de cualquier lectura (máximo 16), señales permitidas, tenant sintético confirmado, SHA exacto, correlación segura, unicidad ordinal y prohibición de exportar/publicar. Procesamiento secuencial. Sólo se admite la respuesta EXTERNAL/ADAPTER REQUIRED, sin afirmar medición real. La salida no expone datos ni autoriza tenant, exportador, acceso, JWT, backups o despliegues. Sin HostedService, registro DI, endpoint, proveedor, API externa, métricas reales, SLO o costo medido.

## A1 y A2 — pruebas ejecutables requeridas

13 pruebas xUnit nuevas positivas/negativas: lote de dos entradas sin permisos, nulo/vacío/sobrecarga, tenant A/B, intento de exportar, tenant inseguro, SHA mezclado/stale/no canónico, señal y correlación inválida, duplicado ordinal, diferencia de caso, entrada maliciosa al final sin lectura parcial, rechazo de fuente, excepción opaca, cancelación y dependencia nula. Las pruebas pueden fallar cuando se relajan gates. A2 además requiere GitHub-hosted Seguridad multiempresa y Authorization por SHA causal con cero fallos.

## A3 y A4 — pruebas y límites

A3: integración real in-process con tipos y adaptador de snapshot existente; NO_APLICA MySQL 8.4 Category=Integration, EF, JWT HTTP real, Playwright, productores de métrica, Prometheus, dashboards live y proveedores: este diff no los toca. Los SLO, cardinalidad real, retención y costos permanecen NO_MEDIDO. A4: un commit fast-forward de código/test/doc/CHANGELOG contiguo; pasar EN_PROGRESO a VALIDANDO con run exacto y owner, jamás PASS antes de CI completa. LISTO únicamente con A0–A4 y Backend Release, build backend, multiempresa, Authorization, Scope Lock y V5 SUCCESS real; readback PLAN/COLA/DAG/BITACORA. A5 de otra Task Javier distinta del autor. Rollback git revert sin force ni pérdida de bitácora. Prohibidos QA/main/PROD.
