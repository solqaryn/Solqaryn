# SOLQARYN V17 — C11-01.M02.06: dependencias, bloqueo y contrato de salida del modelo relacional

**ID:** C11-01.M02.06; **PLAN_MAESTRO:** fila 1597; **TASK autora:** DEV01 Javier (6adbb9fff1548191a1bd5b30d9e9984a). **Rama:** solqaryn/Solqaryn@dev. **PROJECT_SCOPE_LOCK=STRICT.** **Riesgo:** R0, exclusivamente documental.

## A0. Alcance, autoridad y no implementación

Cierra la **definición documental** del lote M02 de C11-01 (claves, relaciones, ownership, integridad, compatibilidad y reversibilidad), no la implementación ni certificación de un esquema. Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, política V15 A0-A5, continuidad V16, V17 y PLAN/COLA/DAG dinámicos. La salida de M02 no equivale a autorización de M03, migraciones reales, actualizaciones Aiven, API, MySQL, QA, main ni PROD. Antes de cualquier sucesora se revalidan todas las dependencias técnicas reales, incluyendo A5 independiente, HEAD y Scope Lock.

**Diff permitido:** este Markdown y una sección autocontenida en CHANGELOG_AI.md. No modifica Domain, Application, Infrastructure, API, Angular, Entity Framework, DDL, JWT, RBAC, inventario, facturación, auditoría física ni los datos de ninguna empresa; tampoco crea workers, servicios, flags, secretos o contratos API reales. La arquitectura vigente permanece .NET 10 y EF Core 10.0.12 con proveedor Oracle MySql.EntityFrameworkCore 10.0.9, Angular 22.2.1 y MySQL 8.4 como integración futura aislada.

**Exclusión V17:** PLAN H1597=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI antes del claim; candado nativo `SQV17_C11_01_M02_06`, id `2000001597`; H pasa por findReplace CAS (occurrencesChanged=1), y posteriormente I=TASK_ID autora con readback. Máximo una EN_PROGRESO propia.

## Entradas versionadas que deben conservarse

| Entrada documental | Blob Git comprobado | Resultado estrictamente conceptual |
| --- | --- | --- |
| M01.06: `docs/v5/c11-01-m01-06-gates-rollback-liberacion-modelo-relacional.md` | `f6908d23b07a1e0646e245287add9be8c18f7913` | 16 gates y ocho acciones futuras de rollback, sin migración |
| M02.01: `docs/v5/c11-01-m02-01-entidades-ownership-relaciones-invariantes.md` | `d1b1a09a096fdfbaa2f2acacb33e53f05bff39fc` | E01-E16, I01-I16 y doce pares futuros |
| M02.02: `docs/v5/c11-01-m02-02-api-dto-errores-validacion-compatibilidad.md` | `9f0d574d4ed01d4a3d3aed142a7cb67db1b9d402` | OP01-OP08, D01-D12 y ER01-ER16, no endpoints |
| M02.03: `docs/v5/c11-01-m02-03-persistencia-indices-migraciones-datos-prueba.md` | `cd539f06d350ead14d19d2c273e5c7566c7b1a83` | K01-K12, etapas M01-M08 reversibles y seis fixtures sintéticos; no DDL |
| M02.04: `docs/v5/c11-01-m02-04-configuracion-tipado-flags-modelo-relacional.md` | `26820024ba144d68c23f7186416d69a1ea2b296e` | F01-F12 y denegación por defecto; flags no activados |
| M02.05: `docs/v5/c11-01-m02-05-eventos-idempotencia-correlacion-limites.md` | `e30ec9f70dee77facbd67d09e117a36adb81caee` | EV01-EV12, I01-I12, P01/N01-P12/N12 futuros; etiqueta EV09 reparada |

Las referencias de entidades y relaciones son candidatos que deberán cotejarse con modelos y mappings reales. `Cliente.EmpresaId` y `Sucursal.EmpresaId` NULL heredados no otorgan pertenencia ni pueden rellenarse por inferencia. El consentimiento de migración requiere una decisión explícita y pruebas de integridad antes de modificar cualquier fila.

## G01-G16: puerta de salida contractual M02 y bloqueos técnicos de M03

| Gate | Condición exigida para futura implementación; si falta = NO_GO |
| --- | --- |
| G01 | Inventario del modelo físico y mappings por commit SHA y proveedor verificados, sin asumir FK o índice por su nombre documental |
| G02 | Catálogo E01-E16 y contratos D01-D12 reconciliados con los consumidores reales, sin introducir campos implícitos |
| G03 | Empresa efectiva tomada de autenticación backend, nunca de EmpresaId, slug, header ni DTO controlable |
| G04 | UsuarioEmpresa, rol, permiso por recurso y revocación comprobados en tiempo de uso; soporte solo lectura cuando aplique |
| G05 | Cliente/Sucursal con owner NULL legacy aislados hasta reconciliación verificable, jamás compartidos globalmente |
| G06 | FK parent-child tenant-aware, movimientos, pagos, facturas y almacenes conservan propietario y transacción consistente |
| G07 | K01-K12 candidatos contrastados con INFORMATION_SCHEMA, EXPLAIN, cardinalidad, collation y duplicados |
| G08 | Migración expand/contract aditiva y versionada; prohibido ON DELETE CASCADE sobre historia financiera |
| G09 | Backup, checksum, conteos y restore con fixtures de MySQL 8.4 EFÍMERO; nunca usar Aiven productivo |
| G10 | Pruebas negativas reales tenant A/B, IDOR/BOLA, sesión revocada y privilegios de soporte denegados |
| G11 | Concurrencia/versión y claves idempotentes verificadas: dos escritores no aplican doble venta, pago o stock |
| G12 | Outbox/eventos futuros sólo tras commit; un evento conceptual no prueba publicación ni exactamente una entrega |
| G13 | Configuración tipada allowlisted, fallback DENY, versionado y rollback; ningún flag de bypass de seguridad |
| G14 | Logs y auditoría con correlación y redacción, sin PII, tokens, SQL ni secretos crudos |
| G15 | CI de .NET 10 + pruebas reales Category=Integration sobre MySQL 8.4 Oracle EF cuando cambien DB/EF, 0 FAIL; SKIPPED/CANCELLED/IN_PROGRESS no son PASS |
| G16 | Scope Lock, V5, A0-A4 causal, documentación, readback institucional y A5 de otra Task Javier; permiso de entorno separado |

