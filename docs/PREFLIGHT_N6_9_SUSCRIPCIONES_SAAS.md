# N6.9.A — Suscripciones SaaS — Auditoría y preflight

## Identidad y corte de inspección

- Proyecto: `SOLQARYN`.
- Repositorio: `jmejia31/Solqaryn`.
- Rama: `Desarrollo`.
- Parent: `N6.9.A` (`PRE`).
- Base HEAD inspeccionado: `39cda5468d73cf79e4c47960eb16da04de3e60da`.
- Prerrequisito inmediato: `N6.8.H` certificado `LISTO_REAL` por receipt `39cda5468d73cf79e4c47960eb16da04de3e60da`.
- Dependencia declarada en catálogo: `N6.1.H`, ya satisfecha antes de la promoción de `N6.9.A`.

Este documento materializa únicamente el preflight exigido por `N6.9.A`. No implementa dominio, migraciones, API, frontend, billing externo ni feature flags.

## Estado real observado

La inspección dirigida del árbol actual confirma que la raíz tenant ya existe como `Empresa` y conserva identidad/lifecycle empresarial. También existe `UsuarioEmpresa` como vínculo de membresía utilizado por la plataforma para resolver autoridad tenant.

En el corte inspeccionado no existe todavía una implementación de Suscripciones SaaS con los nombres del alcance rector:

- no existe `backend/src/Domain/Entities/Plan.cs`;
- no se encontraron archivos cuyo nombre/ruta contenga `Suscripcion` en el árbol actual;
- `AppDbContext` no declara `DbSet<Plan>`, `DbSet<Suscripcion>` ni un DbSet equivalente de límites SaaS;
- el directorio de controladores API no contiene un controlador de suscripciones por nombre/ruta.

Por tanto, `N6.9` no debe tratarse como una reconciliación de un módulo SaaS ya existente: el trabajo posterior debe introducir el modelo de manera explícita y mantener una sola autoridad tenant.

## Alcance de N6.9

El Plan Maestro define el alcance macro como: **crear Plan, Suscripcion, Limites y funciones disponibles cuando aplique**.

La secuencia autoritativa ya catalogada es:

1. `N6.9.A PRE` — auditoría y preflight.
2. `N6.9.B DOMAIN` — dominio e invariantes/contratos.
3. `N6.9.C DB_MIG` — persistencia, constraints, índices y migración.
4. `N6.9.D BACKEND_API` — aplicación, servicios y API.
5. `N6.9.E FRONTEND_UX` — UI/UX cuando aplique.
6. `N6.9.F SEC_AUDIT` — RBAC, auditoría, seguridad y observabilidad.
7. `N6.9.G TEST_CI` — QA, regresión y CI causal.
8. `N6.9.H DOC_CERT` — documentación y certificación final.

`N6.10` es un parent separado de feature flags y depende de `N6.9`; N6.9 no debe adelantar el cierre funcional de N6.10.

## Invariantes que el trabajo posterior debe preservar

### Autoridad tenant

- `Empresa` continúa siendo la raíz autoritativa del tenant.
- Una suscripción empresarial debe quedar ligada de forma inequívoca a `Empresa`; nunca a un `EmpresaId` confiado desde cliente sin validación server-side.
- Toda lectura/mutación tenant-scoped debe derivar el tenant de la identidad/membresía autoritativa ya existente y fallar cerrado si falta o no coincide.

### Catálogo y suscripción

- El catálogo de planes no debe duplicar la identidad de `Empresa`.
- La suscripción debe distinguir con claridad catálogo (`Plan`) de asignación/estado por empresa (`Suscripcion`).
- Límites y capacidades deben tener una sola fuente de verdad; no duplicar valores incompatibles entre dominio, API y frontend.
- Lifecycle, vigencia y cambios de plan deben tener invariantes explícitas antes de persistirse.

### Separación de responsabilidades

- `N6.9.B` define modelo e invariantes; no debe adelantar migraciones o endpoints grandes.
- `N6.9.C` materializa persistencia/migración con rollback verificable y sin tocar Producción.
- `N6.9.D` expone casos de uso/API usando la autoridad tenant del servidor.
- `N6.9.E` no convierte controles visuales en autoridad de seguridad.
- `N6.9.F` cierra autorización/auditoría/observabilidad.
- `N6.9.G` prueba las invariantes anteriores y no debe ocultar fallos.

## Fuera de alcance de N6.9.A

