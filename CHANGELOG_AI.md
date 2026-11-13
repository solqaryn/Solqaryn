## 2026-11-13 — V5 C00-02.M03.06: auditoría estructurada de ciclo V5 [Javier :50]
- Implementación real `backend/src/Application/AutomationV5/V5WorkItemAuditLogger.cs`: logging estructurado de cambios de microtarea con formato estricto, códigos de error permitidos, UUID de correlación y rechazo sin emisión de entradas no seguras; no implementa CAS, BITACORA ni nueva autoridad.
- Pruebas xUnit positivas y negativas `backend/tests/Solqaryn.Tests/Application/V5WorkItemAuditLoggerTests.cs` y contrato `docs/v5/c00-02-m03-06-audit-logging.md`. A0–A3 proporcionales con .NET 10/seguridad, Scope Lock y provenance exact SHA para A4; sin MySQL, frontend, Docker, PR, ramas, QA/main/PROD; reversión `git revert`.

## 2026-11-13 — V5 C00-03.M03.02: servicio Application de cobertura integrada [Javier :00]
- `backend/src/Application/AutomationV5/V5RequirementCoverageProjectionService.cs` implementa proyección de matriz requisito→microtarea para IDs existentes con estado/owner/SHA verificados. Conserva faltantes explícitos, no concede PASS por una referencia documental ni crea microtareas, endpoints o persistencia.
- `backend/tests/Solqaryn.Tests/Application/V5RequirementCoverageProjectionServiceTests.cs` incorpora xUnit positivos y negativos: mapeo uno-a-varios, lote vacío, SHA inválido/obsoleto, snapshot ausente, ID/estado inválido y cobertura duplicada; contrato en `docs/v5/c00-03-m03-02-application-service.md`. Resultado CI .NET/seguridad/Scope Lock exact-SHA pendiente hasta su ejecución.
- Alcance Application puro, sin JWT/RBAC/tenant nuevo, MySQL, Angular, Docker, migraciones, worker ni despacho de eventos. A5 corresponde a otra Task Javier distinta; rollback mediante `git revert` normal, sin force-push.

## 2026-11-13 — V5 C00-02.M03.02: caso de uso Application V5 sin efectos externos [Javier :00]
- Se implementa `backend/src/Application/AutomationV5/V5WorkItemTransitionPlanner.cs`: propuestas inmutables de CLAIM, VALIDANDO y LISTO_DEV con expectativas de estado/owner; rechaza actor no autorizado y retorna intención nula ante gates negativos.
- Pruebas xUnit reales añadidas en `backend/tests/Solqaryn.Tests/Application/V5WorkItemTransitionPlannerTests.cs`; contrato y límites en `docs/v5/c00-02-m03-02-application-service.md`. CI .NET/seguridad/Scope Lock exact-SHA se verificará después de publicación; no atribuir PASS por anticipado.
- No implementa CAS, write a Sheets, endpoint, dispatcher, JWT nuevo, MySQL, migraciones ni acceso tenant; sólo preparación de transiciones. A5 corresponde a otra Task Javier; rollback mediante revert normal, sin reescritura.

## 2026-11-13 — V5 C00-03.M03.01: validación de dominio de cobertura integrada [Javier :50]
- Implementación .NET 10 en `backend/src/Domain/AutomationV5/V5RequirementCoverageRules.cs`: exige referencia institucional, ID del catálogo existente, versión, contrato, prueba y evidencia; rechaza duplicados compuestos y preserva correspondencias muchos-a-muchos sin abrir un backlog adicional.
- Pruebas xUnit `backend/tests/Solqaryn.Tests/Domain/V5RequirementCoverageRulesTests.cs` y contrato `docs/v5/c00-03-m03-01-coverage-domain.md`. A0–A3 con código real, CI dirigida a SHA exacto y Scope Lock; ninguna migración, nuevo dispatcher, endpoint, PR, rama ni promoción QA/main/PROD.

## 2026-11-13 — V5 C00-02.M03.01: reglas de dominio para cola V5 [Javier :50]
- C# real en `backend/src/Domain/AutomationV5/V5WorkItemRules.cs` valida identidad de microtarea, allowlist inyectado, G/O/DAG, exclusividad de owner, candidato, A0–A4, SHA exacto y A5 de revisor distinto; sin efectos externos ni simular CAS de Sheets.
- Pruebas xUnit en `backend/tests/Solqaryn.Tests/Domain/V5WorkItemRulesTests.cs` y contrato `docs/v5/c00-02-m03-01-domain-rules.md`; .NET y seguridad relevantes en CI exact-SHA, Scope Lock obligatorio. Sin nuevas ramas, PR, MySQL, Angular, cambios de tenants ni QA/main/PROD; rollback aditivo `git revert`.

## 2026-11-13 — V5 C01-01.M01.06: gates de identidad universal, rollback y salida documental M01 [Javier :40]
- Documento `docs/v5/c01-01-m01-06-gates-bloqueo-rollback-identidad.md`: siete prerequisitos verificados, sustitución A5 vigente Javier para la M01.05 histórica, fronteras JWT/RBAC/tenant, bloqueo/reversión y liberación M02 solo por preflight individual.
- Alcance TIPO1 documental: P01–P03/N01–N12, A0–A3 proporcionales y A4 Scope Lock + procedencia exact SHA; no modifica API/Angular/MySQL, no certifica runtime, no PR/ramas ni QA/main/PROD.

## 2026-11-13 — V5 C00-04.M01.06: gates de gobierno de cambios y liberación documental M01 [Javier :30]
- `docs/v5/c00-04-m01-06-bloqueo-rollback-liberacion.md` fija seis dependencias verificadas (cinco C00-04.M01 y C00-01.M06.06), A5 correctiva independiente, DAG VALIDADO, matriz de bloqueos, autorización, rollback y condición de evaluación M02 sin despacho automático.
- Solo contrato documental TIPO 1: A0–A3 proporcionales, revisión de fronteras JWT/RBAC/tenant y Scope Lock/procedencia exact-SHA para A4; A5 independiente de otra Task Javier. .NET/Angular/MySQL/Docker/E2E no aplican al diff, sin PR/ramas ni QA/main/PROD.

## 2026-11-13 — V5 C00-03.M02.06: cierre contractual de cobertura M02 [Javier :10]
- Documento `docs/v5/c00-03-m02-06-dependencias-salida-cobertura.md`: matriz R01–R12, seis fuentes M01/M02 con blobs verificables, dependencias y gates estrictos de salida, 4 positivos/12 negativos documentales; no implementación, activación de dispatcher ni M03 automático.
- A0–A3 documentales dirigidos, evidencia A:Z/26, JWT/RBAC/tenant, Scope Lock y validación de procedencia exact-SHA antes de A4; A5 independiente. Sin PR/ramas, QA/main/PROD, migraciones ni proveedores activos.

## 2026-11-13 — V5 C00-03.M02.05: contrato de eventos de cobertura integrada [Javier :10]
- Documento `docs/v5/c00-03-m02-05-eventos-correlacion-cobertura.md`: define tipos de evento lógicos, claves de cobertura e idempotencia, trazabilidad y límites del mapa requisito↔microtarea sin desplegar dispatcher, CAS, API, worker ni DB.
- Contrato documental TIPO 1: oráculos P01–P04/N01–N10, autoridad PLAN/COLA/DAG/BITACORA A:Z, cinco hashes de fuentes, seguridad tenant/JWT/RBAC, Scope Lock exact-SHA y A5 independiente; sin QA/main/PROD.

## 2026-11-13 — V5 C00-03.M02.04: configuración tipada de cobertura [Javier :00]
- `docs/v5/c00-03-m02-04-configuracion-cobertura.md`: contrato de configuración, ámbitos, 11 parámetros tipados propuestos y cuatro feature flags conceptuales no activos, con límites JWT/RBAC/tenant/CI y 4 positivos/10 negativos; sin código ni config real.
- TIPO 1: A0–A3 documentales sobre blob `d61ab68b98c828dfb09a84912cb15de88ce3e69a`, .NET/Angular/MySQL/Docker/E2E NO_APLICA a este diff. A4 commit FF con Scope Lock + V5 attribution exact-SHA, A5 independiente otra Javier; sin QA/main/PROD/branches/PR, rollback `git revert`.

## 2026-11-13 — V5 C00-03.M02.03: persistencia de matriz de cobertura [Javier :00]
- `docs/v5/c00-03-m02-03-persistencia-cobertura.md`: esquema lógico propuesto de vínculos requisito–microtarea–contrato–prueba–evidencia, índices y migración reversible futura. Fija 4 oráculos P01–P04 y 10 negativos N01–N10, sin crear SQL, EF migrations ni nuevo backlog.
- Entrega documental TIPO 1: A0–A3 de diff, fuente, seguridad y casos sobre blob `745e9a3eae0514ee83fe2de12a535fb084548c74`; .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff. A4 requiere commit FF y Scope Lock SUCCESS exact SHA; A5_PENDIENTE de Task Javier diferente, sin QA/main/PROD, ramas ni PR. Reversión por `git revert`.

## 2026-11-13 — Rectificación causal de procedencia V5 C00-04.M01.01–05 (A5 pendiente)
- Se reagrupan siete viñetas preexistentes bajo cinco encabezados originales (DAG20, C01-02.M01.04 y C01-03.M01.03/.04/.05). Las secciones C00-04.M01.01–05 de Javier :10 conservan sus propias viñetas. No se borran pruebas ni entradas historicas.
- El validador `scripts/quality/verify-v5-changelog-provenance.mjs` y workflow `.github/workflows/v5-changelog-provenance.yml` detectan nuevas secciones vacías o atribuciones cruzadas sin compilar código runtime innecesariamente.
- Los A5_FAIL originales siguen vigentes hasta aprobación independiente sobre SHA correctivo por una Task Javier distinta de J10. No se declara A5_PASS automático ni se cambia QA/main/PROD. Rollback mediante revert normal en dev, nunca force-push.

## 2026-11-13 — V5 C00-02.M02.06: contrato de dependencias y salida M02 [Javier :00]
- `docs/v5/c00-02-m02-06-dependencias-salida.md`: integra M02.01–05 con prerequisitos, autoridad PLAN/COLA/DAG, transiciones e idempotencia BITACORA A:Z/26, ejemplo de ciclo y oráculos P01–P03/N01–N08; no crea código/servicio/DB.
- TIPO 1 documental: A0–A3 verificación de fuentes, casos, seguridad y ausencia de secretos sobre blob `95cdaef09583fc859aa25228057e1f2c22f60477`; .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff; A4 posterior con commit FF y Scope Lock SHA exacto; A5 independiente de otra Javier. Sin QA/main/PROD, PR, ramas ni force-push; rollback normal `git revert`.

## 2026-11-13 — V5 C00-03.M02.02: interfaces, DTO y errores de la matriz integrada [Javier :40]
- Documento `docs/v5/c00-03-m02-02-contratos-api-dto-errores-cobertura.md`: contratos de lectura/mapeo/validación de cobertura, DTO propuestos, compatibilidad V5 FINAL, errores de seguridad y 16 oráculos P01–P04/N01–N12; no crea API/DB, nuevas Tasks ni sustituye PLAN H/I.
- TIPO 1 documental, A0–A3 sobre blob 803685cc695660ce2fadee3175b5d97e40e3f3ab; .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff; A4 Scope Lock exact SHA/commit FF dev/readback y A5 independiente pendiente. Sin PR, ramas, force-push ni QA/main/PROD; rollback git revert.

## 2026-11-13 — V5 C00-03.M02.01: entidades e invariantes de la matriz integrada de cobertura [Javier :40]
- `docs/v5/c00-03-m02-01-entidades-ownership-invariantes-cobertura.md`: modelo lógico de doce entidades, catorce invariantes y correspondencia documental R01–R12 entre requisitos, mejoras, contratos, pruebas y evidencias, sin duplicar los 2.340 IDs ni declarar implementación.
- Entrega TIPO 1: A0–A3 documentales antes del commit sobre blob b6b16dfa1775ef07666d108580f6b21efe9b081a; casos P01–P04 y N01–N12 de aceptación contractual; .NET/Angular/Node/MySQL/Docker/E2E NO_APLICA al diff sin código. A4 exige commit FF dev, Scope Lock exact SHA y readback. A5 pendiente de otra Task Javier, sin QA/main/PROD, sin PR/ramas. Reversión git revert.

## 2026-11-13 — V5 C00-02.M02.03: modelo de persistencia, índices y migración reversible [Javier :40]
- `docs/v5/c00-02-m02-03-persistencia-indices-migraciones-datos-prueba.md`: especifica almacenamiento institucional PLAN/COLA/DAG/BITACORA A:Z/monitor, unicidad, índices relacionales **propuestos no implementados**, plan de migración futuro reversible y casos P01–P04/N01–N09 documentales; reconoce que Sheets no proporciona CAS seguro por una simple lectura/escritura.
- TIPO 1 documental: A0–A3 proporcionales sobre blob eb3110d591950728b325396a5cf7902d857fff3f; .NET/Angular/MySQL/Docker/E2E NO_APLICA a este diff y NO se ensayó migración/backup live. A4 exige commit FF, Scope Lock exact SHA y readback; A5 revisor distinto del allowlist Javier; sin QA/main/PROD, PR ni ramas; rollback git revert.

## 2026-11-13 — V5 C00-02.M02.02: contrato de API/DTO, errores y compatibilidad [Javier :40]
- Documento `docs/v5/c00-02-m02-02-api-dto-errores-compatibilidad.md`: contrato documental de lectura, claims, candidato, auditorías, eventos A:Z y cierre con errores lógicos, compatibilidad y casos positivos/negativos. Aclara que no existe CAS nativo demostrado ni nuevo endpoint/worker, con prohibición de elevar permisos tenant.
- TIPO 1 documental sin código ejecutable; A0–A3 revisión documental previa sobre blob 6e8170eea8fe05bba4f5610fced1d159719e03f1; .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff; A4 requiere Scope Lock exact-SHA, readback institucional y A5 pendiente de otra Task Javier. Sin QA/main/PROD, ramas, PR ni force-push; rollback por git revert.

## 2026-11-13 — V5 C03-04.M01.02: actores y contratos de incidentes/continuidad [Javier :00]
- Nuevo `docs/v5/c03-04-m01-02-actores-entradas-salidas-continuidad.md`: 15 actores, 16 interfaces y 12 oráculos positivos/negativos para incidentes, revocación, backup, restore y continuidad, distinguiendo YAML/contratos de ejecución real; 15 predecesores DAG LISTO.
- Entrega documental, sin .NET, Angular, MySQL, Docker, E2E, secretos, datos ni despliegue; A0–A3 documentales, A4 Scope Lock exact-SHA después del FF en dev, A5 por otra Task Javier. Sin QA/main/PROD ni ramas/PR.

## 2026-11-13 — Seis Tasks Javier: multi-microtarea por corrida, vigilancia segura y monitor de última señal
- Se reemplazaron exclusivamente los prompts de seis Tasks Javier con bucle hasta agotar trabajo seguro o límites reales, CI independiente en VALIDANDO y verificación de compañeras con readback y exclusión atómica; sin tocar sus horarios, IDs, títulos ni is_enabled.
- Sheet V5: extendida TAREAS_PROGRAMADAS a AA:AN, añadido Dashboard A41:J51 y CONFIG A149:C150. No se tocaron PLAN_MAESTRO/COLA/BITACORA y sus nueve pestañas se conservan.
- Monitoring de estado nativo es fotografía fechada, no streaming; los reportes por corrida se actualizarán en futuras ejecuciones verificables. Dispatcher por eventos solo análisis futuro, ningún servicio nuevo ni relanzamientos ciegos.
- Rollback seguro: revert documental en dev mediante fast-forward, restauración controlada de prompts/sheet previa verificación; sin force-push, QA/main/PROD, secretos ni datos productivos.

## 2026-11-13 — V5 C03-03.M01.04: riesgos/supuestos/decisiones auditoría de seguridad [Javier :10]
- `docs/v5/c03-03-m01-04-riesgos-supuestos-decisiones-auditoria.md`: 14 riesgos R01–14, 8 supuestos S01–08 y 8 decisiones D01–08, anclados a ocho blobs de código y diez precedentes DAG LISTO. Delimita consultas A/B, auditoría best-effort, logging de secretos, correlación, retención y acceso excepcional sin afirmar una vulnerabilidad ni certificación runtime sin prueba.
- Autor 6af16bb9492c8191b8e8c7d606f11507; A0–A3 PASS documentales, .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff de documentos. A4 exige Scope Lock exact SHA y readback, A5 revisor Javier independiente. Sin QA/main/PROD, permisos/infra/código ejecutable, ramas o PR; rollback normal git revert.

## 2026-11-13 — V5 C00-02.M02.01: entidades y exclusividad [Javier :20]
- Documento `docs/v5/c00-02-m02-01-entidades-ownership-invariantes.md` define diez entidades, 12 invariantes de identidad, DAG, owner, estados y SHA, ejemplo de ciclo y tres casos positivos/nueve negativos para evitar duplicados.
- Cambio exclusivamente documental, sin runtime: A0–A3 proporcionales, A4 Scope Lock SHA exacto, A5 otra Task Javier; no nuevas filas/Tasks, branches, PR ni QA/main/PROD. Rollback git revert.

## 2026-11-13 — V5 C00-02.M02.04: configuración tipada y flags seguros [Javier :20]
- Documento `docs/v5/c00-02-m02-04-configuracion-tipada-ambitos-flags.md` define frontera config entorno/tenant/usuario, cinco blobs existentes, validaciones tipadas y feature flags futuros no activos; tres casos positivos y nueve negativos con Scope Lock.
- Alcance solo documental, sin editar JSON/env/secretos, endpoints, JWT, DB ni Angular. A0–A3 proporcionales, A4 Scope Lock exact SHA, A5 por otra Task Javier. Sin QA/main/PROD, branches o PR; rollback git revert.

## 2026-11-13 — V5 C03-03.M01.02: actores/IO auditoría de seguridad [Javier :10]
- `docs/v5/c03-03-m01-02-actores-entradas-salidas-auditoria.md`: 12 actores ACT01–12, 14 entradas/salidas IO01–14, 10 casos futuros T01–T10 de auditoría de autorizaciones, denegaciones, cambios críticos, correlación, retención y no exposición; 8 blobs reales y 10 antecedentes DAG LISTO. No acredita HTTP/MySQL/export/alerta live ni acceso excepcional real.
- Autor Task 6af16bb9492c8191b8e8c7d606f11507; A0–A3 PASS documentales sobre candidato, A4 Scope Lock exact SHA + readback pendiente, A5 por otra Task Javier. No se modifican .NET, Angular, MySQL, Docker, secretos, QA/main/PROD, PR o ramas; rollback normal git revert.

## 2026-11-13 — V5 C00-02.M02.05: eventos, idempotencia y correlación [Javier :20]
- Documento `docs/v5/c00-02-m02-05-eventos-idempotencia-correlacion.md` define entidades lógicas, 26 columnas A:Z, EVENT_ID/IDEMPOTENCY_KEY, transiciones, SHA causal y rollback, tres casos positivos y nueve negativos; respeta DAG y seis Tasks Javier.
- Alcance estrictamente documental: A0–A3 verificaciones de artefacto/seguridad, A4 Scope Lock del SHA exacto, A5 independiente. No código, .NET/Angular/MySQL/Docker/E2E, PR, ramas ni QA/main/PROD. Sin certificación runtime ni CAS de Sheets inventado.

