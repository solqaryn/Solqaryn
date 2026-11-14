# SOLQARYN V5 FINAL — C02-04.M01.01: objetivo y no-regresión de consola global auditada

**Estado de esta entrega:** preparación exclusivamente DOCUMENTAL de M01.01; no implementación ni certificación del módulo completo. **Ejecutor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridades:** [Standard de Implementación V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md` y Scope Lock STRICT. DAG_TECNICO F=VALIDADO/G=SI habilita **solo M01.01–M01.05 documentales**; sus quince prerrequisitos C02-01/02/03 M01.01–05 se observaron LISTO. No se deriva habilitación para M01.06, M02, cambios IAM, migraciones, QA, main o PROD.

## 1. Objetivo observable de C02-04

Diseñar una **consola global auditada**, separada de la administración ordinaria del ERP por empresa y sucursal. En futuras entregas ejecutables, una acción excepcional sobre empresa ajena exigirá: autoridad global server-side vigente, MFA y reautenticación reciente de la identidad operadora, motivo obligatorio verificable, permiso acotado a operación/recurso/tenant, duración corta con expiración irrevocable, registro de auditoría crítica y denegación al faltar cualquier condición. El mecanismo, esquemas y vigencia exacta se diseñan y prueban posteriormente; aquí no se afirma que MFA, sesiones privilegiadas temporales o acceso excepcional ya existan.

| Área | Contrato objetivo futuro | Frontera obligatoria |
| --- | --- | --- |
| Identidad de plataforma | Rol global comprobado en backend mediante autoridad persistida actual | Un JWT válido o rol de ERP nunca confiere facultad global por sí solo |
| Separación de superficies | Consola global y ERP con rutas, scopes y auditorías inequívocos | No reinterpretar `EmpresaId`, host, header o UI como grant de plataforma |
| MFA / reautenticación | Comprobación adicional fuerte, reciente y ligada a la acción sensible | No permitir bypass por sesión antigua, sesión empresarial o claim falsificado |
| Acceso excepcional | Motivo, ticket/correlation ID, entidad, operación y vencimiento explícitos | Deny-by-default, alcance mínimo, revocación inmediata y expiración automática |
| Datos tenant | Consulta/operación extraordinaria expresa, registrada y minimizada | A y B aislados; nunca dump general, PII arbitraria ni escritura cross-tenant implícita |
| Auditoría crítica | Actor real, alcance, motivo, timestamps, resultado y denegaciones sin secretos | Evidencia inmutable/consultable con acceso segregado; no guardar token ni contraseña |
| Recuperación | Revocación, bloqueo y rollback verificables antes de publicar permiso | No hacer cambios irreversibles, migraciones ni accesos live en esta fase |

## 2. No-alcance de M01.01

- No crear ni habilitar endpoint de soporte global, selector privilegiado, rol nuevo, token excepcional, proveedor MFA o bandera comercial.
- No modificar JWT, BCrypt, `UsuarioEmpresa`, RBAC, `AppDbContext`, migraciones, datos de Aiven, Cloudinary, seguridad de producción ni configuración de secretos.
- No declarar la consola global implementada, prueba de denegación HTTP/MySQL/E2E ejecutada ni A5 independiente acreditada. Este documento prepara criterios **futuros**.
- El soporte DEV mediante allowlist de lecturas no autoriza mutación empresarial, `IgnoreQueryFilters` arbitrario ni bypass de controles de tenant.

## 3. Evidencia estática y límites de inferencia (código de dev inspeccionado)

| Fuente | Git blob SHA | Lectura y límite causal |
| --- | --- | --- |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | Ruta `plataforma/empresas`, autorización y scope global comprobado en puntos concretos; borrador no habilita ERP comercial; no prueba MFA excepcional |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` | `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | `ObtenerActualAsync` valida autoridad global contra base; claims del JWT no se aceptan como rol global |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Allowlist de soporte DEV estricta de rutas GET de catálogo; nunca autorización de acceso privilegiado genérico |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto se construye desde membresía efectiva; no desde valores arbitrarios del navegador |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía activa y rol de empresa; no confiere rol global al usuario |
| `docs/v5/c02-03-m01-01-objetivo-ciclo-vida-provisionamiento.md` | `633123fab2f9585377d72de391a2c1f2ea6d764e` | Aprovisionamiento y ciclo empresarial aún tienen contrato preparatorio, no garantía de activación global segura |

Estos SHA identifican archivos observados, **no** una prueba del sistema en ejecución ni certificación end-to-end del comportamiento futuro.

## 4. Criterios de no-regresión falsables para implementación futura

| ID | Escenario (+ positivo / - negativo) | Resultado observable obligatorio |
| --- | --- | --- |
| NR-01 (+) | Operador de plataforma con rol global vigente, MFA reciente, motivo válido y permiso para consulta puntual de A | Solo recurso autorizado de A, auditoría crítica con actor/alcance/resultado |
| NR-02 (+) | Acceso excepcional emitido por periodo breve a recurso de A | Expira en servidor; permiso y reloj inmutables desde cliente; revocable anticipadamente |
| NR-03 (+) | Auditor revisa una denegación de acceso excepcional | Evento identificable sin revelar token, contraseña ni PII de B |
| NR-04 (-) | Administrador ERP de A solicita acción de plataforma o empresa B | HTTP 403; sin lectura, modificación ni escalada de permisos |
| NR-05 (-) | JWT global alegado por claim `esAdministrador` falso o `Usuario.RolId` | Rechazo; consultar rol vigente server-side |
| NR-06 (-) | Operador global con MFA inexistente, fallido o ya vencido | Denegar la acción sensible, registrar causa segura |
| NR-07 (-) | Reautenticación antigua o sesión robada/revocada | Denegar; no reutilizar aprobación anterior |
| NR-08 (-) | Motivo vacío, texto no válido o ticket no verificable | Rechazar sin crear grant temporal |
| NR-09 (-) | Permiso temporal expirado, revocado o para otro recurso/empresa | Rechazar lectura y escritura; ningún fallback amplio |
| NR-10 (-) | Usuario cambia `EmpresaId`, host, sucursal, path, DTO o `X-Empresa-Id` de A a B | Denegar y no filtrar datos de B |
| NR-11 (-) | Actor invoca `IgnoreQueryFilters` fuera de una operación global expresamente habilitada | Rechazar/bloquear el bypass; no divulgar PII |
| NR-12 (-) | Intento de escritura global con credencial solo lectura de soporte DEV | Denegar; cero mutaciones |
| NR-13 (-) | Fallo de MySQL/auditoría durante operación privilegiada | Fallar cerrado o revertir transacción; no ejecutar acción sin rastro |
| NR-14 (-) | Repetición concurrente de una solicitud excepcional o replay de token | Efecto idempotente/único dentro del permiso; no extender expiración ni duplicar grants |
| NR-15 (-) | Token excepcional, motivo o evidencia contiene secretos/PII excesiva | Rechazo/redacción en auditoría y logs; secretos nunca en repositorio |

**Puerta de implementación futura:** pruebas positivas y negativas en API + persistencia real temporal cuando se escriba código, RBAC/tenant A-B y denegaciones observables; tests de expiración y concurrencia; validación de logs/redacción y rollback. Las pruebas propuestas **no se ejecutan** en M01.01.

## 5. Clasificación y aceptación de esta microtarea

- **TIPO 1 documental:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` (revisión documental de límites de autoridad/JWT/RBAC/tenancy, sin prueba live); `requiere_scope_lock=SI` (integridad del commit en dev).
- **A0 estática aplicable:** estructura, fuentes, diff aditivo, ausencia de secretos e instrucciones de bypass, trazabilidad y compatibilidad con arquitectura.
- **A1 funcional documental aplicable:** objetivo, exclusiones y 15 casos NR verificables; se exige consistencia positiva/negativa del contrato, no una API ejecutada.
- **A2 seguridad documental aplicable:** negar credenciales de ERP como autoridad global, MFA/motivo/expiración futuro y no confiar en tenant del cliente.
- **A3 integración documental aplicable:** consistencia con `C02-01`, `C02-02`, `C02-03`, `PlataformaScopeService` y `PlatformSupportPolicy`; .NET/MySQL/Angular/E2E **NO_APLICA** a este diff no ejecutable.
- **A4 posterior:** verificar nuevo SHA publicado, Scope Lock exact-SHA, archivo/diff final, readback PLAN_MAESTRO/COLA/BITACORA. `A5_PENDIENTE` para otro TASK_ID de las seis Javier; este autor no se autoaprueba.

**Riesgo y reversión:** esta entrega cambia solo documentación; rollback mediante commit inverso normal en `dev` y reconciliación auditada de PLAN/COLA/BITACORA sin borrar eventos previos. Si futura implementación encuentra contradicciones, reabrir corrección trazable y no usar este estudio como permiso de despliegue.