- Crear tablas o migraciones.
- Implementar endpoints o repositorios productivos.
- Implementar checkout/pagos, proveedor de billing, webhooks o secretos.
- Cambiar `main`, Producción o configuración de deploy.
- Implementar feature flags de `N6.10`.
- Rediseñar módulos no relacionados.

## Riesgos causales identificados

1. **Bypass tenant**: aceptar empresa desde payload/query sin ligarla al tenant autenticado permitiría acceso cross-tenant.
2. **Modelo ambiguo de límites**: duplicar límites en varias capas puede producir enforcement divergente.
3. **Lifecycle incompleto**: una suscripción sin estados/vigencia/invariantes explícitas puede permitir combinaciones imposibles o transiciones inseguras.
4. **Persistencia sin unicidad**: permitir más de una suscripción vigente incompatible para una empresa, si el dominio define una sola, debe prevenirse mediante invariantes/constraints coherentes.
5. **Feature enforcement prematuro**: `N6.10` debe consumir contratos certificados de N6.9; N6.9 no debe fingir que la existencia del plan ya aplica feature flags.
6. **Seguridad sólo en UI**: ocultar funciones en frontend sin enforcement backend no cumple el alcance de seguridad.

## Criterios de aceptación derivados para los siguientes parents

### N6.9.B — DOMAIN

- Entidades/objetos/contratos mínimos para Plan, Suscripcion y límites/capacidades cuando aplique.
- Invariantes de lifecycle y relación con Empresa explícitas.
- Sin dependencia del dominio hacia Infrastructure/API.
- Pruebas dirigidas de invariantes y P0/P1=0 antes de CI amplio.

### N6.9.C — DB_MIG

- FKs/constraints/índices coherentes con las invariantes certificadas en B.
- Snapshot/migración reconciliados y rollback documentado.
- Sin ejecución contra Producción.
- Pruebas dirigidas de persistencia y aislamiento tenant.

### N6.9.D — BACKEND_API

- Casos de uso y endpoints fail-closed ante tenant ausente/mismatch.
- DTOs no convierten un tenant suministrado por cliente en autoridad.
- Errores/ProblemDetails y validaciones dirigidas coherentes.

### N6.9.E — FRONTEND_UX

- UI sólo consume contratos backend; no es autoridad de entitlement.
- Loading/vacíos/errores y accesibilidad aplicables.

### N6.9.F — SEC_AUDIT

- Autorización y auditoría de cambios sensibles de plan/suscripción.
- Sin bypass cross-tenant ni secretos expuestos.
- Logging/observabilidad sin filtrar información sensible.

### N6.9.G — TEST_CI

- Unit/integration/contract/security/migration/E2E aplicables.
- Casos negativos cross-tenant y transición de lifecycle.
- Sólo gates causales/aplicables bloquean el cierre.

### N6.9.H — DOC_CERT

- Evidencia/rollback/runbook y documentación contractual actualizados cuando aplique.
- REVIEW_FIRST P0=0/P1=0, DoD PASS y gates causales terminales antes de `LISTO_REAL`.

## Estrategia de rollback

`N6.9.A` es documental/evidencia y no cambia runtime ni esquema; su rollback es la reversión del changeset documental si la inspección se invalida por un cambio posterior del árbol.

Para parents posteriores, cualquier cambio de persistencia debe incluir `Down`/rollback o procedimiento equivalente verificable, y cualquier transición de datos debe tener preflight/postcheck antes de considerarse certificable. No se autoriza ejecución en Producción desde esta cadena.

## Estrategia de pruebas

Orden recomendado por causalidad:

1. pruebas unitarias dirigidas de invariantes de Plan/Suscripcion;
2. pruebas de persistencia/constraints y aislamiento tenant;
3. pruebas de servicio/API con tenant válido, tenant ausente y mismatch cross-tenant;
4. pruebas de autorización/auditoría;
5. frontend/E2E sólo donde exista flujo UI aplicable;
6. CI amplio al final, después de REVIEW_FIRST y pruebas dirigidas.

## Decisión del preflight

`N6.9.A` encuentra un scope real, dependency-valid y no bloqueado externamente. La siguiente unidad correcta es `N6.9.B` y debe crear únicamente el dominio/contratos mínimos antes de persistencia/API.

No se detectó en este preflight un blocker externo causal que requiera acción humana. La ausencia actual de Plan/Suscripcion no es un bloqueo: es precisamente el gap material planificado para los siguientes parents.