## 2026-11-13 — V5 C03-03.M01.03: inventario auditado de seguridad [Javier :10]
- `docs/v5/c03-03-m01-03-inventario-auditoria-seguridad.md`: ocho fuentes de API/servicio/modelo/repository/middleware/test con SHA, 12 controles de cobertura (3 positivos, 8 negativos, 1 control), brechas reales delimitadas: consulta A/B requiere revisar filtros globales; best-effort audit no garantiza durabilidad; correlación no es idempotencia y redacción/retención quedan pendientes.
- Autor 6af16bb9492c8191b8e8c7d606f11507. A0–A3 PASS documentales, sin runtime/.NET/Angular/MySQL/Docker/E2E por no cambiar código ejecutable. A4 Scope Lock exact-SHA y readback tras FF en dev; A5 independiente pendiente. No QA/main/PROD, no PR/branches, rollback `git revert`.

## 2026-11-13 — V5 C03-03.M01.05: aceptación de auditoría de seguridad [Javier :10]
- `docs/v5/c03-03-m01-05-criterios-auditoria-seguridad.md`: 16 criterios AU01–AU16 (autorización/denegación/cambio crítico/acceso excepcional, correlación, retención y no exposición), con ocho fuentes de código y diez prerrequisitos DAG LISTO. Delimita riesgos de tenant A/B, auditoría best-effort vs estricta, redacción PII y pruebas futuras sin declararlas ejecutadas.
- Autor 6af16bb9492c8191b8e8c7d606f11507; A0–A3 PASS documental antes del commit, A4 exact-SHA y A5 independiente pendientes. No ejecuta .NET/Angular/MySQL/Docker/E2E para un cambio de documentación; no modifica runtime, secretos, QA/main/PROD, ramas o PR; rollback `git revert`.

## 2026-11-13 — C00-03.M01.06: cierre M01 matriz integrada V5 [Javier :00]
- Documento docs/v5/c00-03-m01-06-cierre-matriz-integrada.md: 5/5 M01 LISTO con A5 causal, C00-01.M06.06 LISTO+A5, matriz de 12 referencias → mejora/contrato/prueba/cierre, 2 casos positivos y 12 negativos de gobierno documental.
- No crea backlog, cola, PR ni ramas; no ejecuta pruebas de runtime ni libera M02 sin sus puertas. A0–A4 proporcional y Scope Lock exacto; A5 de otra Task Javier, cero QA/main/PROD.

## 2026-11-13 — C00-02.M01.06: cierre documentado de M01 y puerta condicional M02 [Javier :00]
- Contrato docs/v5/c00-02-m01-06-bloqueo-rollback-liberacion-modelo.md: cinco M01 LISTO+A5, cierre C00-01.M06.06 LISTO+A5 causal, DAG VALIDADO, máquina de estados, CAS de owner, BITACORA 26 campos, dos casos positivos y doce negativos documentales.
- Preparación sin ejecución de despachador ni liberación ficticia M02; A0–A4 proporcionales, Scope Lock exact-SHA, A5 de otra Task Javier, sin PR/branches ni QA/main/PROD.

## 2026-11-13 — V5 C03-01.M01.05: aceptación observable de autenticación y recuperación (Javier :10)
- `docs/v5/c03-01-m01-05-aceptacion-autenticacion-recuperacion.md`: 16 criterios AC01–AC16 (2 positivos, 12 negativos, 2 controles) de login/renovar/revocación/recuperación futura, con oráculos HTTP/DB, 10 blobs de código/tests y 10 predecesores DAG LISTO. Diferencia evidencias estáticas/mocks de pruebas runtime exigidas a una futura implementación.
- Autor TASK_ID 6af16bb9492c8191b8e8c7d606f11507. A0–A3 PASS documental antes de publicar: calidad/no secretos, aceptación falsable, seguridad JWT/RBAC/tenant A/B y contratos. Dotnet/Angular/MySQL/Docker/E2E NO_APLICA a este cambio documental. A4 requiere Scope Lock del SHA publicado y readback; A5 pendiente de otra Task Javier. Sin APIs/DB/proveedores/QA/main/PROD/PR/ramas; rollback con git revert no destructivo.

## 2026-11-13 — V5 C03-01.M01.03: inventario de autenticacion y pruebas existentes (Javier :50)
- `docs/v5/c03-01-m01-03-inventario-contratos-codigo-evidencia.md`: ocho fuentes de codigo + tres tests con Git blobs, pruebas delimitadas de login, renovar, JWT, localStorage, tenancy y session-activity; trece controles pendientes para implementación futura.
- Separa archivos reales de mocks/EF InMemory; NO certifica recuperacion, revocacion JWT global, refresh rotativo, MFA/passkeys ni ataques HTTP 429. A0-A3 documentales antes del commit; A4 exact-SHA Scope Lock, A5 otra Task Javier. Sin runtime, secretos, QA/main/PROD, PR ni ramas; rollback git revert.

## 2026-11-13 — C03-02.M01.02: actores, IO y superficies RBAC/ABAC [Javier :00]
- Documento docs/v5/c03-02-m01-02-actores-entradas-salidas-superficies-rbac-abac.md: nueve actores conceptuales, catorce entradas/salidas y catorce escenarios documentales positivos/negativos futuros. Siete blobs de código contrastables; límites JWT/RBAC/tenant/propiedad y exportación.
- Únicamente preparación sin API/DB/UI ni tests runtime nuevos; DAG 10/10, A0–A4 proporcional, A5 otra Task Javier, Scope Lock SHA exacto, sin QA/main/PROD.

## 2026-11-13 — V5 C03-01.M01.02: actores y superficies de autenticación (Javier :50)
- Nuevo `docs/v5/c03-01-m01-02-actores-entradas-salidas-autenticacion.md`: diez actores, catorce entradas/salidas y catorce casos de aceptación FUTURA positiva/negativa. Distingue login/renovar existentes de recovery/MFA/passkeys y revocación global no acreditadas.
- Siete blobs Git revisados, A0-A3 documentales antes de publicar; .NET/Angular/MySQL/Docker/E2E NO_APLICA. A4 requiere commit fast-forward SHA exacto y Scope Lock; A5 a otra Task Javier. Sin runtime, nuevas cuentas, secretos, QA/main/PROD ni PR. Rollback con git revert.

## 2026-11-13 — C03-02.M01.01: objetivo y no-regresión RBAC/ABAC [Javier :00]
- Documento docs/v5/c03-02-m01-01-objetivo-no-regresion-rbac-abac.md define fronteras server-side de empresa, sucursal, propiedad, archivos y exportaciones y 16 criterios futuros positivos/negativos; 7 contratos Git cotejados por blob, DAG 10/10.
- Exclusivamente documental: no implementación ni PASS runtime de RBAC/ABAC; A0–A4 proporcionales, Scope Lock exact SHA y A5 independiente de otra Task Javier; sin JWT/roles/DB/UI/QA/main/PROD.

## 2026-11-13 — V5 C03-01.M01.01: contrato documental de autenticación y recuperación (Javier :50)
- `docs/v5/c03-01-m01-01-objetivo-autenticacion-recuperacion.md` fija objetivo, exclusiones y 16 casos de no-regresión FUTUROS para login, renovación, revocación, recovery, abuso, tenant A/B y MFA/passkeys solo con proveedor aprobado.
- Siete fuentes Git inspeccionadas; tipo 1 documental sin C# ni Angular/MySQL/Docker/E2E, A0-A3 verificación documental dirigida. A4 exige SHA nuevo y Scope Lock exacto. A5 pendiente de otra de cinco Tasks Javier; no QA/main/PROD. Rollback: git revert del commit.

## 2026-11-13 — V5 C02-04.M01.05: aceptación futura de consola global [Javier :40]
- Documento de 24 criterios de aceptación AC01–AC24: 6 positivos y 18 negativos FUTUROS para rol global separado de ERP, MFA/reautenticación, grants temporales, motivo, auditoría crítica, tenant A/B, PII y rollback. Seis fuentes Git respaldan limites, sin pruebas runtime atribuidas.
- TIPO1 documental, A0–A4 proporcional y Scope Lock SHA exacto; A5 de otra Task Javier. No crear permisos/MFA reales ni liberar M01.06/M02, QA/main/PROD.

## 2026-11-13 — C03-02.M01.04: riesgos de autorización y privacidad (Javier :50)
- Nuevo docs/v5/c03-02-m01-04-riesgos-supuestos-decisiones.md: R01–R12, S01–S06 y D01–D06 con trazabilidad a inventario y aceptación, cobertura BOLA/IDOR, claims falsos, sucursales, archivos y exportaciones. No certifica funcionamiento HTTP/MySQL ni aplica cambios JWT/RBAC.
- Documental A0-A4 proporcional, Scope Lock exact-SHA; A5 independiente de otra Task Javier. No cambia código, migraciones, Tasks, QA/main/PROD; rollback git revert.

## 2026-11-13 — V5 C02-04.M01.04: riesgos y decisiones de consola global [Javier :40]
- Matriz documental 15 riesgos R, 8 supuestos S y 10 decisiones D sobre privilegios globales, MFA/reauth, TTL, tenant A/B, PII, soporte DEV limitado y auditoría crítica. Fuentes Git verificadas; mecanismos futuros no implementados ni certificados.
- TIPO1 sin runtime/secretos ni migraciones; A0–A4 documentales y Scope Lock exact SHA; A5 pendiente independiente Javier, sin liberar M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — C00-01.M06.06: cierre documental con A5 causal y mapa DAG (Javier :50)
- Se incorpora docs/v5/c00-01-m06-06-cierre-baseline-mapa-dependencias.md: 35/35 predecesoras LISTO, causal A5 de M04.03 (Chromium 3/3, revisor independiente), cuatro dependientes dinámicos, inventario de blobs y contradicciones. El cierre no despacha automáticamente ni autoriza QA/PROD.
- A0-A4 proporcionales a documento, Scope Lock exact-SHA y readback PLAN/COLA/DAG/BITACORA, A5 independiente pendiente. Sin runtime/DB/UI, Tasks, branches o PR; rollback git revert.

## 2026-11-13 — V5 C02-04.M01.03: auditoría de contratos y evidencia de consola global [Javier :40]
- Inventario documental de diez archivos/blobs observados en dev, capacidades existentes parciales y controles NO_CERTIFICADOS de MFA, reautenticación, grants TTL, auditoría crítica, rollback y consola global. Tres positivos/seis negativos futuros propuestos, no pruebas live.
- TIPO1 documental; A0-A4 proporcional, Scope Lock SHA exacto y A5 independiente. Sin cambios de JWT/RBAC/Angular/.NET/MySQL, grants, datos ni QA/main/PROD.

## 2026-11-13 — V5 C02-04.M01.02: actores y entradas/salidas de consola global [Javier :40]
- Matriz documental de 8 actores, 12 IO y 9 casos (+/-) FUTUROS para consola global: separación de plataforma/ERP, MFA/reautenticación, motivos, grants temporales, aislamiento tenant A/B y auditoría crítica, con seis fuentes Git por blob.
- M01.02 TIPO1 sin cambios .NET/Angular/MySQL/secretos. A0–A4 documentales, Scope Lock exact-SHA, A5 ajeno; no habilita M01.06, MFA real ni QA/main/PROD.

## 2026-11-13 — Reparación de gate de calidad ERP para censo V5 de seis Tasks
- `scripts/quality/priority4_quality_audit.py`: mantiene las comprobaciones estrictas de seguridad e identidad, pero actualiza la aserción obsoleta de ocho Tasks a seis Tasks y comprueba `PHYSICAL_TASKS_REGISTERED=6`.
- `docs/AUTOMATION_STATUS.md`: declara explícitamente `LEGACY_AUTOMATION_RUNTIME=RETIRED`, conservando `VAEP_DISPATCH_AUTHORIZED=FALSE` y `JULES_WORKERS_AUTHORIZED=0`. No se activan motores ni cuentas nuevas.
- El cambio solo alinea la política CI vigente; no modifica frontend/backend, secrets, QA/main/PROD ni las evidencias independientes de M04.03.

## 2026-11-13 — C03-02.M01.05: aceptación RBAC/ABAC documental (Javier :50)
- Nuevo docs/v5/c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md: 14 criterios observables, incluidos BOLA/IDOR, claims falsos, sucursal cruzada, archivos, export y revocación; 6 reglas de no-regresión y rutas de futura prueba HTTP+MySQL/Playwright. Son contratos futuros, no se declaran PASS live.
- DAG VALIDADO 10/10; Scope Lock exact-SHA, A0-A4 documental proporcional y A5 pendiente de otra Task Javier. Ningún cambio runtime, QA/main/PROD, PR, rama ni Tasks.

## 2026-11-13 — C03-02.M01.03: inventario de autorizacion/privacidad V5 (Javier DEV 06)
- Se incorpora docs/v5/c03-02-m01-03-inventario-autorizacion-privacidad.md: 9 contratos Git con blobs, 5 suites de pruebas existentes y matriz de brechas BOLA/IDOR, claims falsos, sucursal cruzada y exportacion ajena. Alcance M01.03 documental; no se interpreta como certificacion de ABAC o integracion live.
- DAG VALIDADO y 10/10 prerrequisitos documentales LISTO; A0 revisa estructura/secretos, A1-A3 proporcionales al documento, A4 requiere SHA de publicacion y Scope Lock. A5 de otra Task Javier; rollback por git revert normal. Sin cambios C#/Angular/MySQL, Tasks, QA/main/PROD, ramas o PR.

## 2026-11-13 — V5 C02-04.M01.01: objetivo/no-regresión de consola global auditada [Javier :40]
- Contrato documental M01.01 de separación plataforma/ERP, MFA/reautenticación, motivo, acceso excepcional temporal y auditoría crítica; 15 criterios NR (+/-) FUTUROS, seis fuentes Git verificadas por blob SHA.
- TIPO1 sin código ejecutable ni cambio de JWT/RBAC/MySQL/Angular/secretos; DAG C02-01/02/03 M01.01-05 prerequisitos LISTO. A0–A4 documentales proporcionales, Scope Lock exact-SHA y A5 independiente; no habilitar M01.06/M02, QA/main/PROD.

## 2026-11-13 — M04.03: regresión Chromium real A→login y A→B; A5 sujeto a CI
- Añadido `frontend/e2e/c00-01-m04-03-route-context.spec.ts` con HTTP público retenido en navegador, una prueba positiva de branding y dos negativas de SEO/tema después de navegar sin recarga. No se simula Angular Router ni AppComponent.
- Añadido workflow dirigido `.github/workflows/v5-m04-03-browser-regression.yml`, que ejecuta lint, build, unitarios y Playwright Chromium en entorno descartable; no requiere deployment Vercel ni toca QA/main/PROD.
- Histórico A5_FAIL permanece hasta evidencia CI exact-SHA y revisión independiente distinta del autor. Rollback normal por revert en dev.

