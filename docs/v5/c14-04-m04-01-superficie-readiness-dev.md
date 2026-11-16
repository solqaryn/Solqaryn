# SOLQARYN V17 — C14-04.M04.01 · Superficie privada de readiness DEV

**Autor:** PRIMARY DEV 03 JAVIER; TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Repositorio:** solqaryn/Solqaryn, rama dev. PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO fila 2144; candado SQV17_C14_04_M04_01 / 2000002144.

## A0 · objetivo, diff y reversión
La microtarea exige una ruta, pantalla o superficie de consulta para el bloque de go-live y monitoreo. Se amplía la superficie privada **ya existente** /plataforma/flujo-v5 (plataformaGuard). No se crea ruta pública nueva, privilegio, controlador, API, dato, job, infraestructura, DNS, dominio, R2, migración ni despliegue. La pantalla describe cuatro puertas: smoke y aislamiento, soporte, observación/alertas, backup/restauración. Toda evidencia se muestra **SIN_VERIFICAR/NO_MEDIDO**; jamás se calcula PASS desde un texto o sesión cliente. Se aprovecha el guard y la sesión servidor ya existentes sin modificar sus límites. Reversión: git revert del commit en dev, sin force-push; BITACORA histórica inmutable.

## A1 · pruebas causales
Playwright/Chromium dirigido: redirección anónima; 403 con payload privado sin filtración; 503 fail-closed; sesión de plataforma 200 sintética con cuatro comprobaciones no certificadas, cero formularios/controles de escritura y viewport 320/1280. Los asserts son negativos y positivos capaces de fallar al remover el guard, mostrar la pantalla sin sesión, inventar PASS o exponer mensajes. No se declara PASS hasta logs CI real del SHA.

## A2 · fronteras de seguridad
La presencia del panel exige sesión servidor en el guard y lectura de signal de sesión, no rol local de empresa. Las respuestas 403/503 se bloquean antes de renderizar la superficie. Ninguna certificación de JWT HTTP real, MFA, tenant A/B de backend, DNS, backup/restore, R2, SLO o seguridad de producción se infiere de mocks.

## A3/A4/A5 · evidencia y gates
A3 pertinente: Playwright real Chromium, TypeScript/Angular build; MySQL 8.4/EF, proveedores o HTTP externos NO_APLICA por no modificarse ni invocarse en este diff. A4 exige Scope Lock, procedencia CHANGELOG V5, pruebas requeridas SUCCESS exact SHA o equivalencia por blobs, readback PLAN/COLA/BITACORA. No usar CANCELLED/SKIPPED/QUEUED como PASS. A5 requiere auditoría independiente de otra Task Javier, nunca autoaprobación. QA/main/PROD prohibidos.

## Reparación causal tras CI Chromium
El job `43909251783` (SHA original `0fa9ca35`) encontró dos pruebas preexistentes de `/plataforma/flujo-v5` que seleccionaban estrictamente `.v5-card` y `.v5-card h2`. El panel aditivo usaba por error esa clase y rompió los conteos/selectores sin afectar autorización. Se aísla la clase a `release-readiness-card` manteniendo `mat-card`, aria-labelledby, cuatro puertas, sesión/guard y estilos Material; no se debilitan los tests antiguos. Repetir Chromium, frontend y Scope Lock en SHA de reparación. No marcar SUCCESS sin nueva CI.

## Corrección de cobertura A3 en GitHub Actions
El workflow existente de Chromium V5 usaba una lista explícita y no incluía el nuevo archivo `frontend/e2e/c14-04-m04-01-readiness-surface.spec.ts`. Su green original no acreditaba los cuatro tests de esta microtarea. Se añade la ruta exacta al filtro `on.push.paths` y a `npx playwright test`, sin quitar pruebas anteriores ni alterar `cancel-in-progress=false` SHA-aislado. La ejecución real del nuevo SHA es requisito causal de A1/A3; no certificar sin log que mencione cuatro pruebas y SUCCESS.
