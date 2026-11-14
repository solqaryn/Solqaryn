# SOLQARYN V5 — C10-03.M01.04: riesgos, supuestos y decisiones de inventario real y archivos futuros

**Microtarea:** C10-03.M01.04; PLAN_MAESTRO fila 1517; DAG_TECNICO fila 1478 F=VALIDADO/G=SI al preflight; autor PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repo:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`; fecha 2026-11-14.

## Hechos delimitados y no-alcance

La fuente exige verificar **inventario real de medios**; sólo si está demostrado que no existen archivos históricos se debe registrar cero y omitir exportación, copia, checksum y carga de migración, preparando almacenamiento seguro para nuevos medios. **En esta microtarea el conteo de objetos históricos sigue NO_VERIFICADO; no se declara cero ni positivo.** No se ha consultado Cloudinary, un bucket, MySQL productivo, DNS, archivos ni secretos. Los contratos de C10-03.M01.01/.02 son documentación preparatoria y no certifican integración ni inventario. Cloudinary es la integración actual declarada arquitectónicamente; R2/Hostinger siguen siendo objetivos futuros sujetos a permisos.

**Alcance real:** clasificar riesgos *potenciales*, supuestos que requieren evidencias y decisiones todavía no aprobadas. El resultado NO es una migración, ni un escaneo, ni una prueba del sistema en ejecución. Ninguna hipótesis se etiqueta bug confirmado.

## Matriz de riesgos R01–R14 — POTENCIALES, no hallazgos acreditados

| Riesgo | Escenario a impedir | Evidencia futura obligatoria |
| --- | --- | --- |
| R01 Conteo falso | Declarar cero por falta de registros visibles | Conciliar metadatos y proveedores por ambiente |
| R02 Cobertura incompleta | Contar tablas MySQL pero omitir objetos huérfanos en storage | Reconciliación de fuentes y fecha/corte |
| R03 Tenancy cruzado | Inventario de A expone existencia/keys de B | Tests y permisos A/B, salidas agregadas saneadas |
| R04 Autoridad del cliente | Header, EmpresaId o slug cambia owner del inventario | JWT/RBAC y contexto tenant server-side |
| R05 Exposición | Enumeración devuelve URLs firmadas, key física o tokens | Revisión DTO, auditoría/logs y redacción |
| R06 Migración prematura | Exportar/copiar objetos antes de manifest/permiso | Gate explícito + control de alcance y readback |
| R07 Integridad | Copia omite bytes o altera checksum/metadatos | Fixture, hashes comparados y manifest verificado |
| R08 Clasificación | Fixture nuevo se incluye como histórico | ID de origen, timestamp y clasificación de fuente |
| R09 Contenido | Se confía en MIME/extensión y no bytes | Tests negativos de contenido/tamaño/stream |
| R10 Retención | Lifecycle elimina objeto sujeto a hold | Matriz de retención y protección restaurable |
| R11 Revocación/cache | Enlace bearer caducado o cambiando tenant persiste | TTL, replay y aislamiento de caché probados |
| R12 Fallos/reintentos | Timeout produce duplicado u objeto parcial visible | Fault injection e idempotencia/compensación |
| R13 Secrets/PII | Bucket key, firma, token o dato personal queda en logs | Negative assertions sobre logs/respuestas |
| R14 Rollback imposible | Cutover sin doble lectura o restauración ensayada | Snapshot/backup y rollback en ambiente efímero |

La ausencia de una prueba causal **no implica** que exista o no una vulnerabilidad; es una brecha de certeza. Toda ejecución futura debe usar permisos reales y ambientes autorizados, no suponerlos.

## Supuestos S01–S08 — PENDIENTES, falsables

| ID | Supuesto sin acreditar | Cómo se verifica después |
| --- | --- | --- |
| S01 | Existe una fuente autorizada que enumera objetos por ambiente | Inventario de configuración y rol sin revelar secretos |
| S02 | Las referencias persistidas corresponden a objetos íntegros | Join de referencias y manifiesto de proveedor |
| S03 | Los objetos tienen tenant/owner resoluble server-side | Muestreo autorizado y test de pertenencia A/B |
| S04 | La cantidad histórica puede ser cero | Conteo autenticado completo, nunca deducción |
| S05 | Retención y backup cubren clases de archivos actuales | Matriz aprobada de clasificación/holds |
| S06 | MIME/tamaño/checksum futuros cubren todos los tipos | Fixtures sintéticos y tests negativos |
| S07 | Se puede operar proveedor futuro sin revelar tokens | Prototipo de credenciales efímeras con mínimos permisos |
| S08 | Existe vía reversible de migración/cutover si hay históricos | Ensayo de conciliación y rollback |

Si S04 se demuestra verdadero, se omiten por completo operaciones sobre históricos (exportación, copia, checksum y carga), no el diseño ni validación de archivos nuevos. Si S04 es falso, se aplica manifiesto previo; ningún camino autoriza corte de proveedor por sí solo.

## Decisiones D01–D08 — ABIERTAS, no aprobadas

| ID | Decisión requerida antes de fases posteriores |
| --- | --- |
| D01 | Dueño y fuente del inventario autorizado, rol, ambiente y ventana de corte |
| D02 | Política de conciliación DB/Cloudinary/otros proveedores y tratamiento de huérfanos |
| D03 | Formato y acceso del manifiesto, owner/tenant y protección de datos/PII |
| D04 | Tipos de medios, límites MIME/bytes/tamaño, quotas y prueba de integración |
| D05 | Retención, hold, eliminación y restauración, con responsabilidades |
| D06 | Semántica de URL temporal, replay, TTL, revocación y caché |
| D07 | Idempotencia, fallos, checksums y estrategia de rollback para migración si aplica |
| D08 | Permiso separado de R2/DNS/cutover, costos, secretos y despliegue |

Estas decisiones requieren evidencia y, cuando proceda, autorización del propietario; **no** se consideran resueltas por una tabla ni por un gate documental DAG.

## Oráculos futuros P01–P08/N01–N12 — NO_EJECUTADOS

**Positivos:** P01 inventario real conciliado demuestra cero y el flujo no exporta/copia/hasha históricos; P02 inventario positivo produce manifiesto sin copiar; P03 fixtures nuevos validan MIME/bytes; P04 A/B conservan separación; P05 firma acotada expira; P06 hold bloquea lifecycle; P07 fallo efímero no deja objeto parcial; P08 rollback de fixture preserva integridad.

**Negativos:** N01 declarar cero desde ausencia de lectura; N02 sólo contar MySQL sin proveedor; N03 usuario A ve objeto B; N04 tenant cliente falsifica owner; N05 usuario lector intenta export/delete; N06 fixture nuevo se cuenta histórico; N07 MIME declarado no coincide con bytes; N08 límite de stream excedido; N09 URL alterada/replay se considera one-time sin estado; N10 log exhibe firma/PII; N11 lifecycle borra bajo hold; N12 desplegar R2/DNS/cutover sin autorización/rollback.

Todos son **FUTUROS NO_EJECUTADOS**. Un PASS requiere test causal, fixture, SHA exacto, ambiente y evidencia; no se reportan números de archivos reales, fallos explotables ni resultados runtime.

## Impacto A0–A5 y rollback

- **A0:** Skill vigente/canónica, HEAD, Scope Lock STRICT, plan/cola/DAG individual y claim CAS sobre ID libre.
- **A1:** 14 riesgos, 8 supuestos, 8 decisiones y P08/N12 trazados a la autoridad V5; no atribuir evidencia futura a presente.
- **A2:** revisado documentalmente default-deny JWT/RBAC/tenant, PII, MIME, retención, URLs bearer, TTL/replay y reversión.
- **A3:** contraste con `docs/v5/c10-03-m01-01-alcance-inventario-real-y-almacenamiento-futuro.md` (blob `ca7ccfaf423ead017a127acde393d53cf6ab181b`), `docs/v5/c10-03-m01-02-actores-entradas-interfaces-inventario-almacenamiento.md` (blob `dde15b96f610dcd0b7712129e69bcba04a8586bd`) y contratos C10-02/M01.01–05. Tests .NET/Angular/MySQL/Playwright: **NO_APLICA** al diff de dos Markdown, no PASS inventado.
- **A4:** commit fast-forward/CAS en dev, check `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success **exact SHA**, PLAN/COLA/BITACORA readback.
- **A5:** revisión independiente posterior por otra Task Javier distinta de la autora; no autoprobación.
- **Rollback:** `git revert` del commit documental, sin reescribir historial. No modifica Domain/Application/Infrastructure/API, Angular, MySQL, Cloudinary, R2, DNS, datos, secretos, workers, Tasks, QA/main/PROD.