## 2026-11-13 — Orden vigente: SOLQARYN con seis Tasks Javier y Alex retirado
- Por orden expresa del propietario, el allowlist comercial y el despacho operativo quedan restringidos exclusivamente a las seis Tasks Javier existentes (:00, :10, :20, :30, :40, :50). DEV 07 y DEV 08 Alex no conservan permisos institucionales de despacho, reactivación, relevo ni revisión A5. Sus Tasks privadas se reportaron pausadas, sin lectura física desde esta cuenta.
- Se actualizaron `docs/AUTOMATION_STATUS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `AGENTS.md` y el Google Sheet V5 (CONFIG, TAREAS_PROGRAMADAS, AUTOMATIZACIONES). Se mantuvieron ID, RRULE y habilitación de Javier. Evidencia histórica en BITACORA, Git y PLAN_MAESTRO conservada, sin interpretación como autorización activa.
- Única microtarea con owner Alex no LISTO identificada: C01-04.M01.01 en VALIDANDO. Instrucción de rescate exclusiva a una Task Javier con lectura física, ausencia de lease/escritura concurrente, CAS de owner y A0–A5 reales; no se ha falseado transición LISTO ni A4. Corrección de clave de compatibilidad de self-test de Skill, sin reintroducir VAEP/Jules. Alcance documental, sin tocar frontend/backend/DB ni QA/main/PROD. Reversión: commit inverso normal y restauración auditada del Sheet, no force-push.

## 2026-11-13 — Rescate cruzado seguro entre Tasks [orden expresa propietario]
- Se autoriza a un ejecutor disponible a resolver bloqueos de microtareas ajenas mediante ayuda con pruebas/CI o correcciones causales, conservando el owner activo. Handoff exclusivo solo sin lease activo, compare-and-swap y eventos idempotentes; revisor A5 siempre independiente. Alex y Javier conservan separación de cuentas y permisos. Ninguna Task ni su horario cambia. 
- Cambia el gobierno canónico AGENTS, AUTOMATION_V13 y AUTOMATION_STATUS, sin alterar código, DAG, asignaciones, QA/main/PROD. Rollback normal git revert, no force-push.

## 2026-11-13 — V5 C02-03.M01.05: criterios de aceptación del ciclo de vida tenant [Javier :20]
- Contrato documental preliminar AC01–AC24: seis positivos y dieciocho negativos FUTUROS para alta/suspensión/reactivación/archivo/baja lógica idempotentes, concurrencia, JWT/RBAC/tenant A/B, retención y rollback; cinco fuentes Git con SHA.
- Sin ejecución runtime ni cambios de entidades/datos/CI; DAG19 documental, A0–A4 proporcional, Scope Lock exact-SHA y A5 otro TASK_ID. No liberar M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — V5 C02-03.M01.03: contratos, código y evidencia lifecycle tenant [Javier :30]
- Inventario documental de seis fuentes Git con SHA, límites de autorización y seis capacidades futuras de alta/suspensión/reactivación/archivo/baja lógica/reparación; sin atribuir runtime implementado.
- A0–A3 dirigidos: 6/6 rutas+hashes, 19/19 prerequisitos DAG LISTO, matriz de seguridad JWT/RBAC/tenant A/B y negativas. No cambia .NET/Angular/MySQL/Docker, CI técnica no aplicable; Scope Lock exact-SHA y A4 pendientes de pospublicación. A5 por otro TASK_ID, sin QA/main/PROD.

## 2026-11-13 — V5 C02-03.M01.01: objetivo y no-regresión de aprovisionamiento [Javier :10]
- Documentación exclusiva del ciclo alta/suspensión/reactivación/archivo/baja lógica idempotente, ocho conceptos y 16 criterios NR (+/−), sobre 6 blobs existentes.
- No crea ruta de aprovisionamiento ni migración; A0–A4 proporcional documental, Scope Lock exact-SHA y A5 pendiente ajeno; no QA/main/PROD.

## 2026-11-13 — V5 C02-03.M01.04: riesgos, supuestos y decisiones del lifecycle tenant [Javier :20]
- Matriz documental preliminar R01–R12, S01–S06 y D01–D08 de altas, suspensiones, reactivaciones, archivo y baja lógica; pruebas positivas/negativas futuras, cinco fuentes Git con SHA.
- Sin cambios de runtime, MySQL, IAM, secretos ni datos, sin afirmación de pruebas live. Gate DAG documental 19 prerequisitos; A0–A4 documentales con Scope Lock exact-SHA; A5 independiente, sin liberar M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — V5 C02-03.M01.02: actores e interfaces de ciclo tenant [Javier :20]
- Preparación documental M01.02 de aprovisionamiento: ocho actores, doce contratos IO, cuatro positivos y doce negativos futuros para alta/suspensión/reactivación/archivado/baja lógica idempotentes; cinco archivos de dominio/seguridad verificados.
- Sin implementación, jobs, datos ni migraciones reales; A0–A4 documentales, Scope Lock exact-SHA, A5 distinta Task; no habilita M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — V5 C02-02.M01.02: actores, entradas y superficies shared/dedicated [Javier :20]
- Matriz documental de ocho actores y doce interfaces IO01–IO12 de placement server-side, shared/dedicated y migración reversible; cuatro positivos y catorce negativos propuestos, seis blobs backend verificados.
- TIPO1 documental; sin router/migración ejecutados, sin DSN del cliente ni cambios .NET/Angular/MySQL/Docker/E2E. Requiere seguridad documental, Scope Lock exact-SHA, A0–A4 proporcionados, A5 independiente; no liberar M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — V5 C02-02.M01.03: inventario shared/dedicated [Javier :00]
- Inventario documental de diez fuentes Git con hash, límites de routing, EF, tenant, hosts, jobs y backup; 2 positivos/4 negativos; no modificaciones runtime ni conexión dedicada.
- Preparación provisional por DAG VALIDADO, Scope Lock exact-SHA y A5 independiente; no habilita M01.06/M02.

## 2026-11-13 — V5 C01-04.M01.05: aceptación funcional y técnica de expediente [Javier :10]
- Documento de 18 criterios AC-01–18 para timeline, decisiones, observaciones, adjuntos, versiones, tenant A/B, concurrencia, UI y comprobación CI causal; incluye negativos y gates por superficie.
- Sin implantación ni tests runtime; A0–A4 documentales, Scope Lock SHA exacto, A5 independiente; no liberar M01.06/M02, ni QA/main/PROD.

## 2026-11-13 — V5 C02-02.M01.05: aceptación shared/dedicated [Javier :00]
- Contrato documental con 20 criterios AC01–AC20, 6 positivos/14 negativos, seis fuentes Git, tenancy, DSN, backfill, migración y rollback; no se crea infraestructura ni se hacen cambios ejecutables.
- DAG permite preparación M01.01–05, no M01.06/M02; Scope Lock exact-SHA y A5 revisión independiente.

## 2026-11-13 — V5 C01-04.M01.04: riesgos y decisiones del expediente universal [Javier :10]
- Registro documental EXP-R01–12, EXP-S01–06 y DEC-01–08 sobre 10 fuentes reales, con riesgo de aislamiento de auditoría, adjuntos legacy, secuencia y seguridad sin certificar vulnerabilidades.
- Sin cambios ejecutables ni despliegues; A0–A4 proporcionales, Scope Lock exact-SHA y A5 pendiente de revisión ajena; rollback por revert normal.

## 2026-11-13 — V5 C02-02.M01.04: riesgos y decisiones shared/dedicated [Javier :00]
- Preparación documental: matriz R01–R10, supuestos S01–S05 y decisiones D01–D06 sobre placement, router, migración, aislamiento y rollback; seis fuentes Git; ningún cambio ejecutable.
- DAG VALIDADO documental; no habilita router, base dedicada, DSN, secretos, MySQL, M01.06/M02 ni QA/main/PROD. Validar Scope Lock y A5 independiente.

## 2026-11-13 — V5 C02-02.M01.01: objetivo shared/dedicated y no-regresión [Javier :30]
- Documento preliminar de placement server-side compartido/dedicado, fail-closed y rollback, con 6 blobs actuales y 13 casos NR (3 positivos, 10 negativos) previstos, sin conexión, migración ni pruebas live.
- A0–A4 documentales con Scope Lock SHA exacto, A5 independiente. No se modifican DSN, Aiven, JWT/RBAC, secretos, QA/main/PROD, ramas o providers.

## 2026-11-13 — V5 C01-04.M01.03: inventario documental de contratos para expediente universal [Javier :10]
- Lectura de 10 blobs reales: auditoría controller/DTO/service/repository/domain, storage de compras, contexto tenant y frontend; brechas de cronología, consulta tenant, adjuntos y versionado sin asumir API de expediente existente.
- Documental; A0–A4 proporcionales, Scope Lock SHA exacto y A5 independiente. No cambian runtime/.NET/Angular/MySQL/Docker/E2E, qa, main ni PROD.

## 2026-11-13 — V5 C02-01.M01.05: aceptación documental contexto tenant [Javier :00]
- Matriz de 18 casos AC01–AC18 (5 positivos, 13 negativos), ocho archivos Git y requisitos futuros de host/JWT/RBAC/membresía. Sin modificaciones de runtime, MySQL, secretos ni permisos.
- Preparación DAG VALIDADO, A0–A4 documentales proporcionales, Scope Lock exact-SHA y A5 por otro TASK_ID; revalidar antes de M01.06/M02.

## 2026-11-13 — V5 C02-01.M01.02: actores, IO y superficies de contexto tenant [Javier :30]
- Matriz documental provisional de 8 actores, 10 interfaces/entradas y salidas, 3 positivos y 10 negativos futuros, con 6 blobs de servidor reales y límites de host/JWT/membresía sin autoridad de cliente.
- Sin implementar API/DB/Angular, DNS ni workers; A0–A4 proporcionales con Scope Lock exact SHA, BITACORA A:Z y revisión A5 distinta. QA/main/PROD no autorizados.

## 2026-11-13 — V5 C02-01.M01.03: inventario de contratos de contexto tenant [Javier :00]
- Preparación documental TIPO1: 12 fuentes de Git, host exacto, JWT, membresía, API, filtros, audit y límites de evidencia; aceptación documental P01-P02/N01-N04; no cambios ejecutables.
- Conservar DAG validado, no adelantar M01.06/M02; A0-A4 documentales y Scope Lock exact SHA; A5 revisión por otra Task.

## 2026-11-13 — V5 C01-04.M01.02: actores, entradas, salidas y superficies de expediente universal [Javier :10]
- Preparación documental exclusiva M01.02: siete perfiles de actor, diez interfaces EXP-IO y seis referencias de código real con límites JWT/RBAC/tenant A/B, adjuntos y versionado.
- No implementa expediente ni API universal. .NET/Angular/MySQL/Docker/E2E no aplican al diff documental; A0–A4 proporcionales y Scope Lock SHA exacto; A5 independiente; sin QA/main/PROD.

## 2026-11-13 — V5 C02-01.M01.01: objetivo y no-regresión tenant server-side [Javier :30]
- Preparación documental preliminar del contexto host/identidad/membresía/ámbito; matriz NR-01–NR-12 con tres positivos y nueve negativos propuestos, cuatro blobs originales contrastables y alcance sin código ejecutable.
- A0–A4 documentales proporcionales y Scope Lock exact-SHA; A5 independiente. Sin migración, autorización nueva, DNS, QA/main/PROD ni pruebas runtime atribuidas.

## 2026-11-13 — V5 C02-01.M01.04: matriz documental de riesgos del contexto tenant [Javier :00]
- Registro inicial de riesgos R01–R10, supuestos S01–S05, decisiones D01–D06, fuentes backend y casos documentales positivos/negativos; no modifica ejecutables, datos, permisos ni infraestructura.
- Preparación provisional DAG VALIDADO. Scope Lock y A4 exact-SHA exigibles; A5 por otra Task y revalidación al cerrar contratos C00/C01.

## 2026-11-13 — V5 C01-04.M01.01: objetivo documental expediente universal [Alex :05]
- Contrato preliminar de timeline, decisiones, observaciones, adjuntos y versionado; define alcance/no-alcance y 12 invariantes positivas/negativas para aislamiento tenant/JWT/RBAC, adjuntos, idempotencia, PII, regresión y rollback.
- Únicamente M01.01 documental: no crea endpoints, datos, UI, migraciones, workers ni adjuntos; .NET/Angular/MySQL/Docker/E2E no aplican. Requiere A0–A4 causal, Scope Lock exact-SHA y A5 independiente; sin QA/main/PROD.

## 2026-11-13 — Autoactivación VALIDANDO por DAG24 (gobierno reactivo)
- El propietario exige reponer la cola al pasar prerequisitos a VALIDANDO. Se especifica gate condicional solo para tareas documentales independientes y A5/gates estrictos para cierres y cambios críticos. Fórmulas DAG_TECNICO!G y COLA!G/O se recalculan automáticamente al cambiar PLAN_MAESTRO; las Tasks hacen el despacho real.
- Fase preparatoria autorizada: C01-04, C02-01, C02-02, C02-03 M01.01-05 (20); cuatro M01.06 preparadas pero bloqueadas por cierres y A5. C00-01.M06.06 exige causal A5 independent sobre M04.03. Reposición futura ante reserva baja se gobierna por verificación técnica por ID, nunca saltando dependencias ni creando tareas ficticias.
- Respaldo previo del Sheet y Doc rector confirmado en Drive; cambio solo de gobierno y fórmulas con lectura posterior, sin tocar aplicación, CI ni prompts/horarios físicos. Rollback normal git revert y restauración de snapshot de hoja si fuera necesario, sin force-push.

## 2026-11-13 — V5 C01-01.M01.05: aceptación preliminar identidad universal [Javier :30]
- Matriz documental ID01–ID16 para Empresa, Sucursal, Usuario, membresías, rol tenant, recursos, seguridad JWT/RBAC y criterios positivos/negativos.
- No implementa la fase ni prueba runtime; A0–A4 proporcionales con Scope Lock SHA exacto y A5 independiente. Sin QA/main/PROD.

## 2026-11-13 — V5 C01-01.M01.03: inventario de contratos de identidad [Javier :10]
- Inventario documental de siete blobs reales de empresa, sucursal, usuario, membresía, rol, contexto tenant y autenticación; dos positivos y ocho negativos de seguridad/alcance.
- TIPO1 documental: sin runtime, .NET/Angular/MySQL/Docker/E2E no aplican; A0-A4 proporcionales y Scope Lock exact-SHA, A5 independiente; sin QA/main/PROD.

## 2026-11-13 — V5 C01-01.M01.04: riesgos, supuestos y decisiones de identidad universal [Javier :50]
- Registro documental R-ID-01–12 y supuestos S-01–06, vinculados a 8 blobs reales de dominio, aplicación, infraestructura y M01.01; fronteras A/B, legacy, claims, contexto y bypass bajo decisión pendiente.
- Solo documentación; ningún cambio runtime, EF/MySQL, Angular, permisos, secretos o despliegues. A0–A4 proporcionales y Scope Lock exact-SHA; A5 independiente para QA/main/PROD.

## 2026-11-13 — V5 C01-02.M01.01: alcance y no regresión capabilities [Javier :30]
- Objetivo documental, límites, decisiones de M02 y 12 invariantes NR01–NR12 para permiso explícito, entitlement, tenant, sucursal, recurso y soporte.
- Sin código ejecutable ni autorización nueva; A0–A4 proporcionales, Scope Lock SHA exacto, A5 por Task distinta; no QA/main/PROD.

## 2026-11-13 — V5 C01-01.M01.02: actores, IO y fronteras de identidad [Javier :40]
- Análisis documental con siete actores, diez entradas/salidas candidatas, referencias a seis blobs de dominio, tres positivos y ocho negativos de tenancy/RBAC/JWT; no implanta API/DB/Angular.
- Protocolo A0–A4 proporcional con Scope Lock exact-SHA, BITACORA A:Z/26 y A5 por Task distinta; no QA/main/PROD.

## 2026-11-13 — V5 C01-02.M01.03: inventario verificable de contratos [Javier :30]
- Inventario de 7 blobs de código/tests con limitaciones explícitas, matriz de fuentes V5, siete brechas de implementación y casos positivos/negativos documentales.
- Ninguna prueba runtime atribuida; Scope Lock exact-SHA para commit solo documental, A0–A4 proporcionales y A5 otra Task; no QA/main/PROD.

## 2026-11-13 — V5 C01-01.M01.01: objetivo y no-regresión de identidad universal [Javier :40]
- Documento inicial: frontera tenant/sucursal/membresía/rol/plataforma, seis fuentes de código con blob SHA y doce pruebas positivas/negativas propuestas (no ejecutadas en runtime).
- Excluye cambios de código, EF/MySQL, Angular, secretos, proveedor y QA/main/PROD. A0–A4 documentales con Scope Lock exact-SHA; A5 independiente pendiente.

## 2026-11-13 — V5 C01-02.M01.05: criterios de aceptación de capabilities [Javier :30]
- Matriz preparatoria AC01–AC16 de aceptación funcional/técnica y seis negativos documentales para RBAC, entitlement, tenant, sucursal y recurso.
- No implementa autorización ni prueba runtime; A0–A4 proporcionales al documento, Scope Lock exact-SHA, A5 independiente; sin cambios QA/main/PROD.

## 2026-11-13 — V5 C01-03.M01.01: objetivo, no-alcance y no-regresión de configuración dinámica [Javier :40]
- Preparación exclusivamente documental: mapa de fronteras, límites de implementación y NR-01–NR-10 positivos/negativos; no activa configuraciones ni modifica runtime, secretos, permisos, DB o proveedores.
- Seguridad JWT/RBAC/tenant, inyección, concurrencia, BITACORA A:Z y rollback delimitados. A0–A4 proporcionales; Scope Lock exact-SHA, A5 independiente pendiente; no QA/main/PROD.

## 2026-11-13 — V5 C01-02.M01.02: actores, entradas y superficies de capabilities [Javier :30]
- Matriz preparatoria de doce actores/consumidores, entradas y salidas, fronteras JWT/RBAC/tenant/sucursal/recurso y seis negativas; decisiones comerciales de M02 siguen pendientes.
- Sin cambios de runtime, permisos, DTO, MySQL ni frontend; A0-A4 documentales con Scope Lock exact-SHA, A5 independiente; no QA/main/PROD.

## 2026-11-13 — V5 C01-02.M01.04: riesgos y decisiones preliminares de capabilities [Javier :30]
- Inventario documental R01–R11, supuestos S01–S06, decisiones D01–D06 y positivos/negativos para RBAC/plan/rubro/tenant/sucursal/recurso, basado en contratos reales.
- Sólo M01.04 documental; no habilita capabilities, modifica API/Angular/MySQL ni certifica runtime. Aplicar A0–A4 proporcionales, Scope Lock exact-SHA y A5 por Task distinta. No QA/main/PROD.

## 2026-11-13 — V5 C00-04.M01.05: criterios de aceptación observables [Javier :10]
- Contrato documental AC01–AC12, P1/P2, N1–N7 y D01–D05 con gates de autorización, DAG, CI exact-SHA, seguridad, BITACORA A:Z y rollback; sin runtime.
- A0–A4 dirigidos, Scope Lock en el SHA publicado y A5 pendiente de revisor independiente; QA/main/PROD no autorizados.

## 2026-11-13 — V5 C01-03.M01.02: actores, límites y entradas/salidas de configuración [Javier :20]
- Se identificaron 6 actores, 8 entradas/salidas propuestas, cuatro superficies fuente con blobs verificables y 2 positivos/6 negativos de tenancy/RBAC/inyección, ámbito/versiones/errores; propuesta M01 sin contrato final ni cambio de runtime. A0–A3 documentales; A4 Scope Lock SHA exacto y readback; A5 revisor distinto. No QA/main/PROD.

## 2026-11-13 — V5 C01-03.M01.03: inventario preliminar de contratos y evidencia [Javier :20]
- Se leyeron y fijaron cuatro blobs de código real (TemaVisual DTO/metadata UI, TemaVisualService HTTP, ThemeApplierService hex allowlist y Program.cs) con afirmaciones y límites explícitos; seis casos documentales D-01–06 positivos/negativos de JWT, RBAC, tenancy e inyección. No se certifica API universal ni MySQL/Angular/browser. A0–A3 documentales, A4 exact-SHA Scope Lock y A5 por otra Task; sólo M01, sin QA/main/PROD.

## 2026-11-13 — V5 C00-04.M01.04: registro de riesgos y decisiones pendientes [Javier :10]
- Matriz documental R01-R10, supuestos, decisiones D01-D05, cinco negativos, gates de seguridad/identidad/CI/tenant/SHA/rollback y relación con A5_FAIL histórico sin certificar PROD.
- Sin cambio ejecutable; A0-A4 proporcionales, Scope Lock SHA exacto tras publicar, BITACORA A:Z/26; A5 independiente y no QA/main/PROD.

## 2026-11-13 — V5 C01-03.M01.04: matriz preliminar de riesgos de configuración [Javier :20]
- Documento de preparación M01 para configuración dinámica: diez riesgos R01-R10, cinco supuestos S01-S05, seis decisiones D01-D06 y tres escenarios negativos de integridad/aislamiento/inyección. Referencias V5/DAG, seguridad JWT/RBAC/tenant y rollback, sin implementación ni migración; no se declaran pruebas .NET/Angular/MySQL. A0–A4 documentales y A5 independiente requeridos. No QA/main/PROD.

## 2026-11-13 — V5 C00-04.M01.03: inventario de contratos, código y evidencias [Javier :10]
- Inventario documental con fuentes GitHub de SHA/blobs, Plan V5, Sheet institucional y DAG; prueba de límites de inferencia, permisos y autorizaciones; criterios positivos/negativos para no confundir CI, fuente y certificación.
- Sin cambios runtime; A0–A4 proporcionales, Scope Lock exact-SHA de publicación, A5 independiente, BITACORA A:Z/26, rollback normal; no QA/main/PROD.

## 2026-11-13 — V5 C01-03.M01.05: aceptación preliminar de configuración tipada [Javier :20]
- Se fija documento M01 de 10 criterios observables con positivos/negativos de JWT/RBAC, tenant A/B, inyección, versionado, errores y rollback. Sólo preparación documental; no runtime, DB, providers ni migración. A0–A3 de integridad documental; A4 Scope Lock del commit exacto y readback, A5 por otro TASK_ID. No QA/main/PROD; revalidar C00-01 y C01-01 antes de fases ejecutables.

## 2026-11-13 — V5 C00-04.M01.02: actores y superficies gobierno de cambios [Javier :10]
- Inventario contractual documental de entradas/salidas, actores Javier/Alex, Drive/Sheets, GitHub, control de estados, interfaces, decisiones y riesgos; positivo y cinco negativos, BITACORA A:Z/26, sin código runtime.
- Validar A0–A4 proporcionales, Scope Lock SHA de commit documental y readback; A5 independiente, no QA/main/PROD. Rollback git revert ordinario.

## 2026-11-13 — Ampliacion V5 DAG20 de preparacion documental independiente [gobierno]
- Backups nativos del Sheet y Plan V5 verificados. Se registran 20 IDs ya existentes M01.01-05 de C00-04 y C01-01/02/03 con once prerequisitos cerrados, sin ciclo, como documentos preparatorios. Gate C00-01.M06.06 individual, no apertura de implementacion ni QA/main/PROD.
- El Doc rector, DAG_TECNICO, AGENTS, AUTOMATION_V13, AUTOMATION_STATUS, CONFIG y Jira se sincronizan con lectura posterior; los seis prompts y horarios Javier y dos Alex no se modifican. Rollback GitHub por git revert sin force-push; desactivar filas nuevas del DAG (F=EN_REVISION) antes de revertir el contrato.

## 2026-11-13 — V5 C00-04.M01.01: alcance de gobierno de cambios [Javier :10]
- Contrato documental de decisiones, autorizaciones, riesgos, gates, casos positivos/negativos y rollback; distingue preparación M01 de implementación/deploy. Fuente V5; sin cambios de runtime, base de datos, secretos o configuraciones.
- A0–A4 documentales requeridas y A5 independiente antes de QA/main/PROD; Scope Lock exact SHA al publicar, BITACORA A:Z/26 e idempotencia. Autor conserva TASK_ID original.

## 2026-11-13 — V5 C00-02.M01.01: rectificación documental A5_FAIL BITACORA A:Z/26 [Alex :05]
- Se corrige la referencia obsoleta A:U/21 a A:Z/26, EVENT_ID A, IDEMPOTENCY_KEY R, TASK_ROLE E, ACCOUNT_OWNER F y TASK_ID G, y se añaden negativos reproducibles para eventos truncados de 21 a 25 campos, claves duplicadas y auto-revisión.
- A0–A3: revisión estática y de contrato documental frente a encabezado vivo de 26 columnas; .NET/Angular/MySQL/Docker/Playwright NO_APLICA al no editar código ejecutable. A4 requiere commit fast-forward dev y Scope Lock del SHA publicado; A5 por otro TASK_ID sobre nuevo SHA, conservando el A5_FAIL histórico. Rollback con git revert ordinario, sin cambios a QA/main/PROD.

## 2026-11-13 — V5 C00-01.M06.05: solicitud documental de A5 independiente y matriz de riesgos [Javier :00]
- Se solicita revisión por otro TASK_ID de M06.01/M06.02/M06.05, preservando A5_PASS de M06.03/M06.04, A5_FAIL transversal M04.03, SHA/blobs, diferencias entre E2E/HTTP/mocks y BITACORA A:Z/26; M06.06 no se libera. Cambios sólo documentales, sin claims de revisión A5 aprobada ni modificación de runtime, DB, workflows, Tasks, QA/main/PROD. Rollback: git revert sin force-push.

## 2026-11-13 — V5 C00-01.M06.02: manifiesto causal de versiones, diffs y artefactos [Javier :50]
- Se agregó un manifiesto reproducible con fuente V5, estado Sheet/BITACORA A:Z/26, SHA, tree, blobs, diff documental +53/+3 de M06.01, jobs reales M10 12/12, seguridad 1109/1109 y Scope Lock SHA exacto, más límites mocks vs HTTP y A5 independiente. Sólo documentación; no modifica código runtime, datos, tareas ni entornos QA/main/PROD.

## 2026-11-13 — V5 C00-01.M06.01: auditoría causal de pruebas vs mocks [Javier :50]
- Se audita evidencia real de GitHub Actions (M10 MySQL/Angular/Playwright 12/12, security xUnit 1109/1109, backend/frontend y Scope Lock) contra fuente exacta 14ea2e23; se distinguen casos HTTP reales, route.fulfill UI sintético, Subjects unitarios, límites RBAC/tenancy y pruebas de rendimiento no productivas. Solo docs: sin cambios de runtime, datos, secretos o despliegues. A5 independiente queda pendiente; QA/main/PROD no autorizado.

## 2026-11-13 — Saneamiento documental C00-03.M01.05 esquema BITACORA A:Z/26 [propietario, sin cambio de Task]
- Alinea dos referencias de aceptación con los 26 campos físicos de BITACORA; incorpora rechazo explícito de eventos 21-25, duplicados y A5 autoaprobado. Preserva la autoria original del TASK_ID y requiere nueva revisión A5 por Task distinta; no implica permiso QA/main/PROD ni pruebas runtime nuevas. Rollback: git revert del commit de documentación, sin force push.

## 2026-11-13 — Corrección A5_FAIL C00-03.M01.04: RI10 BITACORA A:Z/26 [Javier :20]
- Se rectifica en el documento propio RI10: 26 columnas A:Z, EVENT_ID e IDEMPOTENCY_KEY únicos, RUN_ID vacío sin lectura nativa y rechazo explícito de eventos de 21–25 campos; se conserva A5_FAIL del SHA bfb4c325 y se solicita A5 nueva e independiente para el SHA corregido. Sólo documentación; no afecta código ejecutable, fórmulas, Tasks ni QA/main/PROD.

## 2026-11-13 — Corrección A5_FAIL C00-01.M06.03: matriz BITACORA A:Z/26 [Javier :00]
- Corrige referencia documental de 21 a 26 columnas físicas, EVENT_ID/IDEMPOTENCY_KEY, RUN_ID no inventado y negativos contra truncamiento y suplantación; conserva evidencia original de A5_FAIL sobre 66b640fb y exige nuevo A5 independiente. No cambia runtime, Sheets, Tasks, QA/main/PROD ni gates M06.

## 2026-11-13 — Corrección A5_FAIL C00-02.M01.03: BITACORA A:Z/26 [Javier :00]
- Corrige contrato documental original A:U/21 a encabezado físico institucional A:Z/26; define EVENT_ID e IDEMPOTENCY_KEY únicos, RUN_ID vacío sin ejecución comprobable y negativos contra truncamiento/duplicación/suplantación. Preserva A5_FAIL del SHA 81141057 y eventos históricos, solicita nueva A5 independiente tras Scope Lock exact-SHA. Solo documentación; no altera código, Sheets, Tasks, QA/main/PROD.

## 2026-11-13 — V5 C00-03.M01.04: riesgos, supuestos y decisiones de matriz integrada [Javier :20]
- Se incorpora registro documental de diez riesgos RI01–RI10, cinco supuestos S01–S05 y seis decisiones D01–D06, más aceptación P1/P2 y ocho rechazos N1–N8; delimita matriz V5 integrada, exige fuente/ID único, fronteras tenant/PII, CI proporcional, Scope Lock y cero migración de imágenes históricas. No implementa software, migra proveedores, crea IDs ni declara cierre de fase o A5_PASS.

## 2026-11-13 — V5 C00-03.M01.05: aceptación funcional y técnica de matriz integrada [Javier :10]
- Contrato documental con AC-01–AC-10 observables, evidencia mínima por unidad, negativos fail-closed, independencia de autor/A5, Scope Lock y dependencia DAG C00-01.M02; no cierra matriz completa ni implementa runtime.
- TIPO1_DOCUMENTACION: dotnet/Node/Angular/MySQL/Docker/E2E=NO_APLICA; security=SI_REVISION_DOCUMENTAL; scope_lock=SI; A0-A3 documentales tras VALIDANDO y A4 SHA/readback, A5 independiente. No QA/main/PROD.

## 2026-11-13 — V5 C00-01.M06.03: matriz de riesgos, límites y decisiones [Javier :00]
- Registro firmado por Task/SHA con riesgos abiertos M05.04, A5_FAIL original M04.03, strict -warnaserror N1.1/N1.2, CSS budget, TestServer obsoleto y límites de rendimiento CI. Matriz de contradicciones V5/DAG/CI/autoría, sin liberar M06 ni cambiar runtime; Scope Lock y A5 independiente.

## 2026-11-13 — V5 C00-03.M01.03: lectura causal de fuentes y evidencia, sin certificación [Javier :10]
- Inventario documental de doce blobs Git originales (Skill, arquitectura, Scope Lock, contratos, frontend net10, CI y C00-03), fuentes Doc/Sheet V5, seis prerrequisitos M02 LISTO y matriz de evidencia disponible versus por comprobar; ningún color de check histórico implica PASS de nueva implementación.
- TIPO1_DOC, requiere_dotnet/Node/Angular/MySQL/Docker/E2E=NO; requiere_security=SI_REVISION_FRONTERAS; requiere_scope_lock=SI_EXACT_SHA; A0-A3 auditoría documental y A4 diff/readback después de FF dev, A5 de Task distinta. No QA/main/PROD.

## 2026-11-13 — V5 C00-02.M01.05: aceptación funcional y técnica observable del modelo [Javier :00]
- Matriz de diez criterios positivos/negativos para elegibilidad DAG, autoría, estados, A0–A5, CI exact-SHA, BITACORA idempotente y rollback; ejemplo real M05.01. Solo documentación; Scope Lock exact-SHA para cierre, A5 independiente, sin QA/main/PROD.

## 2026-11-13 — V5 C00-02.M01.04: riesgos, supuestos y decisiones [Alex :05]
- Matriz de riesgos R01–R10 y supuestos contrastables S1–S6 del modelo V5: exclusión de custodios, fórmulas COLA, unicidad BITACORA, concurrencia Git, Skill/Scope Lock, CI exact-SHA y separación QA/PROD.
- Sólo docs y CHANGELOG, sin runtime ni permisos nuevos; A0–A3 documentación, A4 exact SHA/readback y A5 independiente pendiente; no QA/main/PROD.

## 2026-11-13 — V5 C00-03.M01.02: entradas, salidas, actores y superficies de matriz integrada [Javier :10]
- Contrato documental con siete entradas autorizadas, seis actores con fronteras de permiso y siete superficies (gobierno, dominio, API/tenancy, EF/MySQL, Angular, integraciones y evidencia), casos positivos/negativos y dependencia C00-01.M02 ya finalizada; no crea nueva microtarea ni certifica matriz completa.
- TIPO1_DOCUMENTACION; requiere_dotnet/Node/Angular/MySQL/Docker/E2E=NO (sin cambio ejecutable), requiere_security=SI_REVISION_DOCUMENTAL y requiere_scope_lock=SI_EXACT_SHA; A0-A3 dirigidas y A4 readback tras FF dev, A5 independiente. No toca QA/main/PROD.

## 2026-11-13 — V5 C00-02.M01.03: inventario causal de contratos y evidencias [Javier :00]
- Fuentes V5 y blobs HEAD, PLAN/COLA/BITACORA y fórmulas E/G/O; contrato de estados e idempotencia, ejemplo real C00-01.M05.01 y negativos de claim/CI; sólo documentación, sin cambios runtime ni autorizaciones QA/PROD. Scope Lock y readback exact-SHA antes de LISTO, A5 independiente.

## 2026-11-13 — V5 C00-02.M01.02: entradas, salidas, actores y superficies [Alex :05]
- Matriz documental de autoridad V5: PLAN_MAESTRO editable, COLA derivada, BITACORA idempotente, GitHub dev, identidades físicas, CI exact-SHA y revisión A5 ajena. Casos positivos/negativos para propiedad, idempotencia, permisos y preservación de fórmulas.
- Sin cambios de runtime, secretos, API, DB, dependencias o Tasks; A0–A3 documentales y A4 Scope Lock exact SHA/readback antes de LISTO. No QA/main/PROD.

## 2026-11-13 — V5 C00-03.M01.01: objetivo y no-regresión de la matriz integrada [Javier :10]
- Delimitación documental de fuente V5 única, perímetro de 2.340 IDs, seis familias de evidencia, dependencias del DAG técnico de preparación transversal y aceptación positiva/negativa fail-closed. No implementa ni certifica la matriz completa, C00-01 final, QA/PROD, migraciones ni autorizaciones.
- Clasificación: TIPO1_DOCUMENTACION; requiere_dotnet=NO; requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO; requiere_security=SI_REVISION_FRONTERAS_Y_SECRETOS_DOCUMENTAL; requiere_scope_lock=SI_SHA_EXACTO. A0–A3 dirigidas durante VALIDANDO; A4 commit FF, Scope Lock y readback; A5 independiente.

## 2026-11-13 — V5 C00-02.M01.01: objetivo, no-alcance y no-regresión del modelo de trabajo [Alex :05]
- Documento acotado de preparación de la cola V5: fuente de 2.340 IDs, PLAN/COLA/BITACORA, exclusividad por TASK_ID, casos de aceptación positivos/negativos, límites de seguridad, A0–A5 y rollback. Sin cambios en runtime, fórmulas institucionales, permisos, API, DB, configuración o Tasks.
- Validación proporcional: revisión estática/documental de diff y coherencia V5/V13; Scope Lock exact-SHA y readback pospublicación antes de LISTO. No atribuir pruebas de Angular/.NET/MySQL/E2E a una entrega documental y no promover QA/main/PROD.

## 2026-11-13 — V5 C00-01.M05.02: contrato HTTP/DTO y errores seguros [Javier :50]
- Nuevo test xUnit TestServer que atraviesa ExceptionHandlingMiddleware de ASP.NET Core 10 en HTTP in-memory; verifica contrato ApiResponse 200, ProblemDetails 400/409, envelope 500 y sentinelas privadas ausentes del cuerpo de respuesta (1 positivo y 4 negativos). No cambia servicios, endpoints, migraciones, base de datos, UI ni dependencias.
- Clasificacion: .NET=SI; Angular/Node=NO; MySQL=NO; Docker=NO; browser E2E=NO; seguridad de respuestas=SI; Scope Lock exact-SHA=SI. A0 revision estatica sin nuevos secretos ni deps; A1-A3 suite GitHub Actions y A4 posintegracion pendientes al publicar; A5 independiente posterior. Sin QA/main/PROD.

## 2026-11-13 — V5 C00-01.M05.01: regresión causal ante respuesta tardía de tienda [Javier :00]
- AppComponent descarta callbacks tardíos de StorefrontIdentidadService cuando cambia ruta a plataforma, nueva página de tienda o se destruye el shell; no restaura SEO ni CSS de tenant anterior. Tres pruebas unitarias de navegación con NavigationEnd y Subjects RxJS cubren positivo y negativos; Priority4 CI las ejecuta en Angular 22 con Node 24. Enmienda también la brecha señalada por A5_FAIL de M04.03, sin autodeclarar una A5_PASS. Sin cambios de API/MySQL/permisos/secretos; requiere CI exact-SHA y rollback normal.

## 2026-11-13 — V5 C00-01.M04.04: corrección del selector de formulario en loading [Javier :00]
- M10 del SHA cca72cfc falló 1/12 porque la etiqueta de submit cambia al comenzar el login. El E2E mantiene disabled y POST único usando selector estable del botón; exigir nuevo SHA y nueva CI. Sin cambios de auth, base de datos, secretos ni permisos.

## 2026-11-13 — V5 C00-01.M04.01: mantenimiento causal del inventario de componentes [Javier :10]
- El check Scope Lock del commit 8bd38254 encontró catalog drift por la nueva vista de ruta desconocida; se conserva el inventario histórico P0-007.A y se añade la nueva ruta al catálogo vigente (138 componentes) con verificador de drift y negativos sin desactivar el gate. No concede certificación del backlog antiguo.

## 2026-11-13 — V5 C00-01.M04.01: ruta pública de página no encontrada [Javier :10]
- La ruta Angular wildcard presenta una vista pública accesible con navegación a login/tienda, sin divulgar query, IDs de empresa o rutas internas. Añade regresión Playwright Chromium real al workflow N8.2; rutas privadas siguen usando guards existentes. Sin API, MySQL, Docker, permisos, QA, main ni PROD.

## 2026-11-13 — V5 C00-01.M04.04: navegación y envío único de login [Javier :00]
- Añade tres escenarios Playwright reales: guard anónimo/formulario inválido, error seguro de credenciales inválidas y POST único durante latencia de la API con tenant server-side. Se incorpora prueba en runner M10 ya autorizado; sin cambios de autenticación, base productiva, secretos o privilegios. Requiere evidencia exact-SHA A0–A4 y A5 independiente.

## 2026-11-13 — V5 C00-01.M04.03: branding visual seguro por tenant [Javier :00]
- ThemeApplierService restringe tokens dinámicos a colores hexadecimales, limpia temas previos y descarta respuestas HTTP tardías A al cambiar a B/plataforma. Añade 4 casos positivos/negativos Angular y paso de CI dirigido Priority4; mantiene M10 E2E, Scope Lock y A0–A5 por SHA. No toca API, datos, roles, QA, main ni producción.

## 2026-11-13 — Corrección CI C00-01.M04.05: segundo toggle de contraseña [Javier :40]
- Primera CI M10 SHA c45c2d86 falló en test E2E propio (8/9 PASS) porque el segundo Enter se disparaba sin verificar foco en el botón actualizado. Ajuste dirigido Playwright: enfocar y acreditar foco en Ocultar contraseña antes de pulsar Enter; sin cambio funcional de login, API, backend o seguridad. Requiere nueva ejecución exact-SHA M10.

## 2026-11-13 — V5 C00-01.M04.06 verificación UI de densidad/progresividad [Javier :50]
- Se amplía prueba Playwright N8.2 de portada pública para medir geometría hero/CTAs 390/1440 px, ausencia de desborde, carga diferida y telemetría de rendimiento. Sin modificar producción ni prometer SLA; fixture branding solo en CI. Requiere Node/Angular lint/build, N8.2 y Scope Lock exact-SHA; A5 independiente.

## 2026-11-13 — V5 C00-01.M04.05: verificación de accesibilidad en login público [Javier :40]
- Añade Playwright real al gate M10 para /login anónimo a 320px: labels, orden de foco teclado, mostrar/ocultar contraseña, formulario habilitado/deshabilitado, contraste y desbordamiento.
- Solo alcance de regresión frontend, sin cambios de API, tenancy, esquemas MySQL, claves, infraestructura o permisos. Exigir CI E2E M10 y frontend/SCOPE_LOCK causal SHA; A5 independiente. No autoriza QA/main/PROD.

## 2026-11-13 — V5 C00-01.M04.02: estados accesibles de dashboard y recuperación [Javier :50]
- Se corrige el dashboard empresarial que quedaba sin contenido cuando GET /dashboard/resumen fallaba o retornaba data=null. Ahora expone estados de carga, error, vacío y éxito, reintento explícito con 44px y foco visible, sin reflejar detalles internos de HTTP. Conserva authGuard/permisoGuard, RBAC y tenant existentes; no cambia API, contrato SQL, migraciones ni secretos.
- Regresión Playwright M10 con API/MySQL efímeros: denegación 503 y ausencia de filtración de mensaje, respuesta vacía y reintento exitoso real. Requiere A0 de diff, Angular lint/build, E2E M10, Scope Lock y readback SHA exacto. No promover QA/main/PROD sin A5 independiente.

## 2026-11-13 — V5 C00-01.M03.04 correccion CI CS0117 en test legacy [Javier :30]
- El commit b64b881f no compilaba por usar EmpresaId en OrdenCompra legacy sin esa propiedad (job 131891745374, error CS0117). Se corrige exclusivamente fixture xUnit para monoempresa; se preserva contrato de seguridad sin fingir aislamiento A/B en esta prueba.
- Reejecutar GitHub Actions en SHA correctivo para .NET, seguridad y MySQL; mantener VALIDANDO y A5 pendiente.

## 2026-11-13 — V5 C00-01.M03.04 persistencia, paginacion extrema fail-closed [Javier :30]
- OrdenCompraRepository.GetPagedAsync calcula offset en long, pagina/tamano acotados, retorna vacio fuera de rango sin desbordamiento int ni filtrado tenant ampliado.
- xUnit positivos y negativos de paginacion monoempresa legacy; A/B solo suites de seguridad CI separadas; GitHub Actions .NET 10/MySQL 8.4 sobre SHA nuevo exigido, sin modificar API, schema, JWT/RBAC, UI o produccion. A5 independiente.

## 2026-11-13 — V5 C00-01.M03.03: ampliar denegaciones de cabeceras duplicadas [Javier :20]
- La corrección de P2001F en SHA `bed9975bd31d1f0a35b478614385c623807bb92b` cambió solo test P2 y el clasificador `ci-impact-routing` marcó el job MySQL como SKIPPED. Para evaluar el candidato completo bajo las pruebas reales pertinentes, la regresión nueva de seguridad amplía los casos a cabecera multi-valor repetida (17/17, 18/18) y orden invertido (18/17): deben fallar cerrados. Es una prueba causal adicional, no una bandera artificial.
- GitHub Actions dotnet/tenancy/MySQL 8.4 deben ejecutarse sobre un nuevo SHA; no atribuir PASS al job SKIPPED ni cerrar DEV antes de A0–A4. No se modifica el código de producción en esta corrección. Rollback: revert normal, sin PR, rama o despliegue.

## 2026-11-13 — V5 C00-01.M03.03: alinear regresión P2001F tras CI fallida [Javier :20]
- GitHub Actions CORE run 43425902857, job 131887123198, compiló el código, pero detectó cinco fallos entre 3402 xUnit: P2001F esperaba fallback desde `X-Empresa-Id=-202`, incompatible con el nuevo contrato fail-closed. Las cinco variantes se corrigen para exigir `ForbiddenAccessException`, cero invocación del controlador, cero autoridad tenant y ningún acceso de permisos/scope. No se relaja la protección en producción ni se alteran migraciones.
- La corrección necesita nuevo commit fast-forward en dev y revalidación CI exact-SHA de .NET 10, seguridad, MySQL 8.4 y A4; M03.03 sigue VALIDANDO. Rollback mediante git revert seguro.

## 2026-11-13 — V5 C00-01.M03.03: selector de tenant inválido falla cerrado [Javier :20]
- `TenantPermissionContext` rechaza cabecera explícita `X-Empresa-Id` vacía, inválida o con múltiples valores antes del fallback de empresa única. Se preservan pertenencia backend verificada, autorización RBAC y selección positiva compatible.
- xUnit dirigido cubre tenant A positivo, fallback sin cabecera, casos inválidos, duplicados y tenant B sin membresía. Validar en GitHub Actions .NET 10 y MySQL 8.4 según impacto del SHA publicado; no atribuir PASS hasta leer logs. Alcance y rollback documentados en `docs/v5/c00-01-m03-03-tenant-auth.md`. DEV únicamente; A5 independiente.

## 2026-11-13 - V5 C00-01.M03.05 batch outbox duplicado fail-closed [Javier :50]
- Prevalida EventoId y ClaveIdempotencia del lote completo para impedir despachos repetidos de un repositorio defectuoso, sin cambio en API/SQL/proveedor.
- Añade pruebas xUnit positivas/negativas; exige CI .NET10/MySQL exact-SHA y A5 independiente.

## 2026-11-13 — V5 C00-01.M03.06: corrección dirigida tras CI xUnit [Javier :10]
- El primer SHA `a1087d334d03941de0b0eba0108d2d411604a61d` compiló en GitHub-hosted .NET 10 pero dev-ci detectó 1 fallo entre 3388 xUnit: `N49FPeriodoContableObservabilityTests` aún esperaba revelar un `ArgumentException.Message` arbitrario. La prueba se reconcilia con el contrato de error público seguro (400 ProblemDetails genérico), verificando además que el texto interno no se expone.
- Mantiene código de producción del primer commit sin modificaciones adicionales. Los controles A1–A4 se reevalúan en el SHA correctivo exacto mediante GitHub Actions; no atribuir PASS hasta la evidencia causal. `VALIDANDO` conserva el mismo TASK_ID. Rollback: git revert ordenado sin reescritura.

## 2026-11-13 — V5 C00-01.M03.06: errores de argumentos seguros y correlación estructurada [Javier :10]
- En `ExceptionHandlingMiddleware`, las excepciones `ArgumentException` devuelven ProblemDetails 400 con detalle genérico y `traceId`; el mensaje arbitrario potencialmente confidencial no se expone al cliente ni se escribe al log. Se registra únicamente el tipo de excepción y referencia estructurada.
- Pruebas xUnit dirigidas: dos negativas con datos privados y una positiva de propagación sin error. No se alteran JWT, RBAC, filtros de tenant, modelos ni migraciones.
- Rutas de validación: GitHub Actions `solqaryn-core-ci.yml` (.NET 10, pruebas backend y seguridad), `dev-ci.yml` (filtrado backend y MySQL según impacto). A0–A4 y SHA exacto quedan pendientes de resultados causales; no se afirma PASS anticipado. Sin cambios en qa/main/PROD; rollback aditivo mediante git revert no destructivo.

## 2026-11-13 - V5 C00-01.M03.02 aplicacion: replay SHA-256 fail-closed [Javier :00]
- Alinea OrdenCompraService.ResolverReintento con el dominio M03.01: compara bytes de SHA-256 en tiempo constante; rechaza fingerprints heredados invalidos y acepta hex equivalente mayuscula sin duplicar ordenes.
- Dos xUnit dirigidos y contrato V5 de limites del servicio. CI .NET/MySQL sobre SHA publicado; A0-A4 y A5 se certifican por evidencia real, nunca por tests simulados. No QA/main/PROD.

## 2026-11-13 — Scope Lock: mutación exacta de la clave del plan
- Corregida exclusivamente la auto-prueba negativa `wrong plan` en `validate-automation-authority.mjs`: ahora modifica `EXECUTION_PLAN_DRIVE_FILE_ID=` en lugar de la primera aparición del ID dentro de la introducción V5; la prueba vuelve a comprobar un plan realmente incorrecto. Se conserva el fail-closed del contrato y la Skill canónica sin modificación.

## 2026-11-13 — Conciliación V5 y selección de IDs; no modifica el progreso de M03
- El commit base `f4b8528f57e82625d343dd1590076df44cacaee2` es antecesor confirmado de `4d582aa48dcdf8e9efb8d4c15ec229cbc0d603a5` (13 commits posteriores de C00-01.M01/M02) y del commit `4fc1f138a3323268d3efb6962b1182ced567c435` que publicó código para C00-01.M03.01; el HEAD vivo avanza y nunca se inmoviliza en el SHA base.
- Seis Tasks Javier verificadas habilitadas con prompts V5 + ruta GitHub Actions; se repararon las claves operativas que aún decían 0/6 y la suspensión antigua pasó a histórico, sin cambiar RRULE/IDs ni el ámbito exclusivo Alex.
- Selector `scripts/validate-dual-task-configuration.mjs` ahora reconoce IDs Cdd-dd.Mdd.dd del V5, rechaza la cola V13 antigua y conserva pruebas de identidad/seguridad. CI de jobs y logs sólo acredita el SHA exacto y los tests realmente ejecutados; un job SKIPPED nunca equivale a PASS.
- Snapshot a la hora de la intervención: C00-01.M03.01 registrada VALIDANDO, 12 LISTO, 5 candidatas PENDIENTE habilitadas; este commit documental no la convierte en LISTO ni cambia PLAN_MAESTRO, COLA, BITACORA, backend o datos. Rollback por git revert con HEAD revalidado.

## 2026-11-13 - V5 C00-01.M03.01 dominio: fingerprint SHA-256 fail-closed [Javier :50]
- Corrige `OrdenCompra.EstablecerIdempotencia`: exige hex ASCII de 64 caracteres y canoniza replay de huella equivalente; rechaza reuso con payload distinto y preserva estado ante error.
- Añade xUnit positivo/negativo de dominio, contrato documentado y límites de evidencia. A0-A3 deben validarse en candidato; A3 .NET/MySQL en GitHub Actions del SHA nuevo, A4 posdev y A5 por revisor ajeno. Sin QA/main/PROD.

## 2026-11-13 - V5 C00-01.M02.05 eventos e idempotencia [Javier :00]
- Definicion documental trazable de correlation HTTP/Kardex, OrdenCompra, MensajeOutbox y MensajeOutboxService, con limites de IdempotencyKey, correlacion, retries y denegacion multiempresa; no cambios runtime, SQL ni providers.
- Estado EN_PROGRESO y VALIDANDO registrado antes de auditorias formales A0-A3; Node 22.16.0 --check, inspeccion dirigida de secretos y 9/9 pruebas documentales (1 positiva, 8 negativas) PASS. Sin afirmar HTTP/MySQL/live ejecutados.
- Commit directo fast-forward dev con A4 CI 5/5 exact SHA y readback pendientes; registrar A5_PENDIENTE para otro TASK_ID, rollback git revert no destructivo, no QA/main/PROD.

## 2026-11-13 — V5 C00-01.M02.04 configuracion tipada y feature flags [Javier :40]
- Reconstruido candidato documental en estado VALIDANDO con mismo TASK_ID; ocho contratos CFG01-CFG08 de configuracion, grants, pilotos UI, aislamiento DB y rollback, fundamentados en cinco blobs reales de dev. No se activaron flags ni modifico runtime, secretos, datos, Angular, QA o PROD.
- A0 Node 22.16.0 --check y patrones de secretos sin coincidencias; A1 matriz documental positiva; A2 negativos dirigidos de fail-closed y tenant A/B; A3 node --test 9/9 PASS (documentacion y fixtures sinteticos, NO HTTP/MySQL/browser/tenancy live).
- A4 requiere diff, SHA publicado, CI exacto y readback PLAN/COLA/BITACORA antes de LISTO_DEV. A5_PENDIENTE para otro TASK_ID, sin autoaprobar QA/main/PROD. Rollback: git revert normal sin force-push.

## 2026-11-13 - V5 C00-01.M02.06 dependencias tecnicas y salida M02 [Javier :00]
- Contrato documental V5 FINAL de las seis unidades M02; puerta G/O del ID separada de cierre del lote, matriz de dependencias y evidencias, negacion de acceso cross-tenant, A5 independiente, CI exact SHA y rollback no destructivo.
- Claim y VALIDANDO preservan TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a en PLAN; candidato preparado y auditado despues de VALIDANDO con Node v22.16.0 --check, secreto/whitespace scan sin hallazgos, Node --test 8/8 (1 positivo, 7 negativos), sin dependencia adicional ni cambio a backend/frontend/MySQL.
- Commit directo fast-forward dev condicionado al HEAD; A4 requiere verificacion posdev SHA, pruebas repetidas y CI 5/5 exact SHA antes de LISTO; A5_PENDIENTE de otro Scope. No se certifican HTTP/MySQL/browser ni se promociona QA/main/PROD. Rollback git revert aditivo.

## 2026-11-13 — V5 C00-01.M02.03 persistencia, indices, migraciones y datos de prueba [Javier :20]
- Se documenta el contrato de nueve invariantes DB01-DB09 de MySQL/EF Core/Oracle, indexes compuestos de Cliente/Producto por EmpresaId, migracion reversible y fixtures aislados A/B. Rutas y blobs Git comprobados en dev, evidencia estatica no es ejecucion DB.
- Cambio acotado a docs/v5/c00-01-m02-03-persistence-contract.md, scripts/tests/c00-01-m02-03-persistence-contract.test.mjs y CHANGELOG. A0 sintaxis Node 22.16.0 y escaneo de patrones de secretos; A1 positivo documental; A2 ocho negativos fail-closed; A3 Node 9/9 PASS. Sin dependencias nuevas ni cambios en runtime, datos, JWT/RBAC, QA/main/PROD.
- A4 requiere commit directo fast-forward, diff, CI exact SHA y readback institucional. A5_PENDIENTE exige otro TASK_ID antes de QA/main/PROD, rollback aditivo git revert sin reescribir historia. No inventar ejecucion MySQL, HTTP, Playwright ni migraciones.

## 2026-11-13 — V5 C00-01.M02.02 contratos API/DTO/errores y validadores [Javier :40]
- Inventario documental V5 de ocho contratos reales en API/Application con hashes de blobs Git: Empresas API/DTO, tenant-context, Productos API/DTO, FluentValidation producto y login, middleware global de errores 400/403/404/409. Se protegen límites JWT, RBAC, compatibilidad legacy, variantes, tenant A/B y no exposición de secretos.
- Transición de PLAN PENDIENTE→EN_PROGRESO→VALIDANDO registrada antes de A0–A3, con mismo TASK_ID, BITACORA y COLA por fórmula. Node22 --check, scan dirigido de secretos y 8/8 pruebas documentales (una positiva, siete negativas) PASS sobre candidato; sin cambios runtime/DB/Angular, ni afirmar HTTP/MySQL/browser/tenancy live.
- Commit directo fast-forward dev, A4 CI exact SHA y readback posteriores necesarios antes de LISTO; A5_PENDIENTE de otro TASK_ID previo a QA/main/PROD; rollback git revert aditivo sin force-push.

## 2026-11-13 — V5 C00-01.M01.05 aceptación funcional y técnica observable [Javier :20]
- Definida matriz AC01-AC12 con señales positivas, negativas y evidencia futura de gobierno, V5, autoría, seguridad multitenant, DTO, MySQL, UX, A0-A5, rollback, proveedores e integración. Estado explícito DEFINIDO_NO_CERTIFICADO; el documento no constituye evidencia live.
- Cambios delimitados a docs/v5/c00-01-m01-05-acceptance.md, scripts/tests/c00-01-m01-05-acceptance.test.mjs y esta nota. Node 22.16.0 node --check y node --test 9/9 (1 positivo, 8 negativos) PASS sobre blobs exactos 308033dd y 74f412ab; revisión dirigida de secretos sin hallazgos y sin dependencias nuevas.
- A0-A3 dirigidas durante VALIDANDO; A4 requiere SHA publicado, diff, CI causal y readback PLAN/COLA/BITACORA. A5_PENDIENTE para otro TASK_ID. Sin cambiar Angular/.NET/MySQL/JWT/tenancy, sin QA/main/PROD. Rollback aditivo git revert del commit tras validar HEAD.

## 2026-11-13 — V5 C00-01.M02.01 ownership, entidades e invariantes [Javier :40]
- Contrato documental verificable sobre nueve fuentes reales de Domain (Empresa, Usuario, ContextoTenantActual, Producto, Variante, MovimientoInventario, Factura, BaseEntity y AuditableEntity), con identificadores de blob rastreables del HEAD de entrada, límites entre tenant A/B y nulidad histórica, normalización, lote/vencimiento, movimiento, auditoría e independencia A5.
- Microtarea V5 FINAL C00-01.M02.01 sin cambios de runtime Angular/.NET/MySQL, JWT/RBAC ni migraciones. Tras EN_PROGRESO se registró VALIDANDO ANTES de A0–A3; Node 22.16.0 --check, revisión de secretos y ocho pruebas dirigidas de contrato (positivo y siete negativos) PASS. Solo se afirma alcance documental; no se certifican .NET HTTP, tenancy live, MySQL, Angular browser ni proveedores.
- Publicación directa fast-forward en dev y A4 sujetas a commit/CI exact-SHA/readback institucional, luego registrar A5_PENDIENTE para TASK_ID ajeno, nunca autorización de QA/main/PROD; rollback git revert aditivo, sin force-push.

## 2026-11-13 — V5 C00-01.M01.06 bloqueo, rollback y liberación de M01 [Javier :20]
- Contrato documental de nueve categorías de NO_GO con evidencias y acción de salida (Skill/Scope Lock, identidad, concurrencia, calidad A0–A4, independencia A5, JWT/RBAC/tenancy, proveedores y medios históricos cero). No se inventa un nuevo lock de M02 ni se suplanta la fórmula de COLA. La salida M01.06 no acredita los seis cierres de M01.
- Fuente: V5 FINAL / C00-01.M01.06. Cambios exclusivamente docs/v5/c00-01-m01-06-release-gate.md y scripts/tests/c00-01-m01-06-release-gate.test.mjs, CHANGELOG aditivo; Node 22.16.0 dirigido 7/7 (positivo y seis negativos), --check, revisión de secretos sin hallazgos, sin dependencias nuevas. Precheck falló inicialmente por omitir palabra GitHub y se corrigió antes de VALIDANDO; auditorías A0–A3 ejecutadas de nuevo después de VALIDANDO 7/7 PASS.
- A4 requiere commit directo FF, comprobación del SHA exacto, CI/GitHub y readback PLAN/COLA/BITACORA. A5_PENDIENTE otro TASK_ID antes de QA/main/PROD. No se afirma HTTP/MySQL/Angular-browser, seguridad de tenancy viva ni migración de datos. Rollback: revert aditivo sin force-push.

## 2026-11-13 — correccion causal de Scope Lock V5 C00-01.M01.03 [Javier :30]
- GitHub Actions exact SHA bac2583c falla Scope Lock y Seguridad porque el nombre de dos archivos incluia una identidad heredada bloqueada por scripts/verify-project-scope.mjs. Se corrigieron solamente las rutas y el import del test; sin alterar politica, scripts de seguridad, runtime ni contratacion.
- Rutas nuevas docs/v5/c00-01-m01-03-source-audit.md y scripts/tests/c00-01-m01-03-source-audit.test.mjs; rutas viejas retiradas del arbol por commit aditivo, sin reset ni force. Node --check PASS; Node --test 9/9 PASS; el patron prohibido ya no aparece en paths ni fuente del test. A4 y LISTO sujetos a nueva CI exact SHA y readback.

## 2026-11-13 — V5 C00-01.M01.03: inventario de fuentes, evidencia y contradicciones [Javier :30]
- Documenta 10 IDs de blob Git verificados, Google Doc V5 FINAL y Sheet 2.340 IDs, limites de hash/autoridad, estado V5, topologia objetivo frente a realidad, pausa/reanudacion y formulas PLAN/COLA. La autoria Git no es firma criptografica del propietario.
- Cambio documental con prueba Node 22.16.0: 9/9 PASS (1 positivo, 8 negativos fail-closed), node --check PASS y escaneo dirigido de secretos PASS. A0 estatico, A1 funcional documental, A2 limites de tenancy/JWT/RBAC/PII sin runtime modificado, A3 integracion de contrato Node PASS. No afirmar HTTP/MySQL/Playwright ni proveedores reales.
- A4 sujeto a lectura Git dev FF exact SHA, diff final y readback PLAN/COLA/BITACORA; A5_PENDIENTE para TASK_ID independiente antes de qa/main/PROD. Rollback git revert protegido, no reset/force.

## 2026-11-13 — V5 C00-01.M01.04: riesgos, supuestos y decisiones pendientes [Javier :20]
- Inventario causal de nueve riesgos R01–R09, cuatro supuestos y decisiones de salida del baseline: orden posterior V5 frente a hold histórico, claim concurrente J10/J20 de M01.02 resuelto sin tomar trabajo ajeno, infraestructura propuesta no desplegada, imágenes históricas cero (NO_APLICA), seguridad A/B y A5 pendientes sin falsos PASS.
- Cambios exclusivos: docs/v5/c00-01-m01-04-risks.md, scripts/tests/c00-01-m01-04-risks.test.mjs y CHANGELOG aditivo. A0: Node 22.16.0 --check y grep dirigido de secretos PASS; A1/A2: positivo + cinco negativos fail-closed, 6/6 Node tests; A3: integración documental/protocolo sin runtime, sin deps nuevas. No se simulan .NET, Angular/Playwright browser, HTTP, MySQL, tenants live, providers ni migración.
- A4 requiere SHA fast-forward, diff y tests pospublicación, checks GitHub exact-SHA y readback PLAN/COLA/BITACORA. A5_PENDIENTE corresponde a otro TASK_ID, sin QA/main/PROD. Rollback: git revert aditivo sin force-push.

## 2026-11-13 — V5 C00-01.M01.02 inventario de actores, interfaces y superficies
- Matriz de entradas, salidas, actores y límites de confianza para plan V5 (solo documentación baseline). Caso positivo y cinco negativos ejecutados con Node 22: 6/6 PASS, análisis estático y scanner de secretos dirigido; sin .NET, Angular E2E, HTTP, MySQL ni disponibilidad de proveedores. Conserva JWT/BCrypt/RBAC/tenancy A/B, separación QA/main/PROD, migración histórica de imágenes NO_APLICA. A5_PENDIENTE independiente; rollback mediante git revert normal. Ningún PR o branch nuevo.

## 2026-11-13 — V5 C00-01.M01.01 alcance y no-regresión
- Documentado objetivo, no-alcance, fuentes Git con huellas, contradicciones y rollback del baseline de la primera microtarea V5. Sin cambios de API, DB, frontend, migraciones, autenticación ni proveedores. Prueba Node 22 dirigida 5/5 (positivo y cuatro negativos) más A0 scan de secretos; sin E2E/HTTP/MySQL/producción. `A5_PENDIENTE` de Task independiente. Commit directo a `dev`, sin PR/ramas.

## 2026-11-12 — Base documental V5 y nueva orden condicional de reanudación
- Plan rector V5 FINAL y Sheet 2.340 IDs verificados; COLA derivada mediante 35.100 fórmulas + 65 gates; DASHBOARD 13; PLAN y BITACORA son fuentes de escrituras reales. Filas auxiliares recortadas y BITACORA sin límite adicional.
- Autoriza reactivar seis Javier tras todos los checks requeridos verdes del SHA exacto; conservar seis prompts/horarios/IDs/V13 A0–A5, retirar únicamente orden temporal OWNER_PAUSE_HOLD después de CI y restaurar peer-watch. Dos Alex sólo desde su titular. Copia de las seis configuraciones: https://docs.google.com/document/d/1w_nqqAPX3bhvMTVjfrDQEr8Weii8jZ4BcaE8qFvnrsw/edit
- Jira KAN-4 informativo, sin duplicar 2.340 issues; backup previo a limpieza: https://docs.google.com/spreadsheets/d/1QjTHCYKaerAtshAIkXKvTXg-KidavnDG18DOw2jLh2k/edit . No cambia runtime ni DB, QA/main/PROD, ni concede permisos nuevos. Revert normal selectivo para rollback de este commit.

## 2026-11-12 — P5-001.C: nuevo contrato delimitado de tokens institucionales
- Nueva prueba de contrato sobre `frontend/src/styles.scss` blob `2eb48562fd77c758662af6b9683649b4f9698a64`, ocho subtests negativos/positivos, workflow exact-HEAD y acta de recuperación. Sin cambios de runtime, DB, API ni secretos.
- Candidato previo SHA256 de Alex irrecuperable; esta reconstrucción corresponde a una nueva intervención autorizada de Javier, no atribuirla a Alex. A4 requiere readback CI+Sheets; A5 independiente pendiente antes de QA/PROD.
- No reactivar las Tasks Javier o Alex ni reclamar microtareas nuevas durante la pausa del propietario.

## 2026-11-12 20:56 America/Tegucigalpa — Pausa ejecutiva y congelamiento de nuevas reclamaciones
- El propietario ordenó pausar seis Javier y dos Alex para modificar el Plan Comercial. Se aplicó actualización física a las seis Javier con is_enabled=false y prompt OWNER_PAUSE_HOLD (IDs, títulos, RRULE y zona sin cambios). Readback físico: cero habilitadas Javier.
- Reanudar compañeras queda expresamente prohibido; únicamente puede terminar una microtarea reclamada antes de recibir la orden y salir sin nuevo claim. DEV 03/04/05/06 estaban in_progress cuando se consultó; el cambio físico no garantiza recepción de la orden por las corridas ya iniciadas.
- Las dos Alex pertenecen a otra cuenta; no se modificaron y su suspensión no está verificada. No inventar ocho pausadas. Su titular debe gestionar la pausa física.
- Sin cambios de código productivo ni de seguridad, QA/main/PROD, PR, secretos o roles. Retorno del scheduler únicamente bajo orden posterior y revisión del plan.

## 2026-11-12 — P5-006.C: delimitación del modal de confirmación institucional (Javier :30)
- Archivo de producto único `frontend/src/app/shared/confirm-dialog.component.ts`, blob `f9b73544cc70beba83a891b2fc89537f0718af8b`. Contrato Cancelar=false/Eliminar=true, título/mensaje interpolados, Material Dialog standalone.
- Prueba estática `scripts/tests/p5-006-c-delimitacion-confirmacion.test.mjs` (Node 22.16.0, seis subtests dirigidos) y `docs/P5-006-C-DELIMITACION-CONFIRMACION-DEV.md` documentan el escenario y negativos. No altera runtime Angular, ni certifica navegador, JWT, MySQL o QA/PROD.
- Rollback con revert normal selectivo; A4 exige SHA exacto/readback tras dev; A5 independiente pendiente para promoción.

## 2026-11-12 — P5-004.C: delimitación estática de detalle modal (Javier :40)
- Fuente única `frontend/src/app/shared/alerts/app-alert.service.ts`, blob `3bde601b54bad7d979fca9029025ce4eb72a44db`, escenario de detalle optativo seguro. No certifica todos los modales ERP.
- Prueba `scripts/tests/p5-004-c-delimitacion-modal-detalle.test.mjs`, Node 22.16.0 6/6 preflight extracto y 8/8 inspección de fuente real en GitHub, sin afirmar browser/E2E.
- Nota `docs/P5-004-C-DELIMITACION-MODAL-DETALLE-DEV.md`; solo test/nota/changelog, A5 independiente pendiente.

## 2026-11-12 — P5-008.D: reuso de alertas institucionales existente (Javier :20)
- REUSE del servicio `frontend/src/app/shared/alerts/app-toast.service.ts` (blob `0478821b246cae5fdf3016a5c25081a1726f925a`): severidades, duración, acción texto Cerrar, anuncio aria y estilos. Sin modificar Angular runtime ni servicios.
- Prueba dirigida `scripts/tests/p5-008-d-reuso-alertas.test.mjs`: Node 22.16.0 6/6 PASS tras VALIDANDO (control positivo de fuente real, 5 negativos sobre duración, texto/HTML, aria, wrapper error y acción de cerrar). SHA256 `89721265805b7f22b4121d61df2790a1de6bc32ee365e07ce1fd82c9325bead2`. Nota `docs/P5-008-D-REUSO-ALERTAS-DEV.md`.
- No certificar navegador, lector, tenants, JWT/DB; A4 SHA/readback poscommit y A5 independiente pendiente. Rollback mediante revert aditivo sin force.

## 2026-11-12 - P5-012.C: delimitacion del mapa de navegacion jerarquica V1 (Alex :35)
- Contrato producto existente frontend/src/app/shared/navigation/navigation-contract.json, blob 7142082462d7bc53ff4f0cdf96f67f21405f3a44, no editado.
- Prueba scripts/tests/p5-012-c-delimitacion-jerarquia-navigation.test.mjs: 5 casos Node real JSON, control de esquema, duplicados, metadatos modulo/accion y admins, rechazo de nietos no soportados; nota docs/P5-012-C-DELIMITACION-NAVEGACION-JERARQUICA-DEV.md.
- Solo DELIMITAR: padre/hijo V1, no profundidad ilimitada, autorizacion backend o navegador. A5 independiente pendiente; rollback aditivo via git revert sin force.

## 2026-11-12 — P5-009.C: delimitación de scroll, foco y Escape (Javier :20)
- Un solo archivo de producto inspeccionado: `frontend/src/app/app.component.ts`, blob exacto `11aa993b011119d33e402f3f2bc1d08f133e34bd`, sin cambios funcionales. Contrato observado: foco/retorno de menú, Escape, scroll lock/release, destino de contenido y limpieza de contexto.
- Test Node v22.16.0 `scripts/tests/p5-009-c-delimitacion-scroll-foco-escape.test.mjs` ejecutado después de VALIDANDO: 6/6 PASS (positivo real-source más cinco negativos); SHA256 `b0aed580f93e6f272b00d1201650b57a70d5d0583568cbf712fb5fa30fc42740`. Nota `docs/P5-009-C-DELIMITACION-SCROLL-FOCO-ESCAPE-DEV.md`.
- Es delimitación estática, no prueba de navegador, foco real, accesibilidad ni permiso multiempresa. A5 independiente pendiente; rollback con revert aditivo, sin force-push.

## 2026-11-12 — P5-014.C: delimitación estática de menú accesible (Javier :40)
- Fuente exclusiva `frontend/src/app/shared/navigation/app-navigation-menu.component.ts` blob `b70ef4ffb252228cf2ab19754a5e30d53f64aca0`, con enlaces semánticos/ARIA y RBAC de navegación, sin alterar el runtime.
- Test dirigido `scripts/tests/p5-014-c-delimitacion-teclado-aria-menu.test.mjs` (Node 22.16.0, 6/6 preflight sobre extracto, y 8/8 pruebas sobre fuente completa GitHub por inspección dirigida); NO certifica navegador, lector de pantalla ni auth backend.
- Evidencia y rollback `docs/P5-014-C-DELIMITACION-TECLADO-ARIA-MENU-DEV.md`; no API, migraciones, datos, dependencias, QA/main/PROD; A5 independiente pendiente.
## 2026-11-12 - P5-017.C: delimitacion responsive de modal de alerta (Alex :35)
- Archivo de producto de referencia sin modificar: frontend/src/app/shared/alerts/app-alert.service.ts, blob 3bde601b54bad7d979fca9029025ce4eb72a44db.
- Test scripts/tests/p5-017-c-delimitacion-layout-responsive.test.mjs: cinco casos Node positivos/negativos sobre ancho 94vw, breakpoint 480px, apilado, foco restaurado, rechazo HTML. Nota docs/P5-017-C-DELIMITACION-LAYOUT-RESPONSIVE-DEV.md.
- Alcance DELIMITAR, no certifica dispositivo real, WCAG, tenancy/JWT/RBAC, QA/main/PROD; A5 independiente pendiente. Reversión selectiva con commit ordinario sin force.

## 2026-11-12 — P5-015.C: delimitación de estados de carga y vacío (Javier :20)
- Archivo real único `frontend/src/app/features/usuarios/usuario-detail.component.html` (blob `bca657fd3ede944cb10c77ddc72063c11eb0ec65`): ramas loading spinner, notFound vacío y usuario cargado con interpolación segura; sin modificar runtime.
- `scripts/tests/p5-015-c-delimitacion-loading-empty.test.mjs` Node22.16.0 (5/5 PASS después de VALIDANDO): positivo, y negativos de spinner ausente, ramas no exclusivas, loaded sin guardia y HTML/PII insegura; `docs/P5-015-C-DELIMITACION-LOADING-EMPTY-DEV.md`. SHA256 `cf33d0e8cca241de789c920b3c869af64c2f1c90143a5fc620e8a4bc46fdea6c`.
- Alcance exclusivamente estático sobre código real; no afirmar navegador, Angular E2E, tenants o datos live. A4 postcommit y A5 revisión por otra Task. Revert aditivo sin force.

## 2026-11-12 — P5-002.C: delimitación estática del shell administrativo (Javier :40)
- Se identifica como único TARGET_PATH de producto `frontend/src/app/app.component.ts`, blob `11aa993b011119d33e402f3f2bc1d08f133e34bd` (HEAD dev comprobado); gate autenticado, navegación lateral, skip-link, foco y reset de contexto.
- Se agrega `scripts/tests/p5-002-c-delimitacion-shell.test.mjs` con un positivo y negativos de auth, accesibilidad, limpieza tenant y HTML inseguro; preflight de lógica Node 6/6 sobre extracto fuente y lectura directa exacta de GitHub 8/8, sin fingir E2E/browsers.
- Nota causal `docs/P5-002-C-DELIMITACION-SHELL-DEV.md` con ruta, SHA, evidencia y rollback. No se modifica Angular runtime, permisos, API, DB, dependencias, QA/main/PROD; A5 por otro TASK_ID después de A0–A4.
## 2026-11-12 — P5-007.C: delimitación de modal de proceso institucional (Javier :20)
- Delimitado el componente real de anulación `frontend/src/app/shared/anular-dialog.component.ts` (blob `d751503438f963f1deebe371cd14623ad6038e16`), sin cambio de runtime; flujo motivado, cancelación segura, texto interpolado y diálogo tipado.
- `scripts/tests/p5-007-c-delimitacion-modal-proceso.test.mjs` prueba el contrato estructural con Node v22.16.0 5/5, positivo y cuatro negativos dirigidos: cancelación, motivo vacío, HTML crudo, contexto perdido; SHA256 `34b37488f05dae7dd68049d892a8436288470a036999f4dfa35324b9ec59332b`. Documentación `docs/P5-007-C-DELIMITACION-MODAL-PROCESO-DEV.md`.
- A0–A3 ejecutadas tras VALIDANDO; A4 requiere readback exact dev, A5 independiente pendiente. No certificar navegador, Angular live, tenancy, MySQL, QA/PROD. Rollback con revert aditivo sin force-push.

## 2026-11-12 - P5-003.C: delimitacion del contrato de cuatro modales (Alex :35)
- Fuente unica de runtime existente: frontend/src/app/shared/alerts/app-alert.service.ts, blob 3bde601b54bad7d979fca9029025ce4eb72a44db; sin modificar Angular.
- Prueba estructural scripts/tests/p5-003-c-delimitacion-contrato-modales.test.mjs (5 casos, positivo y negativos de cancelacion, foco, confirmacion, requisito y HTML) y nota docs/P5-003-C-DELIMITACION-CONTRATO-MODALES-DEV.md.
- Alcance solamente DELIMITAR; no afirma cuatro modales operativos, browser/E2E, autorizacion RBAC/JWT/tenancy ni QA/main/PROD. A5 independiente pendiente; revert selectivo sin force.

## 2026-11-12 — P5-010.C: delimitación de paginación y filtros de listas (Javier :20)
- Foco acotado a `frontend/src/app/core/navigation/list-navigation-state.service.ts` (blob `513765de62e3ad2fac85f1495054448cfea2ba9e`), sin cambiar frontend productivo; se documentan claves permitidas, precedencia query params, `replaceUrl`, almacenamiento tolerante a errores, coerción y clave de usuario.
- Prueba sobre código fuente real `scripts/tests/p5-010-c-delimitacion-paginacion.test.mjs` con Node 22.16.0: `node --check` y `node --test` 5/5 PASS, control positivo y negativos de allowlist, query, historial, namespace, coerción y storage. SHA256 de prueba `8e76f7e14e5e8dfdb4dee591e62436a31e1e5feef453aa5afe3946c753662293`.
- Solo delimitación: no acredita browser real, aislamiento multiempresa, Angular runtime, MySQL ni QA/main/PROD. A5 pendiente de otro TASK_ID. Rollback: revert selectivo aditivo, sin force-push.

## 2026-11-12 — P5-016.C: delimitación de acciones visuales coherentes (Javier :20)
- Contrato real de acción uniforme `Cerrar` en `frontend/src/app/shared/alerts/app-toast.service.ts`, blob `0478821b246cae5fdf3016a5c25081a1726f925a`; no se modifica producto Angular.
- Test de delimitación `scripts/tests/p5-016-c-delimitacion-acciones.test.mjs` 5/5 Node 22.16.0, positivo y negativos de ausencia de fallback Cerrar, wrappers de severidad incongruentes, estilos sin variante, HTML inseguro y contrato vacío. Nota `docs/P5-016-C-DELIMITACION-ACCIONES-DEV.md`.
- Solo evidencia estática del código vivo; no certifica interacción en navegador, lector de pantalla, responsive, JWT/RBAC/tenant, backend ni QA/PROD. Rollback selectivo con revert aditivo en dev sin force. A5 independiente pendiente.

## 2026-11-12 — P5-008.C: delimitación de alertas institucionales (Javier :20)
- Archivo real único `frontend/src/app/shared/alerts/app-toast.service.ts`, blob `0478821b246cae5fdf3016a5c25081a1726f925a`. Escenario acotado de notificaciones toast, no el subsistema modal completo.
- Prueba estática dirigida `scripts/tests/p5-008-c-delimitacion-alertas.test.mjs` (Node 22.16.0; 5/5 precheck) y nota `docs/P5-008-C-DELIMITACION-ALERTAS-DEV.md`: variantes, urgencia accesible, duración y rechazo de HTML inseguro. No cambia Angular ni certifica browser/tenancy/live.
- Reversión selectiva aditiva en dev sin force; A4 requiere SHA y readback del mismo changeset; A5 independiente pendiente antes de QA/PROD.

## 2026-11-12 — P5-013.C: delimitacion de una rama activa de menu (Javier :10)
- Contrato unico de producto `frontend/src/app/shared/navigation/navigation-contract.json`, blob 7142082462d7bc53ff4f0cdf96f67f21405f3a44. Escenario grupo `administracion`, sin editar runtime ni declarar expansion exclusiva del menu.
- Nota `docs/P5-013-C-DELIMITACION-RAMA-MENU-DEV.md` y test Node `scripts/tests/p5-013-c-delimitacion-menu.test.mjs`, casos estructurales positivo y negativos de grupos/rutas/permisos. No se acredita prueba Angular, navegador, JWT/backend ni QA/PROD; rollback mediante revert normal del changeset.

## 2026-11-12 — Identidad DEV 01/05 y selección factible; siete estados COLA sincronizados
- `is_self_owned_automation=false` significa sólo que la automatización no corre para el agente que hace esa consulta; no es revocación de cuenta. `Automations.get 404` no demuestra inexistencia de la Task; conservar `peek` como lectura positiva y validar propiedad/ID/conectores reales/Skill antes de cualquier escritura. Denegación real continúa NO_GO.
- V13 exige escanear TODAS las PENDIENTE habilitadas, comprobar herramientas concretas y continuar con otra elegible si .NET/Angular/browser/proveedor real no permite acreditar pruebas del primer candidato. Sin inventar A0–A5 ni completar falsamente.
- COLA filas 108,110,118,120,127,128,249: E manual VALIDANDO convertido a fórmula que refleja PLAN_MAESTRO H (PENDIENTE al corte). Nota en cada celda conserva antiguo estado, evidencias y autorías J:M. No se cambió PLAN, responsables, PR ni QA/PROD.

## 2026-11-12 — P5-018.C: delimitación del caso de accesibilidad transversal (Javier :50)
- Plan Comercial Inicial V3 P5-018.C, antecedente P1-016.B LISTO. Un solo archivo de producto: `frontend/src/styles/_m10-accessibility.scss`, blob de línea base `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f`. Documentación de foco, reduced motion y forced colors en `docs/P5-018-C-DELIMITACION-ACCESIBILIDAD-DEV.md`.
- Test Node aislado `scripts/tests/p5-018-c-delimitacion-accessibility.test.mjs` con caso positivo y rechazos de eliminación de motion, forced-colors y foco; no altera SCSS de producto, Angular runtime, backend, RBAC, tenants, MySQL, secretos ni entornos QA/main/PROD.
- Esta microtarea solo delimita el escenario; no certifica WCAG 2.2 AA, contraste, zoom, teclado/E2E ni lector de pantalla. P5-018.D/E requieren pruebas de uso real; rollback mediante revert selectivo normal en dev, sin force.

## 2026-11-12 - G0-006.B: cierre de matriz actual F/G/H y rechazo fases historicas (Javier :30)
- scripts/quality/g0-006-b-traceability-matrix.mjs y scripts/tests/g0-006-b-traceability-matrix.test.mjs verifican forma de recibos comerciales F/G/H: ID, test, SHA commit, estados LISTO, dependencia SI, readback institucional, A5 pendiente o PASS sin permiso de promocion; rechazo de fases 8/13 y reactivacion de migracion Angular concluida.
- Las referencias de commit son F=6a10d42e, G=9c6f2158, H=a8de9825 leidas de GitHub y PLAN/COLA; control positivo en test sintetico NO certifica por si mismo fuentes externas ni autoriza QA/main/PROD. La matriz real se registra en el ledger institucional, sin introducir otro Plan Maestro.
- A0 Node22 --check, inspeccion secretos y ausencia de deps; A1 5/5 pruebas de contrato; A2 fases obsoletas, evidencias falseadas y A5 autopromocion rechazadas; A3 20/20 suites Node (B/G/H y compatibilidad D/E/F) PASS. A4 requiere commit exacto FF y readback; A5 pendiente de otra Task; rollback revert selectivo sin force.

## 2026-11-12 - G0-006.H: regresion positiva de trazabilidad V13 Direct DEV (Javier :30)
- Prueba scripts/tests/g0-006-h-positive-traceability.test.mjs verifica recibo sintetico positivo de requisito G0-006.H a codigo/test/auditorias/readback, junto con negatives SHA/arbol, A4, ausencia de tests, SKIPPED, fase historica y falta de evidencia Sheets/BITACORA. No certifica recibos reales ni promocion de entornos.
- A0 Node22.16.0 node --check, escaneo de secretos sin patrones; A1 5/5 Node focused suites PASS; A2 fase obsoleta no reabre Angular; A3 15/15 Node test suites compatibles con G0-006.G y pruebas locales D/E/F, sin dependencias ni modificaciones runtime. A4 exige SHA de commit y readback exacto; A5 otro Scope.
- Rollback via git revert selectivo, sin cambios a frontend/backend/MySQL/JWT/RBAC/QA/main/PROD. Ninguna atestacion externa del control sintetico.

## 2026-11-12 - G0-006.G: rechazo de reapertura historica Angular 8/13 (Javier :30)
- Se endurecio el helper informativo scripts/quality/direct-dev-traceability.mjs para rechazar intentos estructurados de reabrir fases retiradas 8/13 o migracion Angular completada aun cuando se disfracen con scope y hashes comerciales validos. No concede autoridad para publicar ni marca LISTO por si solo.
- Prueba dirigida scripts/tests/g0-006-g-legacy-angular-reopen.test.mjs: 5 suites Node; 10/10 PASS con regresion local de controles D/E/F. A0 Node22 --check y busqueda de secretos; A1 positivo+negativos sinteticos; A2 rechazo de fase heredada fail-closed; A3 API guard Node sin dependencias nuevas, sin mutaciones frontend/backend/DB/RBAC/JWT.
- Evidencia offline, no atestacion real de GitHub/Sheets/A5/QA/PROD; A4 exige commit dev fast-forward, readback SHA y BITACORA; A5 queda PENDIENTE por otro Task. Rollback mediante revert selectivo del commit publicado, sin force.

## 2026-11-12 — Incorporación Alex 2/2 según certificación del propietario
- Alex comunicó readback nativo desde su cuenta de sus dos Tasks existentes: PRIMARY DEV 07 :05 y PRIMARY DEV 08 :35, is_enabled=true, prompts V13 A0–A5 y recuperación mutua, completed y requires_user_action=false.
- Esta conciliación de documentos se basa en reporte de Alex, no en conexión nativa independiente desde Javier. No se tocaron Tasks físicas, permisos ni modelo de razonamiento; mantener límites de seguridad y pruebas A0–A5.

## 2026-11-12 — G0-005.B: puerta estructural de readback DEV/QA/PROD (Javier :20)
- Se agregan scripts/quality/g0-005-b-readback-policy.mjs y 4 pruebas Node dirigidas en scripts/tests/g0-005-b-readback-policy.test.mjs; un control positivo sintético y 33 negativos fail-closed para recibos F/G/H, SSO 503, upstream 503, origen, proyecto, SHA y separación de referencias no secretas.
- El positivo solo afirma STRUCTURAL_CONSISTENCY_NOT_LIVE_ATTESTATION. No certifica proveedores reales, bases/caches/secretos reales, login ni aprobación QA/PROD; ninguna credencial se inspecciona, se almacena o se imprime.
- Implementación Node 22.16.0 sin dependencias nuevas ni modificación de Angular, backend, MySQL, auth, tenancy, QA/main/PROD. Rollback: revert aditivo selectivo de commit directo dev sin force.

## 2026-11-12 - G0-005.H - positivo estructural y regresión de SHA exacto (Javier :50)
- Plan Comercial Inicial V3, microtarea G0-005.H, predecesora G0-005.G LISTO. Agrega scripts/tests/g0-005-h-positive-readback.test.mjs, 5 pruebas dirigidas y regresión A/F/G conjunta 34/34 Node22 PASS. SHA GitHub base observado 323e3333e73ce28369d82032c442705a325a9739, no equivale a SHA READY de Vercel real.
- Caso positivo DEV (sintético): mismo SHA en HEAD, deployment metadata y readback, alias SSO esperado 403, upstream 200. El global permanece NO_GO si QA/PROD no observados. Un segundo positivo completamente sintético utiliza dominios .synthetic.invalid de QA/PROD exclusivamente en memoria para comprobar un contrato estructural, explícitamente NOT_LIVE_ATTESTATION; **NO** representa QA/PROD publicado ni crea configuraciones reales.
- Negativos SHA incorrecto, proyecto Vercel foráneo y no exposición de PII/secretos conservan NO_GO. A0 sintaxis Node + inspección secretos/dependencias; A1 positivos/negativos; A2 fail-closed; A3 regresión Node; A4 requiere readback commit dev tras publicarse; A5_PENDIENTE revisión por otro TASK_ID para promoción.
- Sin cambios de seguridad runtime, JWT/RBAC/tenancy, backend, frontend, datos, workflows, QA/main/PROD, secretos o servicios de pago. Reversión: revert selectivo del commit directo en dev, sin force-push, preservando trabajo concurrente.

## 2026-11-12 - G0-005.G - regresión dirigida 503 y READY ajeno (Javier :50)
- Autoridad Plan Comercial Inicial V3 G0-005.G; depende de G0-005.F ya LISTO. Se agregan 7 pruebas negativas dirigidas en scripts/tests/g0-005-g-negative-readback.test.mjs; se reutiliza sin modificar el clasificador G0-005.A y el fixture sintético G0-005.F.
- Rechaza explícitamente: alias SSO 503, API upstream 503 con SSO 403 permitido, Vercel READY de proyecto ajeno, SHA de deployment distinto a HEAD, origen DEV ajeno y cualquier retorno con PII/secreto. No se consulta ningún origen proveedor en vivo: son casos sintéticos que acreditan lógica del clasificador, no disponibilidad externa.
- A0 Node v22.16.0 node --check y revisión de nuevos secretos/dependencias, PASS; A1 7/7 negativos funcionales PASS; A2 fail-closed de PII y falsos positivos PASS; A3 regresión Node --test G0-005.A/F/G 29/29 PASS. A4 requiere commit SHA exacto dev y readback posterior; A5_PENDIENTE revisión independiente antes QA/main/PROD.
- Rollback por revert selectivo de este commit directo DEV, sin force-push ni modificación de QA/main/PROD, MySQL, seguridad runtime, credenciales ni workflows.

## 2026-11-12 - G0-005.F - fixture sintético 503 para readback DEV (Javier :50)
- Fuente Plan Comercial Inicial V3 G0-005.F; depende de G0-005.A LISTO. Prepara un caso medible de resultado esperado, NO un 503 real: fixtures/g0-005-f-operational-503.json con recibos G0-005.C/D/E, SSO alias 503, API upstream 503, QA y PROD NOT_OBSERVED. DEV SHA del fixture es un identificador SINTÉTICO, no prueba de READY de Vercel.
- Prueba nueva scripts/tests/g0-005-f-operational-503-fixture.test.mjs: 4 casos dirigidos; NO_GO ante 503, NO_GO cuando falta observación de QA/PROD aunque controles DEV se vuelvan sanos, no promoción ni filtración PII. Regresión conjunta con G0-005.A: 22/22 Node v22.16.0 PASS; JSON parse y node --check correctos, cero dependencias o secretos añadidos.
- No se consultaron ni cambiaron qa/main/PROD, proveedor real, permisos, sesiones, MySQL ni infraestructura; ningún resultado de prueba sintética certifica HTTP/SSO/despliegues. Se conserva la custodia del autor y A5_PENDIENTE para revisión distinta.
- Rollback: revert selectivo del commit directo en dev, sin force-push, preservando G0-005.A y sus recibos.

## 2026-11-12 - G0-005.A - cierre de readback operativo con clasificador fail-closed (Javier :50)
- Fuente exclusiva: Plan Comercial Inicial V3, G0-005.A. Se reutilizan sin reescritura los recibos GitHub G0-005.C=0cea0aaae291a4d8161791f2b170d7087d5bc0e1, G0-005.D=e9a486776cbecdec8d6dfb6a7b1172cec6c28877 y G0-005.E=544a28fca6f63a9d5827086ccd07ac645a8f61e6, comprobados contra los commits originales y COLA G335=SI.
- Archivo nuevo scripts/quality/g0-005-a-readback-closure.mjs: clasifica recibos, matriz DEV/QA/PROD, origen, proyecto, SHA GitHub/despliegue y HTTP. El alias público DEV esperado es solqaryn-dev.vercel.app y el alias SSO dev protegido es solqaryn-dev-solqaryn.vercel.app. SSO 401/403 => AUTH_GATED_EXPECTED; SSO 503 y API upstream 503 => HTTP_ANOMALOUS/NO_GO; Vercel READY de proyecto ajeno o SHA divergente => NO_GO. Salida estructural nunca equivale a despliegue vivo ni A5.
- Matriz de observabilidad: DEV origen conocido https://solqaryn-dev.vercel.app/; QA/PROD: origen, proyecto, despliegue SHA, status HTTP y motivos NO_OBSERVADOS en esta ejecución, no se alteraron entornos ni se inventó equivalencia LIVE. Los ejemplos de pruebas Node son sintéticos. Si falta proveedor/lectura autentica el clasificador responde NO_GO; no reemplaza CI ni reviews oficiales.
- Pruebas dirigidas: scripts/tests/g0-005-a-readback-closure.test.mjs, 18/18 PASS Node v22.16.0, 16 escenarios negativos; A0 --check, dependencias nuevas 0, revisión de secretos sin hallazgos; A1 funcional sintético, A2 seguridad fail-closed/PII, A3 integración de blob exacto G0-005.E y regresión; A4 requiere SHA dev publicado + readback. A5_PENDIENTE: revisor TASK_ID distinto antes de QA/main/PROD.
- Rollback: revertir exclusivamente el commit directo G0-005.A en dev sin force-push, preservando los recibos C/D/E y commits de terceros. No tocar runtime auth, tenancy, MySQL, frontend, workflows, qa/main/PROD ni secretos.

## 2026-11-12 — Recuperación entre Scopes de Javier y preferencia máxima
- Seis Javier verificadas enabled. El propietario autorizó a las seis Tasks a reactivar una compañera pausada con una sola acción nativa is_enabled=true, lectura posterior y sin cambiar permisos o configuración.
- Preferencia de máximo razonamiento: instrucción y documentación actualizadas; herramienta de Tasks no expone selección model/reasoning_effort, por lo que no se certifica el nivel nativo. Alex sigue fuera de alcance.
- Se mantiene continuidad sin tope de cierres por corrida, auditorías A0–A5 y control estricto de acceso.

## 2026-11-12 - G0-006.F: preparar caso medible de trazabilidad (Javier :30)
- Fixture sintetico para detectar recibo de G0-006.F que reutiliza requirementId G0-006.E; esperado contract/NO_GO, no certificacion institucional.
- Cinco controles Node dirigidos: lectura fixture, baseline sintetico positivo, requisito ajeno negativo, hash de prueba obsoleto negativo y readback BITACORA ausente negativo. El helper de produccion se reutiliza sin alterarlo.
- A0-A3 dirigidas Node 22.16.0, sin cambios de runtime, RBAC, tenants, DB, CI, API, secretos ni dependencias. A4 requiere SHA exacto publicado y readback; A5_PENDIENTE solicita otra Task antes de QA/PROD. Rollback por git revert del commit directo, sin force.

## 2026-11-12 — Continuidad productiva sin tope de cierres
- Se refuerza continuar en una misma ejecución después de 3, 4, 5 o más cierres, sólo si hay tiempo, elegibilidad y evidencia completa A0–A4, registrando A5 pendiente de revisor externo.
- No autopausar Tasks por metas o cola momentáneamente vacía; si el proveedor impone suspensión o requires_user_action, comunicar causa verificable por run_id. No se garantiza eliminar pausas del proveedor.
- PRIMARY DEV 03 :20 se constató pausada por acción requerida sin causa disponible en el registro.

## 2026-11-12 — G0-006.A: trazabilidad directa V13 A0-A5 (Javier :20)
- Cierre atómico del Plan Comercial Inicial: evidencia C/D/E LISTO y matriz requisito→consumidor→código→prueba→SHA en docs/G0-006-A-TRAZABILIDAD.md, sin crear otro plan.
- Puerta asesora scripts/quality/g0-006-a-closure.mjs y pruebas Node deterministas: 1 control positivo, 23 negativos de legado, A0 estática, A5 falso, SHAs/dependencias/readbacks. La puerta no escribe ni certifica externamente.
- A0-A3 dirigidas a Node 22.16.0; A4 requiere SHA fast-forward/readback dev; A5_PENDIENTE exige otro TASK_ID antes de promover QA/PROD. Rollback: revert aditivo del commit, sin force-push; runtime/tenant/DB intactos.

## 2026-11-12 — Calidad A0–A5, títulos Javier secuenciales y continuidad de Tasks
- Se añaden A0 estática exigente sobre candidato y A5 revisión independiente posdev con evidencia diferenciada. El cierre dev exige A0–A4 PASS + A5_PENDIENTE; qa/main/PROD requiere A5_PASS y autorización aparte. No se deshabilitan CI ni seguridad existentes.
- Los nombres de las seis Tasks Javier se normalizan por minuto, preservando ID, RRULE, propietario, historial y estado habilitado. Las dos Alex permanecen pendientes de actualización física desde su cuenta, con propuesta 07 :05 y 08 :35.
- Pausas automáticas de proveedor: no garantizables ni anulables por prompt; registrar requires_user_action y alertar sin bypass. Mantener throughput, sin nuevos PR, ramas, rulesets ni Tasks.

## 2026-11-12 - G0-005.E - readback operativo DEV negativo dirigido (Javier :50)
- Fuente Plan Comercial Inicial V3 G0-005.E, predecesora G0-005.D LISTO. Clasificador determinista sin llamadas externas para distinguir alias SSO protegido, upstream API 503, proyecto Vercel ajeno, READY de SHA distinto y evidencias incompletas.
- 15 casos sintéticos con Node test; no certifican un despliegue real, paridad externa, login, tenants ni estado LISTO. Resultado positivo solo EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION; evidencia insuficiente NO_GO.
- Archivos scripts/quality/g0-005-e-operational-readback.mjs y scripts/tests/g0-005-e-operational-readback.test.mjs. Rollback: revert selectivo del commit directo en dev sin force; sin cambio de runtime, DB, secretos ni QA/main/PROD.

## 2026-11-12 - G0-004.G - rechazo dirigido de E2E falso verde (Javier :40)
- Alcance exclusivo del Standard Comercial V3 G0-004.G: casos sintéticos de retry tras fallo, skip por configuración de secreto no disponible, navegador omitido y fallo global de worker. Se comprueba FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT/FAIL; control positivo limpio PASS. No equivale a E2E browser real ni solicita leer secretos.
- Script dirigido `scripts/tests/g0-004-g-directed-negative.test.mjs`, compara clasificador existente sin cambiar API, JWT, empresa, MySQL ni frontend productivo. Reversión: revert protegido exclusivamente de esta prueba y nota de CHANGELOG en dev, nunca force-push.

## 2026-11-12 - G0-004.F - fixture medible de resultado E2E (Javier :40)
- Condicion seleccionada: Playwright JSON con 1 prueba pasada y 1 error global de setup; el verdict esperado es FAIL, la salida CLI es codigo 2 y no reproduce detalles del error. Control positivo: quitar el error global deja PASS. Son fixtures sinteticos y pruebas Node; NO equivalen a una ejecucion E2E real.
- Fuente Plan Comercial V3 G0-004.F; depende de G0-004.A LISTO, reusa clasificador `scripts/quality/playwright-outcome-classifier.mjs`. Regresion y fixture versionados en `scripts/tests/g0-004-f-global-setup-error.test.mjs` y `scripts/tests/fixtures/g0-004-f-global-setup-error.json`; rollback por revert protegido solo del commit directo en dev, sin force ni QA/main/PROD.

## 2026-11-12 - G0-004.A - cierre causal de clasificacion Playwright (Javier :40)
- Fuente Plan Comercial Inicial V3 G0-004.A; reutiliza G0-004.C/D/E LISTO. Corrige falso PASS si Playwright JSON contiene `errors` globales de setup/worker con tests positivos. Reportes con `errors` malformado son INVALID_REPORT. Errores globales solo cuentan numericamente, sin exponer titulos, PII, tokens ni contenido de errores.
- Regresion dirigida Node `scripts/tests/g0-004-a-playwright-global-errors.test.mjs`: positivos limpios, fallos globales, reporte sin pruebas, metadatos invalidos, skips, flakes, CLI no cero y no filtracion. Sin atribuir E2E navegador real, MySQL o auth a pruebas sinteticas.
- Archivos: `scripts/quality/playwright-outcome-classifier.mjs`, test nuevo y este CHANGELOG. Rollback: revert protegido del commit directo en dev, sin reescribir historia ni tocar main/qa/prod. A1-A4 y PLAN/COLA/BITACORA requieren readback exact SHA.

# SOLQARYN — Cambios operativos vigentes

## 2026-11-12 — Preparación documental de Alex 2/2 para eventual 6→8
- IDs Alex existentes preservados; horarios físicos registrados :10/:50 y nombres SECONDARY REVIEW 01/03 no alterados. Propuesta pendiente de decisión del titular: PRIMARY DEV 07 minuto :05, PRIMARY DEV 08 minuto :35, sin tocar las seis Tasks de Javier.
- Se deja handoff para sincronización física por Alex de prompts V13 direct-dev, readback real de identidad/horario/prompt y habilitación posterior únicamente por autorización expresa.
- Cuatro auditorías V13 sin cambios. Sin nuevas Tasks, ramas, PR, cambios funcionales o modificaciones de QA/PROD.

## 2026-11-12 — G0-006.E prueba negativa de fases retiradas (Javier :30)
- Cinco pruebas Node dirigidas al validador de trazabilidad de DEV directo: rechazan FASE8, M13, recibos de PR históricos, estados VALIDANDO disfrazados de LISTO, tests SKIPPED, SHA falso, auditoría A4 ausente y readback institucional no comprobado.
- Casos sintéticos, sin reabrir migraciones ni autorizar trabajo ajeno; el control del Plan Comercial sigue siendo externo al validador de forma intencional.

## 2026-11-12 — G0-004.E regresión de evidencia E2E fail-closed (Javier :30)
- Añadidas ocho pruebas Node sobre clasificador Playwright JSON vigente: reintentos fallidos/flaky, skip por secreto opcional, navegador no soportado, suite anidada, reporte vacío/inválido y CLI con salida distinta de cero; un PASS limpio es el control positivo.
- Pruebas sintéticas sin secretos o datos de empresas; solo cobertura de calidad, no autorización de despliegue ni certificación E2E contra navegador real.

## 2026-11-12 — Visibilidad exacta de VALIDANDO y paralelismo real por Scope
- Precisa la transición inmediata a EN_PROGRESO al reclamar microtarea; VALIDANDO sólo al disponer de candidato completo, ANTES de auditorías funcional, seguridad e integración; misma Task corrige sin abandonar la custodia; commit directo dev, cuarta auditoría posintegración y LISTO verificado. Otras Tasks trabajan en paralelo IDs libres. Meta 1/3 por corrida sin falsos cierres.

## 2026-11-12 — Limpieza definitiva de V13 directo en dev
- Reemplazados capítulos históricos contradictorios por un único contrato directo en docs/AUTOMATION_V13.md.
- Censo original y seis horarios Javier conservados, sin reactivación. Alex permanece fuera del control de la cuenta Javier.
- Cuatro auditorías distintas por microtarea, objetivo operativo mínimo 1 LISTO y meta 3 por corrida, commits fast-forward en dev, resolución causal y readback PLAN/COLA/BITACORA.
- Reglas antiguas de PR protegido, espera de approvals, checks bloqueantes, 52 PR FIFO, microfases y pilotos retiradas de las instrucciones activas.
- La historia anterior sigue disponible a través de commits anteriores de GitHub, pero no forma parte del contrato operativo actual.
- No hay cambios funcionales en frontend, backend, esquemas, QA/main/PROD, secretos, proveedores ni Tasks físicas.

## 2026-11-12 — G0-006.D: trazabilidad de cierre directo por cuatro auditorías
- Añadido verificador asesor y pruebas negativas del contrato requisito → código → pruebas → cuatro auditorías → SHA fast-forward dev → readback PLAN/COLA/BITACORA; un recibo PR-only o incompleto no acredita cierre directo.
- El contador histórico preserva sus eventos anteriores pero reconoce recibos DIRECT_DEV_FOUR_AUDITS únicamente cuando están completos y verificables. No concede LISTO, permisos, push ni modifica seguridad/tenants ni arquitectura.

## 2026-11-12 — G0-005.C: readback de DEV, rutas canonicas
- Delimitada la evidencia de equivalencia Vercel/DEV a scripts/vercel-dev-runtime-equivalence.mjs: las rutas comparadas no pueden tener segmentos vacios, separador Windows, caracteres de control ni terminacion de directorio.
- Anexadas cuatro negativas de la misma prueba offline, conservando positivos y el rechazo de cambios runtime/backend. No se infiere despliegue READY ni se toca QA/main/PROD.
- Rollback: revertir solo este bloque del normalizador y las cuatro pruebas, sin cambios en API, migraciones, datos ni credenciales.

## 2026-11-12 — G0-004.D: clasificar resultados Playwright E2E
- Nuevo CLI scripts/quality/playwright-outcome-classifier.mjs para resumir reportes JSON sin nombres ni datos de tests; PASS exige ejecuciones limpias, FAIL/FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT no se cuentan verde.
- Cuatro tests Node con casos positivos, negativos, flaky, skip, reportes invalidos y exit codes 2/3; sin modificar configuracion de runner, secretos, rutas de tenants, frontend ni backend. Rollback: revertir exclusivamente script y test.

## 2026-11-12 — G0-005.D: validación canónica de manifiesto DEV/Vercel
- `scripts/vercel-dev-runtime-equivalence.mjs`: fail-closed de rutas del manifiesto `frontend_before/after` usando segmento top-level canónico, sin `.`/`..` ni separadores Windows/control. Agregadas 4 negativas al self-test offline, positivos preservados.
- Solo contrato de readback, no prueba de un despliegue READY real. Sin cambios de Vercel, Render, credenciales, datos, frontend/backend ni QA/PROD. Rollback: revertir esta validación y las cuatro negativas.

## 2026-11-12 — G0-004.H: regresión positiva del clasificador Playwright E2E
- Nuevo `scripts/tests/g0-004-h-positive-regression.test.mjs`: caso positivo de tres pruebas PASS anidadas y CLI exit 0, junto con negativos FAIL/FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT y sanitización de títulos/errores sintéticos.
- A1 funcional y A2 seguridad fail-closed; A3 integración: Node 22 `node --test scripts/tests/g0-004-h-positive-regression.test.mjs` 4/4 PASS y `node --check` de test y módulo. Módulo original blob `c5085b044ffb0b4ffa8d971ab3280afb4a02c037` sin cambios; no significa ejecución real de navegador Playwright.
- Cambio limitado a test + CHANGELOG, sin runtime, dependencias, secretos, JWT/RBAC, tenant ni datos. Rollback: revertir sólo test G0-004.H y este bloque aditivo.

## 2026-11-12 — G0-004.B: matriz de recibos G0-004.F/G/H y regresión E2E sintética
- Matriz `docs/quality/g0-004-b-e2e-receipts.md` identifica 10 condiciones, rutas y negativos: setup global, worker error, retries false-green, skips por secret y navegador, éxito anidado y sanitización. Nuevo test `scripts/tests/g0-004-b-quality-receipts.test.mjs`: 4/4 Node22 PASS, source classifier exact blob c5085b; no se presenta como una ejecución real del navegador.
- A1 funcional, A2 seguridad fail-closed y A3 integración Node dirigida PASS; no altera runtime, tenants, JWT/RBAC, secretos o dependencias. Rollback selectivo: revertir este test, la matriz y el párrafo de changelog.

## 2026-11-12 — P5-005.C: delimitacion del formulario de anulacion (Javier :30)
- Archivo real inalterado `frontend/src/app/shared/anular-dialog.component.ts`, blob `d751503438f963f1deebe371cd14623ad6038e16`. DELIMITAR, no implementar un modal universal.
- `scripts/tests/p5-005-c-delimitacion-modal-formulario.test.mjs` y `docs/P5-005-C-DELIMITACION-MODAL-FORMULARIO-DEV.md`: Node 22.16.0 `node --check` PASS y `node --test` 6/6 PASS tras VALIDANDO sobre archivo real exact-blob. Negativos de binding, cancelacion, motivo vacio, HTML crudo y dependencia FormsModule; secretos añadidos no detectados.
- A0-A3 dirigidas a delimitacion estatica real. No acredita navegador Angular, API/JWT/RBAC, MySQL ni datos reales. Sin cambios de runtime, paquetes, migraciones, secretos ni infra. Rollback: revert selectivo normal de estos tres archivos; A5 pendiente independiente antes de promocion.

## 2026-11-12 — P5-011.C: delimitación de lista extensa en modal de inventario (Javier :50)
- TARGET_PATH único de producto sin modificar: `frontend/src/app/features/inventario/ajustes-list.component.ts`, blob `01dc08065d6bdcde789a2af467fa749dbb385de0`. Modal bounded-scroll con contador de detalles, sin lista extensa virtualizada; listado principal con `mat-paginator` fuera del modal.
- Prueba estructural Node `scripts/tests/p5-011-c-delimitacion-listas-modal.test.mjs` y acta `docs/P5-011-C-DELIMITACION-LISTAS-MODAL-DEV.md`, positivo y seis negativos de regresión; no se afirma navegador, E2E, API/DB ni tenancy live.
- Sin cambios Angular runtime, dependencias, migraciones ni secretos. A4 debe verificar commit DEV exacto y readback; A5 independiente antes de QA/main/PROD. Rollback selectivo normal, sin force-push.

## 2026-11-12 — P5-006.D: reuso de confirmación institucional (Javier :50)
- REUSE_NO_RUNTIME_CHANGE: `frontend/src/app/shared/confirm-dialog.component.ts` blob `f9b73544cc70beba83a891b2fc89537f0718af8b`, Cancelar=false, Eliminar=true/warn, textos interpolados y Angular Material existente.
- Se reutiliza sin duplicación el test exacto `scripts/tests/p5-006-c-delimitacion-confirmacion.test.mjs` blob `baccb39ccc8845658d6d19773b4cdc4d70fa09fc`; 6/6 Node --test PASS en archivos SHA exactos. Acta `docs/P5-006-D-REUSO-CONFIRMACION-DEV.md`. No se certifica navegador, autorización JWT/RBAC ni QA/PROD.
- Revert selectivo aditivo, A4 exact-commit y A5 independiente pendiente.

## Orden posterior expresa de arranque V5 — 2026-11-12 (prevalece sobre la pausa del 20:56)

- El propietario AUTORIZA la reanudación de las seis Tasks JAVIER_CHATGPT únicamente después de certificar todos los checks requeridos en SUCCESS en el SHA exacto del nuevo commit base de cierre. Hasta el readback de reanudación, los seis `is_enabled=false` continúan describiendo el estado físico real; la pausa anterior es HISTÓRICA y deja de ordenar la ejecución una vez cumplida esta condición.
- Nueva fuente de trabajo: Google Doc canónico `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, V5 FINAL; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. V5 contiene 2.340 microtareas `C00-01.M01.01` a `C15-05.M06.06`, 16 bloques, 65 fases y 390 microfases. El antiguo Plan Comercial V3/V13 y sus IDs históricos no son fuente de nuevos claims; V13 A0–A5 conserva sólo el protocolo de calidad y ejecución.
- `PLAN_MAESTRO!H` es estado único editable con evidencia y responsable; `COLA!A:O` son fórmulas y `COLA!P:Q` calculan dependencias explícitas por fase sin tope de seis. Candidata real: PENDIENTE, sin responsable, `COLA!G=SI` y `COLA!O=SI`, permisos reales, evaluación de factibilidad y exclusión por TASK_ID/ID. No reclamar automáticamente todo lo que esté en SI ni crear duplicados; múltiples IDs independientes pueden avanzar en paralelo.
- Mismo contrato V13: tareas individuales de tamaño ejecutable; PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO únicamente con A0–A4 PASS, commit fast-forward en dev, CI causal SHA exacto y readback PLAN/COLA/BITACORA; A5 por otro TASK_ID para promoción QA/main/PROD, nunca autopromoción. Meta 1 LISTO, hito 3 no techo; continuar mientras haya tiempo/proveedor, dependencias y seguridad. No crear ramas/PR, no modificar QA/main/PROD ni datos existentes de usuarios/empresas.
- RESTAURAR vigilancia y recuperación cruzada exclusivamente entre seis Tasks Javier, IDs y horarios originales :00/:10/:20/:30/:40/:50. Una Task habilitada comprueba los seis IDs y puede intentar `is_enabled=true` UNA vez por compañera pausada si tiene permisos, no existe nueva orden de pausa ni acción humana obligatoria sin resolver; releer estado y registrar evidencia o error. Nunca suplantar, aprobar permisos ni reactivar Tasks de Alex desde Javier. Conservar sus dos IDs :05/:35 para gestión autónoma de su propietario y recuperación mutua sólo en esa cuenta.
- La eliminación física de filas/columnas auxiliares está verificada: DASHBOARD 40×10; CONFIG 150×3; LEYENDA 80×3; TAREAS_PROGRAMADAS 20×26; AUTOMATIZACIONES 20×18; PLAN_MAESTRO 2341×15; COLA 2341×17. BITACORA no tiene límite de crecimiento por esta decisión. Ningún bloqueo estructural impide crecimiento legítimo de BITACORA.
- C10-03 no requiere migrar imágenes inexistentes: inventario real cero → no hay exportaciones ni checksums históricos. Mantener capacidad de archivos futuros segura. La tarea `C15-05.M06.06` es terminal y sólo se habilita con evidencia de cierre de las 2.339 anteriores.
- Jira `KAN-4` es tablero informativo, NO fuente de backlog duplicado ni gate para LISTO. Los respaldos de la estructura y de los seis prompts antes de reactivación quedan preservados; no cambiar títulos, RRULE, zonas, roles, IDs, parámetros de razonamiento ni vigilancia existente fuera del ajuste temporal de pausa.
- Validación de arranque: CI exact-HEAD requerida = SUCCESS, Scope Lock SUCCESS, fuente Google Sheet íntegra, conectores autorizados, y readback de cada Task tras reanudar. Un check SKIPPED puramente auxiliar se identifica y no se falsea como PASS. Un fallo crítico o acciones humanas pendientes bloquean sólo la operación correspondiente.

