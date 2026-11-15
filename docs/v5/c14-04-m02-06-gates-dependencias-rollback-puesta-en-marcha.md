# SOLQARYN V5 FINAL — C14-04.M02.06: dependencias, salida y rollback de go-live

**Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT (`6aeacb7820ac8191b7d768a6f33dfa09`). **Alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** contrato DOCUMENTAL de cierre M02.06. No inicia puesta en producción, migración, DNS, servicios, endpoints, procesos de fondo ni verificaciones live.

## 1. Fuente y preflight de autorización

Autoridad de alcance: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit); Skill local y canónica, AGENTS.md, Scope Lock y arquitectura vigente. La preparación M01 está documentada en [M01.06](c14-04-m01-06-bloqueos-rollback-liberacion-puesta-en-marcha.md); verificar en Git su ruta y contenido antes de cualquier implementación.

Para este único ID, `PLAN_MAESTRO!H2137=PENDIENTE` e I vacía en el preflight; `COLA!G2137/O2137=SI` y `DAG_TECNICO!F2098=VALIDADO/G2098=SI` tras cinco A5 independientes. El DAG exige `C14-04.M01.01–06+A5` y `C14-04.M02.01–05+A5`. El claim debe registrarse con un owner y readback; leer/escribir Sheets no es, por sí solo, una transacción CAS distribuida. No inferir autorización futura desde esta fotografía.

| Predecesor | Evidencia documental original | Alcance verificado por revisión independiente |
| --- | --- | --- |
| M02.01 — dominio y ownership | SHA `89c0ca6e8f8c7541e7e28783391a866b7ff27825` | PM01–PM16, INV01–INV16, T01–T10, sin persistencia nueva |
| M02.02 — API/DTO y errores | SHA `720729e831f5317ab584fa29dc8124be2b200c6a` | OP01–OP10, errores y criterios futuros; ningún endpoint publicado |
| M02.03 — persistencia | SHA `cd20a2a683676fd76d86f54f137be5a739c4ca8a` | DB01–DB14, índices hipotéticos, FX futuros; ninguna migración |
| M02.04 — configuración | SHA `11c01b7bc332921ec59d3c11c8a9acecbd39e7b7` | CFG01–CFG16 e invariantes; sin flags efectivos ni secretos |
| M02.05 — señales e idempotencia | SHA `e80d50fe1c28965a6c6b360d6efcd9586cba29ff` | Contrato conceptual; no dispatcher, webhook, worker ni scheduler |

La revisión A5 posterior de cada uno fue registrada por PRIMARY DEV 04, distinta de la Task autora DEV 03; esto **no** es A5 del presente M02.06. Los controles Scope Lock y procedencia CHANGELOG en los cinco SHA originales terminaron `completed/success`; las CI canceladas no se contaron como PASS runtime.

## 2. Matriz de dependencias y condiciones de salida

| Gate | Prueba de aceptación para transición | Si falla o falta |
| --- | --- | --- |
| G01 | El ID pertenece al plan vigente de 2.340 y usa su fuente original | No crear nueva microtarea ni fila |
| G02 | PLAN H/I confirma estado y autor único en la fila exacta | No transferir owner por antigüedad |
| G03 | COLA G/O y DAG F/G son SI/VALIDADO calculados | No editar fórmulas para liberar M03 |
| G04 | M01.01–06 son LISTO con A5 válida distinta del autor | Mantener bloqueo documental |
| G05 | M02.01–05 tienen A0–A4 y A5 independientes por SHA | Mantener M02.06 sin habilitación |
| G06 | Una Task Javier de seis autorizadas reclama en forma exclusiva | Si hay carrera, abortar acción |
| G07 | BITACORA recibe EVENT_ID e IDEMPOTENCY_KEY únicos, 26 columnas | Rectificación aditiva, nunca reescritura |
| G08 | Git HEAD/parent/diff preserva concurrentes con fast-forward | Releer y reconstruir, no forzar |
| G09 | Scope Lock y procedencia V5 success en SHA publicado | Mantener VALIDANDO, investigar CI |
| G10 | Nueva tarea M03 cambia código REAL y tiene pruebas .NET/seguridad dirigidas | Documento no satisface implementación |
| G11 | JWT, RBAC y aislamiento tenant A/B siguen bajo autoridad backend | Denegar cualquier superficie insegura |
| G12 | Credenciales y PII no entran en documentos, logs ni repositorio | Redactar hallazgo y bloquear exposición |
| G13 | Pruebas de DNS, TLS, backup, restore y cutover reales son evidencias separadas | NO_EJECUTADO, jamás PASS por escrito |
| G14 | A5 del presente M02.06 la hace otra Task Javier sobre SHA exacto | A5_PENDIENTE, no autoaprobación |
| G15 | Gate técnico del sucesor se recalcula con PLAN/COLA/DAG tras LISTO+A5 | No preliberar M03 ni terminales |
| G16 | QA/main/PROD exige autorización posterior y específica | No promover por LISTO_DEV ni A5 |

**Salida documental M02.06:** definiciones de dependencia, bloqueo, reversión y aceptación con trazabilidad. **No alcanza** el smoke final, disponibilidad, restauración, métricas de latencia, cero incidentes, certificado TLS, cero hallazgos o corte comercial; todo ello permanece no evaluado.

## 3. Ruta causal de continuación y no-alcance

