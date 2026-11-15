# SOLQARYN V5 — C01-04.M02.06: dependencias y contrato de salida del expediente universal

**Microtarea:** `C01-04.M02.06` · **Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a` · **Fecha:** 2026-11-14 (America/Tegucigalpa) · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Alcance exclusivamente DOCUMENTAL:** definir el cierre técnico de M02 de expediente universal: cronología, decisiones, observaciones, adjuntos, versiones y propiedad multitenant, con dependencias reales, pruebas positivas/negativas futuras, bloqueos y rollback. **No se crea expediente runtime, endpoint, entidad EF, migración, flag, evento, worker ni pantalla; ninguna prueba HTTP/MySQL/Playwright de esta capacidad se ha ejecutado.** No modifica datos reales, secretos, cuentas, Tasks, QA, main o PROD.

## 1. Autoridad, prerrequisitos y procedencia del cierre M02

Plan ejecutable único: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). Estado institucional: [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), con Skill canónica, AGENTS, Scope Lock y autoridad de dev exacto. Se comprobó ID `C01-04.M02.06`, PLAN fila 265 PENDIENTE sin responsable, COLA fila 265 G/O=SI, DAG fila 267 F=VALIDADO/G=SI, posterior a las revisiones A5 independientes de `C01-04.M02.03–05`. Se registró una sola reclamación por PRIMARY DEV 01 y readback de H/I. El gate M02.06 exige `C01-04.M01.01–06+A5` y `C01-04.M02.01–05+A5`, no acepta un predecesor solo VALIDANDO.

| Predecesora | Evidencia verificable en dev | Contrato limitado al diseño |
| --- | --- | --- |
| `M02.01` | `docs/v5/c01-04-m02-01-modelo-entidades-ownership-invariantes-expediente.md`; blob `bd5c74657b43c5a0ccbabb308ccba08234eee495` | EX01–EX16, IN01–IN18: modelo lógico no implementado |
| `M02.02` | `docs/v5/c01-04-m02-02-api-dto-errores-compatibilidad-expediente.md`; blob `ccbc3afecff67d9089f93af44c1acf89e14d6cd6` | API/DTO/errores hipotéticos, no endpoints |
| `M02.03` | `docs/v5/c01-04-m02-03-persistencia-indices-migraciones-fixtures-expediente.md`; blob `67830136fd7aa25491843026ec29f99c5800a1b0` | P01–P12, IX01–IX08, FT01–FT12, no SQL |
| `M02.04` | `docs/v5/c01-04-m02-04-configuracion-tipada-ambitos-feature-flags-expediente.md`; blob `8104090019d30162595a966610488ff9603a6e32` | CFG01–CFG16, VT01–VT12, no flags reales |
| `M02.05` | `docs/v5/c01-04-m02-05-eventos-idempotencia-correlacion-limites-expediente.md`; blob `844f5973549b596711aa95815c9c47cd57564f99` | EV01–EV14, ID01–ID10, LM01–LM12, sin dispatcher |

La revisión independiente de M02.03, M02.04 y M02.05 por PRIMARY DEV 01 no modifica a sus autores PRIMARY DEV 06, y fue respaldada por SHA, dos archivos Markdown, Scope Lock/V5 exact-SHA, y eventos BITACORA separados. No se atribuyeron como PASS los jobs CANCELLED no causales. El diseño M01 y su A5 permanecen referencia obligatoria; los resultados futuros deben reconciliarse con código y políticas de las capas Domain/Application/Infrastructure/API actuales.

## 2. Contrato verificable de salida de M02 (OUT01–OUT14)

Cada OUT exige evidencia de implementación en una microfase futura y una contraprueba negativa. En este cierre documental sólo se valida la especificación, no su ejecución. `NOT_EVALUATED` es estado correcto sin evidencia.

| Gate | Camino positivo futuro | Camino negativo obligatorio y bloqueo |
| --- | --- | --- |
| OUT01 propiedad | Expediente vinculado a EmpresaId y recurso propio verificados server-side | ID de expediente de B accesible por actor A |
| OUT02 membresía | UsuarioEmpresa activa y permisos RBAC exactos en cada operación | Token válido pero membresía revocada concede acceso |
| OUT03 timeline | Orden estable por `FechaUtc, Id` y cursor ligado a tenant/filtros | Timestamp empatado duplica/omite o cursor B funciona en A |
| OUT04 observaciones | Nueva versión conserva actor, motivo y traza saneada | Edición borra registro previo o cuela PII/JWT en log |
| OUT05 decisiones | Operación exige permiso y revisión independiente donde aplique | Autor o rol sin grant se autoaprueba |
| OUT06 adjuntos | Referencia privada exige validación de ownership y TTL cada descarga | PublicId/URL de B o URL vencida entrega bytes |
| OUT07 versionado | CAS real sobre versión, un ganador entre dos escritores | Dos escrituras con misma versión sobrescriben |
| OUT08 idempotencia | Clave compuesta por tenant/recurso/operación y hash payload | Clave reutilizada con payload distinto duplica efecto |
| OUT09 legado | EmpresaId nulo queda en cuarentena hasta atribución demostrable | Registro legacy sin propietario aparece públicamente |
| OUT10 búsquedas | Contenido y conteos filtrados server-side por tenant y permisos | Filtro por request revela conteo/datos ajenos |
| OUT11 configuración | Límites globales + tenant/plan/rol intersectados, deny por defecto | Flag UI o override tenant eleva autoridad |
| OUT12 eventos | Si existe infraestructura autorizada, efectos tras transacción y reintentos finitos | Documento arranca worker o replay genera decisión doble |
| OUT13 persistencia | FK/índices compuestos, EF/MySQL efímero y rollback ensayado | FK cross-tenant, migración irreversible o escritura real no autorizada |
| OUT14 release | A0–A4 y A5 ajena, CI causal exact-SHA y permiso por entorno | CANCELLED/SKIPPED/IN_PROGRESS/FAIL presentado como PASS o despliegue implícito |

**Condición documental de cierre:** OUT01–OUT14 especificados, con frontera positiva/negativa, dependencia y procedimiento de fallo. **Condición futura de seguridad funcional:** los casos ejecutados y revisados en SHA aplicable, con resultados observados y retests; el documento no prueba ausencia de BOLA, IDOR, fugas A/B ni P0/P1. `LISTO_DEV` de M02.06 es aceptación documental de los contratos, no permiso para migrar, lanzar producto o promover ambientes.

## 3. Dependencias y trabajo real para M03.01–M03.06

- **M03.01 Domain:** reglas de expediente/entrada/observación/decisión, pertenencia a tenant, versión y validación; pruebas positivas/negativas xUnit, sin exponer entidad de B.
- **M03.02 Application:** casos de uso y DTOs versionados; errores seguros, autorización por recurso, paginación determinista, cancellation/transaction boundaries.
- **M03.03 Security:** JWT y membresía revalidados; RBAC por operación, sucursal/empresa, BOLA/IDOR A/B, revocación, pruebas de denegación; no confiar en EmpresaId de navegador.
- **M03.04 Infrastructure:** EF Core 10/MySQL 8.4, llaves compuestas, índices medidos, migración aditiva reversible, ejecución en MySQL efímero de CI; no Aiven real sin autorización.
- **M03.05 Integrations:** adjuntos/medios privados, proveedor, jobs o cola sólo si scope real y autorización la requieren; idempotencia transaccional, TTL, cuotas y reintentos limitados.
- **M03.06 Audit:** observabilidad minimizada, correlación opaca, retención, registro de errores seguro, rollback compensatorio y tests de no filtración de PII/secretos.

Cada M03 necesita **su propio** PLAN H/I, DAG F/G, COLA G/O, preflight de componentes/archivos, exclusividad, publicación directa dev y CI causal exact-SHA. No inferir G=SI por estar M02 LISTO, ni por que M02.06 esté VALIDANDO. No adelantar IAM/JWT/RBAC/tenant, DB/migraciones, eventos, backups ni QA/main/PROD por documentos preparatorios. Se deben comprobar contratos contra código real antes de implementación y corregir obsolescencia legítima sin silenciar tests o analizador.

## 4. Matriz de decisión, evidencia y trazabilidad

Definir para cada test futuro el registro `scope_id`, `microtask_id`, `case_id`, `tenant_fixture`, `actor_membership`, `resource_owner`, `required_grant`, `expected_status`, `observed_status`, `source_sha`, `check_run_id`, `artifact_checksum`, `reviewer_task_id` y `rollback_receipt`. Es propuesta de expediente de evidencia, **no DB/DTO/runtime agregado**. Un error/reintento de GitHub o Google Sheets no autoriza inventar evento ni reemplaza CAS atómico. BITACORA A:Z es append-only y los estados autoritativos son PLAN H/I/K y COLA/DAG reactivos.

| Estado | Semántica exigible |
| --- | --- |
| NOT_EVALUATED | Documento/oráculo disponible pero prueba no ejecutada |
| IN_PROGRESS | Ejecución o CI causal activa; conservar owner y no dar PASS |
| FAIL | Caso negativo falla o check obligatorio no exitoso; reparar y retestar |
| PASS_DEV | Pruebas ejecutadas, checks pertinentes SUCCESS y A0–A4 con readback del SHA |
| A5_PASS | Reviewer TASK_ID distinto al autor verificó diff SHA blobs y evidencias |
| RELEASE_BLOCKED | Sin permiso separado para QA/main/PROD, sin pruebas runtime o P0/P1 abiertos |

## 5. Plan de rollback RB01–RB10

| Código | Trigger y contención autorizada | Evidencia de restauración |
| --- | --- | --- |
| RB01 | Scope Lock fail: detener publicación insegura, corregir causa | Exact-SHA completed/success |
| RB02 | Job requerido pendiente/cancelled: dejar VALIDANDO, otra tarea independiente | CI causal revalidada, nunca PASS fingido |
| RB03 | BOLA/IDOR/tenancy A/B: bloquear funcionalidad y corregir backend | Tests A/B positivos/negativos y revisión |
| RB04 | JWT/RBAC revocado: deny by default e invalidar caché autorizativa | Regresión revocación/roles en CI |
| RB05 | Corrupción de cronología/cursor: deshabilitar nuevo consumidor | Conteo, orden estable y reconciliación |
| RB06 | Adjunto indebido: denegar URL e invalidar acceso | Owner, TTL y firma retesteados |
| RB07 | Error SQL o backfill: parar migración y no tocar reales | MySQL efímero, checksum y reversión aditiva |
| RB08 | Replay/doble efecto: deshabilitar productor no autorizado | Test transaccional idempotente |
| RB09 | Carrera Git/Sheet: abortar perdedor, preservar historia y owner | HEAD CAS, PLAN, BITACORA readback |
| RB10 | Defecto en documento: revert normal sin force-push | Nuevo commit y checks Scope Lock/V5, evento compensatorio |

Un rollback no borra evidencia histórica, no equivale a permiso de restauración sobre datos reales ni certifica recuperación productiva. El objetivo P0/P1 no se declara cumplido por la lista de casos.

## 6. Auditorías A0–A5 de esta entrega

**A0:** cambios limitados a este nuevo Markdown y sección autocontenida de CHANGELOG_AI, OUT01–14/RB01–10, ID/owner/Scope Lock, sin secretos ni encabezados vacíos. **A1:** pruebas positivas y negativas especificadas, todas `FUTUROS_NO_EJECUTADOS`, no tests funcionales pasados. **A2:** revisión estática JWT/RBAC/tenant A/B, privacidad, adjuntos, versiones; no penetración ni implementación. **A3:** rastreo a STANDARD, PLAN/COLA/DAG y M02.01–05, sin alterar Domain/Application/Infrastructure/API/MySQL/Angular. **A4:** publicación dev fast-forward con comparación de HEAD y CAS de Git, diff/blobs exactos, checks Scope Lock/V5 completed/success en SHA correspondiente, BITACORA y readback. **A5:** pendiente de otra Task Javier diferente de PRIMARY DEV 01, con evidencia propia; la autora no se autoaprueba.

**Resultado de esta microtarea:** entrega de contrato técnico M02 únicamente. Rollback inmediato mediante `git revert` normal del commit documental en dev y evento aditivo, sin force-push ni cambio a QA/main/PROD.
