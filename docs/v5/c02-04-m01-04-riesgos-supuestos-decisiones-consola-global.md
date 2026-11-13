# SOLQARYN V5 FINAL — C02-04.M01.04: riesgos, supuestos y decisiones de consola global auditada

**Unidad:** preparación DOCUMENTAL M01.04; autor PRIMARY DEV 05 — JAVIER (`6adc2e55f47c81919f6374e561687076`). Repositorio `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Fuente: [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El DAG VALIDADO habilita esta preparación y no libera M01.06, ejecución de IAM, migración, QA/main/PROD.

**Resultado de C02-04 futuro:** consola global separada de ERP, MFA/reautenticación, motivo obligatorio, acceso excepcional temporal, matriz de grants, auditoría crítica y pruebas de denegación. Aquí se registran *hipótesis y decisiones pendientes*, no implementación ni aprobación de mecanismos. Las fuentes verificables M01.01 [objetivo](c02-04-m01-01-objetivo-no-regresion-consola-global.md), M01.02 [actores e IO](c02-04-m01-02-actores-entradas-salidas-consola-global.md) y M01.03 [inventario](c02-04-m01-03-inventario-fuentes-contratos-consola-global.md) permanecen contratos PRELIMINARES.

## 1. Matriz de riesgos a resolver antes de implementación

| ID | Riesgo, causa y frontera | Control futuro obligatorio | Prueba causal FUTURA |
| --- | --- | --- | --- |
| R01 CRÍTICO | Admin ERP A usa rol empresarial o claim `esAdministrador` para actuar como plataforma | Autoridad global vigente persistida, no derivada de rol empresarial | API deniega 403, cero efecto empresa B |
| R02 CRÍTICO | JWT robado o sesión antigua actúa como operador global | MFA fuerte y reautenticación reciente, ligadas a acción/sesión | Revocación y sesión vencida niegan |
| R03 CRÍTICO | `EmpresaId`/host/header falsificados inducen bypass tenant | Resolver tenant y membresía actual server-side; scope exacto | BOLA/IDOR A→B con HTTP y DB efímera |
| R04 ALTO | Motivo vacío/arbitrario elude responsabilidad | Motivo obligatorio validado, correlación y autorización comprobable | Intento sin motivo se rechaza sin grant |
| R05 CRÍTICO | Grant excepcional sin TTL/expiración o imposible de revocar | Alcance mínimo por acción/tenant/recurso, TTL server-side, revocación inmediata | Expired/revoked no concede, aun con JWT válido |
| R06 CRÍTICO | `IgnoreQueryFilters` en operación global filtra PII o datos ajenos | Allowlist explícita, predicados/segregación y auditoría crítica | Prueba multiempresa A/B de lectura y exportación |
| R07 ALTO | Auditoría falla tras mutación privilegiada | Fail-closed/compensación transaccional cuando corresponda | Falla DB/log obligatoria no confirma acción |
| R08 ALTO | Logs exponen JWT, OTP, contraseña, DSN o PII de terceros | Redacción, minimización, retención y acceso segregado | Inspección de logs/artifacts con secretos sintéticos |
| R09 ALTO | Reintento/replay genera doble permiso o prolonga TTL | Idempotencia por intento, nonce/replay defense, concurrency gate | Peticiones paralelas: único efecto |
| R10 ALTO | Soporte DEV GET se interpreta como privilegio global | Mantener `PlatformSupportPolicy` exact-route + method, no herencia de permisos | POST/PUT/DELETE y rutas alternativas: 403 |
| R11 ALTO | UI Angular oculta controles pero API permite escribir | Validar autorización en API; UI únicamente presentación accesible | E2E + peticiones directas sin interfaz |
| R12 ALTO | Cambio de rol, sucursal o tenant durante sesión excepcional | Reevaluación y revocación server-side en cada acción sensible | Revocación concurrente corta acceso |
| R13 ALTO | Provider MFA no aprobado o indisponible | Fallback fail-closed, sin saltar segundo factor | Timeout/error de MFA: denegación |
| R14 ALTO | Excepción cruza auditoría, caché o retorno asíncrono de B a A | Correlación por tenant/actor, no-store y filtros server-side | Respuesta tardía y caché nunca exponen B |
| R15 MEDIO | Despliegue o rollback parcial deja permiso persistido sin política | Migración aditiva reversible, revocación y rollback dirigidos | Reversión prueba que no quedan grants activos |

**Riesgo residual:** hasta implementar y probar controles reales, MFA y grants temporales siguen **NO_CERTIFICADOS**. Ningún riesgo R01–R15 queda solventado por este documento.

## 2. Supuestos sujetos a validación (NO son autorizaciones)

| ID | Supuesto provisional | Validación obligatoria |
| --- | --- | --- |
| S01 | Roles globales se resuelven mediante `PlataformaScopeService` persistido | Confirmar cobertura en TODOS los endpoints sensibles y pruebas negativas |
| S02 | `ContextoTenantActual` y `UsuarioEmpresa` aportan fronteras A/B | Auditar todos los consumidores/joins/exports y filtros EF |
| S03 | El operador de plataforma requiere MFA/reauth | Definir proveedor aprobado y política de frescura, sin instalar ahora |
| S04 | Auditoría crítica puede garantizar trazabilidad de éxito/denegación | Diseñar transacción, almacenamiento, acceso y retención |
| S05 | El permiso temporal admite TTL corto sin privilegio hereditario | Definir entidad/contrato, revocación, clock skew e idempotencia |
| S06 | Soporte DEV sigue limitado a GET explícitos | Validar rutas y evitar ampliación por wildcard/routing |
| S07 | UI/Angular futura puede mostrar controles accesibles sin decidir permisos | Revisar teclado/foco/código de error y E2E causal |
| S08 | No se requiere cambiar provider Oracle MySQL para modelar la auditoría | Analizar migraciones de EF 10 antes de cualquier decisión técnica |

## 3. Decisiones de diseño / pendientes

| ID | Decisión provisional, no ejecutada | Evidencia/criterio para fijarla |
| --- | --- | --- |
| D01 | Conservar separación fuerte entre autoridad plataforma y rol de ERP | Negative tests con administrador A y actor global legítimo |
| D02 | Privilegios excepcionales deny-by-default, limitados por acción, tenant, recurso | Matriz grants exacta y prueba ABAC/RBAC |
| D03 | MFA y reautenticación son gates de acciones sensibles, no banderas UI | Contrato proveedor MFA, binding a acción y prueba expiración |
| D04 | Motivo obligatorio para cada intento, incluidos rechazos | Auditoría idempotente con correlación y PII minimizada |
| D05 | Revocación inmediata y TTL no extensible desde cliente | Pruebas de concurrencia, replay, retry y token robado |
| D06 | Conservar soporte DEV GET con lista cerrada de rutas | Verificar `PlatformSupportPolicy` y denegar mutaciones |
| D07 | Usar controles de tenant server-side y nunca `EmpresaId`/host del cliente como grant | Casos BOLA/IDOR A/B en MySQL efímero |
| D08 | No aceptar bypass `IgnoreQueryFilters` sin alcance auditado | Guard de autorización y tests queries/exports |
| D09 | Auditoría crítica debe fallar cerrado ante error de persistencia | Fault injection y rollback transaccional en CI |
| D10 | No liberar M01.06 ni M02 por preparación documental | Readback PLAN/COLA/DAG y A5 de otra Task Javier |

## 4. Evidencia real leída y brechas

| Fuente Git observada | Blob SHA | Alcance de evidencia |
| --- | --- | --- |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | `Authorize`, scope plataforma por ruta y auditoría parcial, no MFA |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` | `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | Roles persistidos, no grants TTL |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Soporte DEV solo GET allowlisted |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Tenant derivado de membresía |
| `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` | `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | Bypass de filtros condicionado a ámbito explícito |
| `docs/v5/c02-04-m01-03-inventario-fuentes-contratos-consola-global.md` | `2459fc9e9c7437fb4dc485ad6a82553178d8b9e7` | Inventario documental de hallazgos y ausencias, no prueba live |

