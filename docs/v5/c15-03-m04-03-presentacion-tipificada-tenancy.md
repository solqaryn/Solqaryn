# SOLQARYN V17 — C15-03.M04.03: branding y configuración reactiva segura de auditoría tenancy

**Autor:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). **Riesgo:** R2 por ruta privada de seguridad y tenancy. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **PLAN:** fila 2254; candado V17 `2000002254`.

## A0: alcance, contratos y dependencias

Sobre la superficie Angular privada `auditoria-tenancy-v5.component.ts` ya protegida por `plataformaGuard`, se añade una configuración PRESENTACIONAL de dos campos tipados (`marca: solqaryn | institucional`; `densidad: normal | compacta`) mediante `InjectionToken<Signal<unknown>>` con valor por defecto seguro. El `computed` reacciona a cambios del Signal sin recargar la ruta. El header muestra únicamente textos literales permitidos y la densidad cambia padding encapsulado, sin CSS/HTML/URLs externos. No se añade dependencia ni se altera la arquitectura de capas.

Cualquier objeto extra, valor inesperado, array, entrada nula o prototipo sin claves propias revierte a `solqaryn/normal`. El branding no controla `PlataformaSesionService`, `plataformaGuard`, roles, JWT, EmpresaId, sucursal, revocación ni backend. No se trata de `TemaVisual` empresarial global ni de un override tenant: no se importa tema mutable ni se intercambian estilos de empresas. La ruta permanece de solo lectura, sin endpoints nuevos, pentest, secretos, conexión ni DB.

## A1/A2: pruebas negativas y fronteras

La suite Vitest existente prueba valores tipados y reactividad, rechazo de HTML/CSS/propiedades extra de escalamiento, fronteras nulo/array/objetos/enums, sesión denegada y revocación aunque el branding sea institucional. Se preservan las pruebas M04.01/M04.02 de `403`, `503`, single-flight, error redacted, `plataformaGuard` y ocultamiento inmediato. El test Chromium adicional comprueba DOM real, marca por defecto, densidad segura, ausencia de formularios y cuatro frentes solo con sesión sintética permitida. Todas las pruebas requieren ejecución real de GitHub Actions en SHA causal; no se declaran PASS por este documento.

## A3: integración pertinente y límites

GitHub-hosted Angular 22.2.1: `npm ci`, Vitest específico, lint/build y Playwright/Chromium localhost con sesión sintética (no JWT real). MySQL 8.4, EF, pagos, integración HTTP autenticada, pentest tenant A/B, despliegue y proveedores: NO_APLICA por ausencia de cambios en esas fronteras, y NO se atribuyen resultados sobre ellas. El backend conserva autoridad exclusiva de acceso.

## A4/A5, trazabilidad y rollback

Publicación única por commit fast-forward CAS a `dev` con component, spec, E2E, documento y sección autocontenida en `CHANGELOG_AI.md`. Requerir `verify-solqaryn-project-scope`, V5 procedencia y checks pertinentes Angular/Chromium `completed/success` sobre SHA exacto o equivalencia auditada antes de LISTO_DEV. PLAN/COLA/DAG y BITACORA con evento idempotente y readback; liberar candado V17 después de transición confirmada. Una Task Javier distinta de la autora debe realizar A5 independiente, con libertad para A5_FAIL. Rollback por commit `git revert` normal y nuevas pruebas; sin force-push, QA/main/PROD o cambios de programación.
