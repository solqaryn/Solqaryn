# ERP-N2.1 — SolicitudCompra

## Dictamen

**Estado:** `LISTO / CIERRE FORMAL` una vez que el commit documental de N2.1.H y su CI causal estén verdes.

**Baseline funcional certificado:** `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`.

**Tree funcional:** `ec0c9b03b42b54e8c58794a966b7be55cd3cbffc`.

**CI funcional:** `32172981351` — SUCCESS: Docker, higiene, frontend `npm ci`/lint/build, backend Release sin warnings/errores, 994/994 pruebas backend, migraciones e integración MySQL, verificaciones ERP-N1 y SQL forward. Artifact backend: `9338088182`.

Este documento es la fuente canónica final de ERP-N2.1. El preflight `docs/ERP_N2_1_SOLICITUD_COMPRA_PREFLIGHT.md` queda como antecedente histórico.

## 1. Alcance y autoridad

ERP-N2.1 introduce `SolicitudCompra` como documento empresarial independiente de `Compra`. Su propósito es solicitar y aprobar/rechazar una necesidad de compra **sin** materializar inventario, Kardex, cuentas por pagar, movimientos financieros ni costeo.

La decisión arquitectónica es deliberada: `Compra` mantiene su ciclo transaccional y `SolicitudCompra` no reutiliza `EstadoDocumento` ni hereda el contrato `ConfirmableEntity`.

Lifecycle canónico:

```text
Borrador -> Solicitada -> Aprobada
                    \-> Rechazada
```

`Aprobada` y `Rechazada` son terminales para N2.1. La creación de OrdenCompra pertenece a N2.2 y no se materializa implícitamente desde este punto.

## 2. Dominio e invariantes

El agregado preserva:

- número de solicitud;
- proveedor opcional;
- notas/observaciones documentales;
- detalles por producto y variante cuando corresponde;
- cantidad solicitada positiva;
- costo estimado unitario no negativo;
- actor y fecha de solicitud;
- actor, fecha y resultado de decisión;
- motivo obligatorio y normalizado al rechazar.

Reglas fail-closed certificadas:

- no se puede solicitar un documento sin detalles válidos;
- una transición inválida no deja mutación parcial del estado;
- después de solicitar, el documento deja de ser editable como borrador;
- aprobar/rechazar exige estado `Solicitada`;
- una decisión terminal no admite segunda decisión;
- rechazo sin motivo falla sin persistir éxito falso;
- snapshots de nombre de actor se normalizan.

La regresión contractual mantiene valores estables e independientes para `EstadoSolicitudCompra`: `Borrador=1`, `Solicitada=2`, `Aprobada=3`, `Rechazada=4`.

## 3. Persistencia e integridad

N2.1.C incorporó persistencia EF, constraints, índices, migración y snapshot para SolicitudCompra y sus detalles. Cierre registrado en `5aaab004f9e56f79d4e2fa0580c5bca9687e8519`, precedido por `282e4c546de3a116fcc85df34878bf236b240b24`, `8a9d83bb71eb374586a451770e57f99b44189530` y `f95d1bb02ccc2a77fc2d8f07d31368075c620a1c`.

La migración fue aplicada/certificada en MySQL 8.4 dentro de CI. No se ejecutó DDL/DML en Producción.

## 4. Aplicación, API y concurrencia

N2.1.D cerró el servicio/API en `01770a23cbf9a50e7d21a0a7913f32e31ce6070a`.

Update, Enviar, Aprobar y Rechazar se serializan con `IUnitOfWork` y bloqueo pesimista `SELECT ... FOR UPDATE`, evitando decisiones concurrentes y stale writes sobre el mismo documento.

La API expone operaciones de consulta/paginación/filtros y lifecycle separadas de la Compra transaccional. Los errores de negocio se mantienen fail-closed y el contrato HTTP usa la infraestructura común de errores de Solqaryn.

## 5. Frontend y UX

N2.1.E se cerró en E.1/E.2/E.3:

- `f52f9f746427d18675073ba769c2a78c2f13d900`: contrato TypeScript, servicio `/solicitudes-compra`, rutas y navegación separada;
- `112ef6b8660fb12c80d6981eac81b55f6c32bdec`: listado, filtros, estados y detalle;
- `fe82487c6f4483cf9601dc60cac819648fdd33f4` + `07275df6af316aff83f250c6cf9d9b1b1ad335d3`: crear/editar borrador, selectores, líneas, lifecycle y validación fail-closed.

La UI respeta loading/error/vacío, responsive, accesibilidad básica y permisos runtime.

## 6. RBAC, auditoría, seguridad y observabilidad

N2.1.F quedó cerrado mediante:

- `d3f039efafe0bf7ccfd487ba4ca7c66e07625fc3`: RBAC relacional fail-closed;
- `adea50ac65bacceff42cd23c110afea77817ca44`: auditoría documental transaccional;
- `12b26459004dc01a17b5b2af4602dbb906470bae`: observabilidad, correlation-id, health y configuración segura.

Permisos usados por la superficie SolicitudCompra: `Compras:Ver`, `Compras:Crear`, `Compras:Editar`, `Compras:Confirmar`, `Compras:Aprobar` y `Compras:Rechazar`, según la operación. No existe bypass efectivo por `EsAdministrador`: la autorización depende de grants relacionales.

Crear/Editar/Enviar/Aprobar/Rechazar dejan auditoría con actor, fecha, entidad/referencia y estado relevante; las transiciones críticas se registran dentro de la misma unidad transaccional para evitar éxito funcional sin evidencia de auditoría.

`CorrelationId` válido se propaga a la solicitud/respuesta y logging; valores vacíos/inseguros/excesivos se sustituyen por un identificador seguro. No se introducen secretos en el repositorio.

## 7. QA y certificación

Cobertura específica relevante:

- `backend/tests/Solqaryn.Tests/N21SolicitudCompraContractTests.cs`;
- `backend/tests/Solqaryn.Tests/N21SolicitudCompraDomainRegressionTests.cs`;
- regresiones de API/concurrencia/RBAC/auditoría incorporadas durante D y F;
- build/lint/frontend y aceptación Playwright dentro de gates causales E/F/G.

Cierre G: `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`, CI `32172981351` SUCCESS y 994/994 pruebas backend.

El check de Vercel `solqaryn-desarrollo` puede aparecer limitado por build-rate-limit externo; no es el gate de compilación backend/MySQL/Angular utilizado para certificar N2.1.G. La aplicación `SOLQARYN` sí reportó status success en el baseline consultado.

## 8. Trazabilidad A-H

- **N2.1.A** — preflight y decisión de autoridad independiente: `13ea9a853885e1242d6511cdeadfa722726e4aff`.
- **N2.1.B** — dominio/contratos: `464a7bf0ddc410dc95810b1ee9cc4c762fc523d4`.
- **N2.1.C** — persistencia/migración: `5aaab004f9e56f79d4e2fa0580c5bca9687e8519`.
- **N2.1.D** — aplicación/API/concurrencia: `01770a23cbf9a50e7d21a0a7913f32e31ce6070a`.
- **N2.1.E** — frontend/UX: hasta `07275df6af316aff83f250c6cf9d9b1b1ad335d3`.
- **N2.1.F** — RBAC/auditoría/seguridad/observabilidad: hasta `12b26459004dc01a17b5b2af4602dbb906470bae`.
- **N2.1.G** — QA/regresión/CI: `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`, run `32172981351` SUCCESS.
- **N2.1.H** — este paquete documental/certificación, `TASKS.md`, `CHANGELOG_AI.md` y tablero VAEP reconciliados al cierre.

## 9. DoD N2.1

Se considera cumplido únicamente cuando:

- A-G permanecen `LISTO` con evidencia real;
- lifecycle e invariantes están protegidos por tests;
- persistencia/migración e integración MySQL están verdes;
- backend/API y frontend están compilados/probados;
- RBAC/auditoría/correlation-id están certificados;
- este documento canónico, runbook y ADR están publicados;
- `TASKS.md`, `CHANGELOG_AI.md`, COLA/BITACORA/CONFIG quedan reconciliados;
- el commit documental tiene CI causal suficiente o se demuestra que solo dispara gates documentales sin invalidar el baseline funcional.

## 10. Rollback y recuperación

### Operación

No “recuperar” una solicitud aprobada/rechazada editando estado directamente en base de datos. N2.1 no define reapertura. Un error operativo debe conservar historia y resolverse mediante un documento posterior conforme al proceso autorizado.

### Código

Revertir forward mediante commit sobre `Desarrollo`; no force-push. Si el rollback cruza una migración, comprobar compatibilidad del esquema y datos antes de cambiar código.

### Esquema

No ejecutar `Down` destructivo sobre datos reales como mecanismo normal. Preferir corrección forward o restauración desde backup certificado en ambiente autorizado. Producción queda fuera de alcance.

## 11. Dependencia siguiente

`N2.2.A — Orden de compra — Auditoría y preflight` depende de N2.1.H. No debe abrirse hasta que este cierre documental esté publicado, CI/checkpoint causal revisado y tablero marcado `LISTO`.

## 12. Límites

Este cierre no autoriza merge a `main`, auto-merge del PR #2, ramas nuevas, force-push, despliegues productivos, cambios de secretos o infraestructura de Producción. Tampoco modifica ni duplica el scope exclusivo del worker Jules activo.