No se ejecutaron proveedores MFA, API, MySQL, Angular, migraciones ni tests runtime. Sus requisitos son futuribles, no PASS actuales.

## 5. Clasificación y aceptación de M01.04

**TIPO1 documental:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI` (amenazas JWT/RBAC/tenant revisadas en la matriz), `requiere_scope_lock=SI` (commit documental, exact SHA).

- **A0** estructura de 15 riesgos, 8 supuestos, 10 decisiones; diff solo documental, enlaces a fuentes reales, sin secretos.
- **A1** cada riesgo mapea a verificación observable FUTURA, con positivos/negativos de diseño, no caso HTTP ejecutado.
- **A2** amenazas de escalada plataforma, bypass de tenant, replay MFA, reauth, PII, audit fail-closed y revocación.
- **A3** integración documental con M01.01–03, DAG validado y predecesores; .NET/Angular/MySQL/Docker/E2E **NO_APLICA** al diff.
- **A4** solo tras publicar commit fast-forward dev: SHA exacto, Scope Lock SUCCESS, diff y readback de PLAN/COLA/BITACORA. **A5_PENDIENTE** para revisor Task Javier distinto del autor.

**Reversión:** commit inverso en dev sin force-push, conservación de BITACORA y trazabilidad institucional. No activar permisos ni cambiar production.
