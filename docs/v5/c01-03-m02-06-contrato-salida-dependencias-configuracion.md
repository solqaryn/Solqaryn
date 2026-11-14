# SOLQARYN V5 FINAL — C01-03.M02.06: dependencias técnicas y salida del lote de configuración dinámica

**Unidad planificada:** `C01-03.M02.06`, definir dependencias y contrato de salida M02. **Autor:** SOLQARYN PRIMARY DEV 01 — JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuente de autoridad:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Alcance real:** contrato de salida documental de M02, **no** implementación de flags, catálogo, worker, evento, endpoint, migración EF/MySQL ni despliegue. M01/M02 definen diseño y gates de futuras M03; no conceden nuevos permisos ni habilitan por sí solos QA/main/PROD. Escenarios P/N de este documento: **ORÁCULOS FUTUROS_NO_EJECUTADOS**.

## 1. Dependencias verificadas y procedencia

Se contrastaron los seis artefactos previos del mismo bloque, ya versionados en `dev`. El predicado de selección de esta unidad es: `PLAN H229=PENDIENTE, I229 vacío`; `COLA G/O=SI`; `DAG_TECNICO!F237=VALIDADO, G237=SI`; predecesores `M01.06` y `M02.01–05` LISTO con constancia de A5 independiente. El gate de Sheet es **individual y dinámico**: cualquier divergencia de estado/owner/HEAD invalida este preflight.

| ID | Archivo de contrato preexistente | Blob Git del archivo | Entrega que delimita |
|---|---|---|---|
| `C01-03.M01.06` | `docs/v5/c01-03-m01-06-bloqueo-rollback-liberacion.md` | `03d4fe9d53a10c9eac00dee6fe8e79ddd4e249fd` | cierre preparatorio y límites de M01 |
| `C01-03.M02.01` | `docs/v5/c01-03-m02-01-entidades-ownership-invariantes.md` | `8820c3b28b56a9134c6edc6fe8139c27a21ca340` | entidades conceptuales, ownership, invariantes |
| `C01-03.M02.02` | `docs/v5/c01-03-m02-02-contratos-api-dto-errores-compatibilidad.md` | `04de8c729ed45769d39f8edaff9af5940e02588b` | DTO/API, errores y compatibilidad (no rutas nuevas) |
| `C01-03.M02.03` | `docs/v5/c01-03-m02-03-persistencia-indices-migraciones-fixtures.md` | `81ac4ffae031be5f8ed30bc95e587994c7478010` | persistencia/índices/migración reversible futuros |
| `C01-03.M02.04` | `docs/v5/c01-03-m02-04-configuracion-tipada-ambitos-flags.md` | `0532022ee7f43a69e10bf5b051f098281cf8242b` | tipado, precedencia, ámbitos y flags seguros |
| `C01-03.M02.05` | `docs/v5/c01-03-m02-05-eventos-idempotencia-correlacion-limites.md` | `202d8cb2f2e79adbc2a9ac9a36303665b2dfbb90` | correlación, idempotencia y eventos teóricos no despachados |

**Baseline del código (no ejecución nueva):** `backend/src/Domain/Entities/ConfigEmpresa.cs` (blob `664547d28b513d3187622b02c57a7db1f812f7ec`) ya tiene `EmpresaId` y `Version`; `ConfigEmpresaConfiguration.cs` (blob `c26a46d4381618c960c93bb71806f802969ddcb8`) declara unicidad tenant y concurrencia; `EmpresaConfiguracion.cs` (blob `d3d6e59a15c59bc04fa769f8bcefcfe5f50794e3`) conserva un modelo legacy de branding global; `FeatureFlagsSaaS.cs` (blob `ec0062f4d76fd106365c20f3ba50c214369a3de5`) restringe por plan pero **no** entrega grants RBAC. Declaraciones EF no certifican índices físicos Aiven ni migraciones aplicadas.

## 2. Contrato de salida M02 — listo para implementar, NO implementado

