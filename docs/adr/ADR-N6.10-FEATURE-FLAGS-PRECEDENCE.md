# ADR N6.10 — Precedencia de feature flags SaaS

Estado: Aceptado
Fecha: 2026-09-13 UTC
Autoridad: `docs/VAEP_AUTHORITY.md`
Rama: `Desarrollo`

## Contexto

N6.10 introduce evaluación de capacidades SaaS que debe permanecer aislada por tenant y no puede convertir valores provistos por cliente, cachés compartidas ni defaults permisivos en autoridad de acceso. El candidato funcional certificado por N6.10.G es `cab86ea5b052a1763a3a8b21ea4e9839b99ee429`; el cierre TEST_CI está documentado en `vaep/evidence/receipts/N6.10.G_LISTO_REAL_20260913T040212Z.json`.

## Decisión

La precedencia contractual de resolución es `EXPERIMENT -> TENANT -> PLAN`.

1. **EXPERIMENT** puede especializar una decisión únicamente dentro del tenant ya autorizado y para la clave/módulo evaluado. Nunca crea membresía, cambia la empresa efectiva ni amplía permisos RBAC.
2. **TENANT** es la frontera autoritativa. `EmpresaId` debe resolverse server-side mediante el scope/membresía vigente. Una ruta o payload sólo selecciona el tenant solicitado y debe coincidir con el scope resuelto; ausencia, ambigüedad o mismatch se resuelven fail-closed.
3. **PLAN** aporta el entitlement base persistido para la suscripción vigente del tenant. Un plan inexistente, inactivo, no resoluble o sin regla aplicable no habilita por fallback.

La precedencia no significa que una capa superior pueda saltarse una condición de seguridad inferior: autorización, membresía tenant y RBAC siguen siendo precondiciones obligatorias antes de evaluar el resultado funcional.

## Aislamiento y caché

Toda caché de evaluación debe estar particionada, como mínimo, por identidad tenant autoritativa y por la dimensión funcional relevante (plan/módulo/experimento y versión cuando exista). Está prohibido reutilizar una entrada entre empresas aunque coincidan plan, módulo o variante. La invalidación debe ser selectiva para el tenant/plan/experimento afectado; ante duda de versión o contexto se invalida y se recalcula, nunca se usa un valor compartido como fallback.

Las claves de caché y la telemetría no deben contener secretos. Identificadores internos necesarios para correlación pueden registrarse conforme a la política de auditoría existente, evitando payloads sensibles.

## Seguridad fail-closed

`SuscripcionesSaaSService` resuelve `IUsuarioScopeService.ObtenerActualAsync(empresaId)` antes de lecturas SaaS y rechaza tenant inválido o sin membresía activa. Los endpoints SaaS requieren autenticación y permiso relacional de Configuración. Estas propiedades son parte del contrato y cualquier cambio que las debilite supersede el candidato y exige recovery causal.

## Consecuencias

- No se permite cache global por módulo/plan sin tenant.
- No se permite confiar en `EmpresaId` cliente como autoridad.
- No se permite fallback permisivo ante plan/regla/contexto ausente.
- Cambios posteriores de precedencia requieren ADR nuevo o superseding explícito, pruebas cross-tenant y gates causales de seguridad.
- `main`, Producción, deploy, secretos y PR #2 quedan fuera de este ADR.

## Evidencia

- `backend/src/Application/Services/SuscripcionesSaaSService.cs`
- `backend/tests/Solqaryn.Tests/N610FFeatureFlagsSecurityTests.cs`
- `vaep/evidence/reviews/N6.10.G_REVIEW_FIRST_20260913T035239Z.json`
- `vaep/evidence/receipts/N6.10.G_LISTO_REAL_20260913T040212Z.json`
