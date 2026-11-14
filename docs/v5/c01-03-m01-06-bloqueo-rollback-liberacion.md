# SOLQARYN V5 FINAL — C01-03.M01.06: contrato de bloqueo, rollback y liberación documental

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** cierre **documental** M01 para configuración dinámica, nunca instalación, activación o certificación del servicio en ejecución.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), `PLAN_MAESTRO`, `COLA`, `DAG_TECNICO` y `BITACORA` en Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Una celda LISTO_DEV no demuestra funcionalidad comercial ni autoriza nuevos IDs.

## 1. Resultado y límites del cierre M01

El bloque C01-03 aspira a separar valores de configuración editables del código ejecutable, con claves y tipos permitidos, ámbitos explícitos, precedencia, pertenencia actual y RBAC efectivo en servidor, auditoría, versionado y fallback seguro. Este contrato cierra **la preparación del lote**, no la ejecución de AC ni una matriz persistida. La condición textual «Habilita M02» significa **únicamente** que una futura candidata M02 puede ser evaluada individualmente contra su DAG, owner CAS y pruebas; **nunca** crea una habilitación global, una migración o un permiso.

Queda expresamente prohibido por esta entrega: código o ejecutores de eventos, webhook, dispatcher, automatismos Task-a-Task, workers, nuevos scopes/Tasks, endpoints, DI, cambios JWT/RBAC, concesiones comerciales, appsettings/secrets, MySQL/EF/migraciones, Cloudflare/servicios de pago, alteración de producción y promoción a QA/main/PROD. Los datos propios de A/B son confidenciales y no podrán aparecer en errores, logs o respuestas cruzadas.

## 2. Predecesores y evidencias mínimas revisadas

| ID y fuente | Artefacto comprobado en dev (blob) | Resultado necesario |
| --- | --- | --- |
| C01-03.M01.01 | `docs/v5/c01-03-m01-01-objetivo-alcance-no-regresion.md` · `628844ce4c2a8dbc9e0c3820431759dc9b986500` | Objetivo acotado, no-regresión, sin implementación |
| C01-03.M01.02 | `docs/v5/c01-03-m01-02-actores-entradas-salidas.md` · `b1f9e15ff44f3b2dfc98dade9ae23e72a9a93dd0` | Identidades, fronteras y actores sin privilegio por cliente |
| C01-03.M01.03 | `docs/v5/c01-03-m01-03-inventario-contratos-codigo.md` · `6ef8fbeb95ebef47cd033c3a8114352e17c590c2` | Contratos existentes e incógnitas claramente separadas |
| C01-03.M01.04 | `docs/v5/c01-03-m01-04-riesgos-supuestos-decisiones.md` · `c0e10f674d38d18b8629310b13e8a08ea43179fe` | Diez riesgos, cinco supuestos, seis decisiones aún condicionadas |
| C01-03.M01.05 | `docs/v5/c01-03-m01-05-aceptacion-configuracion.md` · `432b322ae0c488c03d73fb090606b71af733b896` | AC-01–AC-10 positivas/negativas verificables cuando exista código |
| C01-01.M01.06 | `PLAN_MAESTRO!H151/K151` | LISTO y revisión A5 independiente de Javier (J20) |
| C00-03.M01.06 | `PLAN_MAESTRO!H79/K79` | LISTO y revisión A5 independiente de Javier (J10) |

**Preflight individual:** `DAG_TECNICO` ID exacto `C01-03.M01.06`, F=VALIDADO, G=SI; `PLAN_MAESTRO!H223=PENDIENTE/I223` inicialmente vacío y `COLA!G223/O223=SI` antes de la reclamación condicional. Las cinco fuentes M01.01–05 se comprobaron LISTO. En M01.04 y M01.05 el A5 histórico atribuido a una cuenta **no incluida** en el allowlist V5 de seis Javier no da autoridad vigente: se efectuaron revisiones A5 nuevas, independientes por Javier J10, con eventos propios y readback sin borrar la historia. El resto conserva evidencia A5 de Javier autorizado. Antes de M02 volver a leer todos los predecesores y su gate individual.

## 3. Matriz de bloqueo y salida

