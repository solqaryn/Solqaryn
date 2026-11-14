# SOLQARYN V5 FINAL — C00-03.M02.06: dependencias técnicas y contrato de salida del lote de cobertura

**Entrega:** TIPO 1, **documento de diseño**. **Autor:** SOLQARYN PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio y rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridades:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA); Skill, AGENTS y Scope Lock vigentes. Ningún plan archivado, otro repositorio o cuenta reemplaza estas autoridades.

**Resultado exigido:** fijar dependencias técnicas y contrato de salida de M02 para la matriz integrada *requisito previo → mejora → contrato → prueba → evidencia → cierre*, sin crear cola histórica. El cierre documental M02.06 solo habilita la **evaluación por ID de M03**, nunca una habilitación global, reescritura de fase, ejecución automática ni certificación de código futuro.

## 1. Cinco contratos M02 y base de integridad

Las cinco unidades M02.01–05 y M01.06 estaban `LISTO` al preflight, `PLAN_MAESTRO!H85=PENDIENTE`, I85 vacío, `COLA!G85/O85=SI`; no había regla individual de M02.06 en DAG_TECNICO y se exigió el gate conservador y control de concurrencia. Las lecturas de Git se anclan a blobs, no a promesas de pruebas live.

| ID V5 | Artefacto dev y blob SHA | Contrato de entrada/salida |
| --- | --- | --- |
| M01.06 | `docs/v5/c00-03-m01-06-cierre-matriz-integrada.md` — `8f83554e0526089c82cf730abb144e26d0b7da66` | Matriz R01–R12 y riesgos de cobertura, alcance M01 |
| M02.01 | `docs/v5/c00-03-m02-01-entidades-ownership-invariantes-cobertura.md` — `b6b16dfa1775ef07666d108580f6b21efe9b081a` | Entidades de referencia, identidad, ownership y 14 invariantes |
| M02.02 | `docs/v5/c00-03-m02-02-contratos-api-dto-errores-cobertura.md` — `803685cc695660ce2fadee3175b5d97e40e3f3ab` | DTO y operaciones conceptuales con versiones y errores, NO API implementada |
| M02.03 | `docs/v5/c00-03-m02-03-persistencia-cobertura.md` — `745e9a3eae0514ee83fe2de12a535fb084548c74` | Modelo de datos, claves y rollback **propuestos**, sin migración ni MySQL real |
| M02.04 | `docs/v5/c00-03-m02-04-configuracion-cobertura.md` — `d61ab68b98c828dfb09a84912cb15de88ce3e69a` | Configuración tipada, ámbitos y flags **conceptuales**, ninguno habilitado |
| M02.05 | `docs/v5/c00-03-m02-05-eventos-correlacion-cobertura.md` — `a8bfe808b1bad6aebfcf3a9c5296118d3f2f31ca` | Eventos de cobertura, idempotencia y límites **de diseño**, sin dispatcher |

**Diferencia de ámbitos:** C00-02/M02 rige el proceso de trabajo; C00-03/M02 rige la trazabilidad de la matriz de requisitos. Un contrato de una unidad no sustituye la implementación ni la revisión de la otra. Los documentos no ejecutan las transiciones descritas.

## 2. DAG de salida, exclusividad y gates de implementación

