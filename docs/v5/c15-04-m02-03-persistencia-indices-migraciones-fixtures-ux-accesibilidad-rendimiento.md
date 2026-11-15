# SOLQARYN V5 — C15-04.M02.03: persistencia, índices, migraciones y fixtures de auditoría UX

**Autor:** PRIMARY DEV 03 — JAVIER_CHATGPT, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fecha:** 2026-11-14. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** diseño documental M02.03; no se ejecuta DDL, no se crea migración ni se altera una base de datos.

## 1. Autoridad, predecesores y fuentes comprobadas

Autoridad única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, y Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Al reclamo: `PLAN_MAESTRO!H2278=PENDIENTE`, I vacío; `COLA!G2278/O2278=SI`; `DAG_TECNICO!A2239=C15-04.M02.03`, F=VALIDADO/G=SI; después CAS exclusivo H=EN_PROGRESO e I=TASK_ID, con readback y evento aditivo. Los seis predecesores documentales M01.01–06 estaban LISTO con revisión independiente; M02.01 LISTO_DEV de otro autor y M02.02 en ejecución concurrente de otra Task. M02.03 tiene su propia elegibilidad: no hereda A5 del contrato M02.01 ni escribe los archivos de M02.02.

Fuentes observadas en Git (rutas y blobs, no cobertura certificada):
- `backend/src/Infrastructure/Persistence/AppDbContext.cs` (blob `c4b32f6637f1b4426655bb0cd12af8dd9d229866`): `DbSet` comerciales reales, marcas de tenant verificadas y restricciones para escrituras síncronas; no existe un `DbSet` aprobado aquí para mediciones UX.
- `backend/src/Infrastructure/Persistence/CommercialTenantPhysicalConstraints.cs` (blob `d021861d357de201049fece1b6c63b2c9c218250`): exige doce relaciones físicas compuestas empresa/referencia en el dominio comercial; no son índices de auditoría UX.
- `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` (blob `2ba91bab0420f7e0fc840cf9c96e2b974838694d`): los filtros excepcionales no equivalen a autorización universal; una observación debe conservar el contexto de empresa autorizado.
- `backend/src/Infrastructure/Persistence/IdentidadUniversalSnapshotEfSource.cs` (blob `a02ddd5883d3b9f855144c178cb96ecf965a7411`): obtención de identidad acotada a usuario/empresa/sucursal; no autoriza registros cross-tenant.
- `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` (blob `6210d8b12abd77171d95d17e2292c1a319b89263`): EF Core 10.0.12 y proveedor Oracle MySql.EntityFrameworkCore 10.0.9, TargetFramework net10.0.
- Contratos M01.06 (blob `5fff95e8a351a85bb1fee19a8cc95ffb9a8aae96`) y M02.01 (blob `604c55ec350a879f3766b5c655f6f844e91aba0a`), con `UX01–UX12`, `INV01–INV16`, `AT01–AT10` como **modelos conceptuales**, no tablas.

La carpeta de migraciones versionadas existe en Infrastructure. La presencia de archivos o referencias EF **no** acredita un esquema MySQL desplegado ni autoriza su aplicación en Aiven. No se lee ni modifica ninguna instancia viva.

## 2. Política de persistencia y clasificación de datos

La opción predeterminada para M02 es **no persistir evidencia UX en nuevas tablas**: usar artefactos de prueba sanitizados, SHA, metadatos mínimos y referencias idempotentes de BITACORA; evitar capturas persistentes con PII. Una futura necesidad de almacenamiento propio deberá justificarse en una microtarea de implementación distinta, con modelo de amenazas, permisos server-side, retención y migración revisada. Ningún identificador de tenant enviado por el browser se acepta como autoridad.

