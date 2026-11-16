# C14-04.M04.02 — Estados de readiness privada DEV

Autor: SOLQARYN PRIMARY DEV 05, TASK_ID 6adc2e55f47c81919f6374e561687076. Rama exclusiva dev; PROJECT_SCOPE_LOCK=STRICT. PLAN fila 2145; candado SQV17_C14_04_M04_02 / 2000002145. Riesgo R2 conservador por lectura de sesion y preparacion de release; NO se despliega a produccion.

## A0 — alcance, impacto y rollback
La ruta ya protegida /plataforma/flujo-v5 conserva el guard de plataforma, el contrato de cuatro puertas y su panel read-only. La ampliacion distingue carga, exito exclusivo de la revalidacion de sesion, vacio de evidencias operativas, error recuperable y falta de permisos. El backend sigue como unica autoridad: ninguna etiqueta visual concede JWT, RBAC, tenant, MFA, migracion, backup, DNS, R2 ni autorizacion de go-live. No se agrega API, DTO, control financiero, dato, secreto ni dependencia npm. El estado vacio nunca equivale a SUCCESS de smoke, SLO, restauracion o CI.

Archivo de implementacion: frontend/src/app/features/plataforma/plataforma.component.ts. Contrato Playwright: frontend/e2e/c14-04-m04-02-readiness-states.spec.ts. Workflow dirigido: .github/workflows/v5-plataforma-browser-guards.yml (agrega prueba sin retirar las anteriores). No se cambia interfaz de servidor ni persistencia. Rollback mediante git revert normal y sin reescribir BITACORA.

## A1 — evidencia funcional exigida
Cinco escenarios Chromium reales en GitHub Actions: sesion 200 con exito de consulta y evidencia vacia, revalidacion GET retenida que oculta el panel y bloquea doble accion, respuesta 503 que limpia permiso y permite reintento, respuesta 403 que revoca el panel y oculta payload sensible, y visita anonima redirigida antes de mostrar la superficie. Deben observarse los cinco nombres de tests y conclusion SUCCESS en SHA exacto. Los mocks de HTTP, token, tenant y rol son sinteticos.

## A2 — autorizacion y errores
La obtencion de sesion del servidor elimina la sesion anterior antes de GET y la restablece solo si el servidor confirma autoridad. Se clasifica 401/403 como sin_permiso y 503 u otra excepcion como error; la excepcion sincrona tambien libera el estado de carga y limpia sesion. Cualquier revalidacion in-flight hace inaccesible el panel hasta respuesta autorizada. No mostrar cadenas devueltas por servidor ni certificados ficticios.

## A3 — integracion pertinente
Integracion browser Playwright/Chromium real contra frontend local Angular con respuestas HTTP sinteticas, mas lint y build Angular 22.2.1 en GitHub-hosted. No hay conexion real a MySQL 8.4, proveedor externo, JWT de backend ni escrituras HTTP; esas pruebas no son aplicables a esta modificacion visual y no se atribuyen como PASS.

## A4/A5 — gates de cierre
A4 requiere Scope Lock, procedencia V5, pruebas dirigidas y checks pertinentes completed/success por SHA exacto o equivalencia demostrada en todos los blobs de alcance, mas readback de PLAN/COLA/BITACORA. Cualquier FAIL/CANCELLED/SKIPPED/IN_PROGRESS requerido mantiene VALIDANDO. A5 solo revisor Javier de Task fisica distinta, con lectura propia del diff, pruebas y defectos. LISTO_DEV no da permisos QA/main/PROD.


## Corrección regresión CI en vista cobertura compartida
La ejecución GitHub Actions V5 Chromium del SHA 05e875f4 probó 46/48 casos y rechazó dos casos antiguos de /plataforma/cobertura-v5: tras HTTP 403 esperaban estado error seguro, pero el nuevo estado sin_permiso de readiness se aplicaba indebidamente a la pantalla de cobertura. Se aísla el estado sin_permiso explícito únicamente cuando vistaFlujoV5 es true; cobertura conserva error saneado preexistente. Las cinco nuevas pruebas de readiness ya habían pasado en el SHA fallido, pero éste NO es A4 PASS. Se exige nueva ejecución de 48 pruebas, builds y comprobaciones requeridas en SHA de reparación. Ningún test se elimina ni debilita.
