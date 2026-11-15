# SOLQARYN V5 FINAL — C15-03.M01.06: bloqueos, rollback y salida documental de auditoría de seguridad y tenancy

**Autor:** PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Rama:** `solqaryn/Solqaryn@dev`; **PROJECT_SCOPE_LOCK:** `STRICT`. **Tipo:** contrato documental M01.06; NO es un pentest, auditoría de seguridad completada ni certificación de cero vulnerabilidades o cero P0/P1.

**Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, Scope Lock y autoridad V5 vigente. No se ejecutan herramientas contra sistemas productivos.

## 1. Dependencias y estado contractual de preparación

El preflight institucional confirmó `C15-03.M01.01–05` en LISTO, con revisión A5 de una Task Javier distinta de cada autora; COLA G/O=SI y DAG F=VALIDADO/G=SI habilitaron exclusivamente el CLAIM de esta M01.06. Cada antecedente define **preparación**, no prueba integrada de seguridad.

| Unidad | Fuente publicada y revisión independiente acreditada | Evidencia preparada, no probada en vivo |
| --- | --- | --- |
| M01.01 | `6f78719ba9e7318122b2efa1bf37795ef66f6d86`, A5 Task :20 | NR01–NR16 de límites tenant/JWT/RBAC y 8 P/N futuros |
| M01.02 | `4301356a98a98c90927dcd9998d8e0e90a7c97bb`, A5 Task :20 | 12 actores, 12 interfaces IO y 10 fronteras |
| M01.03 | `8c8f962807f7aaa4befa0bc8094442ed2ec7e68c`, A5 Task :20 | 15 blobs de fuentes Git y G01–G12 brechas NO_VERIFICADAS |
| M01.04 | `d6520a9dae5d49a0389faa6112964da85d5732aa`, A5 Task :20 posterior a reparación | 14 riesgos potenciales, 8 supuestos, 8 decisiones |
| M01.05 | `a0a6aa41593f1b113aa86956c56d0cf75037176f`, A5 Task :50 | AC01–AC16 y N01–N16 FUTUROS_NO_EJECUTADOS; 8 NF sin medición |
| M01.06 | Estado inicialmente PENDIENTE sin owner, CAS único a EN_PROGRESO y readback de PLAN/COLA/DAG | Contrato de bloqueo, reversión y puerta de salida documental |

El reparo causal de M01.04 sustituyó el término incompatible con Scope Lock en un commit posterior, sin alterar la autoría original; su A5 se acreditó después contra el SHA corregido. Ninguna de esas A5 equivalió a pentest, prueba HTTP/MySQL A/B o promoción de entorno.

## 2. Doce bloqueos técnicos que las etapas posteriores deben respetar

| Gate | Motivo bloqueante | Condición comprobable para levantarlo en trabajo posterior |
| --- | --- | --- |
| B01 — Identidad | JWT válido pero membresía o rol revocado no debe dar permiso | Test HTTP con rol activo/revocado y sin efectos cruzados |
| B02 — Selección tenant | Header empresa, slug o parámetro del cliente no es fuente de autoridad | Contexto resuelto por servidor y negativos de duplicado/spoof |
| B03 — Sucursal y objetos | IDs de sucursal o recurso de B no son accesibles a usuario A | A/B con lectura, escritura, export y no filtración |
| B04 — Consultas EF | `IgnoreQueryFilters` o acceso sin contexto depende de cuarentena externa | Test integrado de middleware y guard que niega sin empresa validada |
| B05 — FK y escrituras | Entidades asociadas de tenant diferente o `EmpresaId=NULL` no se reatribuyen por inferencia | MySQL efímero, negativas FKs y consistencia transaccional |
| B06 — Superficie pública | Host adulterado y rutas públicas no habilitan datos privados | Prueba de hosts legítimos/falsos y denegación de rutas privadas |
| B07 — Soporte/plataforma | Grant revocado, falso o excesivo no concede acceso global | Fixture de permisos independientes, caducidad y auditoría |
| B08 — Archivos/jobs | Enlace vencido, objeto B o payload de trabajo no autorizado no hereda autoridad | Negativos de pertenencia, TTL y firma servidor cuando existan |
| B09 — Errores y trazas | JWT/PII/secretos/datos tenant B no aparecen en respuesta o log | Capturas redactadas de N01–N16 en CI aislada |
| B10 — CI y evidencia | queued, in_progress, skipped, cancelled, failure o SHA distinto no es PASS requerido | Check-run `completed/success` del `head_sha` exacto y logs causales |
| B11 — Gobierno y A5 | DAG NO, owner concurrente, evento duplicado o revisión propia no liberan alcance | PLAN/COLA/DAG, CAS, BITACORA A:Z y A5 de distinta Task Javier |
| B12 — Entorno | LISTO_DEV o documento no concede QA/main/PROD, cambios de DNS, DB ni proveedor | Autorización diferenciada y nuevos gates de seguridad y rollback |