## 2026-11-13 — V5 C00-02.M01.02 / M01.04: corrección documental del esquema BITACORA (Alex :05)
- Rectificación causal de A5_FAIL en dos documentos de autoría propia: el encabezado vivo de BITACORA contiene 26 columnas A:Z, no el esquema antiguo de 21 A:U. No se modifican PLAN, COLA, fórmulas ni eventos históricos.
- Corrección limitada a los dos docs de C00-02.M01.02 y C00-02.M01.04 más esta entrada aditiva; sin runtime/API/migraciones/secretos. A0–A3: revisión documental causal, 26 encabezados y negativos de duplicación/identidad. A4 pendiente de CI exact SHA tras publicar; A5 independiente pendiente. Rollback: git revert normal sin force. QA/main/PROD no autorizados.

## 2026-11-13 — Rectificación documental A5_FAIL C00-03.M01.01/.02/.03 [Javier :10]
- Corrige esquema físico de BITACORA de A:U/21 a A:Z/26, EVENT_ID A e IDEMPOTENCY_KEY R, sin reinterpretar ni reescribir eventos históricos.
- Añade verificación positiva 26/26 y negativas de truncamiento, duplicación y autoaprobación; preserva A5_FAIL anterior, exige Scope Lock exact-SHA y A5 independiente sobre este nuevo commit. Sin cambios ejecutables, QA/main/PROD, nuevos IDs, ramas o PR.

