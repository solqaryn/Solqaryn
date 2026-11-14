# SOLQARYN V5 — C10-04.M01.02: actores, entradas, salidas y fronteras de caché, jobs, backups y exportaciones

**Unidad:** C10-04.M01.02 · PLAN_MAESTRO fila 1551 · **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. M01.02 prepara **actores, entradas/salidas y fronteras** para aislar jobs/cache/backup/exports por tenant; estos contratos están **PROPUESTOS_NO_IMPLEMENTADOS** y no autorizan ejecutar servicios externos. `C10-04.M01.01` estaba EN_PROGRESO bajo otra Task al iniciar esta unidad: su alcance no se declara aprobado ni se invaden archivos de su autor.

## Evidencia de diseño anterior, sin inferir activación

- `docs/v5/c10-01-m01-02-actores-superficies-storage-neutral.md` (blob `6d18023f7bb42685cce150b6bbe29f1fa20c30c8`): separación Browser/API/Application/Infrastructure, Cloudinary transición y R2 objetivo futuro.
- `docs/v5/c10-02-m01-02-actores-interfaces-r2-dominios.md` (blob `185471a4de72653b6e3a199ee7a8aaa34ef3ea88`): prefijos, URLs y dominios de archivo como contratos sin aprovisionamiento.
- `docs/v5/c10-03-m01-02-actores-entradas-interfaces-inventario-almacenamiento.md` (blob `dde15b96f610dcd0b7712129e69bcba04a8586bd`): **INVENTARIO_DESCONOCIDO ≠ INVENTARIO_CERO**. No se conocieron los archivos reales de clientes por leer el repo.
- La lectura de documentos de otras etapas no sustituye los gates dinámicos de PLAN/COLA/DAG ni pruebas exact SHA. La arquitectura actual es Angular 22.2.1 / ASP.NET Core net10.0 / EF Core 10.0.12 + MySQL 8.4, con control de autorización server-side.

## A01–A14: actores y superficie prevista (sin nuevos permisos)

| ID | Actor o componente | Responsabilidad y límite obligatorio |
| --- | --- | --- |
| A01 | Usuario autenticado empresa A | Solicita descarga/estado propio, sin arbitrar tenant ni permisos |
| A02 | Usuario empresa B | Sin lectura ni inferencia de datos/cache/job/export de A |
| A03 | Administrador empresa | Opera sólo políticas tenant propias mediante RBAC API, no infraestructura global |
| A04 | Operador global autorizado | Acción extraordinaria sólo con autorización auditada y justificación |
| A05 | Interfaz Angular 22.2.1 | Muestra progreso, errores y confirmación; no confiere autoridad |
| A06 | API .NET 10 | Valida JWT, recurso, tenant, permiso, cuota y rechazo antes de encolar |
| A07 | Servicio Application | Orquesta casos de uso tipados sin dependencia directa de SDK cloud |
| A08 | Repositorio/EF MySQL | Posible almacenamiento de metadatos y estados, sin activar migración |
| A09 | Cache lógico por tenant | Futura aceleración con claves segregadas, TTL y revocación |
| A10 | Job/worker eventual | Futuro ejecutor idempotente con lease, cuota y control de cancelación; NO ACTIVADO |
| A11 | Sistema de backup/restore | Futuro responsable de manifiesto, hash, recuperación y pruebas aisladas |
| A12 | Almacenamiento actual Cloudinary | Dependencia de transición declarada; estado real de objetos NO_VERIFICADO |
| A13 | R2/almacenamiento futuro | Propuesta no aprovisionada ni autorizada en esta M01 |
| A14 | Auditor/revisor independiente | Observa evidencia sanada, nunca copia tokens, PII o datos vivos |

Todos los actores futuros requieren contratos de propiedad, retención, scopes y autorización efectivos. **No existe alta de worker, bucket, cron, webhook, cache distribuida, proveedor o cuenta por redactarlos**. No se presupone que los medios históricos sean cero, positivos o accesibles.

## I01/O01–I12/O12: entradas/salidas candidatas

