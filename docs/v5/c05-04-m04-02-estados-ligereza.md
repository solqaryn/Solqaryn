# C05-04.M04.02 — estados reales de la superficie privada de ligereza (DEV)

Autor: PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Rama dev, PROJECT_SCOPE_LOCK=STRICT, PLAN fila 849, V17 namedRange 2000000849. Riesgo V15 R1 Angular 22.2.1, sin cambios de API, JWT, RBAC, migraciones, MySQL ni proveedores.

## A0 — necesidad, alcance y dependencias
La pantalla existente /plataforma ya está protegida por plataformaGuard y requiere sesión de plataforma confirmada por GET /plataforma/sesion. Se añade dentro de la sección de ligereza un feedback accesible que deriva directamente del ciclo de lectura GET /plataforma/empresas ya existente: carga, directorio vacío, error recuperable, éxito y permiso denegado (401/403). Se preservan los datos previos del componente fuera del panel y no se incluyen nombres de empresa ni mensajes crudos de error en el feedback. La clasificación de 401/403 impide reintento ciego; para 503 sólo hay reintento GET explícito mediante cargarEmpresas, que ya aplica guarda contra doble activación. No se añaden llamadas, endpoints, grants, rutas, escritura, caché ni métricas ficticias de Core Web Vitals.

Archivos: frontend/src/app/features/plataforma/plataforma.component.ts, frontend/e2e/c05-04-m04-02-feedback.spec.ts, CI dirigida, CHANGELOG. Consumen Angular Signals, HttpErrorResponse y el servicio existente PlataformaEmpresasService. No se modifican arquitectura, DTO de API ni base de datos. Rollback: git revert normal del SHA causal, sin force-push.

## A1 — pruebas causales exigidas
Playwright Chromium real en GitHub-hosted: usuario anónimo (guard); sesión 403 sintética y no exposición del mensaje; lectura lenta para carga y vacío; respuesta válida con resultado positivo y responsive 320/375/768/1280; respuesta 503 y un reintento exitoso sin mensaje confidencial; 403 de directorio y ausencia de reintento ciego. El backend y los tokens de las fixtures son sintéticos, no se afirma autorización JWT HTTP ni MySQL real. El caso de carga debe esperar una respuesta retenida, no simular un texto estático.

## A2/A3 — límites de seguridad e integración
A2 conserva guard y sesión de backend vigente; deniega visualización sin grant y no muestra strings del servidor o nombres en el panel de feedback. A3 requiere build/lint de Angular 22.2.1 y Playwright/Chromium en GitHub Actions con SHA exacto. MySQL 8.4, EF, broker, integración HTTP backend real y medición de LCP/CLS son NO_APLICA/NO_MEDIDO porque no cambia esos subsistemas ni existe ejecución live en las pruebas.

## A4/A5
A4 exige verify-solqaryn-project-scope, V5 CHANGELOG, frontend build y Chromium directed completed/success exact-SHA, readback PLAN_MAESTRO y BITACORA. Queued/failed/in_progress no son PASS. VALIDANDO es permitido con CI externa real pendiente, conservando owner/SHA/RUN. A5 pendiente hasta revisión independiente por otra Task Javier; no concede QA/main/PROD. Consultada la referencia Angular Developer registrada (angular/angular, pin 6808966993bf2b2d455d9b6245e960a59a01b9a9); no se añadió dependencia ni skill local.

## Defecto QA P2 y regresión causal
Run 43911108150: Chromium 5/6 PASS y el sexto FAIL por asumir redirección /login cuando la revocación del interceptor puede dejar la URL /plataforma. La reparación modifica sólo el assert negativo: verifica que no haya authority-card, que el contenido privado no exista y que se presente denegación, sin exponer mensaje backend. El SHA original no se certifica y se exige nueva CI exact-SHA.
