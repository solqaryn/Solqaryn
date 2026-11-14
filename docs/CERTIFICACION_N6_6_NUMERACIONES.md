# N6.6 — Numeraciones — Certificación canónica

## Estado

- Parent: `N6.6`
- Microtarea de cierre: `N6.6.H — Documentación y certificación`
- Rama autorizada: `Desarrollo`
- Base de cierre documental: `b7e18759773800dc81aaec15a6b319b1b8fba895`
- Predecesor: `N6.6.G = LISTO_REAL`
- Functional head certificado por G: `72e9e9d36a4c69b4dfcc612a087f9119ac04718d`
- Política: `docs/VAEP_AUTHORITY.md`
- Producción/main/deploy/secrets: fuera de alcance y no autorizados.

## Objetivo certificado

N6.6 implementa numeraciones documentales independientes y tenant-aware por empresa, sucursal opcional y tipo documental. La identidad semántica de una secuencia es `EmpresaId + SucursalId? + TipoDocumento`; una secuencia de empresa puede usar sucursal nula y una secuencia por sucursal queda aislada de las demás. La reserva del siguiente número es monotónica y atómica, valida el tenant y la sucursal en servidor, y no usa `MAX()+1`, memoria local ni un contador compartido entre empresas.

## Cobertura material A–G

### N6.6.A — Auditoría y preflight

Se establecieron alcance, dependencias y riesgos antes de implementación. El preflight fijó como invariantes la separación por tenant, la sucursal opcional, el tipo documental explícito, la concurrencia segura y el cierre fail-closed.

### N6.6.B — Dominio y contratos

Los contratos de numeración exponen `EmpresaId`, `SucursalId?` y `TipoDocumento`; la configuración incluye prefijo y longitud de número, y la lectura expone el último valor y estado activo. `SucursalId` sólo es opcional para numeraciones configuradas al nivel empresa.

### N6.6.C — Persistencia, migración y datos

La persistencia usa `SecuenciasDocumento` con FK restrictiva a empresa y sucursal. Para representar de forma segura la sucursal opcional en MySQL se materializa `SucursalScopeKey = IFNULL(SucursalId, 0)` y se aplica unicidad física `EmpresaId + SucursalScopeKey + TipoDocumento`. El modelo mantiene `UltimoValor`, `Prefijo`, `LongitudNumero` y `Activa`, y la migración/snapshot EF quedaron alineados con el modelo de concurrencia certificado.

### N6.6.D — Aplicación, servicios y API

`SecuenciaDocumentoService` valida empresa y sucursal contra el scope tenant del usuario. La reserva se realiza dentro de una transacción corta mediante compare-and-swap sobre `UltimoValor`, con reintentos acotados de concurrencia. Una secuencia inexistente, inactiva, agotada o un scope tenant/sucursal inválido falla cerrado. El API expone los contratos tenant-aware sin convertir parámetros del cliente en autoridad.

### N6.6.E — Frontend y UX

El frontend incorpora contratos, servicio y tarjeta de configuración de numeración dentro de Configuración, con flujo tenant-aware y pruebas dirigidas. La UI representa la configuración; la autoridad y validación del tenant permanecen en backend.

### N6.6.F — RBAC, auditoría, seguridad y observabilidad

La reserva registra auditoría dentro de la misma transacción que consume el número: si la evidencia de auditoría no puede persistirse, la reserva tampoco se confirma. La evidencia incluye usuario, empresa, sucursal/punto de emisión, tipo documental, valor anterior, valor siguiente, origen y canal. Los guards de permisos revalidan el tenant en servidor antes de evaluar autorización, preservando fail-closed.

### N6.6.G — QA, regresión y CI

El cierre G certificó la cohorte causal tenant-aware sobre el functional head `72e9e9d36a4c69b4dfcc612a087f9119ac04718d`:

- REVIEW_FIRST: `PASS`, `P0=0`, `P1=0`.
- DoD: `PASS`.
- ERP-N1.1 Sucursales: run `34686602971 = SUCCESS`.
- ERP-N1.2 Almacenes: run `34686602931 = SUCCESS`.
- M12 Automatización transversal: run `34686602982 = SUCCESS`.
- Gates causales exact functional head: `0 queued`, `0 in_progress` al cierre G.
- El fallo M10 de accesibilidad quedó documentado como `NON_CAUSAL_BASELINE_EQUIVALENT`, con el mismo fallo presente antes del delta revisado; no fue ocultado ni reclasificado como PASS.
- Receipt G: `vaep/evidence/fragments/N6.6.G_LISTO_REAL_20260912T0954Z.json`.

## Invariantes de numeración

1. Toda secuencia pertenece a una `EmpresaId` explícita.
2. `SucursalId` puede ser nula sólo para scope de empresa; una sucursal informada debe pertenecer a la empresa activa solicitada.
3. El tipo documental es obligatorio, normalizado y forma parte de la identidad de la secuencia.
4. No pueden coexistir dos secuencias físicas para el mismo `EmpresaId + sucursal-scope + TipoDocumento`.
5. La reserva es monotónica y atómica; nunca depende de `MAX()+1`, memoria local o un contador global compartido.
6. Una secuencia inactiva, inexistente o fuera del tenant válido falla cerrado.
7. La autoridad tenant se valida en backend; el estado o parámetros del frontend no conceden acceso por sí solos.
8. La auditoría de una reserva exitosa se confirma en la misma transacción que el incremento del contador.
9. Los permisos se evalúan después de revalidar el tenant solicitado en servidor.
10. Los cambios funcionales B–G conservan evidencia y receipts causales; H no altera su comportamiento.

## Rollback y recuperación

N6.6.H es documental/certificación y no introduce cambios funcionales, de esquema ni de infraestructura. Su rollback consiste únicamente en una corrección forward-only de documentación/evidencia si se detecta un defecto documental; no requiere ni autoriza reset, force-push, amend o reescritura de historia.

Ante una regresión funcional de numeración:

1. identificar exact head, empresa, sucursal y tipo documental afectados;
2. reproducir el scope tenant y confirmar la membresía/empresa solicitada;
3. verificar unicidad física y snapshot/migración de `SecuenciasDocumento`;
4. verificar compare-and-swap, transacción y auditoría de la reserva;
5. corregir únicamente en `Desarrollo`;
6. ejecutar pruebas dirigidas y gates causales aplicables;
7. ejecutar REVIEW_FIRST y no certificar con `P0/P1 > 0`.

## OpenAPI / ADR / ERD / runbook

H no cambia endpoints, DTOs, modelo de dominio ni esquema respecto de B–G, por lo que no introduce un nuevo cambio OpenAPI, ADR o ERD. Esta certificación consolida el contrato ya implementado: scope `EmpresaId + SucursalId? + TipoDocumento`, unicidad física tenant-aware, reserva monotónica/atómica y auditoría transaccional. Cualquier cambio futuro de contrato o esquema deberá generar su propia evidencia causal y documentación específica.

El runbook de recuperación de H es el procedimiento anterior: reproducir por scope, validar tenant, verificar persistencia/concurrencia/auditoría, corregir en `Desarrollo` y recertificar con REVIEW_FIRST + gates causales.

## Criterio de cierre H

`N6.6.H` puede declararse `LISTO_REAL` únicamente si:

- esta certificación y la evidencia de cierre son revisadas;
- REVIEW_FIRST resulta `PASS`;
- `P0=0` y `P1=0`;
- DoD resulta `PASS`;
- no existe delta funcional posterior al functional head certificado por G sin validar;
- la cohorte causal de G permanece válida o el exact-head documental demuestra equivalencia de control/evidencia;
- cualquier fallo no causal permanece visible y sustentado contra baseline, sin bajar gates;
- PR #2 permanece `OPEN + DRAFT`, sin merge ni auto-merge;
- `main`, Producción, deploy y secrets continúan intocados.

## Restricciones preservadas

- `PARENT_CLOSE_FIRST=TRUE`.
- `TASKS_FIRST_JULES_ON_DEMAND`: sin Jules artificial, auto-refill ni filler.
- R3 prohibido.
- Sin ramas nuevas, force-push, reset, amend, merge/auto-merge ni git destructivo.
- Sin deploy, secretos ni modificaciones a Producción/main.
- PR #2 debe permanecer abierto, Draft y sin merge.
