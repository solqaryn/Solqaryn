# C01-02.M01.05 — Criterios observables de aceptación de capabilities y permisos

**Unidad exclusiva:** `C01-02.M01.05`, preparación documental V5 FINAL. **Autor:** PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Autoridad:** [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) y `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

**Objetivo de fase, todavía no certificado:** modelar capacidades por rubro, plan, rol, tenant, sucursal y recurso, con denegación por defecto y matriz de capabilities. M01.05 sólo fija criterios preliminares de aceptación funcional/técnica. No implementa la matriz, su persistencia, sus endpoints ni pruebas de integración. C01-01 y el cierre C00-01 deberán revalidarse antes de M01.06, M02–M06 o despliegue. El DAG autoriza preparación documental M01.01–05, no liberación de implementación.

## Contratos inspeccionados, sin atribuir nuevas pruebas de runtime

| Contrato | Evidencia fuente | Invariante a conservar |
| --- | --- | --- |
| Contexto tenant | `backend/src/Domain/Security/ContextoTenantActual.cs`, blob `7377d2d752985cc9baf31d08280f38ad94a6a114` | Resolver tenant desde membresía activa y exigir empresa correcta |
| Matriz roles/permisos | `backend/src/Application/Services/PermisoService.cs`, blob `278a8fa90dec43609d20157208a143f5794dcc35` | Rol activo, grants persistidos, dueño empresa y no bypass administrativo |
| Catálogo de permisos | `backend/src/Application/Common/CatalogoPermisosBase.cs`, blob `6060b4c54c0428902603f2157984b693a324e8f3` | El catálogo enumera, nunca autoriza por sí solo |
| Soporte restringido DEV | `backend/src/Application/Security/PlatformSupportPolicy.cs`, blob `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Lista cerrada de rutas/acciones y solo lectura |
| Pruebas existentes de catálogo | `backend/tests/Solqaryn.Tests/N04CatalogoPermisosRuntimeTests.cs`, blob `00a9044cd8d400cde58b7f4bce63ff98a7432e94` | Catálogo debe cubrir permisos exigidos por la API, sin afirmar ejecución ahora |
| Preparación V5 de actores y riesgos | `docs/v5/c01-02-m01-02-actores-superficies.md`; `docs/v5/c01-02-m01-04-riesgos-supuestos-decisiones.md` | Contratos preliminares, no permiso comercial operativo |

## Criterios funcionales, negativos y evidencias futuras

| ID | Escenario observable | Condición de aceptación | Prueba exigible cuando se implemente | Rechazo si falta evidencia |
| --- | --- | --- | --- | --- |
| AC01 | Usuario A con membresía activa, grant RBAC y entitlement de rubro/plan válido | ALLOW solo en empresa A y recurso de su ámbito | API autenticada + caso positivo y auditoría segura | No marcar capability funcional |
| AC02 | Usuario A sin grant explícito pero con plan válido | DENY; plan jamás crea un grant | API 403 y lectura de grants sin modificación | Bloquear promoción |
| AC03 | Usuario A con grant pero plan/rubro no habilitado | DENY por intersección de restricciones | API 403, plan verificado y ausencia de mutación | Bloquear promoción |
| AC04 | Empresa A intenta acceder al recurso de B | DENY sin revelar existencia ni datos de B | API/DB multiempresa A→B y B→A, con filas de ambas | Bloquear por riesgo crítico |
| AC05 | Sucursal S1 accede a recurso owned por S2 sin permiso expreso | DENY o caso permitido solamente por política explícita documentada | Prueba con recursos y sucursales reales de test | No asumir regla de sucursales |
| AC06 | Rol inactivo, sistema o de otra empresa intenta alterar grants | DENY y 0 mutaciones, 0 filtración | Tests negativos + persistencia MySQL cuando exista cambio | Bloquear edición |
| AC07 | Administrador marcado `EsAdministrador` sin grants | DENY; no bypass | Test unitario/service y API | Bloquear privilegio implícito |
| AC08 | Token expirado, inválido o sin membresía | 401/403 seguro, no leer PII ni recursos | Negativos JWT, anónimo, rol sin membresía | Bloquear autenticación |
| AC09 | UI oculta acción pero petición se fuerza manualmente | Backend repite autorización y deniega sin grant | Test API independiente de Angular; E2E si se modifica UI | No aceptar verificación solo visual |
| AC10 | Soporte plataforma fuera de allowlist o petición de escritura | DENY aunque sea usuario de soporte | Negativo contra ruta y método no permitidos | Sin ampliar rutas de soporte |
| AC11 | Revocación de grant/capability en sesión existente | Nueva operación DENY según vigencia definida en contrato M02 | Tests post-revocación/caché e integración cuando aplique | No declarar revocación garantizada |
| AC12 | Recurso legacy sin `EmpresaId` o con owner ambiguo | DENY/cuarentena hasta regularización autorizada | Consulta DB con dato negativo y sin mutación | No tratar null como público |
| AC13 | Datos privados en errores, respuestas o logs | Ningún JWT, hash, PII o secretos publicados | Escaneo de campos/respuestas/logs y redacción | Bloquear seguridad |
| AC14 | Catálogo de permisos divergente de controllers/API | 0 permisos requeridos sin representación de grant | `N04CatalogoPermisosRuntimeTests` en .NET CI exact SHA | Bloquear contract drift |
| AC15 | Concurrencia y cancelación de operaciones comerciales | Denegación no produce cambios y acciones autorizadas mantienen transacción | Prueba real de persistencia/rollback del caso afectado | No cerrar integración |
| AC16 | CI/certificación y trazabilidad | A0–A4 PASS sobre SHA preciso, A5 independiente antes de QA | Check runs `head_sha` exacto, logs, PLAN/COLA/BITACORA 26 campos | No interpretar SKIPPED/PENDING como PASS |

