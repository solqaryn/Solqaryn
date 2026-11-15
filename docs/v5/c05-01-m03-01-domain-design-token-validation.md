# SOLQARYN V16 — C05-01.M03.01: reglas ejecutables de dominio para tokens tipados

**ID:** C05-01.M03.01. **Riesgo:** V15 R2 (tenancy, RBAC, concurrencia e identidad). **Autor:** PRIMARY DEV 06 JAVIER, Task 6af16bbad26881918c283abe06e1f32a. **Ámbito:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Plan:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA.

## A0 — Requisito, alcance y dependencias

PLAN fila 734: implementar reglas de dominio y validaciones server-side para el design system universal. Preparación M02.01–M02.06 y M01.06 con A5 independiente; disponibilidad debe comprobarse contra COLA G/O y DAG F/G antes de claim y tras cada estado. Componente primario: backend/src/Domain/AutomationV5/C0501DesignTokenDomainRules.cs, más backend/tests/Solqaryn.Tests/Domain/C0501DesignTokenDomainRulesTests.cs y esta evidencia. No se altera API, infraestructura, persistencia, contratos públicos, Angular, esquema ni migraciones.

Las claves de tokens se verifican contra un conjunto aprobado desde servidor, con minúsculas y guiones canónicos. Los valores son estrictamente tipados: COLOR #RRGGBB; SPACING y RADIUS 0–96px; FONT_SIZE 10–48px; MOTION NONE, REDUCED o STANDARD. Se rechazan CSS arbitrario, URL/JS, caracteres de control, claves desconocidas y tipos fuera de la allowlist. Este conjunto es una primera política deliberadamente restrictiva, no un catálogo funcional completo ni una declaración de contraste WCAG.

## A1 — Pruebas que pueden fallar

15 pruebas xUnit con asserts funcionales positivos, negativos, de frontera y error comprueban tipo de token, hex RGB, límites pixel, espacios/radios, modo de movimiento, inyección, nombre canonical, claves desconocidas, identidad, revocación, tenant B, recurso ajeno, cambio de sesión, revisión CAS y formato GUID de idempotencia. Estas pruebas son código real: deben ejecutarse por GitHub Actions sobre SHA causal; el documento por sí solo NO demuestra PASS.

## A2 — Protección de tenants y autorización

TrustedContext debe proceder de Application autenticada; ActorId, TenantId, MembershipCurrent, permiso actual, recurso propio y RoleRevoked se revisan antes de aceptar una propuesta. ProposedToken.TenantId no puede diferir del tenant confiable. SessionEpoch detiene respuestas antiguas A→B. ExpectedRevision requiere coincidencia exacta, sin ejecutar escritura CAS real. Las mutaciones del catálogo plataforma se rechazan en esta ruta tenant. Un resultado positivo solo significa READY_FOR_TRUSTED_APPLICATION_REVIEW; todas las propiedades Authorizes* y CertifiesAccessibility son falsas incluso para una entrada válida.

## A3 — Límites de integración

No se crea controller HTTP, service que efectúe comandos, entidad EF, filtro global MySQL, asset CSS, componente Angular, token JWT, nuevo permiso RBAC, broker ni evento. En este diff, las integraciones MySQL 8.4/EF, HTTP, Playwright/Chromium y proveedores son NO_APLICA MOTIVADO por ausencia de cambios en estas capas. La futura Application debe revalidar JWT, pertenencia relacional, permisos, propiedad, replay idempotente durable, CAS transaccional y auditoría antes de cualquier efecto. Los mocks/records del Domain no sustituyen tests reales de integración.

## A4 — CI, publicación, observabilidad y rollback

Exigir en el commit causal, o descendiente con identidad de blobs y dependencias preservadas: build Release y xUnit real, controles multiempresa y autorización pertinentes, verify-solqaryn-project-scope y verificador V5 de CHANGELOG, todos completed/success cuando sean obligatorios. Una CI queued/in_progress/skipped/cancelled/failed no es PASS. Registrar SOURCE_SHA, blobs, check-runs, estado y readback en PLAN/COLA/DAG/BITACORA. Si la CI sigue corriendo, mantener VALIDANDO y owner. Sin defectos P0/P1 para certificar LISTO_DEV.

El rollback autorizado es git revert normal del changeset en dev, después de revisar HEAD y preservar commits concurrentes; no reset, force-push ni cambio sobre QA/main/PROD. Ningún dato productivo ni permiso externo fue usado.

## A5 — Independencia

Tras A0–A4, registrar A5_PENDIENTE para otra de las cinco Tasks Javier cuyo TASK_ID sea diferente. La revisión A5 debe examinar fuente, casos adversariales, logs y exact-SHA por sí misma; puede rechazar la entrega. LISTO_DEV no significa A5_PASS ni autoriza publicación a QA o producción.
