# N6.7 — Configuración independiente — Certificación canónica

## Estado

- Parent: `N6.7`
- Microtarea de cierre: `N6.7.H — Documentación y certificación`
- Rama autorizada: `Desarrollo`
- Base de cierre documental: `0f7f9fedf8e162a060f652eab7466c5ab196f399`
- Predecesor: `N6.7.G = LISTO_REAL`
- Functional head certificado por G: `e83b450f549ea4e0dafbc47760e60f910aafad70`
- Política: `docs/VAEP_AUTHORITY.md`
- `main`, Producción, deploy, secretos y merge de PR #2: fuera de alcance y no autorizados.

## Objetivo certificado

N6.7 implementa configuración independiente por tenant/empresa para identidad y operación: nombre, RTN/identificación, dirección, logo, moneda, zona horaria, impuestos, parámetros documentales, correo y plantillas. La autoridad tenant se resuelve y valida en backend; la UI no concede scope por sí sola. La persistencia mantiene una configuración única por empresa, las mutaciones fallan cerrado ante tenant inválido o error de auditoría y el get-or-create tolera la carrera concurrente devolviendo el ganador persistido sin ocultar errores cuando no existe ganador.

## Cobertura material A–G

### N6.7.A — Auditoría y preflight

Preflight cerrado con alcance, dependencias, riesgos, rollback y estrategia de pruebas documentados. Receipt: `vaep/evidence/fragments/N6.7.A_LISTO_REAL_20260912T1033Z.json`.

### N6.7.B — Dominio y contratos

Dominio/contratos tenant-aware cerrados con REVIEW_FIRST PASS, P0=0/P1=0, DoD PASS y gates causales exact-head PASS. Receipt: `vaep/evidence/fragments/N6.7.B_LISTO_REAL_20260912T121102Z.json`.

### N6.7.C — Persistencia, migración y datos

Persistencia/migración/snapshot cerrados con integridad tenant y unicidad de configuración por empresa, REVIEW_FIRST PASS, P0=0/P1=0, DoD PASS y gates causales PASS.

### N6.7.D — Aplicación, servicios y API

Servicios/API tenant-scoped cerrados con contratos HTTP, validación server-side y auditoría fail-closed; REVIEW_FIRST PASS, P0=0/P1=0, DoD PASS y backend build/tests causales PASS.

### N6.7.E — Frontend y UX

UI de configuración tenant cerrada con contratos, formularios/estado/errores y editor de plantillas; M10 exact-head y frontend lint/build/E2E certificados, REVIEW_FIRST PASS, P0=0/P1=0 y DoD PASS. Receipt: `vaep/evidence/fragments/N6.7.E_LISTO_REAL_20260912T1410Z.json`.

### N6.7.F — RBAC, auditoría, seguridad y observabilidad

Controles tenant, permisos relacionales, auditoría fail-closed y regresiones de seguridad cerrados con REVIEW_FIRST PASS, P0=0/P1=0, DoD PASS y gates causales terminales. Receipt: `vaep/evidence/fragments/N6.7.F_LISTO_REAL_20260912T1523Z.json`.

### N6.7.G — QA, regresión y CI

G cerró sobre functional head `e83b450f549ea4e0dafbc47760e60f910aafad70`. La recuperación causal resolvió la carrera de creación concurrente de configuración tenant: el agregado perdedor se desacopla, se relee el ganador y se preservan cancelación y fallos reales. Evidencia terminal:

- REVIEW_FIRST: PASS, P0=0, P1=0.
- Release build: PASS, 0 warnings, 0 errors.
- Backend no-Integration: 2072/2072 PASS, incluyendo `N67GTenantConfigConcurrencyTests`.
- Authorization/files/secrets/tenant gate: run `34702577997`, job `103576834245`, SUCCESS.
- MySQL 8.4 isolated backup/restore proof: run `34702577997`, job `103576834156`, SUCCESS.
- Backend release/tests: run `34702577970`, job `103576834061`, SUCCESS.
- Receipt G: `vaep/evidence/fragments/N6.7.G_LISTO_REAL_20260912T0938-0600.json`.

## Invariantes certificados

1. Toda configuración pertenece a una empresa/tenant explícito y único.
2. El tenant solicitado se valida en backend; parámetros o estado de UI no son autoridad de acceso.
3. Configuración sensible no expone secretos ni convierte credenciales en datos visibles de configuración.
4. Las mutaciones conservan permisos relacionales y auditoría fail-closed.
5. Una carrera concurrente de get-or-create retorna el ganador persistido cuando existe; si no puede establecerse un ganador, el error original no se oculta.
6. `OperationCanceledException` mantiene su semántica independiente de la recuperación de conflicto.
7. Logo, moneda, zona horaria, impuestos, parámetros documentales, correo y plantillas permanecen tenant-scoped.
8. Los cambios funcionales B–G conservan evidence/receipts causales; H no modifica runtime, esquema, workflows, dependencias ni infraestructura.

## Rollback y recuperación

N6.7.H es exclusivamente documental/certificación. Su rollback es forward-only sobre documentación/evidencia; no requiere ni autoriza reset, force-push, amend o reescritura de historia. Una regresión funcional futura debe reproducirse por tenant, verificar unicidad/persistencia, permisos, auditoría y la carrera concurrente, corregirse sólo en `Desarrollo`, ejecutar pruebas dirigidas y recertificarse con REVIEW_FIRST + gates causales aplicables.

## OpenAPI / ADR / ERD / runbook

H no introduce endpoints, DTOs, entidades, migraciones ni cambios de esquema adicionales a B–G, por lo que no requiere un nuevo OpenAPI, ADR o ERD. Este documento actúa como certificación y runbook de cierre: identificar tenant y operación, reproducir el scope, validar persistencia/tenant/permisos/auditoría, corregir en `Desarrollo`, ejecutar directed tests y sólo cerrar con REVIEW_FIRST PASS, DoD PASS y P0/P1=0.

`TASKS.md` y `CHANGELOG_AI.md` son históricos y no gobiernan estado vivo. N6.7.H no cambia el contrato funcional/proyecto ya certificado por A–G; consolida documentación y evidencia, por lo que no reescribe esos históricos para fabricar un cambio de estado operativo.

## Criterio de cierre H

`N6.7.H` puede declararse `LISTO_REAL` únicamente si:

- esta certificación y la evidencia de cierre son revisadas;
- REVIEW_FIRST = PASS;
- P0=0 y P1=0;
- DoD = PASS;
- no existe delta funcional, esquema, workflow, dependencia o infraestructura posterior al functional head certificado por G sin validar;
- la matriz causal de G permanece válida por equivalencia funcional del delta documental H;
- no se ocultan fallos ni se baja ningún gate;
- `main`, Producción, deploy, secrets y merge/auto-merge de PR #2 permanecen intactos.

## Restricciones preservadas

- `TASKS_FIRST_JULES_ON_DEMAND`; sin Jules artificial, auto-refill, queue-floor, filler ni `WAIT_FOR_JULES`.
- R3 prohibido.
- Sin ramas nuevas, force-push, reset, amend, merge/auto-merge ni git destructivo.
- Sin deploy, secretos ni cambios a Producción/main.
