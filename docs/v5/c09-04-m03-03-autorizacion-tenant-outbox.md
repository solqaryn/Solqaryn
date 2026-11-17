# SOLQARYN V19 — C09-04.M03.03: autorización tenant fail-closed en outbox

Autor: DEV06 `6af16bbad26881918c283abe06e1f32a`. Repositorio `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`; PLAN fila 1420, lock V17 `2000001420`. Riesgo R2 (tenancy/RBAC). Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES vigente.

## A0 — Requisito, contrato y alcance

El servicio `MensajeOutboxService.RegistrarAsync` ya consulta `IUsuarioScopeService.ObtenerActualAsync(empresaId)`, que en infraestructura obtiene la membresía vigente desde `UsuarioEmpresa` y valida usuario, empresa y rol activos. Este cambio preserva esa autoridad server-side y añade defensa ante un contexto defectuoso/devuelto incorrectamente: exige EmpresaId exacto, UsuarioId positivo, RolId positivo y RolNombre no vacío. Un rol administrador global nunca sustituye la membresía de la empresa. La autorización de funciones concretas sigue perteneciendo a sus filtros/casos de uso de entrada; este cambio NO inventa un permiso de envío general.

Diff limitado a `Application/Services/MensajeOutboxService.cs`, nueva prueba xUnit `C0904M0303OutboxAuthorizationTests.cs`, este documento y `CHANGELOG_AI.md`. Sin alteraciones a Domain, contratos públicos, API, DI, EF, schema, índices, JWT, servidor HTTP, frontend, colas reales ni proveedores.

## A1 — Pruebas adversariales causales

Siete casos xUnit: denegar ausencia de membresía, tenant B diferente, usuario no válido, rol no válido, nombre de rol blanco, fallo del servicio de membresía sin lectura ni escritura de outbox y aceptar membresía válida no administradora del tenant A con clave normalizada. El repositorio espía cuenta lecturas y escrituras para demostrar cero operaciones tras denegación. Estas pruebas son in-process con dobles de IUsuarioScopeService e IMensajeOutboxRepository; requieren su ejecución auténtica en GitHub Actions .NET 10 Release; el texto no equivale a PASS.

## A2 — Seguridad y límites

La fuente `UsuarioScopeService` ya exige usuario autenticado, membresía activa, usuario no bloqueado, empresa activa, rol empresarial vigente y ámbito correcto. La validación adicional protege contra valores de scope malformados o cruzados aunque el adaptador cambie. No acepta `EsAdministrador` como permiso cross-tenant. El rechazo no revela existencia de empresa ajena ni toca outbox. No se certifica por estos stubs revocación con MySQL real, JWT HTTP, permisos por acción, HMAC, webhook, SMTP ni WhatsApp.

## A3 — Integración

Aplicable: Domain + Application y pruebas reales xUnit ejecutadas por GitHub-hosted en .NET 10 Release, más CI de seguridad y autorización pertinente. No aplica migración/EF/MySQL 8.4 efímero porque no se cambian esquema, queries ni proveedor; no se declara integración DB real. No aplica Playwright, proveedores externos o medición de producción.

## A4 — Publicación y A5

Exigir commit fast-forward exclusivo en `dev`, checks `verify-solqaryn-project-scope`, procedencia V5, build Release/xUnit, seguridad y autorización `completed/success` por SHA causal; ni SKIPPED ni IN_PROGRESS cuentan PASS. Registrar SHA/RUN y readback PLAN, COLA, DAG y BITACORA V17. Solo pasar LISTO con A0–A4 verificadas; A5 exclusivamente otra Task Javier distinta. Riesgos residuales: cobertura HTTP/DB/consentimiento/proveedor = NO_MEDIDO. Rollback: `git revert` normal verificando HEAD, nunca force-push ni QA/main/PROD.