## Criterios técnicos transversales

1. **Autoridad:** la decisión final es server-side. El grant RBAC y el entitlement comercial son restricciones acumulativas, no permisos intercambiables. La precedencia exacta de rubro/plan/sucursal/recurso está pendiente de M02; aquí se exige fail-closed.
2. **Aislamiento:** no confiar en `EmpresaId` recibido, claim no verificado, ruta UI ni estado frontend. Rechazos deben conservar datos sin mutaciones ni filtraciones.
3. **Pruebas proporcionales futuras:** cuando M02/M03 introduzca código, `requiere_dotnet=SI` para API, `requiere_mysql=SI` si cambia persistencia, `requiere_node_angular=SI` si cambia UI y `requiere_e2e=SI` cuando haya flujos de navegador; verificarlos por cambio real. No aplicar estas herramientas a este documento sin diff ejecutable.
4. **Negativos indispensables:** plan sin permiso, rol sin grant, A→B, recurso sin dueño, soporte fuera de allowlist, UI adulterada y revocación. Un test sintético de matriz sin backend no demuestra acceso real.
5. **Trazabilidad:** matriz futura `rubro×plan×rol×tenant×sucursal×recurso×acción` con mapeo actor/entrada/salida/regla/caso/evidencia/rollback; no introducir una autoridad paralela al RBAC.
6. **Release:** A5 de otro TASK_ID, permisos explícitos del propietario y gates separados para qa/main/PROD; ninguna aprobación por esta unidad.

## Aceptación estricta de esta unidad documental M01.05

- **P1 documental:** comprobar que AC01–AC16 tienen escenario, condición, prueba y bloqueo observable, y que la matriz no otorga grants nuevos.
- **P2 documental:** cada frontera de usuario/tenant/plan/rol/sucursal/recurso y soporte tiene un caso negativo comprobable en la implementación futura.
- **N1 documental:** rechazar `LISTO_RUNTIME` cuando solo existe este documento.
- **N2 documental:** rechazar que `EsAdministrador` sea bypass, o que una capacidad comercial venza un 403 backend.
- **N3 documental:** rechazar `SKIPPED`, SHA anterior o CI pendiente como PASS obligatorio.
- **N4 documental:** rechazar A5_PASS por la propia autora o promoción qa/main/PROD.
- **N5 documental:** rechazar una microtarea nueva fuera de los 2.340 IDs o una ruta V13 histórica.
- **N6 documental:** rechazar BITACORA truncada a 21 campos: contrato real A:Z/26, EVENT_ID y clave únicos, RUN_ID vacío sin lectura nativa.

Son criterios revisables **en el documento**, no ejecuciones de API, .NET, Angular, MySQL o browser realizadas en esta M01.05.

## Evaluación A0–A5 del documento y rollback

`requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` porque se modifican solo Markdown y CHANGELOG. `requiere_security=SI` revisión documental JWT/RBAC/tenant/PII. `requiere_scope_lock=SI` commit de `dev`. A0: formato, diff, secretos y CHANGELOG preservado; A1: P1/P2, N1–N6; A2: fail-closed y datos/roles; A3: fuentes reales, DAG VALIDADO, once prerrequisitos LISTO; A4: commit directo fast-forward, checksum de blobs, Scope Lock exact-SHA y readback PLAN/COLA/BITACORA; A5: exclusivamente revisión por Task diferente. **Rollback:** `git revert` normal del SHA publicado, conciliación de evidencias aditiva sin eliminar historia. No se ejecutan cambios de runtime ni QA/main/PROD.
