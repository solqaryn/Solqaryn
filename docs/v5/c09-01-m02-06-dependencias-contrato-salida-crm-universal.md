# SOLQARYN V17 — C09-01.M02.06: dependencias técnicas y contrato de salida del CRM universal

**Unidad:** C09-01.M02.06 · **PLAN_MAESTRO:** fila 1309 · **autor:** PRIMARY DEV 05 / JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076` · **riesgo R0 documental** · **fecha:** 2026-11-15. **Ámbito:** solqaryn/Solqaryn, rama `dev`, PROJECT_SCOPE_LOCK=STRICT. **Fuente canónica:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), V15/V16/V17, PLAN_MAESTRO/COLA/DAG y Skill local/canónica.

**Alcance real:** fijar el contrato de salida del lote **documental M02** de CRM universal (historial, segmentos, oportunidades, actividades y seguimiento), sus dependencias, aceptación futura, bloqueos y reversas. **No se desarrollan** endpoints, DTO ejecutables, entidades EF, cambios de MySQL, migraciones, servicios, Angular, CRM operativo, mensajes, campañas ni automatizaciones externas. Ningún estado LISTO de documentos equivale a A5 o habilita por sí solo una operación sensible de clientes, consentimiento, finanzas, exportación o despliegue.

## 1. Evidencia de los antecedentes y sus límites

| Predecesor técnico | Aporte documental que debe quedar preservado | Límite comprobable para M03 |
| --- | --- | --- |
| `C09-01.M01.01–06+A5` | alcance, actores, fuentes, riesgos, decisiones y gates con revisión independiente | aprobación preparatoria no certifica seguridad runtime |
| `C09-01.M02.01+A5` | 16 conceptos y 20 invariantes de propiedad `Empresa`, cliente, timeline, actividad, oportunidad y segmentación | modelos propuestos, sin tablas, exportación ni permisos nuevos |
| `C09-01.M02.02+A5` | ocho operaciones API, ocho DTO, doce errores, dieciséis validaciones conceptuales | no hay endpoints ni contratos implementados por la redacción |
| `C09-01.M02.03+A5` | doce estructuras lógicas, doce índices candidatos, ocho etapas de migración futura, doce fixtures propuestos | EXPLAIN, constraints, transacciones y MySQL no ejecutados |
| `C09-01.M02.04+A5` | dieciséis opciones tipadas, doce reglas de precedencia y flags futuros | opciones inertes; ninguna bandera permite saltar RBAC/tenant |
| `C09-01.M02.05+A5` | eventos CRM, idempotencia y correlación propuestos | no se produce ni consume evento o worker |
| `C01-04.M01.06+A5` y `C07-04.M01.06+A5` | contratos transversales exigidos expresamente por DAG | no transferir autorización de otros capítulos |

La **condición de entrada** de esta unidad es la fórmula viva `DAG_TECNICO!F1270=VALIDADO/G1270=SI` y `COLA!G1309=SI/O1309=SI`, con `PLAN_MAESTRO!H1309=PENDIENTE/I1309` vacía antes del CLAIM. El candado V17 es `SQV17_C09_01_M02_06`, `namedRangeId=2000001309`, limitado a H:I de la fila 1309; nunca sustituye los gates o el análisis de riesgo. **Toda sucesora de M03 exige releer su propio DAG/COLA, no hereda un SI histórico.**

**A5 de M02.01–05:** el preflight leyó su habilitación agregada computada, tras revisiones independientes, sin concluir por ello que algún escenario funcional haya sido ejecutado. A5 de **esta M02.06** sigue **PENDIENTE** hasta revisión real por otra Task Javier de autor distinto. Ni C01 ni C07 ni M01/A5 otorgan acceso a QA, main, PROD o credenciales externas.

## 2. Fronteras arquitectónicas de M03 (NO IMPLEMENTADAS)

La implementación real deberá respetar Domain → Application → Infrastructure → API → Angular, sin duplicar el ERP ni introducir un segundo `Cliente` con propietario inventado. Domain establece invariantes y versión de agregado; Application verifica caso de uso, pertenencia y permiso; Infrastructure persiste/consulta MySQL con filtro de Empresa y constraints compuestas; API valida autenticación/autorización, entrada y errores sin PII; Angular solo representa estado, nunca concede privilegios. Los nombres `PerfilCRM`, `InteraccionCRM`, `ActividadCRM`, `OportunidadCRM`, `SegmentoCRM`, `PreferenciaContacto`, `EvidenciaConsentimiento` y `ClaveIdempotenciaCRM` son **propuestos**, no entidades ya creadas.

Riesgo transversal crítico: `Cliente.EmpresaId=NULL` legado requiere conciliación explícita y demostrable; no asumir propietario, backfill masivo, acceso público ni una empresa por defecto. `EmpresaId` de payload, `ActorId` proporcionado por cliente, host o flag UI jamás son autoridad. Cada recurso y cada relación necesita ownership de tenant comprobado; el JWT válido no anula revocación de membresía ni consentimiento revocado. El historial/segmentación nunca alimenta contactos, campañas o exportación sin finalidad legal/permisos.

## 3. Dieciséis puertas verificables para futura salida técnica M03

| Gate | Exigencia causal futura | Bloquear cuando ... |
| --- | --- | --- |
| G01 | Identidad Task Javier/Skill canónica, Scope Lock STRICT y SHA `dev` verificables | repositorio/HEAD/actor distintos o permisos faltantes |
| G02 | PLAN/COLA/DAG true por ID exacto y todos los A5 predecesores exigidos | cualquier M02 o M01 pendiente, A5_FAIL o dependencia no validada |
| G03 | Claim V17 exclusivo, único EN_PROGRESO propia, CAS H/I y BITACORA idempotente | lock ocupado, carrera o readback inconsistente |
| G04 | Modelo de Cliente/Empresa real y referencias existentes identificado por archivos HEAD | crear otra identidad CRM o asumir owner de legacy `NULL` |
| G05 | En Application cada operación exige membresía activa y RBAC correspondiente | JWT sin rol o usuario revocado conserva acceso |
| G06 | Aislamiento tenant A/B en API, Application, queries EF y relaciones compuestas | IDOR/BOLA, filtros opcionales o datos B accesibles desde A |
| G07 | Acceso por cliente/recurso y consentimiento actual, propósito y retención mínimos | contacto/segmento/export sin permiso o tras opt-out |
| G08 | Lecturas timeline paginadas, cursor determinista, presupuestos de consulta y filtros allowlisted | scans ilimitados, SQL/JS inyectable, conteos de B |
| G09 | Comandos y cambios de etapa con CAS/versión e idempotencia por tenant/actor/operación | doble actualización, clave reutilizada con payload diferente |
| G10 | Errores tipados seguros y rechazo por defecto ante contexto inválido | error filtra PII, revela clientes de B o confirma mutación fallida |
| G11 | Migraciones EF/MySQL 8.4 **efímero** con restricciones owner, rollback y prueba real | tests sólo mock, migración irreversible o datos reales |
| G12 | Índices y FK compuestos medidos con EXPLAIN y dataset sintético | suposiciones sobre performance o cascadas destructivas |
| G13 | Eventos CRM futuros auditables sin worker implícito; correlación sin secretos | mensajes at-least-once duplican efectos o se filtra JWT |
| G14 | UI accesible (teclado/foco/móvil), guard servidor, y pruebas Chromium si hay cambio Angular | guard solo frontend o pantalla anuncia éxito no confirmado |
| G15 | Build .NET 10 Release, xUnit y tests negativos JWT/RBAC/A/B, integración dirigida y checks Scope Lock/V5 SUCCESS del SHA pertinente | FAIL, CANCELLED, SKIPPED o IN_PROGRESS requerido, o equivalencia no acreditada |
| G16 | A5 independiente por Task Javier distinta y rollback probado antes de liberar cualquier sucesora | autoaprobación, revisor ajeno a allowlist o evidencia insuficiente |

**Interpretación de gates:** G04–G15 son exigencias para **código futuro**, no resultados PASS de este R0. No iniciar migración/DDL, importación real, SaaS productivo, workers, mensajes, secretos ni QA/main/PROD por entregar el contrato. Ante incertidumbre técnica o de seguridad clasificar R2 y dejar VALIDANDO/NO_GO de ese scope hasta prueba real.

## 4. Contrato de salida y matriz de doce pares FUTUROS_NO_EJECUTADOS

| Caso | Aceptación positiva futura | Rechazo/negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | tenant A ve únicamente sus clientes autorizados | ID de Cliente B o Cliente legacy `NULL` sin reconciliación |
| P02/N02 | miembro A activo consulta timeline permitido | JWT revocado/rol lector fuera de ámbito accede a B |
| P03/N03 | historia paginada estable por FechaUTC+Id | empate de timestamp omite/duplica filas o filtra B |
| P04/N04 | oportunidad A cambia etapa permitida con versión | transición inválida o CAS perdedor muta etapa |
| P05/N05 | actividad A se asigna a miembro activo A | responsable B/inactivo acepta asignación |
| P06/N06 | segmento A con filtros allowlisted consulta subset A | filtro arbitrario SQL/JS o escaneo cross-tenant |
| P07/N07 | consentimiento actual habilita finalidad concreta | opt-out posterior y propósito distinto permiten contacto |
| P08/N08 | operación idempotente repite un único resultado | misma clave/otro payload o tenant B comparte respuesta |
| P09/N09 | fallo MySQL durante operación revierte todo | filas parciales, auditoría huérfana o éxito falso |
| P10/N10 | migración Up/rollback efímero preserva ownership A/B | FK cruzada, nulos legacy forzados o Down destructivo |
| P11/N11 | auditoría mínima correlaciona actor/tenant permitido | PII/JWT/secreto en log, error, URL, archivo o caché |
| P12/N12 | UI móvil accesible refleja estado confirmado por servidor | offline/refresh/guard solo Angular muestran acción exitosa |

**Ejecución en M02.06:** `0/12` pares, resultado `NO_MEDIDO`. No se emitió un request HTTP ni se hizo DB, xUnit, Angular/Playwright, seguridad runtime o prueba de integración durante esta unidad. En la implementación R2, cada caso requiere asserts capaces de fallar ante defectos, logs del job exact-SHA y readbacks de persistencia; un build verde aislado no sustituye `Category=Integration` en MySQL 8.4 efímero.

## 5. Ocho rutas de reversión y continuidad

1. **DAG/A5:** si aparece A5_FAIL, dependencia NO o revisor inválido, conservar bloqueos y revisar origen; nunca escribir G/O=SI manualmente.
2. **Seguridad:** si hay fuga cross-tenant, JWT revocado admitido, exportación no autorizada o PII expuesta, bloquear esa operación y preservar evidencia; no desactivar validadores.
3. **Legacy NULL:** aislar `Cliente.EmpresaId=NULL` hasta reconciliación de owner con evidencia; no backfill por presunción ni borrar sin consentimiento/retención legal.
4. **Persistencia:** probar rollback aditivo y constraints en MySQL efímero, con recuentos antes/después, restores y referencias A/B; no probar sobre Aiven real o PROD.
5. **Concurrencia:** operaciones CAS/idempotentes deben reintentar sólo con contexto válido; no duplicar eventos/actividades/ventas ante timeout.
6. **CI:** un job CANCELLED/SKIPPED/FAIL no es PASS; verificar blobs de fuente y descendiente, scripts y dependencias; conservar VALIDANDO si falta prueba.
7. **UI/flags:** desactivar cambios futuros por bandera segura sin borrar historia ni conceder permisos; revertir CSS/flujo con tests de acceso/routing/móvil.
8. **Rollback documental:** `git revert` normal, commit aditivo, eventos BITACORA intactos, sin force-push/reset ni alteración de seis Tasks, horarios, main/QA/PROD.

## 6. A0–A5 y definición de terminado DOCUMENTAL

**A0:** revisión de fuentes del lote M01 y M02.01–05 + A5 y del DAG calculado, gates G01–G16, frontera de seguridad, tabla P/N, rollback y diferencia limitada a esta unidad y sección autocontenida de `CHANGELOG_AI.md`.

**A1 documental:** consistencia de predecesores/puertas y casos positivos, negativos, frontera y error escritos; no tests de runtime. **A2 documental:** lectura estática tenant A/B, JWT/RBAC, revocación, `Cliente.EmpresaId=NULL`, PII/consentimiento, CAS y no regresión de seguridad. **A3 documental:** `NO_APLICA` compilación .NET, HTTP/API, EF/MySQL, Angular/Playwright y proveedor externo, ya que solo se publican dos Markdown. **A4:** commit a `dev` con fast-forward/CAS, checks `verify-solqaryn-project-scope` y procedencia V5 `completed/success` por SHA exacto o descendiente verdaderamente equivalente, eventos idempotentes por transición y readback PLAN/COLA/DAG/BITACORA. **A5:** requiere Task Javier **distinta** de la autora, con revisión real de documento, diff, SHA, riesgos y evidencia; un LISTO A0–A4 no equivale a A5_PASS.

**Condición de salida:** completar solo el contrato M02.06 R0. La sucesora M03 (código, IAM, DB o frontend) no está liberada por esta narración: únicamente su fórmula real, A5 independiente y preflight propio pueden habilitarla. No cambiar QA, main, PROD, secretos, presupuestos ni datos reales. La entrega no constituye autorización comercial para campañas, datos personales, facturación o integración de terceros.
