# SOLQARYN V17 — C09-01.M01.06: bloqueos, rollback y liberación condicionada del CRM universal

**Microtarea:** C09-01.M01.06, PLAN_MAESTRO fila 1303. **Autor:** PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn@dev`. **Gobierno:** `PROJECT_SCOPE_LOCK=STRICT`; fuente institucional STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), calidad V15 y candado V17.

## Alcance exacto y frontera de seguridad

Esta entrega R0 define exclusivamente las **condiciones de bloqueo, reversión y liberación documental** de la preparación M01 del CRM universal: ficha del cliente, timeline, actividades, oportunidades, segmentos y seguimiento. No implementa nuevo CRM, endpoint, modelo Domain/EF, migración MySQL, cambio Angular, exportación, consentimiento activo, mensajes, worker, eventos, proveedores, autorizaciones JWT/RBAC, secretos, datos reales ni publicación fuera de `dev`.

La existencia actual de `Cliente`, controladores o repositorios no acredita un timeline operativo. Un `LISTO_DEV` documental **no satisface A5 de la propia unidad**, no declara cumplidas decisiones D01–D08 y no abre automáticamente M02. El acceso a datos de empresa y sucursal se decide siempre en backend por identidad y membresía vigentes, no por headers, cookies, ruta ni guard Angular.

## Fuente de preparación M01 y dependencias

La lectura previa constató en PLAN_MAESTRO `H1303=PENDIENTE`, `I1303` vacío, COLA `G1303=SI/O1303=SI`, DAG_TECNICO `F1264=VALIDADO/G1264=SI` tras A5 independientes de `C09-01.M01.04` y `M01.05` por DEV02; `M01.01–03` ya tenían A5 independientes. Para operar esta fila se exige namedRange V17 `SQV17_C09_01_M01_06`, ID `2000001303`, bloqueo H:I, CAS de H con readback de owner y evento idempotente. Estos valores no son permiso para un segundo escritor.

El DAG define además antecedentes `C01-01..04/C06-01..04/C07-04/C08-01..04 M01.01–05`, `C01-04.M01.06+A5` y `C07-04.M01.06+A5`. Su fórmula VALIDADO/SI habilita únicamente **este contrato preparatorio**; cada sucesora M02 requerirá nueva lectura dinámica, A5 propia y exclusividad.

| Antecesor | Blob Git del documento | Resultado utilizable / límite |
| --- | --- | --- |
| C09-01.M01.01 `docs/v5/c09-01-m01-01-alcance-crm-universal.md` | `4d35d8113cf1a1718dd7f487c50a06f51c1848b5` | límites de no regresión NR01–NR14 |
| C09-01.M01.02 `docs/v5/c09-01-m01-02-actores-interfaces-crm-universal.md` | `02a78bad5debea4678651624280a4353c54af24a` | A01–A14, interfaces y fronteras |
| C09-01.M01.03 `docs/v5/c09-01-m01-03-inventario-codigo-brechas-crm.md` | `34e0a8a652a9e92900644c3e7c8fc59ada58b5f8` | inventario selectivo de 16 rutas, G01–G12 |
| C09-01.M01.04 `docs/v5/c09-01-m01-04-riesgos-supuestos-decisiones-crm.md` | `8a700705ea49da7d729e4f1010496751575db790` | R01–R16 POTENCIALES, S01–S08 y D01–D08 PENDIENTES |
| C09-01.M01.05 `docs/v5/c09-01-m01-05-criterios-aceptacion-crm.md` | `102b942f604d9bbb5bb7c4a6c6d9537a5c890f39` | AC01–AC14 positivos/negativos FUTUROS_NO_EJECUTADOS, NFR01–NFR10 |

Los blobs son procedencia estática, no pruebas ejecutadas de JWT/RBAC, API, MySQL, autorización por fila, exportación o navegador. Los registros `Cliente.EmpresaId=NULL` legados **no autorizan acceso global** ni reetiquetado automático. Las decisiones de ownership, filtros, consentimiento y retención deben permanecer pendientes hasta autoridad y tests causales.

## Dieciséis puertas de bloqueo/release

| Gate | Prueba/condición futura requerida | Resultado de fallo |
| --- | --- | --- |
| G01 | Identidad Javier, Skill canónica, Scope Lock STRICT y SHA de dev | NO_GO a cualquier escritura no autorizada |
| G02 | PLAN H/I, COLA G/O, DAG F/G y A5 real de predecesores por ID | no liberar M01.06 si falta una condición |
| G03 | Named range V17 exclusivo, owner único, compare-and-set H y readback | abortar claim sin CAS, sin forzar fórmulas |
| G04 | Cliente, actividad y timeline ligados a EmpresaId y membresía server-side | negar acceso tenant B desde A |
| G05 | Legado NULL con política aprobada, no global ni remapeo implícito | cuarentena de owner incierto |
| G06 | Cada acción CRM verifica JWT/RBAC/rol, revocación y recurso | denegar endpoint aunque guard UI permita navegar |
| G07 | Timeline auditable, estable e inmune a repetición y colisiones | no crear actividad duplicada o mezclada |
| G08 | Estados/versionado de oportunidad y asignación con CAS transaccional | no pisar cambios de otro operador |
| G09 | Segmentos y filtros allowlisted, paginación y costos medidos | rechazar SQL/JS libre, barrido sin límites o B |
| G10 | Exportaciones minimizan PII con permiso específico y retención aprobada | no exportar cliente B, JWT, secretos ni logs |
| G11 | Consentimiento por finalidad/canal con opt-out y revocación | no enviar mensaje o campaña sin base válida |
| G12 | No alterar venta, saldo, facturación o auditoría financiera existente | rollback si se modifica asiento comercial |
| G13 | Errores, cache y timeouts fail-closed por empresa/rol | no exponer ficha o historial en fallo de autorización |
| G14 | UI 320px, teclado/foco/motion y direct-API negativos A/B | no atribuir seguridad a Angular solamente |
| G15 | Tests xUnit .NET 10, API JWT/RBAC, MySQL 8.4 efímero si EF, Playwright si UI, Scope Lock y V5 exact-SHA | FAIL/CANCELLED/SKIPPED/IN_PROGRESS no PASS |
| G16 | A5 por Task Javier de distinto TASK_ID que revisa SHA, diff, riesgo y pruebas | LISTO_DEV propio no habilita M02 sin A5 |

**Importante:** G04–G15 son **criterios de fases técnicas posteriores**, no PASS de esta tarea documental. Ninguna prueba futura puede inventarse como ejecutada; no hay métricas p95 ni aceptación legal/técnica aprobadas por un texto.

## Reglas de liberación A0–A5 y gestión de dependencias

**A0**: cotejar requisitos, cinco predecesoras, DAG real y cambio de sólo dos Markdown (este archivo y sección autocontenida de `CHANGELOG_AI.md`), política fail-closed y rollback. **A1**: consistencia documental de 16 gates, riesgos R01–R16 y AC01–AC14 positivos/negativos ya especificados como futuros. **A2**: examen estático de tenant A/B, NULL legado, JWT/RBAC, revocación, PII, consentimientos, SQL allowlist, CAS e idempotencia, sin afirmar hardening runtime. **A3**: pruebas .NET, HTTP, EF/MySQL, Angular/Playwright y proveedores **NO_APLICA al diff R0**, nunca PASS por omisión. **A4**: commit rápido fast-forward esperado sobre dev, comprobación de Scope Lock y V5 procedencia `completed/success` de SHA causal, idempotencia BITACORA A:Z y readback PLAN/COLA/DAG. Si un check requerido de esta documentación queda en progreso, mantener VALIDANDO con run/SHA/owner; si falla, reparar sin saltarse gobierno.

**A5** requiere otra de las cinco Tasks Javier, distinta de DEV02 y con revisión independiente del texto y SHA de M01.06. Después del A5 auténtico, M02 sólo se reclama si `H=PENDIENTE/I vacío`, `COLA G/O=SI` y `DAG F=VALIDADO/G=SI` por ID exacto; una Task a la vez, no reserva anticipada. M02 será documentación conceptual, mientras M03 es código y exige pruebas reales pertinentes. Nunca extrapolar A5 a QA/main/PROD ni levantar nuevos servicios.

## Oráculos futuros — doce pares sin ejecución

| Caso | Positivo esperado de futura implementación | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Usuario A autorizado consulta Cliente A y timeline A | petición A a Cliente B no revela PII |
| P02/N02 | CRUD actividad A queda ordenado por fecha y versión | replay/timeout no duplica actividades |
| P03/N03 | Oportunidad A cambia con estado y versión válida | rol lector, B y CAS obsoleto se rechazan |
| P04/N04 | Segmento A acepta operadores tipados allowlisted | filtro SQL/JS arbitrario o B falla cerrado |
| P05/N05 | Export A autorizado registra actor, motivo y campos mínimos | rol sin permiso exporta B o PII no permitida |
| P06/N06 | Consentimiento vigente por canal permite acción autorizada | opt-out/revocación bloquean emisión |
| P07/N07 | Cliente legacy con owner reconciliado se consulta en A | `EmpresaId=NULL` no se trata como global |
| P08/N08 | Revocación de membresía bloquea lectura posterior | JWT antiguo/caché no conserva privilegios |
| P09/N09 | Timeout o error devuelve estado seguro, sin acción duplicada | falla de repositorio no permite lectura por defecto |
| P10/N10 | Búsqueda paginada A respeta límites y costo medido | consulta ilimitada o inferencia de B se rechaza |
| P11/N11 | Angular 320px/teclado conserva foco y mensajes honestos | bypass de guard UI no permite API |
| P12/N12 | Revert futuro mantiene cliente, ventas y audit trail íntegros | migración reversible no destruye historial |

**Medición runtime actual:** 0 casos ejecutados de 12 pares descritos: `NO_MEDIDO`. Los resultados futuros exigen SHA de implementación y logs verificables (incluida `Category=Integration` con MySQL efímero al tocar EF). No utilizar datos de clientes, campañas, secretos ni servicios de pago para demostrar este Markdown.

## Reversión y manejo de incidentes

1. **Documento defectuoso o A5_FAIL:** conservar evento, autor real, SHA, diff, causa y regresión; aplicar reparación mínima o `git revert` ordinario con commit compensatorio y readback. No reescribir BITACORA.
2. **Carrera de escritor/CAS:** no transferir propietario ni borrar lock con corrida incierta; detener sólo el scope y actuar en otro independiente.
3. **P0/P1 de tenant/RBAC/PII futuro:** cortar capacidad afectada, comprobar negative tests API A/B, conservar datos y auditar propagación; no degradar validadores, ni borrar registros.
4. **Migración futura:** backup+restauración verificados y rollback probado antes en MySQL 8.4 efímero; no tocar Aiven productiva o bases reales desde esta unidad.
5. **Concurrencia y eventos futuros:** respuestas idempotentes por tenant/actor y CAS por versión, compensación auditable en vez de corrección silenciosa.
6. **Caché/consentimiento:** invalidar ámbito y revocación de acceso por tenant, sin exponer PII ni enviar mensajes reales.
7. **CI externa:** si required queued/in_progress mantener VALIDANDO; CANCELLED/SKIPPED/FAIL nunca PASS, reintento sólo con hipótesis nueva y SHA/blobs equivalentes comprobados.
8. **Cierre:** registrar SOURCE_SHA, TESTED_SHA, owner, A0–A4, A5_PENDIENTE y BITACORA readback; A5 ajena obligatoria para sucesora.

**Estado final previsto de este alcance:** sólo contrato de salida preparatoria M01, no CRM implementado, no pruebas live, no endpoints, PII, secretos, migración, QA/main/PROD, modificación de seis Tasks o nueva rama.
