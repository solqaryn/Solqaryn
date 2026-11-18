# C10-04.M05.06 — Regresión, rendimiento y accesibilidad proporcionales (V19)

**Estado de entrega: incremento auténtico R2 de pruebas; no cierre integral M05.**

## Alcance A0
- Una suite Playwright real para Chromium sobre la ruta privada `/plataforma/continuidad-operativa-v5`, un workflow GitHub-hosted aislado con `cancel-in-progress: false`, este documento y CHANGELOG.
- Sin cambios al frontend productivo, .NET, MySQL, EF, DTO, JWT, infraestructura, workers, proveedores, datos ni entornos.
- Previas M04/M05 se validan contra PLAN/COLA/DAG y A5 independientes; este test NO hereda permisos productivos.
- Rollback: `git revert` del commit fast-forward sin reset ni force-push.

## Pruebas A1–A3
Se definen siete escenarios en el runner Chromium:
1–3. Vista a 320/768/1440, estructura semántica, cuatro controles, no desbordamiento, límite de nodos DOM y ausencia de mutaciones.
4. Teclado real Tab/Enter con foco visible, resultado seguro y solicitud read-only.
5. HTTP 403 en revalidación, ocultamiento, sin texto interno del proveedor.
6. HTTP 500 no autoriza acceso ni filtra información de proveedor.
7. Preferencia reduced-motion sin transiciones y navegación accesible.

**Corrección de conteo**: son siete pruebas individuales (tres anchos más cuatro escenarios), no ocho; el workflow reporta el resultado real de Playwright, no un PASS predeterminado. A1 positivos/negativos/fallo, A2 UI denegación sintética y ausencia de mutaciones, A3 Chromium real Angular en loopback. Los estados de sesión y API son **sintéticos**, no JWT real firmado ni tenant A/B por backend.

## Límites imprescindibles
El propósito final de M05 (regresión, rendimiento y accesibilidad proporcionales a aislamiento, reintentos, límites y restauración) todavía requiere evidencia auténtica del worker/backup/restore y la integración de negocio si ese alcance se pretende certificar. Este incremento de navegador NO certifica recuperación real, tiempos RPO/RTO, transacciones de backup, Cloudflare R2, Aiven, aislamiento JWT/HTTP o producción. M05.06 debe permanecer VALIDANDO hasta todos los requisitos causales A0–A4 y A5 independiente por otra Task Javier. No inferir disponibilidad de producción ni cambiar DAG/cola manualmente.

## CI A4
Exigir `verify-solqaryn-project-scope`, V5, Angular lint/build y job Chromium del SHA exacto en `completed/success`; `queued/in_progress/skipped/cancelled/failed` no es PASS. Registrar run, job, SHA, exact blobs y readback Sheets/BITACORA. No declarar LISTO con CI externa pendiente ni con evidencias incompletas.

## Reparación causal de CI (2026-11-18)
- En SHA f3c46fb7de9326f926641f4b819179f106e3aa2c, el job 44337755037/134918974609 ejecutó 7 pruebas Chromium: 6 PASS y 1 FAIL por `transitionDuration` calculado `1e-05s` (0,01 ms) bajo prefers-reduced-motion, frente al assert textual `0s`.
- Se ajusta únicamente el oráculo E2E: analiza unidades s/ms, exige resultados numéricos finitos y duración <= 0,0001 s (0,1 ms), preservando denegación, teclado, layout y no-mutaciones. No modifica CSS ni runtime y no elimina escenarios.
- Requiere nuevo job dirigido Chromium y Scope Lock/V5 exact-SHA completados en SUCCESS antes de certificar siquiera este incremento de navegador. El alcance integral de workers/restauración y A5 independiente siguen sin acreditarse; conservar VALIDANDO hasta evidencia causal completa.