1. **Autoridad V5:** 2.340 IDs institucionales únicos. `PLAN_MAESTRO!H` es fuente de estado mutable; I conserva TASK_ID responsable, `COLA!E` lo refleja por fórmula y G/O indican puertas derivadas. `DAG_TECNICO!F=VALIDADO` y sus prerequisitos son evidencia auxiliar causal, no asignación.
2. **Contrato de entrada M03:** `C00-03.M02.01–06=LISTO` uno por uno, `C00-03.M01.06=LISTO`, fase y bloque reales, `COLA!G/O=SI`, no owner en PLAN I, seguridad/permiso del actor y no conflicto de archivos. Para cada M03 revisar RESULTADO_EXIGIDO y DAG individual; si no está en DAG, conservar gate conservador. Si una dependencia no está satisfecha, `NO_GO` del ID afectado, no fabricar su liberación.
3. **A5 y riesgos:** un `LISTO_DEV` con A5_PENDIENTE no equivale a A5_PASS ni autoriza QA/main/PROD. Los A5_FAIL históricos permanecen visibles hasta corrección y revisión independiente por una de las otras cinco Tasks Javier. Antes de promociones o cierres de fase sensibles deben revisarse los gates A5 causales, no asumir que la mera pertenencia a M02 los supera.
4. **Concurrencia:** dos Tasks no escriben el mismo ID; reclamo `PENDIENTE→EN_PROGRESO` con readback y evento único. La API ordinaria de Google Sheets no garantiza **CAS transaccional** de dos escritores; se debe comprobar ausencia de colisión y detener si no se puede garantizar exclusión. La meta `MAX(24,3*N)` no es permiso de despacho.
5. **Evidencia y publicación:** en VALIDANDO se auditan A0–A3 sobre candidato real, se publica FF directo a `dev` sin PR/ramas/force-push, y A4 exige check runs relevantes `head_sha=SHA_EXACTO` y readbacks. Los `SKIPPED`/pendientes no son PASS de prueba requerida. Una prueba .NET, Angular, MySQL, Docker o E2E se exige al futuro código que la necesite, no a este Markdown.
6. **BITACORA:** 26 campos A:Z, `EVENT_ID` y `IDEMPOTENCY_KEY` únicos, append-only y `RUN_ID` exclusivamente si lo devuelve el proveedor. Cualquier rastro de autor de cuenta retirada es evidencia histórica, no autorización. `TAREAS_PROGRAMADAS!AA:AN` es último autorreporte, no un candado ni un servicio de telemetría streaming.
7. **Cierre terminal:** `C15-05.M06.06` requiere las 2.339 microtareas anteriores certificadas, no existe una 2.341. Ningún resultado de M02.06 elimina controles JWT, BCrypt, RBAC, tenant A/B, PII, secretos y rollback.

## 3. Matriz de cobertura R01–R12 y prueba futura causal

| Ref | Riesgo, contrato o frontera que M02 define | Prueba exigible a la unidad que realmente implemente |
| --- | --- | --- |
| R01 Fuente V5 | Identificar requisito y microtarea canónica | catálogo 2.340 sin duplicados ni plan histórico despachable |
| R02 Identidad | Actor, owner, reviewer independientes | negativo de TASK_ID externo/no autorizado |
| R03 Exclusión | Claims y cambios de cobertura deduplicables | concurrencia real sin doble ganador, no inferir CAS de readback |
| R04 Dependencias | DAG validado, precedencias y G/O | negativo con predecesor no LISTO aunque O=SI |
| R05 Toolchain | .NET 10/Angular 22/MySQL 8.4 por impacto | job real requerido del SHA publicado, no build genérica |
| R06 A0 | Diff, secretos, contratos y dependencia vulnerable | reproducción de lint/inspección previa donde aplique |
| R07 A1/A2 | Positivos/negativos, JWT/RBAC/tenant A/B | denegaciones de backend + pruebas live cuando corresponde |
| R08 A3 | Consumidores, DB, integración, migración | MySQL/EF/Angular contract tests causales si se cambian |
| R09 A4 | SHA, parent, diff, rollback, readback | check-run `completed/success` del SHA exacto |
| R10 A5 | Revisor no autor y controles QA | revisión independiente de otro Javier por SHA |
| R11 Auditoría | BITACORA A:Z, claves únicas e idempotencia | rechazo de evento 21 columnas, RUN_ID inventado o duplicado |
| R12 Recuperación | Reversibilidad, seguridad y trazabilidad | `git revert` no destructivo y recuperación de datos solo si realmente probada |

Son **familias documentales de cobertura**, no nuevos IDs. Ninguna celda anterior acredita pruebas API/MySQL/browser ejecutadas ni valida la migración futura a Hostinger/Cloudflare/R2.

## 4. Ejemplo de orquestación V5 — solo oráculo documental

