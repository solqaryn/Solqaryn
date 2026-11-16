# SOLQARYN V17 — C11-01.M02.05: eventos, idempotencia, correlación y límites del modelo relacional

**ID:** `C11-01.M02.05` · **PLAN_MAESTRO:** fila 1596 · **Autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`) · **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT · **Clasificación:** R0 documental, no modifica runtime.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Skill canónica, AGENTS, calidad V15, continuidad V16 y candado V17; PLAN/COLA/DAG_TECNICO vigentes. **Requisito de la microtarea:** definir eventos, idempotencia, correlación y límites para el modelo relacional normalizado C11-01 con claves/relaciones/ownership, integridad y compatibilidad futura.

## 1. Fuentes y delimitación

- `C11-01.M01.01–06`: objetivo, superficies, inventario, supuestos, criterios, gates y rollback; un contrato documental no autoriza acceso MySQL, modificación de esquema, worker o despliegue.
- `docs/v5/c11-01-m02-01-entidades-ownership-relaciones-invariantes.md`: E01–E16, ownership, legacy nullable, 16 invariantes y 12 P/N futuros.
- `docs/v5/c11-01-m02-02-api-dto-errores-validacion-compatibilidad.md`: operaciones futuras OP01–OP08, DTO propuestos D01–D12, errores ER01–ER16, sin endpoints nuevos.
- `docs/v5/c11-01-m02-03-persistencia-indices-migraciones-datos-prueba.md`: doce claves/índices candidatos K01–K12, ocho etapas futuras de migración, seis fixtures sintéticos y 12 P/N no ejecutados.
- Gobierno del claim: antes de escribir, PLAN H1596=PENDIENTE/I vacío, COLA G/O=SI, DAG fila 1557 F=VALIDADO/G=SI; candado V17 `SQV17_C11_01_M02_05`, ID `2000001596`; CAS H `occurrencesChanged=1`, I=TASK_ID y readback. Solo esta unidad EN_PROGRESO propia.

**Arquitectura vigente:** Domain ← Application ← Infrastructure, API composition root; ASP.NET Core 10 y EF Core 10.0.12 con Oracle MySql.EntityFrameworkCore 10.0.9/MySQL 8.4. Esta propuesta no introduce un broker, bus, scheduler, outbox físico, worker, transacción, entidad o mensaje real. Los nombres de eventos son contratos futuros, **no mensajes publicados**. Ninguna fila de Aiven, secretos, JWT, cliente o ambiente productivo se toca.

## 2. Doce eventos conceptuales, NO IMPLEMENTADOS

Todos llevan `schemaVersion`, `eventId`, `correlationId`, `causationId`, `recordedAtUtc`, `actorRef`, `scopeRef`, `aggregateVersion`, `idempotencyDigest` y un `payload` mínimo/allowlisted. El evento sólo sería emitido después del commit confirmado de una operación autorizada; un intento rechazado nunca se transforma en hecho de negocio exitoso.

| ID | Evento futuro | Aggregate y condición de emisión futura |
| --- | --- | --- |
| EV01 | EmpresaScopeValidated | Contexto/empresa del backend confirmados, sin otorgar autoridad desde DTO |
| EV02 | MembershipGranted | UsuarioEmpresa activa y rol scoped auditados, con actor autorizado |
| EV03 | MembershipRevoked | Revocación efectiva; prioritaria frente a jobs/reintentos antiguos |
| EV04 | OwnershipReconciled | Cliente/Sucursal legacy NULL reconciliado con evidencia explícita |
| EV05 | ProductReferenceBound | Producto/variante del mismo tenant, sin vínculo A→B |
| EV06 | WarehouseAssignmentVerified | Almacén/sucursal con propietario confirmado |
| EV07 | CommercialUnitCommitted | Venta/detalle/stock/factura confirmados en una sola unidad |
| EV08 | PaymentAllocated | Pago aplicado a factura propia sin duplicación |
| EV09 | MovimientoInventarioConfirmado | Movimiento validado con producto, almacén y actor scoped |
| EV10 | AuditReferencePersisted | Referencia y resultado seguro, sin PII/JWT/SQL crudos |
| EV11 | SchemaCompatibilityObserved | Lectura no destructiva de compatibilidad de mapping/versiones |
| EV12 | RecoveryCompensationRecorded | Compensación formal y auditable, no borrado de historial |

**No afirmar** que estas clases o tablas existan. Para una auditoría de operación rechazada podrían existir eventos técnicos de rechazo **distintos** de los doce eventos de éxito de negocio; jamás mezclar ambos.

## 3. Doce invariantes de idempotencia y correlación

| Regla | Contrato fail-closed propuesto |
| --- | --- |
| I01 | `eventId` global único por evento confirmado; si reintenta la misma operación, retorna el resultado original |
| I02 | `idempotencyDigest` deriva en servidor de tenant autenticado + tipo de operación + recurso + clave cliente normalizada + hash canónico del payload; no se acepta autoridad del cliente |
| I03 | Misma clave con payload diferente produce conflicto tipado 409, sin segundo efecto |
| I04 | Reserva y resolución de clave futuras deben ser atómicas con la transacción comercial, no dos escrituras independientes |
| I05 | `correlationId` identifica la cadena; `causationId` identifica su causa real, sin reutilizar el ID de otra operación |
| I06 | `aggregateVersion` verifica monotonía y conflicto optimista; no sobrescribir versión stale |
| I07 | Tenant A jamás recibe, consume, correlaciona ni reintenta eventos de B, aun si conoce un ID |
| I08 | Membresía/rol revocados se revalidan cuando la operación exige autoridad; un evento antiguo no concede permisos posteriores |
| I09 | Estado de éxito sólo después de commit; timeout con resultado desconocido exige lookup seguro, no repetir ciegamente |
| I10 | Fecha UTC, tamaño/payload, batch, frecuencia, TTL y retención tienen presupuesto medido y decisiones explícitas; nunca infinitos |
| I11 | Logs/eventos excluyen tokens, credenciales, SQL, IP/PII cruda, cadenas DB y campos de payment instrument |
| I12 | Contratos versionados permiten lectura de versiones soportadas; versión desconocida, digest inválido o proveedor no confiable = cuarentena/NO_GO |

**Límites futuros propuestos:** máximo de lote, tamaño de payload y ventana de deduplicación deben derivarse de carga observada, configuración tipada y soporte a rollback. No inventar aquí números de producción ni crear colas eternas. Reintentos futuros: backoff finito, jitter, dead-letter/compensación controlados, sin vulnerar revocación, tenant ni idempotencia.

## 4. Flujo contractual y puntos de denegación

1. Autenticar en backend; resolver empresa/membresía/rol vigente y permisos por recurso.
2. Validar solicitud canónica, tenant, agregado, límites, expectedVersion y clave de idempotencia.
3. Determinar si existe ejecución previa por digest y payload; retornar resultado igual o conflicto seguro.
4. Ejecutar futura transacción de escritura autorizada con rollback real y eventual registro outbox en la **misma transacción** cuando se implemente.
5. Confirmar commit; sólo entonces permitir publicación eventual con `eventId` estable. Repetir entrega no duplica efecto.
6. El consumidor futuro comprueba versión de esquema, firma/origen cuando proceda, tenant, estado de revocación y unicidad del evento antes de efecto externo.
7. Correlacionar auditoría segura `correlationId`/resultado, mantener evidencia y retención autorizada, no guardar secretos.
8. Si hay inconsistencia, bloquear reenvío, registrar compensación sin borrar eventos y ejecutar recuperación auditada.

**Condición de fallo:** sin confirmación de commit, sin owner, payload no canónico, duplicado conflictivo, versión stale, revocación, cross-tenant, exceso de cuota o resultado ambiguo no se crea un evento exitoso. No se infiere entrega exactamente una vez a partir de una cola externa.

## 5. Doce pares P/N FUTUROS_NO_EJECUTADOS

| Par | Positivo a implementar | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | A genera evento scoped A con actor autorizado | A intenta emitir o leer evento B conocido |
| P02/N02 | Retry misma clave+payload retorna mismo efecto | Misma clave con payload distinto duplica operación |
| P03/N03 | Dos escrituras concurrentes CAS producen una confirmada | Ambas aplican venta/stock/pago |
| P04/N04 | Evento exitoso sólo aparece tras commit | Excepción deja evento exitoso sin dato persistido |
| P05/N05 | Consumidor verifica schemaVersion conocido | Versión desconocida se ejecuta silenciosamente |
| P06/N06 | Correlation/causation forman DAG causal consistente | Causa de otra empresa/op sin autorización |
| P07/N07 | Revocación efectiva impide acción posterior | Retry antiguo resucita rol/membresía |
| P08/N08 | Fecha UTC y orden de aggregateVersion coherentes | Reorder/stale causa sobrescritura |
| P09/N09 | Consulta segura resuelve timeout de commit | Reintento ciego produce pago duplicado |
| P10/N10 | Carga dentro de límites tipados acotados | Batch/payload gigante o proveedor no allowlisted |
| P11/N11 | Auditoría tiene identificadores técnicos seguros | Log publica PII/JWT/SQL/token |
| P12/N12 | CI integración MySQL 8.4 efímera y rollback real PASS | Build verde/skipped/in_progress se certifica como integración |

**Pruebas ejecutadas en esta microtarea:** 0/12 en runtime. Estos son oráculos futuros, no assertions xUnit, HTTP, MySQL, Playwright, integración de provider ni mensajes realmente emitidos. Estado de performance `NO_MEDIDO` sin numerador/denominador.

## 6. Seguridad, dependencias, auditoría y A0–A5

**A0:** contrato M02.05 y antecedentes, diff sólo este Markdown y sección autocontenida de `CHANGELOG_AI.md`; no cambia capas, DTO, API, migrations, Angular ni infraestructura. Consumidores futuros: Application/Infrastructure/API y posible proyección de UI, siempre detrás de auth del backend. **A1:** revisión de EV01–EV12, I01–I12 y P01/N01–P12/N12; 0/12 ejecutados. **A2:** límites conceptuales tenant A/B, roles, revocación, idempotencia, replay, PII, fraude por mezclas de referencias; sin afirmar enforcement nuevo. **A3:** .NET/xUnit, MySQL 8.4 EF/Oracle, HTTP, Playwright, broker externo `NO_APLICA` porque sólo cambian archivos Markdown. **A4:** publicación fast-forward dev con SHA causal, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact-SHA; PLAN/COLA/DAG/BITACORA readback y hashes de los dos archivos. **A5:** independiente por otra Task Javier distinta del autor; no autoaprobación.

**No desbloqueos falsos:** M02.06 y M03 siguen sujetos al DAG real, A5 y decisiones pertinentes; un LISTO_DEV R0 no autoriza ejecutar outbox, migración, JWT, proveedor de mensajes, QA/main/PROD, cutover ni cuotas pagadas. Los consumidores que cambien código serán R2 si afectan seguridad, tenancy, concurrencia o persistencia, con pruebas causales MySQL 8.4 aislado y escenarios negativos reales.

**Rollback de la entrega:** revisión de HEAD y writer, revert normal fast-forward de los Markdown o corrección aditiva, preservando HISTORY/BITACORA A:Z; no force-push/reset ni alterar fórmulas G/O. El rollback de efectos futuros requiere compensación y restauración ensayadas, no se ejecuta aquí.