| Frente | Salida contractual | Gate de M03 antes de escribir código |
|---|---|---|
| Autoridad de actor/tenant | Resolver identidad y membresía desde servidor, no aceptar `EmpresaId` de URL/DTO/header/claim arbitrario; permiso RBAC relacional y ownership del recurso | Prueba A/B + membresía revocada + rol inactivo + BOLA/IDOR, cero lectura y mutación ajenas |
| Catálogo de claves | Catálogo allowlisted con `KeyCanonical`, tipo cerrado, `AllowedScopes`, sensibilidad, defaults DENY, versión de esquema | Selección de tipos y compatibilidad JSON/DTO acordadas; ninguna clave admin autoriza `disable_rbac`, `worker.runForever` o secretos |
| Precedencia | `PLATFORM` sólo con autoridad específica, `TENANT`, `BRANCH`, `RESOURCE` únicamente si son scopes autorizados y con owner validado | Política exacta, colisiones/ausencia de owner y semántica de herencia/resolución probadas en backend, sin herencia cross-tenant |
| Persistencia | `ConfigEmpresa` tenant-first y `Version` son reales; modelo `ScopedConfigValue`, `ConfigChangeAudit`, `ConfigIdempotency` es solo candidato | Preflight EF 10/MySQL 8.4 efímero, FK/índices por owner, NULL/colisiones, reversibilidad, transacción y auditoría; NO DDL en Aiven |
| API/DTO | Mantener `EmpresaConfiguracionController`, `IEmpresaConfiguracionService` y clientes existentes sin reemplazo silencioso | Versionar DTO futuro, 401/403/404/409/503 seguros, `expectedVersion` server-side, pruebas HTTP dirigidas |
| Flags/comercial | `PoliticaModulosSaaS` aplica restricciones comerciales donde corresponda, **nunca** concede `RolPermiso` | Evaluar la intersección explícita membresía + RBAC + plan/rubro + recurso + sucursal; fallo de una fuente => DENY |
| Eventos y trabajo asíncrono | Sólo sobre conceptual de evento post-commit; idempotencia por actor + tenant + operación + key + versión, `correlationId` opaco | **NO IMPLEMENTAR AHORA** webhook, outbox, scheduler, worker, dispatcher ni eventos entre Tasks; toda implementación posterior exige orden autorizada y CI causal |
| Rollback/observabilidad | Cada cambio futuro debe declarar prueba de reversión, trazas no sensibles y límites de reintentos/cuotas | Identificación de causa real, lectura por versión, no reenviar secreto ni crear duplicados cross-tenant |

El contrato **no** fusiona `EmpresaConfiguracion` legacy con `ConfigEmpresa` sin migración independiente. La UI Angular proyecta visibilidad, pero la API sigue siendo autoridad de seguridad. La referencia de secreto SMTP es opaca: jamás transportarla en eventos, errores, DTO público ni logs.

## 3. DAG y condición de liberación

1. **Esta entrega M02.06:** únicamente documenta las dependencias y salidas. Puede recibir `LISTO_DEV` si A0–A4 documental y Scope Lock/V5 exact-SHA completan; la revisión A5 pertenece a otra Task Javier distinta del autor. No se autoaprueba.
2. **Siguiente fase M03:** cada subunidad se reevalúa individualmente contra PLAN/COLA/DAG vivos; no hereda habilitación de M02.06. M03.01 Domain, M03.02 Application, M03.03 authorization/tenant, M03.04 EF adaptador o migración, M03.05 proveedor/worker **solo cuando aplique y se autorice**, M03.06 auditoría. Código y pruebas reales por SHA.
3. **Checks causales:** cambios Domain/API requieren builds y xUnit .NET 10; Angular 22.2.1 exige npm/build/test y Playwright cuando aplique; EF/MySQL requiere servicio 8.4 efímero, rollback y fixtures A/B; seguridad IAM/JWT/RBAC necesita pruebas negativas aisladas. `queued/in_progress/cancelled/skipped/failed` **no** equivale a PASS requerido.
4. **Gates posteriores:** M04/M05/M06 y A5 independientes no se liberan por diseño. Ninguna API secreta, QA/main/PROD, Hostinger/Cloudflare, DNS ni datos productivos quedan autorizados por el cierre de M02.
5. **Colisiones/ownership:** un único claim condicional por ID; no crear ramas/PR/Tasks adicionales; serializar commits fast-forward y revalidar HEAD antes/después de escribir CHANGELOG. BITACORA es append-only; readback PLAN/COLA y DAG tras cada cierre.

## 4. Matriz de aceptación documental (futura, NO ejecutada)