## SOLQARYN V5 — C03-01.M01.04, registro documental de riesgos de autenticación (2026-11-13)

- PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`) prepara `docs/v5/c03-01-m01-04-riesgos-supuestos-decisiones-autenticacion.md` para registrar R01–R14, S01–S08 y D01–D08 respecto de login, renovación, revocación, recuperación y factores futuros.
- Alcance TIPO1 exclusivamente documental. Sin cambios de código ejecutable, DB, JWT, permisos, proveedores o entornos. A0–A3 verificaciones documentales; A4 requiere Scope Lock exact SHA y readback de PLAN/COLA/BITACORA, A5 revisión independiente pendiente. Reversión con git revert no destructivo.

## 2026-11-13 — SOLQARYN V5 C03-03.M01.01 (PRIMARY DEV 04 Javier :30)
- Preparación documental de objetivo, no-alcance y catorce contratos falsables de no-regresión para auditoría de autorización/denegación, cambios críticos, correlación y protección de secretos.
- Inventario estático de cinco fuentes backend con sus blobs; distingue expresamente evidencia de código de pruebas HTTP/MySQL/retención futuras. No implementa auditoría, permisos ni política de retención y no libera M01.06/M02.
- A0–A3 documentales dirigidas en VALIDANDO; A4 exclusivamente después de commit FF dev, Scope Lock exact SHA y readback PLAN/COLA/BITACORA A:Z; A5_PENDIENTE de otra Task Javier; no qa/main/PROD, secretos ni pagos.
- Rollback mediante revert normal del commit publicado sin reescribir trabajo concurrente.

## 2026-11-13 — SOLQARYN V5 C03-04.M01.01 (PRIMARY DEV 04 Javier :30)
- Objetivo de continuidad, respuesta a incidentes, revocación y backup/restore en preparación M01.01; criterios CO01–CO16 positivos/negativos y decisiones pendientes, sin ejecución de proveedor.
- Fuentes estáticas verificadas: workflows backup-restore de DEV con triggers restringidos, runbook y test de servicio con mocks. Distinguir existencia de YAML de drill efectivo; no fijar RPO/RTO ni confundir environments DEV/Desarrollo.
- Tipo 1 documental: sin .NET/Angular/MySQL/Docker/E2E ejecutables; controles de seguridad y Scope Lock sí aplican. A0–A3 documentales en VALIDANDO, A4 solo después de SHA publicado, A5 por otra Task Javier.
- No toca secretos, QA/main/PROD, infraestructura, Tasks ni servicios pagos. Rollback: git revert no destructivo.

## 2026-11-13 — SOLQARYN V5 C03-04.M01.04 (PRIMARY DEV 04 Javier :30)
- Registro documental de 16 riesgos, 8 supuestos y 9 decisiones pendientes del contrato de incidentes, revocación, backup/restore y continuidad para C10/C12.
- Coteja cuatro fuentes runtime/workflow con blobs, distingue la existencia de jobs de restauración demostrada, y registra discrepancia DEV/Desarrollo como gate que requiere lectura real.
- Documentación solamente (A0–A3 proporcional); .NET/Node/Angular/MySQL/Docker/E2E no aplican, Scope Lock exact SHA sí. A4 solo poscommit, A5 independiente Javier.
- Sin secretos, restore, migraciones, cambios de proveedores, QA/main/PROD, nuevas ramas/PR ni Tasks. Rollback git revert normal, con revisión de trabajo concurrente.

## 2026-11-13 — SOLQARYN V5 C03-04.M01.03 (PRIMARY DEV 04 Javier :30)
- Inventario documental de contratos backup/restore/revocación con 10 fuentes reales y hashes de blobs, 16 oráculos positivos/negativos IV01–IV16, brechas BR01–BR09; sin afirmar ejecución de los workflows.
- Separación entre YAML/triggers/fixtures y restauración MySQL 8.4, y discrepancia documentada DEV/Desarrollo que requiere autenticación de settings; JWT/RBAC/tenant A/B y PII fail-closed.
- TIPO1 doc: A0–A3 revisión documental; sin cambios de runtime, scripts ni workflows, .NET/Angular/MySQL/Docker/E2E NO_APLICA, Scope Lock exact SHA requerido para A4 y A5 pendiente de otra Task Javier.
- No QA/main/PROD, pagos, secretos o backups. Rollback git revert normal con HEAD revalidado.

## 2026-11-13 — SOLQARYN V5 C03-04.M01.05 (PRIMARY DEV 06 JAVIER :50)
- Contrato documental de 18 criterios falsables (8 positivos, 10 negativos) para incidentes, revocacion JWT, backup cifrado, restore MySQL 8.4 descartable, aislamiento A/B, rollback y continuidad; NO se ejecutaron los escenarios futuros.
- Fuentes estaticas con blobs: dos workflows de backup/restore, runbook DEV/Desarrollo, prueba Moq de renovacion, matriz C03-04.M01.04; 15 prerequisitos documentales de C03-01/02/03 registrados LISTO, DAG VALIDADO.
- TIPO1: no cambios runtime, contratos API/DTO, migraciones, secretos, permisos o infrastructure; dotnet/Angular/MySQL/Docker/Playwright NO_APLICA para el changeset; A0-A3 documentales y control de JWT/RBAC/tenant en requisitos; Scope Lock exact SHA requerido en A4.
- A5_PENDIENTE para otra Task JAVIER, M01.06/M02/C10/C12 siguen sujetos a gates propios, sin QA/main/PROD; rollback git revert normal, sin force-push.
