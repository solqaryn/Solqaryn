# SOLQARYN V5 FINAL — C02-03.M02.01: entidades, ownership y reglas de invariantes del ciclo de vida tenant

**Microtarea:** C02-03.M02.01 | **Autor:** PRIMARY DEV 06, JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a | **Rama:** solqaryn/Solqaryn dev | PROJECT_SCOPE_LOCK=STRICT | **Fecha:** 2026-11-15.

**Fuentes de autoridad:** Standard de Implementación de Automatizaciones V5 FINAL (Drive ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA institucionales; skill canónica, AGENTS.md y documentos C02-03.M01.01–06 en dev. C02-03.M01.06 está LISTO con A5 independiente de Task 06; M02.01 fue reclamado sin otro owner y con COLA G/O=SI y DAG F=VALIDADO G=SI.

**Clase:** diseño exclusivamente documental del modelo lógico y las invariantes. No agrega columnas, tablas, clases, endpoints, jobs, roles, tokens, configuraciones ni eventos emitidos. Los términos SUSPENDIDO/ARCHIVADO/BAJA_LOGICA son estados PROPUESTOS, no estados persistidos ni capacidades operativas.

## 1. Estado real comprobado y límites

- En backend/src/Domain/Entities/Empresa.cs, Empresa conserva Nombre, Rtn, Direccion, LogoUrl, LogoPublicId, Activa y métodos Activar/Desactivar. Eliminado devuelve false con NotMapped: no existe baja lógica persistida acreditada por esa propiedad.
- En backend/src/Domain/Entities/UsuarioEmpresa.cs, UsuarioEmpresa vincula UsuarioId, EmpresaId, RolId y Activa; el rol es de la empresa de la membresía, no de operador global. Eliminado también es NotMapped y devuelve false.
- ContextoTenantActual y UsuarioScopeService sirven como contratos existentes de contexto/autorización empresarial, sin constituir permisos de ciclo de vida universal.
- Las etapas M01 definen alta, suspensión, reactivación, archivo y baja lógica idempotentes como PROPUESTAS; AC01–AC24, G01–G16 y R01–R08 son criterios y procedimientos futuros, no pruebas ejecutadas.

## 2. Matriz de entidades lógicas y autoridad

| ID | Entidad/objeto | Estado real | Propietario y claves lógicas | Relación y prohibición |
| --- | --- | --- | --- | --- |
| E01 | Empresa | EXISTENTE (dominio) | Empresa.Id, identidad empresarial | Raíz tenant; Activa no implica estado completo de lifecycle ni soft-delete |
| E02 | UsuarioEmpresa | EXISTENTE (dominio) | UsuarioId + EmpresaId, RolId de esa membresía | N:1 Empresa; ninguna membresía concede permiso global de plataforma |
| E03 | ContextoTenantActual | EXISTENTE como contrato de autorización, no tabla lifecycle | Usuario autenticado + Empresa validada en servidor | Contexto derivado, nunca confiado a localStorage, host o payload |
| E04 | SolicitudCicloTenant | PROPUESTA, NO IMPLEMENTADA | IdSolicitud + empresa objetivo + actor plataforma | Solicita transición; no transforma entradas cliente en autorización |
| E05 | TransicionTenant | PROPUESTA, NO IMPLEMENTADA | EmpresaId + tipo + estadoOrigen + estadoDestino | Validar grafo permitido y rol plataforma para cada transición |
| E06 | VersionTransicionTenant | PROPUESTA, NO IMPLEMENTADA | EmpresaId + versión monotónica | Fencing/CAS impide dos estados activos simultáneos por carrera |
| E07 | RegistroIdempotenciaTenant | PROPUESTA, NO IMPLEMENTADA | Actor autorizado + EmpresaId + operación + clave + huella payload | Mismo key/payload reutiliza respuesta; payload distinto da conflicto seguro |
| E08 | PuntoRecuperacionTenant | PROPUESTA, NO IMPLEMENTADA | SolicitudId + paso + versión | Checkpoint sólo si la operación durable confirma el paso; nunca saltar grants |
| E09 | VinculoHostTenant | PROPUESTA como objeto lifecycle; revisar binding existente | EmpresaId + host normalizado + revisión | Host nunca elige EmpresaId autorizado ni desplaza el tenant de otra empresa |
| E10 | VinculoAlmacenamientoTenant | PROPUESTA como objeto lifecycle; revisar storage actual | EmpresaId + recurso + prefijo autorizado | Ningún destino DSN, bucket o prefijo aportado por cliente es autoridad |
| E11 | MarcaRevocacionTenant | PROPUESTA, NO IMPLEMENTADA | EmpresaId + versión de permisos + instante | Suspensión/revocación invalida sesiones y consumidores, no revive roles |
| E12 | EvidenciaAuditoriaTransicion | PROPUESTA, NO IMPLEMENTADA | SolicitudId + correlationId + autor/verificador | Registro inmutable y redactado sin JWT, DSN, secretos ni PII completa |
| E13 | RetencionLegalTenant | PROPUESTA, NO IMPLEMENTADA | EmpresaId + política + retención vigente | Impide destrucción física mientras existan obligaciones documentales |
| E14 | AccionCompensatoriaTenant | PROPUESTA, NO IMPLEMENTADA | SolicitudId + paso + compensación idempotente | Rollback parcial verificable sin tocar empresas ajenas ni borrar historial |

Las entidades E04–E14 requieren diseño de persistencia y compatibilidad en M02.03/M03.04 antes de cualquier despliegue. No introducir por inferencia FK, índice, tabla física o migración en esta M02.01.

## 3. Relaciones y ownership propuestos

1. Empresa 1:N UsuarioEmpresa: sólo una membresía activa del usuario en la Empresa objetivo permite las acciones empresariales otorgadas, jamás una acción global de alta/baja por sí sola.
2. Empresa 1:N SolicitudCicloTenant y 1:N TransicionTenant: cada comando queda ligado a la misma raíz empresarial verificada, actor autorizado y versión.
3. SolicitudCicloTenant 1:1/1:N RegistroIdempotenciaTenant y PuntoRecuperacionTenant: efectos y respuestas quedan registrados antes de reconocer éxito; claves no cruzan empresa u operación.
4. Empresa 1:N VinculoHostTenant, VinculoAlmacenamientoTenant y MarcaRevocacionTenant: validar vínculo y revocación en backend antes de exponer la superficie, sin mutación automática de infraestructuras.
5. SolicitudCicloTenant 1:N EvidenciaAuditoriaTransicion y AccionCompensatoriaTenant; Empresa 1:N RetencionLegalTenant. No borrar ventas, auditorías, referencias fiscales ni archivos por transición documental.

## 4. Invariantes obligatorias I01–I16 para la futura implementación

| ID | Invariante | Falla denegada y prueba futura |
| --- | --- | --- |
| I01 | Cada cambio exige operador de plataforma autorizado server-side | Admin de empresa A intenta suspender B: denegar, 0 mutaciones |
| I02 | Identidad empresarial se obtiene de contexto y objeto verificados | EmpresaId/host/DSN del cliente no autoriza datos de B |
| I03 | UsuarioEmpresa mantiene unicidad lógica por UsuarioId+EmpresaId | Repetir membresía no duplica ni eleva rol |
| I04 | EstadoOrigen y estadoDestino obedecen grafo explícito versionado | ARCHIVADO a ACTIVO sin autorización y controles: denegar |
| I05 | VersionTransicionTenant avanza sólo por CAS de un ganador | Dos suspensiones/reactivaciones concurrentes producen único orden |
| I06 | Idempotencia incluye EmpresaId, operación, clave y huella exacta | Mismo key con payload distinto o de B: conflicto, cero efectos |
| I07 | Alta incompleta nunca habilita comercio ni permisos | Fallo después de Empresa antes de membresía: compensar o bloquear |
| I08 | Suspensión deniega operaciones privadas y jobs del tenant | Token anterior/caché stale no habilita escritura nueva |
| I09 | Reactivación nunca restaura grants revocados sin nueva validación | UsuarioEmpresa inactiva sigue inactiva hasta autorización real |
| I10 | Archivado y baja lógica respetan retención/contabilidad | Rechazar DELETE/DROP o pérdida de FK y adjuntos |
| I11 | Un checkpoint solo refleja efecto durable y recuperable | Reintento en paso fallido no duplica empresa/evento/pago |
| I12 | Host, storage y proveedor son recursos del tenant verificado | Evento de A no actúa en B ni acepta prefijos ajenos |
| I13 | Auditoría redacciona JWT, password, DSN, PII y credenciales | Logs/DTO ante error no exponen secretos ni identidad ajena |
| I14 | Readback de API y persistencia concuerda con versión y owner | Resultado 200 sin commit persistente no es éxito |
| I15 | Evidencia CI requerida se ata a SHA exacto y causa | CANCELLED/SKIPPED/IN_PROGRESS nunca PASS |
| I16 | A5 de cambio DEV requiere otra Task Javier y gates satisfechos | Autor de M02.01 no se autoaprueba; QA/main/PROD permanece cerrado |

## 5. Escenarios contractuales positivos/negativos, NO EJECUTADOS

| Caso | Tipo | Estímulo futuro | Observación requerida |
| --- | --- | --- | --- |
| P01 | positivo | Plataforma autorizada crea empresa A una vez | Estado coherente, unicidad y trazas sin habilitar antes de gates |
| P02 | positivo | Reenvío idéntico de alta A | Misma respuesta lógica; cero duplicados |
| P03 | positivo | Suspender A y luego validar API y workers | Escrituras A denegadas y datos retenidos |
| P04 | positivo | Reactivar A tras revalidación de membresías | Estado nuevo y sesiones revocadas no restauradas |
| N01 | negativo | Admin A solicita baja de B | 403/denegación sin acceso a B |
| N02 | negativo | Mismo key con payload diferente | Conflicto sin nuevo efecto |
| N03 | negativo | Carrera suspender/reactivar sobre A | CAS de versión deja una única transición válida |
| N04 | negativo | Alta falla entre pasos | Checkpoint reparable, tenant no falsamente activo |
| N05 | negativo | Archivo con retención contable vigente | Bloqueo, sin DELETE/DROP físico |
| N06 | negativo | JWT revocado o rol obsoleto solicita transición | Rechazo antes de persistencia |
| N07 | negativo | Host/bucket/DSN externo apunta a B | No cambio de binding ni acceso B |
| N08 | negativo | Build o check requerido FAILED/CANCELLED | VALIDANDO; no LISTO ni A5 ficticios |

Son escenarios de aceptación futura; NO se ejecutaron tests .NET, HTTP, EF, MySQL, Playwright ni operaciones de lifecycle en este documento.

## 6. Verificación proporcional y handoff A0–A5

**A0:** validar fuente V5, PLAN/COLA/DAG exactos, impacto solo docs/v5 y CHANGELOG, estructura Markdown, sin secretos ni archivos de runtime. **A1:** inspección documental de matriz E01–E14, relaciones, invariantes I01–I16 y P/N; comprobar que propuestas no se afirmen implementadas. **A2:** verificar fail-closed JWT/RBAC, plataforma versus empresa, aislamiento A/B, revocación, retención, idempotencia y PII. **A3:** coherencia con M01.01–06, AC01–AC24, G01–G16 y R01–R08; los consumers actuales de Empresa y UsuarioEmpresa se mantienen intactos. **A4:** publicación fast-forward con CAS de HEAD, SHA y diff, Scope Lock y V5 de CHANGELOG completed/success exact-SHA, readback PLAN/COLA/DAG/BITACORA. Las suites runtime son NO_APLICA a un diff documental, NO PASS. **A5:** revisión independiente por otra Task Javier distinta del autor, sobre blobs y pruebas documentales reales; no autoaprobar.

**Siguiente:** diseñar API/DTO M02.02 y persistencia futura M02.03 sólo si el DAG de cada ID sigue SI, por reclamaciones distintas; M02.06 exige además sus predecesores. No crear eventos, migraciones, workers, cuentas, permisos o interfaces productivas a partir de un modelo propuesto. **Rollback de esta entrega:** git revert normal del commit documental en dev, manteniendo historia BITACORA; nunca force-push, QA/main/PROD o datos reales.