| Caso | Tipo | Entrada de futura prueba | Resultado exigido |
|---|---|---|---|
| P01 | Positivo | Admin de empresa A con membresía, grant y clave editable en su tenant | Solo valor/versionado A; auditoría A, ningún acceso B |
| P02 | Positivo | Dos lecturas con misma `KeyCanonical`, scope y versión estables | Resultado determinista, nunca caché ALLOW indefinida |
| P03 | Positivo | Update con `expectedVersion` actual y clave tipada permitida | Una sola revisión, registro post-commit idempotente |
| P04 | Positivo | Revocación explícita de flag de A con acceso adecuado | DENY siguiente operación, B permanece inalterado |
| N01 | Negativo | Token/URL A con `EmpresaId=B` por body/header | DENY, sin enumeración de B |
| N02 | Negativo | `EsAdministrador` sin `RolPermiso` vigente | DENY aun con plan/flag activos |
| N03 | Negativo | Membresía revocada o rol sin autoridad de escritura | DENY, cero mutación y sin respuesta con secretos |
| N04 | Negativo | Sucursal/recurso B a través de sesión A | DENY, sin caché ni evento cruzado |
| N05 | Negativo | Clave desconocida, tipo inválido, HTML/JS/SQL, confusable | 400/422 seguro, no crear/evaluar key |
| N06 | Negativo | `expectedVersion` viejo o colisión de escritores | 409 y ninguna escritura silenciosa |
| N07 | Negativo | Persistencia/auditoría indisponible, timeout o respuesta tardía | Fail-closed; no declarar commit inexistente |
| N08 | Negativo | Replay con idempotencyKey de A desde B | No compartir resultado; denegación por tenant/actor |
| N09 | Negativo | Solicitud para cambiar `disable_rbac`, secretos o scope PLATFORM desde tenant | DENY incondicional, sin bypass |
| N10 | Negativo | Intento de usar este documento para activar worker, webhooks, DDL o QA/PROD | NO_GO; implementación requiere otro scope/gate |
| N11 | Negativo | Dos versiones o valores privados de ámbitos incompatibles | DENY en ambigüedad, no elegir primera fila |
| N12 | Negativo | Log/DTO público intenta publicar token JWT, referencia SMTP o datos tenant B | Omitir/redactar, sin fuga |

**Resultado de auditoría del documento:** P01–P04 y N01–N12 son *oráculos de diseño*; NO se han ejecutado como tests de backend, API, navegador, MySQL o seguridad. Tampoco se afirma haber instrumentado métricas, workers ni eventos.

## 5. Matriz de impacto y cierre A0–A5

- **Componentes y rutas:** se añade sólo este archivo `docs/v5` y encabezado propio de `CHANGELOG_AI.md`; código `Domain/Application/Infrastructure/API/Angular` permanece intacto. No cambian interfaces ni DTO .NET.
- **Datos/secretos/seguridad:** cero migraciones y cero escritura MySQL; preservadas las fronteras JWT/RBAC/tenant A/B/PII/revocación, sin publicar contraseñas o URLs firmadas.
- **Consumidores:** `C01-03.M03.*`, configuración legacy tenant-first, configuraciones de branding y proyección UI; M02 no integra rutas nuevas ni dispara procesos.
- **A0:** comparar árbol/diff, blobs, sin secretos, sección CHANGELOG encabezado+viñetas contiguas y sin `##` vacío.
- **A1:** verificar integridad y coherencia lógica de P01–04/N01–12; `NO_EJECUTADO_RUNTIME`.
- **A2:** revisar denegación cruzada A/B, grants, vigencia, CAS, PII, inyección y revocación por inspección documental.
- **A3:** cotejar dependencias con los seis artefactos previos, código actual y contrato M02; binarios, HTTP, Docker/MySQL y Playwright son `NO_APLICA_POR_DIFF_SOLO_MARKDOWN`, jamás PASS inventado.
- **A4:** commit `dev` fast-forward con lectura exacta de HEAD; Scope Lock y verificador V5 `completed/success` en el SHA publicado y readback institucional.
- **A5:** `A5_PENDIENTE` de otro TASK_ID dentro de seis Javier; cierre documental DEV no autoriza promoción.
- **Rollback documental:** `git revert` normal del commit de esta unidad, preservando BITACORA histórica y registrando compensación aditiva si fuera necesaria. Rollback futuro de DB/API requiere plan y pruebas propios.
