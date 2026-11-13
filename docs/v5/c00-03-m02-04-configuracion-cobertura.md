# SOLQARYN V5 FINAL — C00-03.M02.04: configuración tipada, ámbitos y feature flags de cobertura

**Microtarea:** C00-03.M02.04, definición documental de configuración tipada para la matriz integrada requisito → mejora → contrato → prueba → cierre. **Autor:** PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. Repositorio exclusivo `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuente única: V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

**Resultado exigido:** cada requisito previo se representa en una mejora, contrato, prueba y criterio de cierre dentro de los 2.340 IDs V5, con evidencia de matriz integrada y **sin cola histórica separada**. Este contrato define tipos y límites de futura configuración de *cobertura*, no crea flags, settings, clases .NET, rutas Angular, almacenes ni nuevas autorizaciones.

## 1. Fuentes, dependencias y límites

| Documento dev leído | Blob SHA | Alcance |
| --- | --- | --- |
| `docs/v5/c00-03-m01-06-cierre-matriz-integrada.md` | `8f83554e0526089c82cf730abb144e26d0b7da66` | matriz R01–R12 y salida M01 |
| `docs/v5/c00-03-m02-01-entidades-ownership-invariantes-cobertura.md` | `b6b16dfa1775ef07666d108580f6b21efe9b081a` | relación de requisito con ID/owner |
| `docs/v5/c00-03-m02-02-contratos-api-dto-errores-cobertura.md` | `803685cc695660ce2fadee3175b5d97e40e3f3ab` | contratos/errores documentados, no API implementada |
| `docs/v5/c00-03-m02-03-persistencia-cobertura.md` | `745e9a3eae0514ee83fe2de12a535fb084548c74` | modelo propuesto de persistencia, no migración |
| `docs/v5/c00-03-m01-05-acceptance.md` | `40f78cf1b69563f21a3fb8210de3001bac3afbec` | aceptación documental, reglas negativas |

En el preflight, `C00-03.M01.06`, `C00-03.M02.01–03` estaban `LISTO`, `PLAN_MAESTRO!H83=PENDIENTE/I83` vacío, `COLA!G83/O83=SI`, con excepción individual DAG inexistente (puerta conservadora). Esto no equivale a liberar M03 ni promover QA/main/PROD.

## 2. Contrato tipado de configuración **propuesto**, sin activar

| Parámetro lógico | Tipo/restricción futura | Autoridad y riesgo |
| --- | --- | --- |
| `Coverage.SourceVersion` | string/version no vacío, referencia V5 validada | versión de fuente Doc; no plan V3/V13 ejecutable |
| `Coverage.RequiredEvidenceKinds` | conjunto allowlisted `DOC,TEST_REAL,CI_SHA,READBACK,A5` | regla de clasificación, nunca prueba automática sin ejecución |
| `Coverage.CanonicalIdRange` | conjunto fijo de 2.340 IDs; no entrada arbitraria | `PLAN_MAESTRO!A` único, nunca una fila adicional |
| `Coverage.RequiredStates` | enum PENDIENTE/EN_PROGRESO/VALIDANDO/LISTO | estado vivo exclusivamente `PLAN_MAESTRO!H`, no caché mutable |
| `Coverage.LinkIdentity` | clave compuesta (source_id,source_version,microtask_id,kind) | referencia versionada, no duplicar scope |
| `Coverage.RequireShaExact` | boolean lógico siempre verdadero en evidencia de código | check-runs deben tener SHA correcto y completed/success |
| `Coverage.MinimumNegativeCases` | entero positivo definido por riesgo y no por cuota rígida | positivos y negativos causales, sin mocks insuficientes |
| `Coverage.IndependentReview` | TASK_ID distinto dentro de las seis Javier | nunca A5_PASS del autor ni de una cuenta externa |
| `Coverage.AllowProvisionalDoc` | futuro boolean, sólo análisis M01/M02 explícitamente permitido | nunca libera IAM, JWT, RBAC, tenant, DB, M01.06/M06.06 ni despliegue |
| `Coverage.ReportMode` | enum `MISSING,PROPOSED,VALIDATED,EVIDENCED` | etiqueta informativa, no estado PLAN ni señal de permiso |
| `Coverage.SecretRedaction` | siempre obligatoria para PII/JWT/DSN/API keys | registros y eventos con referencias/huellas no sensibles |

**Feature flags conceptuales, NO activos:** `coverage_readonly_preview` para ver matriz de manera informativa; `coverage_gap_report` para enumerar faltantes; `coverage_strict_sha` para informes fail-closed; `coverage_shadow_compare` para lectura futura de réplica sin escribir. Ninguno se crea o habilita aquí. Un flag de UI jamás concede acceso, modifica PLAN H/I, crea una Task, aprueba A5 o habilita producción.

## 3. Ámbitos, consumidores y trust boundaries

- **Global de gobierno:** Doc V5 y Sheet único fijan IDs, estados y priorización; sólo Scope Lock, Skill y fuentes autorizadas pueden restringir operaciones. No introducir configuración paralela de estados ni backlog.
- **Por microtarea:** un ID V5 y sus antecedentes, contratos, pruebas y SHA; no reutilizar evidencia entre IDs sin correspondencia causal comprobada.
- **Por ejecución:** seis Tasks Javier habilitadas son el único allowlist. No confiar en cabeceras de cliente, nombre de Task o un `is_self_owned_automation=false` aislado como prueba positiva/negativa de autoridad.
- **Por tenant y usuario:** JWT/BCrypt, RBAC server-side, membresía real y aislamiento empresa A/B siguen intactos. La cobertura documental nunca concede permisos sobre tenant B ni facilita datos sensibles.
- **Por ambiente:** `dev` para commits FF y CI; `qa/main/PROD`, infraestructura, secretos, backups y deploy requieren permisos nuevos. No insertar configuración en Vercel/Render/Aiven.
- **Por archivo/CI:** cambios de tipos/diseño Markdown no equivalen a compilar ni ejecutar `.NET`, `MySQL` o navegador. Cuando un futuro cambio afecte esos runtimes se activa su gate real por SHA exacto.

**Compatibilidad:** no cambiar `PLAN_MAESTRO!H/I`, ni fórmulas de `COLA!E/G/O`, ni `BITACORA!A:Z` (26 campos con EVENT_ID/IDEMPOTENCY_KEY). `DAG_TECNICO` fija excepciones VALIDADO para IDs específicos; objetivo `MAX(24,3*N)` orienta descubrimiento pero no crea elegibilidad. La telemetría `TAREAS_PROGRAMADAS!AA:AN` es último reporte, jamás autoridad para claims.

## 4. Escenarios de aceptación positivos y negativos (documentales, NO runtime)

| Caso | Resultado del diseño |
| --- | --- |
| P01 | Referencia R01 con versión válida enlaza un ID V5 existente, conserva fuente y estado PLAN H/I |
| P02 | Una tarea sin evidencia real se registra como faltante, no como implementada |
| P03 | Check exact-SHA completed/success se asocia a evidencia de esa misma revisión, no a otro SHA |
| P04 | Consulta preview de una relación requisito–contrato devuelve solo referencias no sensibles, sin emitir grant |
| N01 | Feature flag de frontend intenta conceder tenant B o rol plataforma → rechazar, exigir backend RBAC |
| N02 | Fuente histórica propone ID 2.341 o backlog propio → rechazar por V5 |
| N03 | `COLA!O=SI` pero G=NO o DAG pendiente → no claim ni liberación |
| N04 | Otro TASK_ID posee VALIDANDO sin CAS real → no escribir sobre H/I |
| N05 | Se intenta A5_PASS con la misma TASK_ID autora o cuenta ajena → rechazar |
| N06 | Job obligado SKIPPED/QUEUED/FAIL/otro SHA → conservar VALIDANDO, no aprobar A4 |
| N07 | Se registra evento con 21 columnas, ID duplicado o RUN_ID inventado → rechazar |
| N08 | Config contiene JWT secret, credenciales, DSN o PII → no publicar ni registrar |
| N09 | Se propone activar un flag en QA/PROD desde dev → NO_GO |
| N10 | Se presenta el catálogo propuesto como configuración ejecutable o E2E realmente probado → rechazar |

Estos P01–P04 y N01–N10 son oráculos documentales comprobables por lectura y análisis; **no** sustituyen pruebas de integración de características futuras.

## 5. Impacto A0–A5 y rollback

**Clasificación:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`. Impacto permitido: únicamente este Markdown más sección aditiva en `CHANGELOG_AI.md`, sin tocar código, API, DTO real, SQL, configuraciones activas, secretos, jobs ni datos. Dependencias: cinco blobs citados y PLAN/COLA/DAG auditados. Consumidores potenciales M02.05/06 y trabajo futuro, sin autorizarlo automáticamente.

A0: revisar blobs/diff/atribución, Markdown y patrones de secretos; A1: coherencia P01–P04/N01–N10 como condiciones falsables; A2: JWT/RBAC/tenant/secretos/autoría e independencia sin crear grants; A3: contratos con M01.06 y M02.01–03, fórmulas y 26 columnas. Subpruebas `.NET/Angular/MySQL/Docker/E2E` = `NO_APLICA` al diff documental, **no PASS técnico**. A4: publicación FF en dev, SHA exacto y checks obligatorios (Scope Lock + V5 attribution si se dispara), diff/readback; A5_PENDIENTE de otra Task Javier. QA/main/PROD no autorizados. Rollback documental: `git revert` aditivo sin force-push ni eliminación de eventos históricos.
