# SOLQARYN V16 — C13-03.M02.03: persistencia, índices y fixtures futuros para evidencia de rendimiento

**ID:** C13-03.M02.03; PLAN_MAESTRO fila 1954; autor PRIMARY DEV 03 JAVIER, Task \`6adc2e4bd8a48191ac7d8641ed95a1a3\`; rama \`solqaryn/Solqaryn@dev\`; \`PROJECT_SCOPE_LOCK=STRICT\`. **Clasificación R0 documental:** no se crea esquema, DbSet, tabla, columna, índice, migración EF, endpoint, instrumentación, cookie, caché ni métrica ejecutada.

**Fuente autorizada:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\` y PLAN/COLA/DAG. El resultado exigido es **definir** persistencia, índices, migraciones y datos de prueba, sin modificar el runtime. M01.06 mantiene G01–G16, RB01–RB08 y pruebas T01–T10 futuras; M02.01 ofrece E01–E14, REL01–REL12 e INV01–INV18 conceptuales; M02.02 plantea OP01–OP08, DTO01–DTO08, ERR01–ERR12, VAL01–VAL16 sin rutas API creadas. M02.03 no presupone que esos diseños ya tengan tablas o endpoints.

## 1. Invariantes previos obligatorios

- El presupuesto propuesto de bundle **650–750 KB frente a 1–2 MiB** sigue **SIN_RESOLVER**. No inventar un umbral aprobado ni un porcentaje de cumplimiento.
- LCP, INP, CLS, TTFB, métricas móviles, contrastes y muestreo siguen **NO_MEDIDO** para esta microtarea, porque no se ejecutó navegador, build instrumental, API ni MySQL por su alcance R0.
- Angular 22.2.1 se limita a UX; autoridad de JWT/RBAC/tenancy, revocación y datos empresariales pertenece a ASP.NET Core 10 backend. MySQL 8.4 y EF Core 10/Oracle provider son persistencia vigente, pero **no se toca su esquema**.
- El tenant A/B es una frontera de seguridad real, no una dimensión opcional de anonimización. Ninguna futura métrica, caché, muestra, reporte o cursor podrá exponer empresa, permisos, costos, PII ni sesión ajena.
- Las evidencias de medición futuras deben preservar el SHA de código, blob/artefacto, versión de herramienta, perfil, fecha UTC, número de muestras y método; \`NULL\` o ausencia de autoridad no dan permiso global.

## 2. Doce responsabilidades de persistencia conceptuales P01–P12

| ID | Familia de información candidata | Propiedad, caducidad y regla de integridad a estudiar |
| --- | --- | --- |
| P01 | Baseline de versión | SHA, config aprobada o decisión PENDIENTE, ruta de prueba; no reescribir histórico |
| P02 | Catálogo de perfiles sintéticos | Dispositivo, red, navegador, viewport, accesibilidad; sin identificadores personales |
| P03 | Identidad de run y herramienta | ID de CI, fuente, sello UTC, límites de tamaño, estado completed/failed |
| P04 | Escenario de ruta | Clave estable y público/privado; la autoridad privada se revalida en backend |
| P05 | Definición de métrica | Unidad, metodología, percentil, versión del umbral, estado NO_MEDIDO cuando falte |
| P06 | Distribución de observaciones | Muestra/suma agregada, procedencia, retención y límites, no guardar payload sensible |
| P07 | Presupuesto de artefactos | Decisión explícita, aprobador, unidad normalizada, documento y vigencia |
| P08 | Manifiesto de recursos críticos | Hash/clase/dimensiones, nunca URLs privadas ni tokens de descarga |
| P09 | Proyección de presentación tabular | Paginación estable, permiso de columnas, contexto tenant derivado de auth |
| P10 | Política de caché autorizada | Púbico/privado, Vary/ETag/TTL y borrado seguro, no persistir permisos revocados |
| P11 | Evidencia de accesibilidad | Perfil, casos teclado/foco/movimiento reducido, origen de captura no sensible |
| P12 | Relación de rollback | SHA anterior, dependencias, artefactos y evaluación posterior, sólo evidencias inmutables |

**P01–P12 son tipos lógicos de información y preguntas de diseño, no tablas nuevas.** Antes de elegir almacenamiento real, verificar necesidad versus GitHub Actions artifacts, logs existentes, observabilidad autorizada y políticas de retención. No crear duplicados de empresa, usuario, rol ni tenant y no migrar datos sin una tarea R2 independiente.

## 3. Índices y restricciones candidatos IDX01–IDX12 (SIN DDL)

| ID | Acceso o constraint futuro a evaluar | Riesgo/negativo a verificar antes de crear un índice |
| --- | --- | --- |
| IDX01 | (tested_sha, metric_definition, profile) | No mezclar valores de commits o perfiles incompatibles |
| IDX02 | (run_id, artifact_digest) | Correlación reproducible; digest no debe ser un secreto |
| IDX03 | (scenario_id, visibility_class) | Privado no puede inferirse por URL ni consultarse públicamente |
| IDX04 | (sample_timestamp_utc, retention_state) | Purga auditada, sin retener PII o sesión |
| IDX05 | (metric, unit, percentile, sample_count) | Comparación rechazada cuando faltan unidades o muestras |
| IDX06 | (budget_decision_id, approved_at) | Pendiente no equivale a aprobado; no crear duplicidad de versión |
| IDX07 | (asset_class, bundle_sha) | Manifest no expone URLs internas ni rutas privadas |
| IDX08 | (route_id, browser_profile) | Riesgo de cardinalidad explosiva; evaluar costo de almacenamiento |
| IDX09 | (scenario_id, status, occurred_at_utc) | No registrar mensajes SQL, JWT ni datos de usuario |
| IDX10 | (rollback_id, prior_sha, current_sha) | Imposible confirmar revert si SHA no desciende causalmente |
| IDX11 | (tenant_scope, authorized_object) | Solo si el backend define clave segura; jamás permitir NULL global |
| IDX12 | (evidence_reference, audit_author) | Firmas y hashes de fuente sin incluir secretos ni datos productivos |

IDX01–IDX12 son **consultas y restricciones posibles**, no DDL ni diseño de PK/FK decidido. Primero examinar esquema/migration snapshot reales, \`EXPLAIN\`, selectividad, tamaño de claves, UTC/collation y semántica de índices únicos con NULL en MySQL 8.4 efímero. No crear índices redundantes, cascadas destructivas, partición de tenant por columna falsa o tablas de RUM sin autorización explícita.

## 4. Ocho etapas futuras de migración MIG01–MIG08 (NINGUNA EJECUTADA)

| Paso | Precondición R2 y reversibilidad a acreditar |
| --- | --- |
| MIG01 | Determinar si hace falta persistencia en DB o basta evidencia CI, y registrar decisión de arquitectura |
| MIG02 | Inventariar esquema/índices/versiones reales de desarrollo y datos sintéticos, sin tocar Aiven productivo |
| MIG03 | Revisar controles JWT/RBAC, aislamiento tenant, privacidad, retención, volumen y límites de cuota |
| MIG04 | Proponer cambios exclusivamente aditivos, con comparativa EF snapshot y migración generada |
| MIG05 | Probar orden de escritura, concurrencia, idempotencia y apagado de ingesta con rollback seguro |
| MIG06 | Correr contra servicio **MySQL 8.4 efímero**, con realmente \`Category=Integration\`, 0 FAIL y logs del SHA |
| MIG07 | Ensayar up/down, índices, pérdida de red, replays, cancelación y reconciliación por tenant |
| MIG08 | Aprobar impacto, seguridad, reversión y retención en autorización separada; sin QA/PROD implícitos |

Un rollback de datos o DDL real no es automáticamente reversible por \`git revert\`: requiere diseño probado, respaldo y autorización separada. El cambio presente es exclusivamente Markdown y su eventual revert no modifica datos.

## 5. Fixtures futuros F01–F12 (casos positivos/negativos, 0 ejecutados)

| ID | Positivo FUTURO | Negativo/frontera FUTURA |
| --- | --- | --- |
| F01 | Run de SHA correcto crea evidencia correlacionada | SHA diferente o digest falso se rechaza |
| F02 | Perfil móvil registra viewport/red/reduced motion | Muestra sin perfil no produce PASS |
| F03 | Resultado LCP tiene unidad, percentil y N válido | Valor sin denominador queda NO_MEDIDO |
| F04 | Dos budgets con decisión explícita son comparables | Conflicto 650–750KB/1–2MiB no se aprueba |
| F05 | Manifiesto público sólo muestra recursos públicos | URL privada o token filtrado se rechaza |
| F06 | Política caché admite ETag/304 público | Sesión o datos tenant se cachean públicamente y FALLA |
| F07 | Caso de ruta privada respeta tenant A/rol | Token B o revocado no lee resultado privado A |
| F08 | Paginación conserva cardinalidad y permisos | Cursor mezcla empresas o pierde importes |
| F09 | Retención elimina muestras sintéticas fuera de plazo | Purga borra auditoría crítica o deja PII |
| F10 | Comparación usa misma unidad y entorno | KB vs MiB sin normalizar se rechaza |
| F11 | Registros de errores evitan PII y secretos | Cookie/JWT/identificador personal en logs falla |
| F12 | Rollback restaura snapshot y causalidad esperada | Revert sobrescribe commit concurrente o CI cancelada se intenta aprobar |

**Estado de estas pruebas: FUTUROS_NO_EJECUTADOS, numerador 0/12.** No existe certificación real MySQL, Lighthouse, Playwright, net10, seguridad HTTP ni regresión de UI en esta tarea R0. Las futuras tareas de implementación deberán registrar build/test-run exact SHA, perfiles, filtros, logs y porcentajes con denominador.

## 6. A0–A5 y salida

- **A0:** fuente M01.06, M02.01/.02, autoridad/ownership, P01–P12, IDX01–IDX12, MIG01–MIG08 y F01–F12; diff individual de documento + CHANGELOG, rollback especificado.
- **A1:** inspección de contratos y oráculos positivos/negativos propuestos; no atribuir ejecución real.
- **A2:** requisitos de tenant A/B, JWT/RBAC/revocación, no cache pública de datos privados, PII, retención y límites, sin cambios a controles runtime.
- **A3:** MySQL EF, HTTP, Angular, browser y proveedor **NO_APLICA para diffs Markdown**; obligatorios al cambiar código o infraestructura pertinente.
- **A4:** publicar mediante fast-forward con comparación de HEAD, Scope Lock y V5 provenance \`completed/success\` sobre SHA real, registrar PLAN/COLA/DAG y BITACORA con readback; jobs cancelados u omitidos no son PASS.
- **A5:** revisión independiente pendiente por otra Task Javier que no sea autora, con evidencias auditables; LISTO_DEV R0 no equivale a A5 ni aprobación QA/PROD.

**Rollback R0:** \`git revert\` normal del commit, tras verificar HEAD actual y efectos sobre otros autores. BITACORA se conserva íntegra. No se altera SQL, registros, nube, secretos o configuraciones. El presupuesto, métricas y aceptación del rendimiento permanecen abiertos.
