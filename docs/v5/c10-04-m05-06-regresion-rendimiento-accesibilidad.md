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

## Corrección adicional del parser de unidades (2026-11-18)
- El primer SHA de reparación `80c3337e` continuó 6/7 por una expresión regular sobreescapada: `\\d` en literal RegExp TypeScript identifica una barra literal y no los dígitos de `1e-05s`. El fallo reproducible fue el assert booleano de duración en job 44339659378/134925819595.
- El nuevo parche reemplaza únicamente el patrón por `\d` y `\.` correctos dentro del literal RegExp, sin modificar umbral 0,1 ms, CSS ni casos funcionales. Debe ejecutarse Chromium real y verificarse CI sobre el nuevo SHA antes de PASS; M05.06 permanece VALIDANDO y la integración real worker/restore sigue pendiente.

## V19 — ensayo MySQL 8.4 efímero adicional (DEV03)

El workflow dirigido agrega un segundo job independiente que ejecuta un script de comprobación real contra MySQL 8.4 efímero de GitHub Actions. Usa dos tenants totalmente sintéticos, sesiones MySQL consecutivas para simular pérdida de conexión tras persistir un claim, reanudación condicional con versión, mysqldump, eliminación de la base y restauración real de esquema y filas. Verifica integridad mediante digest ordenado, aislamiento de filas tenant A/B, rechazo preventivo de dump adulterado por checksum y rollback transaccional.

Este incremento solo será PASS después de GitHub Actions completed/success del SHA propio; al publicarse las pruebas quedan pendientes. No demuestra caída de un worker desplegado, dos consumidores concurrentes, migraciones EF de negocio, autenticación JWT HTTP, MySQL Aiven, cifrado/custodia de backups, RPO/RTO, Cloudflare R2 ni recuperación de producción. Los datos son inventados y se destruyen al terminar.

A0: workflow, script, documentación y CHANGELOG; ningún cambio de código productivo o de base real. A1/A2: asserts de filas A/B, integridad, persistencia y rollback. A3: proveedor MySQL 8.4 real pero efímero. A4: Scope Lock, V5 y ambos jobs dirigidos exact-SHA con readback institucional. A5: revisor Javier distinto de DEV03. Sigue VALIDANDO sin liberar M06, R3, QA/main/PROD. Rollback: git revert después de verificar HEAD.