| Clave | Dato propuesto en eventual evidencia | Tratamiento y límite |
| --- | --- | --- |
| PD01 | `scopeKey`, `planItemId`, `sourceSha` | Requeridos; solo IDs del plan actual y SHA Git real |
| PD02 | `surfaceKey`, ruta canónica y versión de UI | Guardar ruta normalizada, sin query strings con PII ni tokens |
| PD03 | `tenantScope` y `branchScope` | Resolver en servidor; ausente para observaciones públicas sin dato privado |
| PD04 | `actorRole` y permiso esperado | Categoría no identificante; nunca JWT, cookie o usuario real |
| PD05 | `viewportPx`, zoom, orientación, dispositivo sintético | Capturar método y contexto; no inferir accesibilidad por el campo |
| PD06 | `criterionRef`, `resultStatus`, herramienta y versión | Estado inicial `NO_EJECUTADO`; PASS solo con ejecución evidenciada |
| PD07 | `metricType`, unidad, red, muestra, baseline | Valores nulos hasta medición real; no inventar LCP/INP/CLS |
| PD08 | `artifactRef`, hash y depuración | Referencia con ACL/retención; nunca URL pública de captura privada |
| PD09 | `findingId`, severidad propuesta, reproducción | No elevar P0/P1 sin escenario reproducible |
| PD10 | `authorTaskId`, `reviewerTaskId`, `eventKey` | A5 con IDs distintos dentro de las seis Tasks Javier |
| PD11 | `observedAtUtc`, procedencia y política de caducidad | No usar timestamp local como identificador nativo de corrida |
| PD12 | `stateVersion` / concurrencia | CAS lógico antes de mutación; conflicto no se sobrescribe |

Para cualquier almacén futuro: separar evidencia compartible de evidencia tenant-privada, restringir acceso por autorización backend, cifrar secretos fuera del repositorio y minimizar datos a nivel de campo. Evitar conservar HTML, headers de autorización, sesiones, emails y números de documento en fixtures o capturas.

## 3. Índices y restricciones propuestos, no implementados

Los siguientes son **candidatos sujetos a EXPLAIN y revisión del modelo físico**, no instrucciones SQL para ejecutar. No se crean índices hoy.

| Clave | Consulta futura y candidato | Negativo / riesgo a comprobar |
| --- | --- | --- |
| IX01 | `(scopeKey, sourceSha, surfaceKey)` para observaciones de un commit | Nunca confundir SHA de otra corrida con el vigente |
| IX02 | `(tenantScope, surfaceKey, observedAtUtc)` para evidencia tenant-privada | Consulta sin filtro de tenant debe denegarse |
| IX03 | `(findingId, sourceSha)` para referencia de defecto | Hallazgo externo no se asocia a empresa distinta |
| IX04 | `UNIQUE(eventKey)` si se crea bitácora técnica futura | Duplicado por retry no produce segunda aprobación |
| IX05 | `(scopeKey, resultStatus, criterionRef)` para revisión de criterios | Un `NO_EJECUTADO` no se agrega a PASS |
| IX06 | `(artifactHash, retentionClass)` para control de evidencias | Hash no sustituye ACL ni valida contenido/autoría |

Las claves se normalizarán según tipos reales y cardinalidad comprobada; los índices de tenant deberán comenzar por la columna de ámbito cuando el patrón de acceso lo exija, verificando que ninguna clave única permita contaminación entre empresas. Medir selectividad, plan de consulta, costos de escritura y tamaño antes de aceptar un índice; no agregar redundancias a las doce FK físicas comerciales existentes. Relaciones futuras entre registros de auditoría exigirían restricciones compuestas, no confianza en la UI.

## 4. Plan de migración condicionado y rollback

1. **Preflight futuro:** comprobar scope M03 expresamente habilitado, HEAD exacto, versión MySQL 8.4 y proveedor EF real del ambiente efímero, identidad con permisos mínimos y contrato de retención. Mantener QA/main/PROD cerrados.
2. **Diseño expand-only:** revisar snapshot del modelo EF, migración generada y diff SQL; validar compatibilidad hacia atrás, claves FK de empresa y separación de PII. No invocar `Database.Migrate()` sobre Aiven real en esta microtarea.
3. **Validación efímera:** ejecutar en GitHub Actions con MySQL de servicio aislado y datos sintéticos; demostrar escenarios autorizado/denegado, transacciones, idempotencia, índices y restauración. Una prueba `queued`, `skipped`, `cancelled` o `failed` no es PASS.
4. **Lanzamiento futuro sujeto a orden adicional:** expandir de forma aditiva, backfill seguro con límites y checkpoint, dual-read controlado si se autoriza, verificación de integridad y contrato de retiro por fase. Ninguna de estas operaciones se realiza hoy.
5. **Reversión:** para este commit, `git revert` normal y evento BITACORA aditivo. Para una migración futura, respaldos/restauración ensayada, comprobación de datos y reversión de compatibilidad aprobada antes de modificar cualquier entorno.

