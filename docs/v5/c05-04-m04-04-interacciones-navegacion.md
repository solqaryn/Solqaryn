# C05-04.M04.04 — formularios, navegación, mensajes y prevención de doble acción

PLAN_MAESTRO fila 851, ámbito C05-04 / Ligereza y percepción / M04.
Autor: SOLQARYN PRIMARY DEV 06 JAVIER; TASK_ID 6af16bbad26881918c283abe06e1f32a.
Repositorio solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.
Lock atómico V17 SQV17_C05_04_M04_04 (2000000851). Riesgo V15 R1.

## A0 — alcance y límites
La tarea exige **verificar** las interacciones privadas, no crear otra aplicación.
Se inspecciona el componente existente `PlataformaComponent` protegido por
`plataformaGuard`, el panel `.ui-lightness-v5` y la lectura GET de directorio.
Este changeset agrega una suite Playwright/Chromium que recorre realmente el DOM,
un workflow GitHub Actions SHA-aislado y este contrato. No edita el componente,
backend, JWT/RBAC, MySQL, permisos ni rutas compartidas; evita conflictos con
C05-04.M04.02, que desarrolla otros estados de la misma vista.

Criterios específicos: anónimo y sesión 403 sintética sin superficie privada,
teclado Tab/Enter real con foco y `aria-pressed` reversible,
mensajes de error 503 opacos, 403 del directorio sin nuevo intento,
deshabilitar `Actualizar directorio` mientras está pendiente GET,
resultado vacío autorizado, detección de POST/PUT/PATCH/DELETE indebido.
Esta superficie de consulta no contiene formularios editables: se verifica
la ausencia de elementos de entrada, no se inventa una prueba de validación
de formularios que no existen.

## A1–A3 — evidencia dirigida
Se aportan seis pruebas Playwright que deben ejecutarse en Chromium real
sobre SHA publicado. A1 cubre positivos, denegaciones, reintentos, frontera
de solicitud lenta, teclado y mensajes de error. A2 cubre únicamente
guard y escenarios sintéticos 403, no autenticación JWT HTTP real ni
aislamiento de tenant servidor (no se modifica dicho control).
A3 = navegación y eventos DOM/browser real local con API sintética;
MySQL/EF, datos reales y proveedores externos NO_APLICA al diff.
No presentar pruebas escritas como aprobadas antes de GitHub Actions.

## A4–A5
GitHub Actions debe mostrar `completed/success` para el job dirigido
`C05-04.M04.04 - Angular build y Chromium`, frontend, Scope Lock,
V5/changelog y checks pertinentes sobre SHA exacto o equivalente con
integridad de blobs acreditada. CI QUEUED/IN_PROGRESS/FAIL no es PASS.
El workflow contiene `concurrency` por `github.sha` y
`cancel-in-progress: false`; no desactiva otros controles.
Registrar BITACORA y readback tras cada transición. Si la CI está externa,
mantener VALIDANDO con owner real y SHA/RUN hasta REVIEW_FIRST.
A5 exige revisor de otra Task Javier con ID distinto. No existe
autorización para QA/main/PROD ni modificación de producción.
Rollback: git revert normal del commit de pruebas, workflow y documento.