| Puerta | Condición para habilitar **evaluación** de M02 | Resultado ante ausencia, contradicción o carrera |
| --- | --- | --- |
| Identidad y alcance | Skill canónica coincidente; `AGENTS.md`, Scope Lock STRICT y `dev` con SHA leído | NO_GO para publicar, reclamar o certificar |
| Dependencias | Cinco M01 propias y dos raíces del DAG en LISTO con A5 por Javier distinto del autor | NO hasta corrección A5 y readback; no confiar en marcado histórico |
| Datos y separación | Claves/tipos/allowlist/owner/scope aún por definir; sin cliente como autoridad tenant | NO a API/DB/grants, no copiar tenant B ni aprobar excepciones implícitas |
| Seguridad de entrada | Rechazar HTML/JS/CSS/SQL arbitrarios, URL javascript:, secretos y valores fuera de límites | Denegación segura, sin ejecución parcial ni fuga PII |
| Autorización | Membresía servidor vigente, tenant, sucursal, recurso, permiso relacional y plan/rubro como restricciones conjuntas | El plan o un flag nunca concede RBAC; B→A/A→B y revocación DENY |
| Estado/concurrencia | CAS exclusivo PLAN owner, BITACORA con EVENT_ID/IDEMPOTENCY_KEY únicos y 26 columnas | Duplicado o HEAD obsoleto bloquea escritura, nunca sobrescribir historia |
| Pruebas de implementación | CI .NET/API/Angular/MySQL/Playwright sólo cuando haya diff causal; SHA exacto, negativas A/B | SKIPPED/CANCELLED/PENDING/FAILED no son PASS exigido |
| Revisión y destinos | A0–A4 reales y A5 distinta Javier; autorización expresa adicional antes de QA/main/PROD | LISTO_DEV no equivale a promoción o lanzamiento |

## 4. Rollback, no-regresión y respuesta a fallos

**Sólo reversión documental:** si este archivo o su trazabilidad inducen una habilitación incorrecta, detener exclusivamente la candidata afectada, documentar causa y revertir **mediante commit Git revert normal**, tras comparar HEAD y modificaciones concurrentes; nunca force-push, reset compartido, borrado de BITACORA, reescritura histórica ni sustitución del owner ajeno. Conservar la cadena de evidencia de A5 históricos como historia, sin tratarlos como revisiones autorizadas actuales.

Una futura implementación deberá demostrar por separado versionado/concurrencia optimista, recuperación de fallos transaccionales EF/MySQL, scoped configuration tenant A/B, ausencia de secretos/PII en logs, prueba de fallo parcial y rollback real antes de publicar un servicio. Ninguno de esos ensayos runtime se atribuye a M01.06.

## 5. Oráculos documentales positivos/negativos para A0–A3

- **P01:** siete predecesores explícitos con cinco blobs reales y las dos raíces `LISTO/A5_JAVIER`; `DAG` id exacto y plan/cola coherentes.
- **P02:** requisitos de tipo, allowlist, ámbito, ownership, seguridad, versionado y fallback reflejados sin inventar DTO/tabla/endpoint existentes.
- **P03:** matriz de denegación conecta controles de R01–R10, S01–S05, D01–D06 y AC-01–AC-10 al diseño futuro sin convertir riesgos/criterios en pruebas funcionales completadas.
- **N01:** si llega `EmpresaId` cliente de B, nunca inferir que pertenece a A; acceso cruzado siempre bloqueado hasta autorización servidor demostrada.
- **N02:** un plan, rol legacy o feature flag nunca constituye autorización RBAC ni habilita un recurso, sucursal o secreto por sí solo.
- **N03:** código HTML/JS/SQL/CSS dinámico o claves desconocidas deben rechazarse en contrato tipado; no aprobar por mera documentación.
- **N04:** ausencia de A5 por uno de seis Javier, DAG ausente, CI cancelada o fuente SHA distinta no libera M02 o una fase terminal.
- **N05:** carrera de owners, HEAD distinto o evento duplicado se registra/reevalúa sin dos escritores o manipular auditoría.
- **N06:** `LISTO_DEV` M01.06 jamás equivale a JWT, API, MySQL/EF, browser o QA/main/PROD certificados.

**A0** estructura, diff Markdown-only, fuentes exactas, secretos y CHANGELOG no intercalado. **A1** oráculos P01–P03 y N01–N06 verificables en texto (no tests live). **A2** aislamiento tenant/JWT/RBAC/PII fail-closed documental. **A3** siete dependencias y consumidores M02–M05, con pruebas futuras explícitas. **A4** sólo después de publicación fast-forward, Scope Lock/V5 de SHA exacto y readback institucional. **A5** queda PENDIENTE de Task Javier distinta del autor J10. `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` para esta diferencia exclusivamente documental, no se atribuirá PASS de checks irrelevantes.

**Cierre limitado:** M01 preparado con contratos de bloqueo verificables; cualquier desarrollo M02/M03 y cualquier promoción exigen gate individual actualizado y revisión independiente. No se implementaron ni activaron eventos.