| Par | Entrada futura tipada | Salida / verificación requerida |
| --- | --- | --- |
| I01/O01 | Principal JWT y operación | Decisión backend de acceso/denegación por tenant y permiso |
| I02/O02 | Recurso cacheable y versión | Clave tenant-scoped con TTL y invalidación comprobable |
| I03/O03 | Solicitud de trabajo idempotente | ID opaco, estado y clave única por tenant sin duplicar efectos |
| I04/O04 | Reintento de trabajo autorizado | Reprogramación acotada, contador y error seguro |
| I05/O05 | Cancelación de job propio | Cancelación auditada sin borrar evidencia obligatoria |
| I06/O06 | Pedido de exportación | Manifiesto propio minimizado, límites y expiración |
| I07/O07 | Referencia a objeto existente | Metadatos sólo del tenant; inventario real NO_VERIFICADO |
| I08/O08 | Petición de backup autorizada | Snapshot/manifiesto con checksum y retención definida |
| I09/O09 | Prueba de restore en efímero | Resultado de integridad sin modificar BD productiva |
| I10/O10 | Estado de progreso o error | Respuesta segura sin exponer PII, bucket, tokens ni rutas internas |
| I11/O11 | Política de proveedor por entorno | Config tipada, deny-by-default sin secreto en documento |
| I12/O12 | Solicitud de deshabilitar feature | Estado reversible preservando historial, leases y auditoría |

Un ID opaco no equivale a autorización. El backend debe resolver el tenant real y comprobar cada recurso. Una respuesta de error o caché no puede usar datos de otro tenant ni exponer estado de privilegio de un tercero.

## F01–F09: fronteras de confianza

| ID | Cruce técnico | Control obligatorio |
| --- | --- | --- |
| F01 | Browser/Angular → API | Nunca confiar en EmpresaId, slug, host, cookie o cabecera para owner |
| F02 | API → Application | JWT/RBAC/resource/tenant y validación DTO antes de trabajos o export |
| F03 | Application → Cache | Claves por tenant, versión de permiso, TTL, invalidación y no contaminación A/B |
| F04 | Application → Job | Idempotencia, una sola ejecución causal, CAS lease y sin duplicados |
| F05 | Job → MySQL/EF | Transacción, aislamiento de tenant, límites, errores y rollback probado |
| F06 | Job → Proveedor externo | Credenciales mínimas por entorno, cuotas, sin invocar R2 no autorizado |
| F07 | Backup → Restore | Manifiesto, hash, snapshot aislado, permiso explícito y no producción |
| F08 | Export → Usuario | URL corta, alcance por recurso, privacidad, saneado y revocación |
| F09 | Errores/logs → auditoría | Correlación mínima, sin secretos, datos personales o URL bearer |

La interfaz Angular es sólo presentación. Servidor, job, caché y almacenamiento deben fallar cerrado ante JWT inválido, permiso revocado, versión vieja, cabecera host no fiable, reintento duplicado, pérdida de red, corrupción de copia o datos legados sin owner. Los backups de clientes requieren prueba de restauración sobre un ambiente efímero antes de cualquier decisión de migración real.

## Matriz FUTUROS_NO_EJECUTADOS para M02–M05

| Caso | Positivo futuro | Negativo necesario |
| --- | --- | --- |
| P01/N01 | Consulta cache A retorna sólo A | Clave o sesión B no revela entradas A |
| P02/N02 | Trabajo autorizado ejecuta una vez | Retry o carrera no produce doble side effect |
| P03/N03 | Export propio vence con TTL | B o token viejo no obtiene export A |
| P04/N04 | Restore efímero confirma checksum | Snapshot corrupto o tenant ajeno falla cerrado |
| P05/N05 | Revocación invalida cache/job futuro | Permiso revocado no mantiene acceso mediante cache |
| P06/N06 | Fallo de proveedor ofrece error reversible | Timeout o excepción no libera dato/acción por defecto |

Estos P/N son **casos diseñados, no pruebas realizadas**. La demostración posterior requerirá código real, xUnit/HTTP de tenant A/B, MySQL efímero, CI exact SHA y pruebas de recuperación; para UX, npm build/Playwright accesibilidad según aplique. No se afirmó PASS de runtime por este documento.

## Impacto de seguridad, dependencias y reversión

**Ahora:** sólo este archivo y sección autocontenida de `CHANGELOG_AI.md`. No se alteran Domain, Application, Infrastructure, API, DTO, MySQL/EF, frontend, secretos, entidades, IAM/JWT/RBAC, proveedores, DNS, nube ni datos vivos. **Futuro:** diseñar políticas tipadas de cache/jobs/retención y RBAC sin acceso desde `EmpresaId`/slug/header; auditoría sin PII, cotas de retry, cuotas y restore verificable.

**Gobernanza:** A0 inventario de contratos y límites; A1/A2 escenarios documentales positivos/negativos y aislamiento; A3 coherencia con C10-01/02/03; A4 dos Markdown por fast-forward CAS con Scope Lock y comprobación V5 en SHA exacto; A5 independiente por otra Task Javier. Rollback mediante commit `git revert` ordinario del changeset específico tras revisar HEAD, sin reescribir historial ni BITACORA. Ninguna de estas actividades habilita QA, main, PROD, Hostinger, Cloudflare R2, compras, migraciones ni ejecuciones entre Tasks.
