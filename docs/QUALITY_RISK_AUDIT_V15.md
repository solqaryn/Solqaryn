# SOLQARYN — Política de calidad adaptativa V15

PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
QUALITY_RISK_AUDIT=V15_RISK_ADAPTIVE_A0_A5
SINGLE_ACTIVE_DEV=1

Objetivo: evitar retrabajo de las 2340 microtareas con pruebas honestas; no existe garantía absoluta de perfección. Conservar las seis Tasks Javier e idéntico horario. No añadir cinco auditorías universales ni declarar auditoría retroactiva de unidades anteriores.

## Clasificación obligatoria por impacto

R0: documentación o configuración declarativa sin efecto ejecutable. Verificar criterio de aceptación, fuente, DAG y procedencia; compilar o probar MySQL ajeno se declara NO_APLICA motivado.
R1: código funcional local; pruebas positivas/negativas, fronteras, errores/cancelación e impacto sobre consumidores.
R2: JWT/RBAC, multitenancy, pagos, inventario, PII, migración EF/MySQL, CI, integraciones, exportaciones, concurrencia o autorizaciones. Exigir casos adversariales pertinentes: tenant A/B, IDOR/BOLA, rol, revocación, replay/idempotencia, CAS, transacción/rollback. MySQL 8.4 aislado realmente ejecutado si persistencia cambia; Playwright/Chromium si UI cambia. Los mocks no prueban integración de DB o HTTP.
R3: producción, transacción monetaria real, secretos o eliminación irreversible: NO_GO sin permiso específico. Riesgo incierto => R2.

## Evidencia de A0 a A5

A0: requisitos y criterios de aceptación vinculados al ID, diff, arquitectura, dependencias DAG y rollback.
A1: asserts capaces de detectar defectos: positivo, negativo, frontera y error; en R2 ampliar casos adversariales y mutación dirigida cuando corresponda.
A2: controles de seguridad, tenant/propiedad, JWT/RBAC, revocación cuando aplican; NO_APLICA solo con motivo concreto.
A3: integración aplicable de MySQL, API, frontend o proveedor en entorno autorizado; no sustituir por build ni mock.
A4: commit dev, SHA exacto o descendiente equivalente con blobs inalterados, checks obligatorios completed/success, Scope Lock/V5 y readback Sheets/Bitacora; SKIPPED/CANCELLED/IN_PROGRESS no equivalen a PASS.
A5: revisor distinto del autor entre las seis Tasks Javier, lectura independiente de requisitos, código, pruebas y defectos probables; permite A5_FAIL/PENDIENTE y reparación con test de regresión. No firmar por el relato del autor.

Solo una EN_PROGRESO por Task. Pasar la anterior a LISTO o a VALIDANDO con CI externa realmente pendiente antes de tomar otra independiente. No alterar escrituras concurrentes, no desbloquear sucesoras técnicas antes de LISTO y A5 real cuando DAG lo exija. Conservar REVIEW_FIRST.

## Prevención de fallos posteriores

Registrar en BITACORA QA_DEFECT_FOUND/A5_FAIL/QUALITY_ESCAPE_CONFIRMED/REOPENED_FOR_REPAIR con ID, gravedad, SHA, causa y prueba de regresión, sin borrar evidencias previas. No cerrar P0/P1 sin corrección. Medir A5_REJECTION_RATE, POST_LISTO_DEFECT_RATE, REOPEN_RATE y porcentaje de R2 con negativos pertinentes, siempre con numerador, denominador, periodo y fuente, o NO_MEDIDO. Muestrear R2 representativas en cierres futuros; ampliar ante defectos sistémicos, no hacer auditoría vacía de las 2340.

## Precedencia y rollback

V13/1000 era capacidad advisory, nunca permiso para 1000 escrituras. V15 impone MAX_ACTIVE_WRITES_PER_TASK=1. El nombre histórico scripts/automation-capacity-v13.mjs se conserva por compatibilidad, aunque su política es V15. Respaldos privados Drive: Sheet entero, plan y seis prompts exactos NO OPERATIVOS. Para restaurar, volver a prompts por Task ID, celdas CONFIG precisas y Git mediante commit revert después de verificar HEAD; jamás force-push, eliminar BITACORA, reactivar Alex o tocar QA/main/PROD.
