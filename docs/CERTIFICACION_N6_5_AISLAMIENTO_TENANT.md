# N6.5 — Aislamiento tenant — Certificación canónica

## Estado

- Parent: `N6.5`
- Microtarea de cierre: `N6.5.H — Documentación y certificación`
- Rama autorizada: `Desarrollo`
- Base de cierre documental: `c478efc89eb8ff3c1d1928ccd02965e3e0edd009`
- Predecesor: `N6.5.G = LISTO_REAL`
- Política: `docs/VAEP_AUTHORITY.md`
- Producción/main/deploy/secrets: fuera de alcance y no autorizados.

## Objetivo certificado

N6.5 impide tenant leakage entre empresas y obliga a que la autoridad tenant provenga de una membresía `UsuarioEmpresa` válida, activa y coincidente. El frontend no concede autoridad por `localStorage`, query string ni estado visual; el backend valida la empresa solicitada y resuelve el rol/permisos desde la membresía empresarial. La ausencia o inconsistencia del tenant falla cerrado.

## Cobertura material A–G

### N6.5.A — Auditoría y preflight

Dependencias `N6.2.H` y `N6.4.H` verificadas; alcance de aislamiento, riesgos y criterios de aceptación establecidos antes de implementación.

### N6.5.B — Dominio y contratos

Se certificó `ContextoTenantActual` a partir de `UsuarioEmpresa` activa/coincidente. El rol efectivo se obtiene de la membresía tenant-aware y se prohíbe usar `Usuario.RolId` como autoridad empresarial.

### N6.5.C — Persistencia, migración y datos

Se reutilizó la persistencia ya certificada de `UsuarioEmpresa`: unicidad `(UsuarioId, EmpresaId)`, FKs restrictivas, estado de membresía y comportamiento legacy fail-closed. No se creó migración duplicada ni backfill que invente empresa/rol.

### N6.5.D — Aplicación, servicios y API

Los casos de uso y servicios consumen contexto tenant validado. Los contratos HTTP rechazan tenant ausente/inválido y no aceptan identidad empresarial derivada sólo del cliente.

### N6.5.E — Frontend y UX

El flujo de login/selección de empresa valida el tenant contra backend. Un tenant recordado se revalida; `403` o inconsistencia revocan el contexto. El frontend mantiene UX tenant-aware sin convertirse en fuente de autoridad.

### N6.5.F — RBAC, auditoría, seguridad y observabilidad

La autorización usa el tenant solicitado/verificado para resolver permisos. Los filtros `RequierePermiso`/`RequiereAlgunoPermiso` no regresan al rol global legacy. Se cubren fail-closed, aislamiento de roles y auditoría relevante.

### N6.5.G — QA, regresión y CI

QA recuperó fixtures legacy que recargaban la SPA y perdían contexto tenant, ajustó M9/legacy bulk-load a login tenant-aware y completó la cohorte causal. Exact product head certificado: `364bde3ea346d46b1acfb69800807b3718c8da0c`.

Evidencia terminal de N6.5.G:

- REVIEW_FIRST: `P0=0`, `P1=0`.
- 37 check-runs exact-head terminales sin `failure`, `queued` ni `in_progress`.
- 27 workflow runs exact-head sin `failure`, `queued`, `in_progress`, `cancelled`, `timed_out` ni `action_required`.
- Vercel `build-rate-limit` permanece no causal y no autoriza deploy.
- Receipt de cierre G: commit `c478efc89eb8ff3c1d1928ccd02965e3e0edd009`.

## Invariantes de aislamiento

1. Ningún request empresarial obtiene autoridad sin tenant validado.
2. La empresa solicitada debe pertenecer a una membresía activa del usuario.
3. El rol y permisos efectivos se resuelven por `(UsuarioId, EmpresaId)`.
4. Cambiar `empresaId` en cliente/header no concede acceso por sí mismo.
5. Un usuario con roles distintos por empresa conserva aislamiento de permisos.
6. La ausencia de tenant, membresía inactiva o empresa distinta falla cerrado.
7. Estado visual/local del frontend nunca sustituye validación backend.
8. No existe fallback autorizado al `Usuario.RolId` global para operaciones tenant-aware.

## Rollback / recuperación

N6.5.H no introduce cambios funcionales, de esquema ni de infraestructura. El rollback de esta microtarea consiste únicamente en revertir documentación/evidencia de certificación. Los cambios funcionales B–G conservan sus propios commits/receipts y deben revertirse causalmente sólo si una regresión atribuible lo exige; nunca mediante force-push ni reescritura de historial.

Ante incidente tenant:

1. identificar exact-head y endpoint/flujo afectado;
2. reproducir con dos empresas y membresías distintas;
3. verificar resolución `UsuarioEmpresa` y empresa solicitada;
4. confirmar rechazo fail-closed cuando falte/mismatch tenant;
5. corregir exclusivamente en `Desarrollo`;
6. ejecutar pruebas dirigidas + gates causales;
7. REVIEW_FIRST con `P0=0/P1=0` antes de certificar.

## OpenAPI / ADR / ERD

No se requiere un cambio OpenAPI, ADR o ERD adicional en H porque esta microtarea no altera contratos, modelo ni esquema respecto de B–G. La autoridad documental de este cierre consolida el comportamiento ya implementado y probado; cualquier cambio futuro de contrato/schema deberá generar su evidencia específica.

## Criterio de cierre H

`N6.5.H` puede declararse `LISTO_REAL` únicamente si:

- este documento y la evidencia de cierre son revisados;
- REVIEW_FIRST resulta `PASS`;
- `P0=0` y `P1=0`;
- no existe delta funcional posterior a `N6.5.G` sin validar;
- la cohorte causal previa permanece válida o el exact-head documental demuestra equivalencia control/evidence-only;
- PR #2 permanece `OPEN + DRAFT` y `main`/Producción/deploy/secrets continúan intocados.

## Restricciones preservadas

- `PARENT_CLOSE_FIRST=TRUE`.
- AntiG permanece `RESERVED_INACTIVE` / fuera del camino crítico.
- Sin Jules artificial, R3, ramas nuevas, force-push, merge/auto-merge, secretos ni deploy.
- PR #2 debe permanecer `OPEN + DRAFT`.