Para una microtarea M03 existente con todos los predecesores en LISTO: leer PLAN H/I, COLA G/O y DAG, comprobar actor y archivos sin conflicto, registrar un único claim, crear candidato, marcar VALIDANDO antes de A0–A3, ejecutar pruebas técnicas proporcionales usando GitHub Actions real, publicar directo a `dev` por FF, verificar CI sobre SHA exacto y hacer A4/readback. Registrar A5_PENDIENTE y LISTO solo con todas las auditorías requeridas; otra Task Javier revisará A5. Si un job necesario sigue `in_progress`, conservar exclusivamente ese ID en VALIDANDO y continuar otro independiente. Este es un **ejemplo** y no un registro de haber implementado ni ejecutado M03.

## 5. Aceptación del documento — oráculos, NO ejecución runtime

| Caso | Tipo | Resultado esperable |
| --- | --- | --- |
| P01 | + | Seis contratos M02.01–06 y M01.06 anclados a ID/blob, correspondencia R01–R12 trazable sin nuevo plan |
| P02 | + | PLAN I vacío, H PENDIENTE, COLA G/O SI y prerequisitos reales LISTO → ID puede ir a preflight, no a LISTO automático |
| P03 | + | A0–A3 documentales PASS, FF dev exacto, Scope Lock/procedencia PASS y readback → A4 de este documento puede cerrar |
| P04 | + | Contrato/proof ausente → cobertura marcada PENDIENTE; ningún runtime ficticio |
| N01 | − | M02.05 aún VALIDANDO o M02.06 pendiente, M03 con G/O=SI → bloquear M03 por lote incompleto |
| N02 | − | Owner ajeno o CAS no demostrado → denegar doble claim/relevo especulativo |
| N03 | − | Falta DAG VALIDADO o hay prerrequisito real incompleto → no liberar por cuota `MAX(24,3*N)` |
| N04 | − | Evento 21 campos, RUN_ID imaginario o clave duplicada → no append ni LISTO |
| N05 | − | Check obligado de SHA distinto, skipped o in_progress → conservar VALIDANDO |
| N06 | − | Revisor A5 es el autor o está fuera de seis Javier → no A5_PASS |
| N07 | − | Una unidad exige código pero solo tiene Markdown → no certificar implementación |
| N08 | − | Consulta o prueba de tenant A intenta usar datos B/PII/secreto real → rechazar |
| N09 | − | El documento se cita como prueba de migración MySQL/EF/Angular/Docker/Cloudflare → NO_APLICA, no PASS runtime |
| N10 | − | Una Task intenta crear 2.341, PR, branch, alterar QA/main/PROD, permisos o RRULE → NO_GO |
| N11 | − | Corrida de otro autor sobre mismos archivos/ID o HEAD avanzó → revalidar y conciliar sin reescritura |
| N12 | − | A5_FAIL previo sin corrección causal se presenta como aprobado → bloquear promoción y conservar evento histórico |

Estos son casos de revisión documental y deben probarse técnicamente si una microtarea posterior implementa su comportamiento.

## 6. Impacto, auditorías proporcionales y reversión

**Impacto de esta unidad:** únicamente `docs/v5/c00-03-m02-06-dependencias-salida-cobertura.md` y sección aditiva de `CHANGELOG_AI.md`. Ninguna modificación de API, DTO .NET, Domain, Application, Infrastructure, Angular, MySQL, índices, migraciones, workers, CI, proveedores, permisos activos o secretos. Consumidores: futuros M03–M06, revisores y el DAG institucional. `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`.

**A0** integridad Markdown, hashes, atribución y patrones de secretos; **A1** P01–P04/N01–N12 contra fuentes y condiciones; **A2** independencia, JWT/RBAC/tenant A/B, denegación y privacidad sin ejecutar nuevos permisos; **A3** seis blobs de fuentes y readback PLAN/COLA/DAG/BITACORA A:Z; **A4** commit FF, parent/diff, Scope Lock y procedencia del SHA exacto, readback. Las subpruebas .NET/Angular/MySQL/Docker/E2E son `NO_APLICA` solo porque este diff es documental, no PASS. **A5_PENDIENTE** de otro TASK_ID Javier, A5_PASS independiente antes de promoción con autorización específica.

**Rollback:** `git revert` normal del commit tras revalidar HEAD; conservar bitácora histórica, nunca force-push ni reset destructivo. No autorización para QA/main/PROD ni migración real.
