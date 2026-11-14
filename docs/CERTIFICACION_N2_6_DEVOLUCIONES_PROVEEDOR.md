# Certificación N2.6 — Devoluciones a proveedor

## Estado

**VALIDANDO / candidate de cierre documental.**

Este documento no convierte N2.6.H ni N2.6 en `LISTO` por sí solo. La promoción final pertenece al control-plane VAEP y exige reconciliar el HEAD que contenga este paquete con sus gates causales y con P0/P1=0.

## Alcance certificado por evidencia de repositorio

ERP-N2.6 implementa una vertical empresarial de devoluciones a proveedor con:

- agregado y detalle dedicados;
- lifecycle `Borrador → Confirmada → Anulada`;
- idempotencia de creación por `Idempotency-Key` + fingerprint;
- persistencia relacional propia;
- enlaces explícitos a proveedor, orden, recepción y factura;
- autoridad física basada en `ExistenciaVariante`;
- locks de concurrencia antes de la mutación física;
- procesador de stock y writer de Kardex coordinados por Application;
- auditoría estricta de operaciones mutables;
- API protegida por autenticación y permisos relacionales del módulo Compras;
- frontend/UX y navegación implementados en la fase N2.6.E;
- regresiones de idempotencia y seguridad en N2.6.F/G;
- migración y snapshot EF de N2.6.

## Contrato API certificado

Ruta base: `/devoluciones-proveedor`.

Permisos:

- listar/consultar → `Compras/Ver`;
- crear → `Compras/Crear`;
- editar → `Compras/Editar`;
- confirmar → `Compras/Confirmar`;
- anular → `Compras/Anular`.

La creación exige header `Idempotency-Key`. El controller está protegido por `[Authorize]` y usa ProblemDetails para errores HTTP explícitos como ausencia del header o recurso inexistente.

## Persistencia certificable

Migración:

`20260821173500_N2_6_DevolucionProveedorPersistencia`

Estructura principal:

- `DevolucionesProveedor`;
- `DevolucionProveedorDetalles`;
- unicidad de número de devolución e idempotency key;
- unicidad de línea por devolución + detalle de recepción;
- FKs externas restrictivas;
- cascade sólo cabecera → detalles;
- checks de dominio trasladados al esquema donde corresponde;
- precisión monetaria/cantidad `18,4`.

La migración incluye pre/post guards en `Up`. Su `Down()` elimina las dos tablas N2.6 y no contiene `DownGuard`; esta limitación queda tratada explícitamente en el runbook y debe formar parte del criterio de release.

## Inventario y Kardex

La certificación funcional de N2.6 depende de que confirmar/anular mantengan atomicidad entre:

1. adquisición de locks de `ExistenciaVariante`;
2. procesamiento físico de devolución/reversión;
3. Kardex;
4. cambio de estado del documento;
5. persistencia;
6. auditoría estricta.

No se acepta `ProductoVariante.Cantidad` como autoridad ni una transición documental desconectada de los efectos físicos requeridos.

## Evidencia QA requerida para el cierre

Antes de `N2.6.H = LISTO` deben estar reconciliados, sobre el HEAD estable que contenga este paquete:

- Development terminal y verde;
- Acceptance terminal y verde;
- Fase 8 terminal y verde;
- M13 terminal y verde;
- gates adicionales causales de migración/modelo cuando apliquen;
- P0=0;
- P1=0;
- REVIEW_QUEUE de supports H atendida o clasificada sin artifacts integrados por confianza;
- CONFIG/COLA/BITACORA coherentes;
- TASKS/CHANGELOG reconciliados sin declarar un cierre anterior a la evidencia.

## Limitaciones y decisiones explícitas

- El `Down()` de N2.6 es destructivo para sus tablas; no existe `DownGuard` embebido.
- El procedimiento exacto de backup/restore depende del entorno y no se afirma como implementado universalmente por este paquete.
- La documentación no autoriza Producción, deploy, merge, secretos ni cambios de infraestructura.
- Un support Jules `COMPLETED` no equivale a integración: requiere REVIEW-FIRST, scope/base/diff/evidencia y cap ATTEMPT1+R2.

## Resultado de esta certificación

El módulo está **documentalmente preparado para validación final**, pero el estado final debe permanecer `VALIDANDO` hasta que el commit documental sea publicado en `Desarrollo` y sus gates causales terminen correctamente. Sólo entonces VAEP puede marcar N2.6.H y el rollup N2.6 como `LISTO` y promover el siguiente punto elegible.