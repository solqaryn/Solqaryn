# SOLQARYN V19 — C00-04.M04.06 | Densidad y progreso de gobierno

PROJECT_SCOPE_LOCK=STRICT. Repositorio exclusivo solqaryn/Solqaryn, rama dev.

## A0 — Alcance, arquitectura, impacto y revertibilidad
- Gate del plan: M03.01-06 y M04.01-05 con LISTO y A5 independiente; PLAN_MAESTRO fila 133, COLA G/O=SI y DAG_TECNICO fila 171 F=VALIDADO/G=SI, revalidar dinámicamente.
- Verifica la superficie privada existente en /plataforma/gobierno-cambios-v5 con plataformaGuard y PlataformaSesionService de backend; no modifica controlador, DTO, IAM, JWT, RBAC, base de datos, configuración tenant ni despliegue.
- Cinco pruebas Playwright y workflow GitHub-hosted exclusivos en CI. Ninguna ruta ni lógica runtime se publica desde este ID, por no haberse identificado un defecto de runtime que justifique mutarla.
- Riesgo R1 por prueba funcional de UI con red sintética, sin cambios de API. Reversión mediante git revert normal del commit, nunca force-push.

## A1 — Cinco escenarios que pueden fallar
- Anónimo no entra ni ve pasos; caso adversarial de permisos.
- 320/390/768/1280 píxeles: cuatro pasos visibles, una o dos columnas reales según CSS calculado, límites de caja y cero desbordamiento horizontal.
- Revalidación con GET diferido: estado de carga accesible, botón deshabilitado, un solo GET mientras está pendiente y recuperación sin duplicados.
- HTTP 503: no se muestra información del proveedor, no hay pasos protegidos ni retry espontáneo; se recupera solo tras acción explícita.
- Modo solo lectura: cambios de viewport no ejecutan POST/PUT/PATCH/DELETE ni consultan PLAN/COLA/BITACORA; sin prometer SLO/performance medidos.

## A2–A3 — Seguridad e integración proporcional
- La autorización real permanece en el backend; JWT de localStorage y /plataforma/sesion son sintéticos en Playwright. Solo se certifica comportamiento de frontend denegado y seguro, no tenant A/B, MySQL, HTTP real, WCAG global o servicios externos.
- A3 requiere que el runner GitHub-hosted levante Angular local con npm y Chromium verdaderos, ejecute los cinco tests, lint y build. MySQL 8.4 e integración EF son NO_APLICA: el cambio solo añade pruebas de UI, documentación y CI dirigida.

## A4–A5 — Requisitos de cierre
- Publicar directo dev con fast-forward y SHA verificado; verificar GitHub Actions requerido completed/success para Chromium dirigido, build Angular, Scope Lock, V5 provenance y seguridad pertinente en SHA exacto o descendiente de blobs equivalentes demostrados.
- QUEUED/IN_PROGRESS/CANCELLED/SKIPPED no son PASS. Mantener VALIDANDO con RUN/SHA propietario mientras CI está pendiente y releer PLAN/COLA/DAG/BITACORA; no liberar sucesoras técnicas por VALIDANDO.
- A5 pendiente de otra Task Javier diferente de la autora de este commit; no constituye permiso para QA/main/PROD ni evidencia de auditoría productiva.

## CI_REPAIR V19 — selector de región, no seguridad ni semántica de producto
- SHA fuente e81b2a6162ff6ce8bde6c6f2841459f323d11b54: Chromium GitHub-hosted job 133786376283 ejecutó cinco tests; 3 PASS, 2 FAIL por Playwright strict mode al usar getByRole(status) global. El shell también expone un anuncio sr-only role=status, por lo que el locator global seleccionó dos elementos. No afirmar P0 ni defecto de backend.
- Reparación mínima: seleccionar únicamente el estado propio de la región .state-banner [role=status] en las tres aserciones, conservando texto, fallos, prueba negativa y la interacción real. No modificar guard, código runtime, pruebas de seguridad ni CI para aceptar errores.
- Re-ejecutar workflow GitHub-hosted exact SHA nuevo; hasta SUCCESS conservar VALIDANDO, A5 pendiente y candado V17 con readback. El fallo original queda preservado en CI/GitHub y BITACORA de reparación; revert normal si procede.
