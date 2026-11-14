# Certificación N4.6 — Plan de cuentas

## Alcance y autoridad

Esta certificación documenta el cierre del plan de cuentas jerárquico de Solqaryn
en la rama `Desarrollo`. El exact-head funcional evaluado fue
`9d649bbbb4279e41e8cf5b7f5f9b84c26cc362bf`.

La implementación reutiliza la entidad `CuentaContable`, el enum
`TipoCuentaContable`, la persistencia EF existente, el repositorio y los
patrones de autorización/auditoría del proyecto. No crea catálogo contable
legal especulativo ni modifica producción.

## Gates A-H

| Gate | Resultado | Evidencia real |
|---|---|---|
| N4.6.A | LISTO_REAL | Preflight y dependencia N4.6.B confirmados por el handoff de control. |
| N4.6.B | LISTO_REAL | `2658d5b0139e85957463cb227f11ea65f42bef13`, dominio `CuentaContable` y `TipoCuentaContable`. |
| N4.6.C | LISTO_REAL | Persistencia, relación jerárquica, migración MySQL 8.4 y snapshot; cierre autoritativo en `12542f37132dfd4488e27e197ef548af19dee337`. |
| N4.6.D | LISTO_REAL | DTOs, servicio, repositorio, árbol jerárquico, validación de padre/ciclos y endpoints protegidos en `a4392c44` + `9d649bbb`. |
| N4.6.E | LISTO_REAL | Feature Angular `/plan-cuentas`, árbol, alta/edición, validaciones, estados de UI y permisos en `9d649bbb`. |
| N4.6.F | LISTO_REAL | `[Authorize]`, permisos `Finanzas`, auditoría y contrato RBAC; validación runtime/seguridad del exact-head exitosa. |
| N4.6.G | LISTO_REAL | Development `#33828121004`, aceptación `#33828121038`, Fase 8 `#33828121029`, M13 `#33828121086` y M10 `#33828121034`: todos `SUCCESS`. |
| N4.6.H | LISTO_REAL | Esta certificación append-only, con operación y rollback documentados; P0/P1 atribuibles al alcance: `0/0`. |

`Solqaryn CI` quedó `SKIPPED` y no se usa como PASS. Fase 2 terminó con un
fallo externo de `npm audit` por HTTP 503 de `registry.npmjs.org`; no fue una
regresión causal del changeset y no se altera el código para ocultarlo.

## Operación y rollback

La migración de `CuentaContable` es forward-only sobre ambientes descartables
de CI y debe aplicarse mediante el flujo EF aprobado. Antes de operar un
ambiente no descartable se debe verificar backup, historial
`__EFMigrationsHistory`, snapshot y revisión humana; esta certificación no
autoriza migraciones productivas.

El rollback operativo es restaurar el ambiente desde el backup aprobado o
ejecutar el procedimiento de recuperación del ambiente de Desarrollo. No se
autoriza eliminar datos, revertir historia Git ni ejecutar `Down` contra
Producción. Los cambios de cuentas deben corregirse mediante una nueva
migración compatible y revisión de integridad jerárquica.

## Cierre

El padre N4.6 queda cerrado como `LISTO_REAL` sobre la evidencia indicada,
con `P0=0` y `P1=0`. El siguiente parent dependency-valid es N4.7; este
changeset no inicia su scope.