1. Tras la publicación de M02.06, verificar diff, Scope Lock y procedencia V5 exact-SHA. Pasar `VALIDANDO→LISTO` sólo con A0–A4 documentales completos, BITACORA y readback.
2. Conservar `A5_PENDIENTE` hasta revisión de Task Javier distinta del autor. La A5 es independiente y se registra en PLAN K/BITACORA sin alterar owner.
3. Releer DAG/COLA para sucesoras M03 después de A5; ninguna regla se fuerza. M03 requiere cambios reales en las capas correspondientes, comprobaciones de aislamiento, pruebas positivas y negativas, CI causal y reversión reproducible.
4. En un eventual go-live autorizado, exigir inventario de proveedores reales y autorización separada para DNS/TLS, migración MySQL, Cloudflare/R2, despliegue, restauración, costos y secretos. La arquitectura actual Render/Vercel/Aiven/Cloudinary continúa sin cambio por este documento.
5. No crear eventos ni llamadas entre Tasks; el esquema de eventos M02.05 es diseño futuro, no autorización de un ciclo automático.

**Impacto:** dos archivos Markdown (este contrato y sección CHANGELOG). Sin cambio Domain/Application/Infrastructure/API/frontend/MySQL, ni proveedores, redes, registros de usuarios, sesiones, jobs, cuentas o programaciones. JWT/RBAC/tenant A/B permanecen bajo los controles reales existentes. Observabilidad prometida no sustituye telemetría medida.

## 4. Recuperación y rollback de casos identificados

| Caso | Acción segura |
| --- | --- |
| R01 — Scope Lock FAIL | Investigar job exacto, reparar causa, nuevo SHA, jamás bypass |
| R02 — Git CAS/HEAD competido | Releer HEAD, preservar secciones ajenas, fast-forward normal |
| R03 — Owner o lease disputado | No transferir sin ausencia de escritor y exclusividad real |
| R04 — CI requerida pendiente | Mantener ese ID VALIDANDO, no declarar PASS |
| R05 — Check CANCELLED/SKIPPED | No aprobar; validar causalidad antes de cualquier reintento |
| R06 — A5 del mismo autor | Rechazar, mantener A5_PENDIENTE |
| R07 — Fórmula DAG bloqueada | Inspeccionar predecesores/A5; no forzar G/O |
| R08 — Datos secretos o acceso cruzado | Fail-closed, registrar hallazgo minimizado y test negativo |
| R09 — Diferencia de BITACORA | Registrar evento correctivo append-only, sin borrar el original |
| R10 — Documento defectuoso | Git revert ordinario, nuevo control SHA y auditoría aditiva |

## 5. Oráculos documentados, sin pruebas runtime ejecutadas

| Caso | Positivo futuro | Negativo que debe impedir salida |
| --- | --- | --- |
| T01 | A5 de seis Javier, autor diferente y SHA evidenciado | Autoaprobación de la propia Task |
| T02 | M02.01–05 cumplen precedentes en DAG calculado | M02.06 se libera con cualquier A5 faltante |
| T03 | PLAN H/I conserva owner y transición única | Dos escritores sobre un mismo ID |
| T04 | Git y CI requerida usan exactamente SHA publicado | SUCCESS de SHA viejo se reutiliza |
| T05 | JWT/RBAC de backend determina permisos de empresa | Header de empresa B da acceso desde A |
| T06 | Backup restaurable se prueba en entorno efímero autorizado | Documento se presenta como restore aprobado |
| T07 | DNS/TLS de futura publicación tiene autorización expresa | Se modifica producción por A5 documental |
| T08 | Observabilidad mide fallo y degrada de manera segura | Alerta silenciada y se declara cero incidentes |
| T09 | BITACORA idempotente conserva 26 campos y orden | Evento repetido sobrescribe historia |
| T10 | Rollback deja intacto el trabajo de otra Task | Reset/force-push elimina commits ajenos |

**T01–T10 = CRITERIOS_FUTUROS_NO_EJECUTADOS.** No son un test de integración, pentest, restore real, aceptación de cliente, monitoreo de SLA o autorización de lanzamiento.

## 6. Auditoría A0–A5 de este changeset

- **A0:** procedencia M01.06 y M02.01–05, exactitud de tablas G/R/T, dos Markdown y ausencia de secretos/encabezados vacíos.
- **A1:** coherencia de casos positivos/negativos como contrato documental. Los T01–T10 se dejan explícitamente sin ejecutar.
- **A2:** inspección de restricciones JWT/RBAC/tenancy A/B y no difusión de PII; no cambio a código runtime, por tanto HTTP/EF/MySQL son `NO_APLICA`, no PASS.
- **A3:** cotejo con STANDARD, Skill, M02 predecesoras, PLAN/COLA/DAG/BITACORA; build y Playwright no causales para un diff sólo Markdown.
- **A4:** publicar en dev con fast-forward y CAS de HEAD, comprobar commit/diff/blobs, controles Scope Lock y V5 `completed/success` en ese SHA, eventos/readbacks institucionales.
- **A5:** sigue pendiente hasta una revisión independiente por Task Javier autorizada distinta de DEV04.

**Rollback:** revert por commit aditivo de GitHub y nueva CI/documentación de evidencia; nunca reset destructivo, force-push, edición de BITACORA anterior ni alteración de QA/main/PROD.
