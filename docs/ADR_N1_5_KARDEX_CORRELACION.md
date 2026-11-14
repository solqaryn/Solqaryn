# ADR — ERP-N1.5 Kardex: correlación durable y clave física

- Estado: Aceptado
- Fase: ERP-N1.5
- Rama de certificación: `Desarrollo`
- Autoridad técnica: implementación y CI del repositorio

## Contexto

El Kardex empresarial necesita correlacionar movimientos derivados de una misma operación sin convertir headers HTTP, textos legacy o identificadores efímeros en autoridad de negocio. También debe conservar el contexto físico real de inventario sin inventar Almacén/Ubicación cuando una operación legacy todavía no lo proporciona.

## Decisión

1. `CorrelationId` se persiste como dato durable y saneado en el movimiento de inventario.
2. Las transiciones documentales generan correlaciones determinísticas por operación y acción, diferenciando al menos confirmación y anulación.
3. `IKardexMovimientoWriter` es el boundary canónico de escritura del Kardex.
4. La identidad física de stock permanece en `ExistenciaVariante` mediante `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`.
5. El Kardex registra esas dimensiones cuando existen realmente; para consumidores legacy, Almacén/Ubicación permanecen nullable en lugar de inferirse o fabricarse.
6. `ProductoVariante.Cantidad` no vuelve a ser autoridad de stock; se conserva únicamente como bridge agregado donde el cutover aún lo requiera.
7. La consulta se optimiza con índices compuestos alineados con filtros y orden temporal, manteniendo migraciones reversibles.

## Consecuencias

### Positivas

- trazabilidad extremo a extremo entre petición, auditoría y movimientos;
- menor riesgo de duplicidad o ambigüedad entre confirmar/anular;
- consultas por dimensión física/origen/correlación index-friendly;
- compatibilidad progresiva con históricos que no tienen contexto físico completo;
- separación clara entre autoridad de stock vivo (`ExistenciaVariante`) e historial (`MovimientoInventario`).

### Costos y restricciones

- los productores deben usar el writer canónico y no persistir movimientos ad hoc;
- los contratos legacy requieren compatibilidad nullable hasta completar sus cutovers físicos;
- retirar índices o `CorrelationId` exige migración/rollback explícitos;
- no se autoriza derivar un almacén o ubicación por conveniencia cuando el documento origen no los provee.

## Seguridad y observabilidad

El identificador de correlación usado por auditoría y Kardex debe provenir del pipeline saneado. Un valor HTTP bruto o inseguro no se persiste directamente como autoridad de trazabilidad. El aislamiento de lectura continúa sujeto a autenticación, permisos relacionales y `UsuarioScope` fail-closed.

## Validación

La decisión quedó cubierta por las pruebas de N1.5 C–G y por `Desarrollo - Compilación y pruebas #31918223873`, `SUCCESS` sobre `4871da115e72d205513ea23aa9fe95c1e4818e6b`, incluyendo backend/unitarias, frontend, Docker, migraciones e integración MySQL.

La optimización de consulta quedó materializada mediante la migración reversible `20260816005000_N1_5_KardexQueryIndexes`.

## Rollback

El rollback se realiza por reversión explícita en `Desarrollo`, nunca mediante force-push. Las migraciones de índices se revierten sólo después de comprobar que ninguna consulta o despliegue depende de ellas. Ninguna acción de este ADR autoriza cambios en Producción o merge del PR #2.
