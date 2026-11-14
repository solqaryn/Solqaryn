# ROLLBACK — N2.5 Three-Way Match

## Principio
Rollback fail-closed. Separar reversión de código de reversión estructural de datos.

## Reversión lógica / código
Si existe defecto funcional, priorizar revertir/deshabilitar la superficie N2.5 sin tocar datos históricos. Esto puede incluir endpoint, DI/servicio y frontend correspondiente mediante un cambio controlado en Desarrollo, seguido por CI causal. No se ejecuta despliegue productivo desde VAEP.

## Reversión estructural — DESTRUCTIVA
La migración `20260821053500_N2_5_ThreeWayMatchPersistencia` implementa en `Down()`:
1. `DropTable("ThreeWayMatchDiscrepancias")`.
2. `DropTable("ThreeWayMatchResultados")`.

Por tanto, un rollback estructural elimina toda evidencia persistida de N2.5. No existe DownGuard que impida la operación cuando hay datos.

La migración N2.5 no elimina `OrdenesCompra`, `RecepcionesCompra` ni `FacturasProveedor`, pero la pérdida de las tablas N2.5 sigue siendo una pérdida real de auditoría/conciliación.

## Precondiciones obligatorias antes de cualquier Down
1. Autorización humana explícita para operación destructiva.
2. Quiescencia de escrituras del módulo.
3. Backup/export verificable de `ThreeWayMatchResultados` y `ThreeWayMatchDiscrepancias`.
4. Validación del backup y recuentos/checksums o reconciliación equivalente.
5. Plan de restore documentado y probado.
6. Postchecks definidos para esquema, datos y contratos upstream.
7. Criterio de ABORT: si falta cualquiera de los controles anteriores, no ejecutar `Down()`.

## Restore / postcheck
Después de una reversión autorizada, verificar explícitamente versión de migración, existencia/ausencia esperada de objetos, integridad de N2.2/N2.3/N2.4 y posibilidad de restaurar evidencia N2.5 desde el backup. No declarar rollback exitoso solo porque el comando terminó.

## Restricción operativa
Este documento no autoriza operaciones en Producción. VAEP/ChatGPT/Jules no ejecutan rollback productivo ni cambios de infraestructura productiva.