G01-G16 son criterios futuros, no gates aprobados por la existencia de esta tabla. Si una condición falla, la sucesora técnica se mantiene bloqueada aunque COLA o DAG presente un prefiltro SI desactualizado. Para M02 actual, sólo se certifica el texto R0 y su trazabilidad.

## R01-R08: reversión futura — propuesta NO EJECUTADA

1. **R01** Capturar SHA, modelo físico, migraciones, índices y contratos vigentes en entorno de prueba, con inventario del owner y línea base.
2. **R02** Construir backup y restore verificables de fixtures sintéticos A/B, checksums y conteos antes de expandir esquema.
3. **R03** Probar modo lectura dual y compatibilidad legacy; si NULL se interpreta como global o hay exposición A/B, detener.
4. **R04** Aplicar migración efímera con proveedor Oracle EF real; verificar integridad y relaciones inversas.
5. **R05** Ejercitar transacciones fallidas, violación FK, concurrencia, revocación, replay y compensaciones sin pérdida financiera.
6. **R06** Ante defecto, abortar nuevas escrituras, restaurar desde snapshot sintético o reversa compatible según historia; jamás borrar auditoría.
7. **R07** Comprobar hashes, conteos, idempotencia, trazabilidad, rollback y ausencia de divulgación de PII después de la reversión.
8. **R08** Registrar PASS/FAIL real con numerador/denominador, revisor independiente, SHA, DAG, BITACORA y autorización de entorno.

Rollback de este archivo documental: `git revert` normal tras releer HEAD y proteger cambios de otros autores, sin force-push, reset, borrado de BITACORA ni modificación manual de COLA/DAG.

## T01-T12: oráculos futuros positivos y negativos — 0/12 ejecutados

| Caso | Positivo exigido en implementación posterior | Negativo que debe fallar |
| --- | --- | --- |
| T01 | Empresa A obtiene entidades propias autenticadas | A enumera filas de B mediante ID conocido |
| T02 | Membresía y rol vigentes permiten la acción | Rol revocado o token viejo conserva permiso |
| T03 | Cliente legacy NULL permanece en cuarentena | NULL recibe dueño por inferencia silenciosa |
| T04 | Padre, hijo y sucursal coinciden en empresa | Padre B e hijo A crean FK cruzada |
| T05 | Índice permite consultas tenant-aware estables | Clave UNIQUE global rompe B o filtra A |
| T06 | Up de migración conserva registros y checksum | Down elimina pagos, stock o historia |
| T07 | Commit comercial confirma venta/stock/factura | Fallo intermedio deja pago o stock parcial |
| T08 | Reintento misma clave/payload retorna resultado | Clave igual y payload distinto duplica efecto |
| T09 | CAS/version actualiza una sola vez | Dos escritores aceptan versión stale |
| T10 | Evento se emite sólo tras commit válido | Error deja evento de negocio exitoso |
| T11 | Logs registran correlación y errores opacos | Exponen JWT, SQL, PII o secretos |
| T12 | CI real MySQL 8.4 + Scope Lock exactos PASS | Build aislado, CHECK SKIPPED o job en curso se declara certificación |

**Ejecución aquí: 0/12 pruebas runtime.** A1 es inspección estática de oráculos, no asserts xUnit ejecutadas. Para un cambio de implementación futuro R2, MySQL/EF, seguridad A/B, roles/revocación, transacciones y rollback se ensayan realmente en CI, con resultados y logs.

## A0–A5, DAG y condición de liberación

- **A0:** diff de un documento nuevo y una sección autocontenida de CHANGELOG; dependencia M01+A5 y M02.01–05+A5 revalidada contra PLAN/COLA/DAG; no alteración de arquitectura ni runtime.
- **A1:** auditoría estática de G01-G16, R01-R08, T01-T12 (positivos y negativos futuros), 0/12 ejecutados.
- **A2:** contratos conceptuales sobre tenant A/B, JWT/RBAC, revocación, ownership legacy, auditoría, errores seguros e idempotencia; sin nuevas garantías runtime.
- **A3:** .NET, HTTP, MySQL/EF, Angular/Playwright y proveedores NO_APLICA por diff R0 sin código o esquema; no se deduce certificación técnica.
- **A4:** commit fast-forward en dev, Scope Lock y V5 `completed/success` en SHA exacto o descendiente auditado con blobs equivalentes, y readback PLAN/COLA/DAG/BITACORA. No contar queued/in_progress/failed como PASS.
- **A5:** revisión independiente por otra de las seis Tasks Javier, con posibilidad de A5_FAIL; autor nunca se autoaprueba.

**Salida M02→M03:** sólo habilitar la siguiente etapa si H=LISTO A0–A4, A5 independiente donde DAG lo exija, PLAN I original, COLA G/O y DAG F/G actuales y prerrequisitos técnicos satisfechos. Aun así, autorización de DB/API real, cambios de datos y despliegue exige preflight de riesgo R2 y pruebas reales. No se crean Task/PR/branch, ni cambios en QA, main, producción, dominios, secretos, cuentas o proveedores.
