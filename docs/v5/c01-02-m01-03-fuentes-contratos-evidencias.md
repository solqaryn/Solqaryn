# C01-02.M01.03 — Inventario verificable de contratos, código y evidencia de capabilities

**Módulo:** V5 FINAL, `C01-02.M01.03`, alcance PREPARACIÓN DOCUMENTAL. Ejecutor: PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`; únicamente `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridad: [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Resultado de fase todavía no ejecutado:** modelar capacidades por rubro, plan, rol, tenant, sucursal y recurso con denegación por defecto. La microtarea M01.03 únicamente **lee** contratos, código y evidencia disponibles, sin presuponer certificación. Los contratos de C01-01 y cierre C00-01 deberán revalidarse antes de M01.06/M02–M06; no anticipar implementación ni autorización.

## Evidencia de código fuente leída directamente desde GitHub dev

| Identificador | Ruta | Blob leído | Conclusión local de lectura | Evidencia no acreditada |
| --- | --- | --- | --- | --- |
| SRC01 | `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía usuario-empresa, rol tenant y clave lógica; `ObtenerRolEfectivo` verifica identidad/estado | No prueba de DB multiempresa actual |
| SRC02 | `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Fábrica de contexto de membresía y `ExigirEmpresa` fail-closed | No prueba HTTP ni claim JWT |
| SRC03 | `backend/src/Application/Interfaces/IUsuarioScopeService.cs` | `5345ee2de1b1e942bff8eded658f5dc27c0a6745` | Contexto tenant explícito; fallback legacy sin tenant usa `null` por defecto | No evidencia de todos los consumidores migrados |
| SRC04 | `backend/src/Application/Services/PermisoService.cs` | `278a8fa90dec43609d20157208a143f5794dcc35` | Lectura de grants existentes y edición de roles empresariales bajo restricciones; no confundir matriz con entitlement comercial | No verificación de todas las rutas/DTO actuales |
| SRC05 | `backend/src/Application/Common/CatalogoPermisosBase.cs` | `6060b4c54c0428902603f2157984b693a324e8f3` | Identificadores técnicos de módulo/acción; autoridad efectiva es grant relacional | No mapa de capacidades por rubro/plan implementado |
| SRC06 | `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Soporte DEV restringido a una allowlist de lecturas GET específicas | No autoriza soporte general ni escritura |
| SRC07 | `backend/tests/Solqaryn.Tests/N04CatalogoPermisosRuntimeTests.cs` | `00a9044cd8d400cde58b7f4bce63ff98a7432e94` | Test xUnit existente que comprueba catálogo contra atributos de controller/API | **No ejecutado en esta M01.03** |

La lectura anterior es reproducible por URL `https://github.com/solqaryn/Solqaryn/blob/dev/<ruta>` y SHA blob exacto, pero no reemplaza pruebas de seguridad/funcionalidad ejecutadas. La presencia de archivos o tests NO constituye PASS de integración. Para runtime futuro usar jobs GitHub Actions de SHA exacto y negative tenancy A/B.

## Dependencias, contratos V5 y naturaleza de la evidencia

| Referencia | Función | Límite explícito |
| --- | --- | --- |
| `docs/v5/c00-01-m02-01-domain-ownership.md` | Ownership e invariantes de entidades, riesgos de `EmpresaId` nulo | Es documentación preexistente, no certificación de futura matriz |
| `docs/ERP_N0_4_RBAC_RELACIONAL.md` | Contrato relacional `Usuario.RolId → Rol → RolPermiso → Permiso` | Estado/certificaciones históricas no sustituyen CI actual |
| `docs/v5/c01-02-m01-02-actores-superficies.md` | Entradas, salidas y doce actores/consumidores preliminares | No ejecuta ni publica endpoints de capabilities |
| `docs/v5/c01-02-m01-04-riesgos-supuestos-decisiones.md` | Riesgos de grants, tenant A/B, planes, sucursales y secretos | Decisiones D03–D05 continúan sin implementación |
| `docs/v5/c01-02-m01-05-aceptacion-capabilities.md` | AC01–AC16, criterios positivos/negativos y evidencias futuras | Son escenarios observables a ejecutar cuando exista código |
| `DAG_TECNICO!A:G` | `VALIDADO` para este ID documental, once predecesores a verificar LISTO | No libera M01.06, M02 ni migración por analogía |
| `PLAN_MAESTRO!H/I`, `COLA!E/G/O` | Fuente de estados, autoría y elegibilidad del ID | Nunca escribir manualmente estado en COLA |
| `BITACORA!A:Z` | EVENT_ID, IDEMPOTENCY_KEY y autor/revisor en 26 columnas | No aceptar eventos truncados, claves duplicadas ni RUN_ID inventado |

## Brechas para C01-02.M02+ y pruebas exigibles

1. **Matriz de capabilities:** no se ha demostrado un modelo persistido que cruce rubro, plan, rol, tenant, sucursal, recurso y acción. Crear contrato en M02 con precedencia, defecto DENY y revocación explícitos; no inventar un endpoint por este documento.
2. **Fuente de entitlement:** falta definir quién emite/actualiza el permiso comercial por plan/rubro y cómo se combina con `RolPermiso`; exigir intersección restrictiva, no bypass.
3. **Membership/tenant:** comprobar mediante pruebas API y DB de escenario A→B y B→A, tokens inválidos y membresías inactivas cuando se cambie runtime.
4. **Sucursales y recursos:** confirmar propiedad, esquema y filtros efectivos; recursos `EmpresaId` nulo permanecen bloqueados sin autorización nueva.
5. **Seguridad:** no inferir que `EsAdministrador` elude grants; políticas globales de soporte no equivalen a permiso empresarial. Evitar exponer hashes, tokens y PII.
6. **Integración:** requerir `dotnet build/test` y MySQL real sólo para código/persistencia afectados; Angular/Playwright sólo si cambia frontend/flujo visible, siguiendo workflows reales.
7. **Identidad y rollback:** correlacionar cada prueba al SHA exacto, revisar logs y fallos; para cambios ejecutables no certificar con documentación ni con jobs SKIPPED.

## Casos positivos/negativos de verificación documental (no funcional)

**P1:** cada fuente SRC01–SRC07 tiene path, blob, contrato observable y límite de evidencia; las ausencias se identifican sin marcar la matriz de capabilities como implementada. **P2:** la matriz de dependencias separa preparación M01 de implementación y revalidación posterior del C01-01 final.

**N1:** una clase encontrada no se considera API pública ni capacidad habilitada. **N2:** no interpretar `EsAdministrador` o plan comercial como permiso universal. **N3:** negar interpretación de `EmpresaId=null` como propiedad global. **N4:** rechazar CI verde de SHA anterior o job requerido `SKIPPED`. **N5:** rechazo de A5_PASS por la Task autora y QA/main/PROD sin permiso nuevo. **N6:** rechazo de BITACORA con menos de 26 campos o identidad/RUN_ID inventados.

Estos escenarios son comprobaciones de trazabilidad y límites, NO pruebas .NET/API/MySQL/Playwright ejecutadas en esta microtarea.

## Clasificación, A0–A5 y reversibilidad

`requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO` por **dos archivos de documentación exclusivamente**. `requiere_security=SI` por auditoría documental JWT/RBAC/tenant. `requiere_scope_lock=SI` por modificación de `dev`.

A0: revisar diff, blobs, secreto accidental, estructura Markdown y preservación de CHANGELOG; A1: P1/P2 y N1–N6 documentales; A2: inspección de denegación por defecto y fronteras cross-tenant; A3: fuentes reales, DAG y once predecesores LISTO; A4: commit directo fast-forward con SHA exacto, Scope Lock SUCCESS, readback PLAN/COLA/BITACORA. A5: otra Task, nunca autoaprobar. **Rollback:** `git revert` no destructivo del commit, rastro institucional aditivo, ninguna escritura en QA, main, PROD, infraestructura, datos o secretos.