**Estado al redactar:** estos gates son criterios futuros y no representan fallos observados, pruebas ejecutadas o certificación de seguridad. La dependencia de una cuarentena externa del guard EF es una **hipótesis de riesgo**, no una vulnerabilidad demostrada. Los G01–G12 y S01–S08 permanecen NO_VERIFICADOS hasta reproducción; D01–D08 siguen ABIERTAS. Tampoco se declara cero P0/P1, cero exposición de tenant o ausencia de rutas inseguras.

## 3. Contrato de pruebas causales y negativos para la etapa siguiente

Para levantar un gate de código real, usar exclusivamente GitHub Actions del SHA de implementación, con .NET 10, MySQL 8.4 efímero, tenant A/B sintético, rol autorizado y revocado, sucursal A/B, logging redactado y rollback. Angular 22.2.1/Playwright sólo si UI resulta causal. Registrar por caso: entrada, owner, actor/tenant artificial, endpoint/componente, esperado, observado, persistencia, redacción, logs, SHA, check, PASS/FAIL/NO_ACREDITADO y evento A:Z. La ausencia de endpoint o proveedor se documenta `NO_APLICA_RUNTIME_NO_IMPLEMENTADO`, jamás PASS.

Los AC01–AC16/N01–N16 de M01.05 son **FUTUROS_NO_EJECUTADOS**. Un resultado distinto de `completed/success` en el check necesario conserva la microtarea de código en VALIDANDO y no permite contar cierre. La auditoría integral de seguridad sólo podrá declararse cuando todas las negativas causales aplicables estén reproducidas y cada hallazgo crítico esté corregido y reensayado independientemente.

## 4. Rollback, cambio de estado y liberación condicionada

Si Scope Lock, procedencia CHANGELOG, integridad del diff o A5 requerido falla, detener el **scope afectado**, anotar causa y conservar owner. El retorno de este documento es `git revert <SHA>` normal tras releer HEAD; sin force-push, reset, modificación de BITACORA histórica ni reactivación de Tasks ajenas. El relevo de owner requiere inactividad acreditada y CAS con un único ganador. Sin evidencia de seguridad no modificar roles, políticas JWT, datos productivos, migraciones, secretos, dominio, Cloudflare/R2 ni QA/main/PROD.

El estado LISTO de esta **preparación M01.06** permite sólo **reevaluar la elegibilidad de M02** con predecesores, DAG F/G, PLAN H/I, permisos, Scope Lock exact-SHA y revisión independiente A5 cuando proceda. No equivale a aprobar auditoría técnica real ni a abrir paso automático a M03/M06 o producción. Un A5_PASS documental no concede permisos extras.

## 5. Impacto y auditorías A0–A5

**Diff autorizado:** sólo este Markdown y una sección indivisible de `CHANGELOG_AI.md`; no se cambian API, Domain/Application/Infrastructure, controladores, middlewares, DTO, JWT, RBAC, EF/MySQL, Angular, tests runtime, workflows, secretos, logs, worker ni infraestructura. Consumidores documentales futuros: evaluación M02 y auditoría de tenancy bajo sus propios gates.

- **A0:** prerequisitos M01.01–05 con A5 independiente, gates B01–B12 y fuentes exactas documentadas, revisión estática de no-alcance.
- **A1:** escenarios positivos/negativos y condiciones de contención escritos y coherentes; AC01–AC16 y N01–N16 siguen NO_EJECUTADOS, no se simula PASS funcional.
- **A2:** análisis de frontera JWT/RBAC/tenant A/B y protección de secretos en el contrato; pruebas de autorización real NO_APLICA a este diff Markdown, no se acreditan como PASS.
- **A3:** referencias de fuentes, precedencias DAG y evidencia institucional, sin ejecutar HTTP/MySQL/Angular/Playwright para un cambio documental.
- **A4:** publicación fast-forward por CAS sobre HEAD vigente, diff de dos Markdown, checks Scope Lock y verificador V5 en `completed/success` del SHA publicado; lectura posterior de PLAN/COLA/BITACORA.
- **A5:** PENDIENTE para una de las otras cinco Tasks Javier con TASK_ID distinto del autor; jamás autoaprobación ni permiso QA/main/PROD.

**Reversión:** `git revert` ordinario del commit de la unidad, sin reescribir fuentes ni suprimir registros históricos.