No se prescribe `DROP`, `ALTER`, `CREATE INDEX` ejecutable ni cambios de conexiones. Si la base real discrepa, se registra un fallo causal y se detiene solo la operación insegura.

## 5. Fixtures y oráculos de aceptación futura

| Caso | Fixture sintético y estímulo | Resultado positivo futuro | Resultado negativo futuro |
| --- | --- | --- | --- |
| FX01 | Tenant A/B con datos ficticios distintos | A ve solo evidencia A bajo permiso real | A consulta `findingId` de B y obtiene denegación sin filtración |
| FX02 | Visitante de tienda pública | Solo se obtiene señal pública saneada | Cookie o slug de B no revela evidencia privada |
| FX03 | Actor sin RBAC de auditor | Backend deniega escritura/lectura privadas | Guard Angular no eleva autorización API |
| FX04 | Captura 320 px, zoom 200%, teclado y foco | Evidencia reproducible por ruta implementada | Screenshot aislada no se marca WCAG PASS |
| FX05 | `prefers-reduced-motion` y error de red | Estado de error y recuperación registrados | Animación oculta foco/estado o spinner infinito |
| FX06 | Reintento con `eventKey` idéntica | Un registro efectivo | Dos filas duplicadas no pasan idempotencia |
| FX07 | SHA antiguo mezclado con HEAD nuevo | Fuentes se separan por SHA | Check viejo no valida publicación nueva |
| FX08 | Retención vencida de artefacto privado | Revocación y depuración verificable | URL pública continúa accesible tras caducidad |
| FX09 | Índice candidato en MySQL efímero | `EXPLAIN` y plan de consulta observables | Una consulta sin tenant evade el filtro |
| FX10 | Migración efímera y rollback ensayado | Esquema y datos sintéticos recuperables | Migración destructiva no se declara reversible |
| FX11 | Resultado `NO_EJECUTADO` y valor métrico nulo | UI y reporte conservan estado correcto | Se calcula promedio/LCP sin datos |
| FX12 | A5 de otra Task Javier con SHA original | Autor y revisor distintos verificables | Autoaprobación se rechaza |

**Estado hoy:** FX01–FX12 son `FUTUROS_NO_EJECUTADOS`; sin datos, migraciones, scripts, requests HTTP, Playwright, MySQL efímero ni pruebas WCAG ejecutadas por este documento. No se atribuye cobertura o rendimiento operacional.

## 6. Impacto, auditorías y cierre

- **Componentes reales modificados:** solamente este Markdown y una sección autocontenida de `CHANGELOG_AI.md`. Ningún `.cs`, `.ts`, SQL, API, DTO, esquema, secreto, worker, feature flag o UI cambia.
- **Capas y consumidores:** contrato documental para futuras auditorías Angular y servicios .NET; Domain/Application/Infrastructure/API conservan responsabilidades, JWT/RBAC/tenant se autorizan en servidor. M02.04/M02.05/M02.06 siguen independientes.
- **A0:** revisión de fuentes y compatibilidad con M01/M02.01, PD01–12, IX01–06 y FX01–12.
- **A1:** coherencia documental de escenarios positivos/negativos; pruebas de navegador `NO_EJECUTADAS`, no PASS.
- **A2:** preservación conceptual de aislamiento tenant A/B, minimización de PII, JWT/RBAC, privacidad y fail-closed; sin cambios runtime.
- **A3:** trazabilidad hacia `AppDbContext`, filtros tenant, contratos físicos y modelos UX; HTTP, EF runtime y MySQL `NO_APLICA_DOCUMENTAL`.
- **A4:** diff de dos Markdown en dev, parent fast-forward, Scope Lock y procedencia CHANGELOG `completed/success` del SHA publicado, readback PLAN/COLA/BITACORA.
- **A5:** `PENDIENTE` para otra Task Javier con TASK_ID distinto; no promoción QA/main/PROD.

No existe aprobación para nuevas migraciones, desplegar auditorías, publicar métricas reales ni activar proveedores. Rollback documental = `git revert` ordinario con nueva evidencia; jamás force-push o edición histórica de BITACORA.
