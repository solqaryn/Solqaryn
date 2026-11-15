# SOLQARYN V17 — C14-02.M03.04: adaptador aislado de evidencia para aceptación de seguridad

**ID institucional:** C14-02.M03.04, PLAN_MAESTRO fila 2069. **Autor:** PRIMARY DEV 04 Javier (TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09). **Riesgo:** V15 R2, JWT/RBAC, BOLA/IDOR, tenant y concurrencia. Solo repositorio solqaryn/Solqaryn y rama dev; PROJECT_SCOPE_LOCK=STRICT. Candado V17 SQV17_C14_02_M03_04, namedRangeId 2000002069.

## A0 — Ámbito, arquitectura, predecesoras y rollback

C14-02.M03.04 implementa un **adaptador Infrastructure .NET 10 real pero aislado**, que captura evidencia sintética immutable para ejercitar las reglas Domain V5SecurityAcceptanceDomainRules y el gate C1402SecurityAcceptanceAuthorizationGate (M03.01–03). No crea un proveedor autenticado, controlador, transacción MySQL, migración, index, worker real, webhook, rutina de restablecimiento, servicio de red, secreto ni despliegue. La frontera entre Domain / Application / Infrastructure se conserva. Habilitación DAG F=VALIDADO, G=SI y COLA G/O=SI se comprobó antes de reclamar; no implica autorización para ejecutar una prueba de penetración, descargar archivos o leer otro tenant. Rollback mediante git revert normal contra HEAD actual, nunca reset o force-push.

Archivos del changeset: backend/src/Infrastructure/AutomationV5/C1402SecurityAcceptanceIsolatedSnapshotAdapter.cs, backend/tests/Solqaryn.Tests/Infrastructure/C1402SecurityAcceptanceIsolatedSnapshotAdapterTests.cs, este documento y sección completa de CHANGELOG_AI.md.

## A1 — Comportamiento real implementado y verificación

El adaptador captura identificadores resource/tenant/actor/SHA y registros escalares sin observadores mutables. Antes de delegar al gate Application rechaza request nulo, IDs de recurso contradictorios, tenant B, actor distinto, SHA obsoleto, propiedad persistida y autoría de la evidencia capturada incompatibles. Las pruebas xUnit cubren 13 escenarios testigo positivos/negativos/de frontera/error, que deben ejecutarse realmente en CI sobre el SHA publicado. Un resultado nominal dice AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED: no concede permiso ni certificación.

## A2 — Seguridad y denegación por defecto

Se reutilizan validaciones comprobables de Domain + Application: JWT/membresía/RBAC declarados, BOLA/IDOR, tenant A/B, sucursal, host, expiración de enlaces, service actor/job, consentimiento implícito prohibido, auditoría, bypass, revocación y hash de procedencia. Todos los atributos de capacidad de acceso/lectura/descarga/escritura/restauración/publicación del adaptador son permanentemente false. Una respuesta de snapshot sintética no reemplaza pruebas HTTP con JWT real, query filters EF, MySQL 8.4 ni pentest.

## A3 — Integración causal y límites

Se requiere compile + test .NET 10 Release bajo GitHub-hosted Actions y el enlace real Infrastructure→Application→Domain. Este changeset aislado no toca EF, esquema MySQL, API/HTTP, Playwright, Cloudinary ni proveedores de producción, por lo que esos tests LIVE son NO_APLICA a esta unidad, no PASS. M03 posteriores deben implementar fuente confiable y pruebas de integración efectivas, con autorización separada.

## A4 — Publicación y gates

Publicar un único commit fast-forward/CAS en dev, verificar diff, blobs, seguridad multiempresa, Authorization files/secrets/tenant, Backend Release y xUnit, Scope Lock exact-SHA y procedencia V5 SUCCESS. Si un check requerido queda QUEUED/IN_PROGRESS, conservar H=VALIDANDO con owner, SHA y RUN externos comprobados. Al completar pruebas A0–A4, LISTO_DEV exige BITACORA idempotente, PLAN/COLA/DAG/readback dirigido y liberar candado V17 una vez. No usar SKIPPED/CANCELLED como PASS.

## A5 — Independencia y límites

Revisión por otra Task Javier distinta del autor, susceptible de A5_FAIL. No certifica autenticación de API, base de datos, seguridad global, pagos ni la autorización de QA/main/PROD. Datos, tenants, configuración física de las seis Tasks y secretos permanecen intactos.
