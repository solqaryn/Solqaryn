# SOLQARYN V5 — C02-04.M03.04: adaptador aislado de evidencia de consola global

**Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R2, autorización, MFA, tenancy. **Fuente autorizada:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, C02-04.M03.04.

## Implementación real

Se añade `V5GlobalControlPlaneIsolatedSnapshotAdapter` en Infrastructure .NET 10. Captura únicamente un `V5GlobalControlPlaneAuthorizationGate.Evidence` entregado por un caller de backend, sin conectarse a proveedores, DB o API. Rechaza actor, tenant y SHA contradictorios, no permite que un request altere el target, y ejercita el **código existente** de `V5GlobalControlPlaneAuthorizationGate` (Application) y `V5GlobalControlPlaneDomainRules`, incluyendo grants de plataforma independientes, MFA, reautenticación, revocación y auditoría.

El resultado favorable `AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED` exige otro adaptador auténticamente autorizado; **no concede permisos**. El componente no se registra en DI, no implementa JWT/HTTP real, no crea lease excepcional, no lee ni enumera tenants, no persiste logs, no genera migraciones MySQL, ni realiza escrituras. La evidencia sintética de xUnit jamás es certificación de las fuentes auténticas.

## A0–A5 y validación por riesgo

- A0: commit individual Infrastructure + xUnit + documento + CHANGELOG, respeto del DAG C02-04.M02.01–06+A5 y M03.01–03+A5, ningún cambio de arquitectura o credenciales.
- A1: **nueve métodos xUnit reales**: lectura nominal sin grants, request nulo/actor falso, tenant cruzado, SHA caducado, mismatch de snapshot, revocación/MFA, separación de grants plataforma, request malicioso, cancelación/constructor.
- A2: casos adversariales tenant A/B, actor distinto, grants actuales, MFA, revocación, identidad/verificación backend, bypass/intentional write. **No interpretar snapshots en memoria como JWT HTTP ni aprobación real**.
- A3: build .NET 10 Release y xUnit verdaderos sobre CI del SHA, más seguridad/autorización dirigida. MySQL 8.4 Category=Integration NO_APLICA al diff sin EF/DDL, no se declara PASS falso. Playwright tampoco aplica a este diff sin UI.
- A4: Scope Lock, procedencia V5, backend/seguridad/autorización `completed/success` en SHA origen o equivalente con blobs idénticos y readback PLAN/COLA/BITACORA. In-progress, skipped, cancelled, failed no son PASS.
- A5: revisión independiente por otra Task Javier distinta de la autora con criterio propio para rechazar.

**Rollback:** `git revert` sin force-push ni destruir evidencia institucional. Sin QA/main/PROD, JWT real, persistencia, grants ni datos productivos.
