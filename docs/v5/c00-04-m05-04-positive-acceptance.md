# SOLQARYN V19 — C00-04.M05.04 aceptación positiva del gobierno V5

PROJECT_SCOPE_LOCK=STRICT; sólo solqaryn/Solqaryn@dev.

## A0 — alcance, interfaces y control de impacto
- Criterio de PLAN: caso positivo de aceptación; PLAN fila 137 H=PENDIENTE/I vacío antes de claim, COLA G/O=SI y DAG fila 175 F=VALIDADO/G=SI. Dueño único DEV02 mediante candado V17 2000000137, CAS H=1 y readback.
- Prueba de aceptación real sobre ruta `/plataforma/gobierno-cambios-v5` existente con `plataformaGuard`, `PlataformaSesionService` y componente protegido Angular; sin mutaciones de runtime. El contrato solamente explica gobierno, sin leer PLAN/COLA/BITACORA ni otorgar aprobación.
- Riesgo R2 conservador por archivo de workflow CI nuevo; el cambio no modifica IAM/JWT/RBAC, API, EF/MySQL, datos productivos o permisos.

## A1 — pruebas reproducibles que pueden fallar
- Positiva: sesión global sintética 200 permitida, cuatro controles DOM en orden, títulos reales, contenido estático, ningún formulario o autoridad institucional.
- Positiva: revalidar tras navegación efectúa dos GET en total (guard y botón), muestra éxito limitado a solo lectura y no emite POST/PUT/PATCH/DELETE.
- Positiva: enlace de regreso apunta a administración de plataforma y navega realmente, sin convertir control informativo en una acción de gobierno.
- Negativa de frontera: un 200 con `ambito=Empresa` y `esAdministrador=false` nunca autoriza pantalla global; redirige a login y no muestra pasos.
- Todas son aserciones reales Angular + Playwright Chromium; los datos de sesión son interceptados sintéticamente, NO HTTP/JWT/tenant real.

## A2–A3 — segregación y ejecución
- No exponer secretos de proveedor, verificar HTML y rutas reales, ninguna mutación. El permiso global se verifica sólo a nivel de contrato del frontend contra GET simulado; las pruebas NO certifican autorización del backend, membresías, Aiven, restauración, WCAG completa ni producción.
- A3: Node24, npm11, Angular22 build/lint y cuatro Chromium con servidor efímero localhost en GitHub-hosted de SHA exacto. CI con `cancel-in-progress=false` y grupo por SHA; no cancela checks anteriores.

## A4–A5, rollback
- Requiere job dirigido real, Scope Lock, V5 procedencia, frontend/seguridad pertinentes y readback PLAN/BITACORA con SHA. QUEUED, IN_PROGRESS, SKIPPED o CANCELLED no son PASS.
- Publicación serializada fast-forward en dev, VALIDANDO a la espera de CI externa, cierre LISTO sólo con A0–A4 causales. A5 por otra Task Javier diferente de DEV02.
- Rollback: `git revert` aditivo sin force-push; jamás QA, main ni PROD.
