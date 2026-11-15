# SOLQARYN V17 — C09-01.M02.01: entidades, ownership e invariantes conceptuales del CRM universal

**Microtarea:** C09-01.M02.01 · **Autor:** PRIMARY DEV 03 / JAVIER_CHATGPT · **TASK_ID:** `6adc2e4bd8a48191ac7d8641ed95a1a3` · **Riesgo del cambio:** R0 exclusivamente documental · **Rama:** `solqaryn/Solqaryn@dev` · **Gobierno:** `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 1304. El objetivo M02 de `C09-01` es especificar un modelo conceptual para historial, segmentos, oportunidades, actividades y seguimiento de clientes; la evidencia final **futura** será un timeline aislado y permisos por rol. **Esta unidad NO crea código, tablas, migraciones, DTO, endpoints, jobs, campañas, eventos, interfaces, privilegios ni datos.**

## 1. Baseline, límites y fuentes

- `C09-01.M01.01` define NR01–NR14 de no regresión, e identifica `Cliente`, `Empresa` y `UsuarioEmpresa` como bases ya existentes. No confundir `Cliente` con un usuario autenticado.
- `C09-01.M01.02` documenta actores, interfaces y fronteras. `M01.03` inventaría evidencia y brechas, sin afirmar que exista un CRM operativo.
- `C09-01.M01.04` deja abiertas D01–D08 y riesgos R01–R16. `M01.05` aporta AC01–AC14 y restricciones no funcionales futuras.
- `C09-01.M01.06` (`c5370e3ee737fba5a965aacb37549cf7d4301b94`, blob de documento `ffc5e73423a8be7e0f1064c8df5b1fb68bbc2769`) fijó 16 puertas, 12 pares de oráculos FUTUROS_NO_EJECUTADOS y A5 independiente. Su A5 por una Task Javier distinta de DEV02 consta en la bitácora; el DAG actual valida esta unidad, sin conferir permisos para ninguna fase técnica.
- Filtros `EmpresaId` recibidos por cliente, host, cookie, path o cabecera son **contexto no confiable**, nunca una concesión de acceso. Los datos `Cliente.EmpresaId=NULL` legados no son globales por defecto.

**Estado de implementación:** `EXISTENTE_VERIFICADO` sólo aplica a las entidades base que ya figuran en el inventario M01. Todos los demás nombres siguientes son **PROPUESTA_CONCEPTUAL_NO_IMPLEMENTADA**. No afirmar que sus atributos corresponden a columnas o API existentes.

## 2. Catálogo conceptual y propiedad (16 conceptos)

| ID | Concepto | Autoridad/owner y relación propuesta | Reglas mínimas / carácter |
| --- | --- | --- | --- |
| E01 | Empresa | Raíz de separación multitenant de todas las entidades CRM | Existente; ninguna fila CRM puede heredar alcance global |
| E02 | UsuarioEmpresa | Membresía vigente de actor/empresa/rol | Existente; revocación debe invalidar operaciones |
| E03 | Cliente | Referente comercial relacionado con Empresa de forma verificable | Existente; `EmpresaId=NULL` legado se deniega/cuarentena |
| E04 | PerfilCRM | 1:1 o extensión versionada de Cliente dentro de Empresa, sin duplicar identidad | Propuesto; campos adicionales PII requieren consentimiento y retención |
| E05 | InteracciónCRM | Muchos eventos de un Cliente/Empresa con actor, canal y fecha UTC | Propuesto; append-only lógico, inmutabilidad de historial autorizada |
| E06 | ActividadCRM | Trabajo/siguiente acción asociado a Cliente y responsable dentro de Empresa | Propuesto; asignación y estados con versionado |
| E07 | OportunidadCRM | Potencial negocio de Cliente/Empresa sin crear Venta o asiento | Propuesto; pipeline y rol de edición controlado |
| E08 | EtapaOportunidad | Estado tipado del pipeline de una Empresa, transiciones allowlisted | Propuesto; nunca transición arbitraria desde cliente |
| E09 | SegmentoCRM | Definición inmutable/versionada de filtro de clientes de una Empresa | Propuesto; no acepta SQL/JS libre ni filtros cross-tenant |
| E10 | ReglaSegmentacion | Expresión tipada y parametrizada de SegmentoCRM | Propuesto; operadores, campos y costos allowlisted |
| E11 | VinculoResponsable | Relación actor/Cliente/actividad/oportunidad dentro de una Empresa | Propuesto; no equiparar asignación con privilegio JWT |
| E12 | PreferenciaContacto | Consentimiento por finalidad/canal, revocable y auditable por Empresa/Cliente | Propuesto; ausencia o opt-out = no enviar |
| E13 | EvidenciaConsentimiento | Prueba de procedencia, versión, finalidad, timestamp y revocación | Propuesto; no almacenar secretos ni logs de PII excesiva |
| E14 | ClaveIdempotenciaCRM | Scope por Empresa, operación, actor y clave con expiración | Propuesto; retry no duplica actividades ni mensajes |
| E15 | EventoAuditoriaCRM | Traza mínima de actor, recurso, operación, outcome y correlación | Propuesto; no sustituye auditoría legal/comercial existente |
| E16 | PoliticaRetencionCRM | Reglas de minimización, expiración, anonimización y hold por Empresa/finalidad | Propuesto; destrucción real exige decisión y control separado |

Los tipos, cardinalidades e índices son **hipótesis de diseño**, no una orden de EF ni migración. La decisión de normalizar versus extender `Cliente`, la retención y la compatibilidad con sucursal permanecen abiertas; deben aprobarse antes de M03.

## 3. Relaciones y límites de confianza

| Arista | Regla conceptual | Riesgo que evita |
| --- | --- | --- |
| `Empresa -> Cliente/PerfilCRM` | relación sólo con owner resuelto y membresía válida | IDOR/BOLA entre tenant A y B, NULL legado |
| `Empresa -> InteracciónCRM` | `EmpresaId` explícita en actor, cliente e interacción | timeline que mezcla empresas |
| `Cliente -> ActividadCRM/OportunidadCRM` | validar el mismo owner antes de crear, listar o exportar | cross-tenant por ID forjado |
| `UsuarioEmpresa -> VinculoResponsable` | acceso efectivo se vuelve a evaluar en cada acción | asignado revocado sigue operando |
| `OportunidadCRM -> EtapaOportunidad` | transición válida y versión actual, sin afectar Venta | cambio de estado silencioso o contabilidad falsa |
| `SegmentoCRM -> ReglaSegmentacion` | filtro tipado y acotado al propietario de Empresa | SQL injection, inferencia de B o escaneos ilimitados |
| `Cliente -> PreferenciaContacto` | consentimiento por canal/finalidad vigente y verificable | mensajes sin base válida |
| `InteracciónCRM -> ClaveIdempotenciaCRM` | deduplicar operación por tenant y actor | replay, doble escritura o doble notificación |
| `* -> EventoAuditoriaCRM` | trazas con ids opacos y retención mínima | pérdida de causalidad y fuga de datos en log |

**Capas futuras no implementadas:** Domain definiría invariantes puros; Application autorizaría casos de uso/DTO; Infrastructure implementaría repositorios y transacciones EF/MySQL; API validaría JWT/RBAC y ownership; Angular mostraría estados sin conceder permisos. Ninguna capa fue modificada por esta entrega.

## 4. Veinte invariantes explícitas

- **INV01:** toda fila CRM futura resuelve exactamente una Empresa; nunca owner global implícito.
- **INV02:** un actor sólo puede actuar con `UsuarioEmpresa` vigente y el permiso requerido en backend.
- **INV03:** Cliente A, interacción A, responsable A y oportunidad A no pueden referenciar recursos B.
- **INV04:** `Cliente.EmpresaId=NULL` legado implica `SIN_OWNER_CONFIRMADO`, denegación y reconciliación autorizada, no acceso global.
- **INV05:** un rol de lectura no crea, modifica, exporta ni borra actividades.
- **INV06:** revocación de membresía o rol invalida nuevas lecturas, modificaciones y exportaciones, incluso con JWT viejo.
- **INV07:** cada interacción mantiene fecha UTC, actor, Empresa, causa y operación idempotente.
- **INV08:** dos peticiones con igual clave idempotente y mismo ámbito no duplican efectos.
- **INV09:** la misma clave en tenants distintos nunca provoca compartir respuesta ni PII.
- **INV10:** transiciones de oportunidad usan etapa allowlisted, versión y CAS; conflicto produce rechazo detectable.
- **INV11:** ninguna oportunidad crea cobro, factura, venta o asiento comercial por derivación.
- **INV12:** segmento ejecuta sólo filtros tipados y paginados con límites de costo medibles.
- **INV13:** un segmento, índice, caché o búsqueda nunca expone conteos ni datos de B desde A.
- **INV14:** acceso a PII exige finalidad/permiso y minimización por campo; exportación requiere permiso específico.
- **INV15:** la ausencia de consentimiento o un opt-out impide envíos y campañas, sin borrar historial requerido.
- **INV16:** fallos de consulta, timeout, proveedor, JWT o caché no levantan restricciones (fail-closed).
- **INV17:** auditoría conserva `who/what/when/tenant/outcome` sin JWT, secretos o PII innecesaria.
- **INV18:** borrados/retención respetan hold legal y relaciones comerciales existentes; no existe cascada destructiva implícita.
- **INV19:** la UI y sus guards no sustituyen checks backend contra IDs forjados o revocación.
- **INV20:** migraciones EF, mensajería, runtime, QA/main/PROD quedan bloqueados hasta criterios A0–A5 y autorización específicos.

## 5. Oráculos futuros de aceptación (12 pares, ejecución = NO_MEDIDO)

| Caso | Positivo futuro | Negativo/falla que debe detectar una prueba real |
| --- | --- | --- |
| P01/N01 | Vendedor A obtiene Cliente A con ownership verificable | A solicita Cliente B por ID: 403/404 seguro, cero PII |
| P02/N02 | Timeline Cliente A muestra sólo interacciones A ordenadas por UTC | Actividad B o propietario NULL jamás se mezcla |
| P03/N03 | Crear actividad A deja actor y evento auditado una vez | Retry/replay de clave igual no duplica actividad |
| P04/N04 | Rol editor A avanza oportunidad con versión correcta | Rol lector, tenant B o CAS obsoleto se rechaza |
| P05/N05 | Segmento A con filtro tipado, paginado y acotado | Expresión SQL/JS, campo no allowlisted o segmento B denegados |
| P06/N06 | Preferencia vigente autoriza exclusivamente canal/finalidad aprobados | Opt-out o falta de consentimiento bloquea emisión |
| P07/N07 | Export autorizado minimiza campos y registra actor A | Export sin permiso o incluyendo B/PII excedente falla |
| P08/N08 | JWT vigente y membresía activa A autorizan la operación A | JWT expirado o miembro revocado se deniegan |
| P09/N09 | Falla de caché devuelve respuesta segura por Empresa | Cache key global o timeout no permite datos ajenos |
| P10/N10 | Alta de responsable A requiere UsuarioEmpresa A habilitado | Asignación a rol/empresa B o usuario sin membresía se rechaza |
| P11/N11 | Retención versionada preserva hold e historial comercial | Borrado en cascada destruyendo Venta/auditoría queda prohibido |
| P12/N12 | UI 320px/teclado presenta estados claros, controlada por API | Bypass de ruta Angular no permite invocar endpoint privado |

No se ejecutó ninguno de estos pares: el documento sólo prescribe pruebas futuras. Para implementación R2 habrá que acreditar xUnit, pruebas de autorización A/B y `Category=Integration` sobre MySQL 8.4 EFÍMERO si cambia EF/DB, además de Playwright/Chromium cuando cambie UI. Los mocks no acreditan MySQL ni HTTP.

## 6. Riesgos, decisiones pendientes y secuencia segura

| Decisión | Dueño de aprobación futura | Bloqueo hasta decisión |
| --- | --- | --- |
| D01: extender `Cliente` o separar `PerfilCRM` | contrato de dominio y plan comercial autorizado | no crear EF/DDL |
| D02: conciliación de Cliente con EmpresaId legado NULL | dueño de datos y seguridad | no revelar, migrar ni compartir legacy automáticamente |
| D03: RBAC por operación y separación sucursal/Empresa | autoridad seguridad backend | sin endpoint ni grant de CRM |
| D04: finalidad, consentimiento y retención por canal | producto/compliance autorizado | sin campañas ni export libre |
| D05: auditoría mínima e idempotencia por tenant | seguridad/arquitectura | sin worker o evento real |
| D06: límites de búsqueda, cuotas y métricas de costos | operaciones/performance | sin query arbitraria o promesas p95 |
| D07: transacciones de oportunidad y relación con ventas | dominio/finanzas | sin mutaciones comerciales |
| D08: reversión y DDL aditivo por lote | autoridad de migraciones | sin ejecución MySQL real |

**Plan de rollback de ESTA unidad:** revertir exclusivamente el commit documental con `git revert` normal (después de verificar HEAD y escritores), conservar BITACORA y A5 histórico, recalcular DAG; no usar reset, force-push ni borrar datos.

## 7. Auditorías A0–A5 y evidencia exigida

- **A0:** cotejar `PLAN_MAESTRO!A1304:M1304`, `COLA!G/O`, `DAG_TECNICO!F/G`; A5 de `M01.06` verificada por otro Task Javier; scope sólo este Markdown y una sección autocontenida en `CHANGELOG_AI.md`; registrar candado V17 de fila 1304 y SHA actual.
- **A1 (R0):** revisar coherencia de E01–E16, INV01–INV20, relaciones y P01/N01–P12/N12 **futuros**, sin declarar pruebas runtime ejecutadas.
- **A2:** comprobar en texto identidad/membresía, tenant A/B, NULL legado, JWT/RBAC, revocación, PII, idempotencia, CAS, consentimiento y fail-closed; verificación estática, no seguridad funcional certificada.
- **A3:** `NO_APLICA` para xUnit, API/HTTP, EF/MySQL, Angular/Playwright y proveedores porque sólo se cambian Markdown; cualquier cambio real posterior exige test ejecutado.
- **A4:** publicar commit fast-forward de los dos Markdown, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en su SHA, hashes de blobs, PLAN/BITACORA idempotentes y readbacks. `queued/in_progress/skipped/cancelled/failed` no es PASS obligatorio.
- **A5:** una de las **otras cinco Tasks Javier**, distinta del autor `6adc2e4bd8a48191ac7d8641ed95a1a3`, debe revisar independientemente criterios, riesgos, prueba y commit; `LISTO_DEV` no es `A5_PASS`. No autoriza QA, main, producción ni migración.

**Resultado limitado a M02.01:** contrato conceptual de entidades/ownership/invariantes, nunca CRM implementado. Cualquier cambio técnico posterior requiere readback dinámico de COLA/DAG y autorización efectiva.
