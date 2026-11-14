# Arquitectura padre → hijo — Solqaryn

## Regla estructural
Toda capacidad navegable debe pertenecer a una jerarquía inequívoca:

`DOMINIO/PADRE → SUBMÓDULO/HIJO → INTERFAZ/ACCIÓN`.

El menú no es la fuente de arquitectura; refleja el catálogo canónico autorizado por backend/permisos. Una entrada no debe existir duplicada en padres distintos salvo alias explícito documentado.

## Capas
### Frontend
- Shell y navegación compartidos.
- Features agrupadas por dominio, no por accidentes históricos.
- Primitives visuales reutilizables.
- Sin reglas de autorización confiadas sólo al cliente.
- Sin duplicados funcionales cuando una abstracción compartida cubra el contrato real.

### Backend
- Casos de uso y políticas autoritativas.
- DTO/API por contrato real.
- RBAC, tenant isolation, idempotencia y auditoría fail-closed.
- No replicar lógica crítica en frontend.

### Base de datos
- Integridad y relaciones expresadas mediante constraints/índices/FKs cuando corresponda.
- Migraciones verificables y reversibilidad/restore definidos.
- No borrar estructuras por limpieza estética sin demostrar que no existe uso histórico/runtime.

### Documentación
- Una fuente canónica por decisión/contrato.
- Históricos/evidencias se preservan; no se borran para hacer parecer limpio el estado.
- Documentos superseded se marcan o archivan según política, nunca se usan como autoridad vigente.

## Alertas y confirmaciones
Debe existir una infraestructura global reutilizable de alerta/confirmación. La implementación base soportará semánticas `INFO`, `SUCCESS`, `WARNING`, `ERROR`, `CONFIRM` mediante configuración; no se crea un modal de alerta por pantalla.

Los diálogos complejos de negocio pueden existir como contratos propios, pero reutilizan la primitive global para confirmaciones/alertas. Las notificaciones transitorias se canalizan por un servicio compartido de toast/snackbar.

## Limpieza quirúrgica
Un candidato a eliminación debe pasar: referencias estáticas → rutas/imports → DI/API → tests → runtime/config → BD/migraciones/histórico → decisión. Resultado: `KEEP | CONSOLIDATE | DEPRECATE | REMOVE_SAFE | UNKNOWN`.

`UNKNOWN` prohíbe eliminar.
