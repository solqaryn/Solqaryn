# SOLQARYN V5 FINAL — C02-04.M01.05: aceptación observable de consola global auditada

**Unidad:** M01.05, preparación exclusivamente DOCUMENTAL de consola global C02-04. **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridades: [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill y código real. DAG F=VALIDADO/G=SI y quince predecesores C02-01/02/03 M01.01–05 LISTO; no extiende esta autorización a M01.06, implementación IAM ni QA/main/PROD.

**Salida contractual futura de C02-04:** separar plataforma y ERP con MFA, reautenticación, motivo y acceso excepcional temporal; evidencia mínima real posterior de matriz de grants, auditoría crítica y pruebas de denegación. **La entrega presente únicamente redacta criterios falsables futuros; no ejecuta 24 pruebas**, no desarrolla funcionalidades ni asegura que MFA, TTL o consola global existan.

## 1. Tabla de aceptación funcional y de seguridad (FUTURA)

| ID | Tipo | Estímulo/precondición futura | Oráculo observable requerido |
| --- | --- | --- | --- |
| AC01 | POS | Actor plataforma con rol persistido vigente consulta catálogo global autorizado | API 200 con campos mínimos, paginación y auditoría con correlación |
| AC02 | POS | Actor plataforma autorizado solicita acción excepcional con MFA/reauth recientes, motivo y scope A | Autorización solo operación/recurso/empresa A; log de quién/qué/por qué |
| AC03 | POS | Auditor segregado lee un evento de acceso concedido | Evidencia de actor, instante, motivo sanitizado, recurso y resultado, sin secretos |
| AC04 | POS | Auditor segregado lee una denegación en tenant B | Registro visible bajo permiso de auditoría, ninguna facultad de mutación |
| AC05 | POS | Actor revoca grant temporal existente | Nueva solicitud con el token anterior se deniega sin reabrir TTL |
| AC06 | POS | Operación autorizada sobre A completa con auditoría transaccional | Un solo efecto empresarial correcto y evento idempotente íntegro |
| AC07 | NEG | Administrador empresarial A invoca consola global para B | 403, cero datos de B y cero cambio de permiso |
| AC08 | NEG | JWT manipulado o claim `esAdministrador` / `Usuario.RolId` falso | Denegación sin tratar claims como rol global |
| AC09 | NEG | MFA ausente, falla, no ligado a acción o token OTP usado anteriormente | 401/403, no se crea permiso excepcional |
| AC10 | NEG | Reautenticación anterior a ventana exigida o sesión revocada | Denegar antes de acceder a datos/mutación |
| AC11 | NEG | Motivo vacío, excesivo, malformado o ticket no válido | Error validado sin grant ni efecto |
| AC12 | NEG | Token/grant temporal expirado, revocado o para otro scope | 403, no se extiende TTL en servidor |
| AC13 | NEG | `EmpresaId`/host/`X-Empresa-Id` manipulado A→B | Sin lectura/actualización de B; filtros server-side |
| AC14 | NEG | Operador sin permiso de mutación intenta escritura global | 403, cero transacciones |
| AC15 | NEG | Soporte DEV GET solicita POST/PUT/DELETE o ruta no permitida | 403, `PlatformSupportPolicy` no se interpreta como grant universal |
| AC16 | NEG | `IgnoreQueryFilters` no auditado, raw query o export amplia | Guardias obligatorios y cero filas ajenas |
| AC17 | NEG | Auditoría crítica o MySQL falla al persistir operación sensible | Fallo cerrado y rollback/compensación, sin éxito ficticio |
| AC18 | NEG | Repetición concurrente de solicitud excepcional con misma clave | Único efecto; no duplicar grants/logs críticos |
| AC19 | NEG | Elevación solicitada durante cambio/revocación de rol global | Reevaluación actual y denegación inmediata |
| AC20 | NEG | Tenant B responde tardíamente mientras UI cambia a A | Cancelar/filtrar, nunca presentar datos B en A |
| AC21 | NEG | Cache/CDN conserva respuesta privilegiada o falta `no-store` | No se reutilizan datos globales entre usuarios/tenants |
| AC22 | NEG | Logs y artifacts contienen JWT, contraseña, OTP, DSN o PII excesiva | Sanitización verificable, sin exfiltración ni secretos |
| AC23 | NEG | Proveedor MFA no disponible o timeout | Denegación, no fallback a simple sesión |
| AC24 | NEG | Rollback de grant/migración falla a mitad | Estado consistente o rollback controlado sin privilegio huérfano |

**Oráculo de severidad:** AC07–AC24 son denegaciones y comprobaciones fail-closed. Cualquier acceso indebido interempresa, concesión sin MFA/motivo/TTL, o acción privilegiada sin auditoría debe bloquear implementación/promoción. La matriz **no** implica que el workflow actual ejecute estos casos.

## 2. Relación con superficies de ejecución real a probar después

| Superficie | Evidencia exigida en futura implementación | CI/runner realmente requerido entonces |
| --- | --- | --- |
| `PlataformaEmpresasController`, `PlataformaScopeService` | HTTP positivo/negativo de rol global persistido, no desde claims | .NET 10 build/test y DB temporal cuando sean tests EF |
| `ContextoTenantActual`, `UsuarioEmpresa`, filtros API | Negación A/B BOLA/IDOR y pertenencia real | .NET/MySQL 8.4; test cross-tenant real |
| Mecanismo MFA/reauth FUTURO | Prueba vinculada a acción, frescura, revocación, replay y error de proveedor | Unit/integration/contract tests propios; proveedor real solo si autorizado |
| Grants excepcionales TTL FUTUROS | Actor, motivo, entidad, scope, expiración, idempotencia y rollback | MySQL efímero, transacciones, reloj controlado |
| Consola Angular FUTURA | Ruta, teclado, foco, contraste, error 403, no mostrar B en A | npm/lint/build; Playwright/Chromium si se implementa UI |
| Registro de auditoría crítica FUTURO | Evento éxito/fallo, retención y redacción sin PII, fallo DB fail-closed | Pruebas de integración EF/MySQL y análisis de logs, sin usar PROD |
| Soporte DEV `PlatformSupportPolicy` | GET permitidos exactos y rechazo de escritura/rutas alternativas | Test de autorización backend negativo |

**Infraestructura de pruebas:** GitHub Actions efímero, sobre SHA exacto nuevo cuando exista código. Un job SKIPPED/CANCELLED/PENDING nunca prueba un requisito. No es condición para este diff documental compilar .NET/Angular/MySQL que no cambia.

## 3. Trazabilidad de fuentes existentes (inspección estática)

| Fuente real leída | Git blob SHA de referencia | Límite |
| --- | --- | --- |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | Scope global y auditoría en rutas concretas, no consola completa |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` | `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | Rol global server-side, no MFA |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Allowlist GET DEV, no mutación |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Membresía tenant, no grant global |
| `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` | `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | Guard de lectura, no cobertura total |
| `docs/v5/c02-04-m01-04-riesgos-supuestos-decisiones-consola-global.md` | `4ea99399da5db5ce20d849893cd75972fb67e8b4` | R15/S8/D10 riesgos y decisiones, NO implementadas |

M01.01–M01.04 describen objetivos, interfaces, evidencia y riesgos para que esta matriz de aceptación tenga trazabilidad. Antes de implementar, releer blobs actuales, revisar toda API/DTO/DB y consumidores, no aceptar documentación como PASS runtime.

## 4. Cierre documental, auditorías A0–A4 y revisión independiente

**TIPO 1:** `requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI` revisión documental de JWT/RBAC/tenant, claims, MFA, revocación y auditoría; `requiere_scope_lock=SI` para integridad del commit.

- **A0**: diff documental, vínculos y seis hashes de fuentes, ausencia de secretos, 24 criterios sin duplicados.
- **A1**: criterios observables 6 positivos / 18 negativos FUTUROS; un AC ejecutable requiere probarse realmente en su entrega futura.
- **A2**: aislamiento de empresa A/B, denegación por defecto, MFA/reauth, privilegio mínimo/TTL, log y PII.
- **A3**: encadenamiento a documentos M01.01–04 y DAG VALIDADO con 15 prerequisitos LISTO; compilación/test de consola no aplica a M01.05.
- **A4**: después de commit directo dev, verificar SHA/Scope Lock SUCCESS exacto, diff y readback PLAN/COLA/BITACORA.
- **A5**: dejar `A5_PENDIENTE` para otra Task Javier distinta de la autora. Un PASS documental no habilita QA/main/PROD ni M01.06/M02 automáticamente.

**Rollback y seguridad:** commit inverso normal sin force-push y notas institucionales aditivas; no borrar BITACORA, no modificar secretos ni usuarios ni proveedor MFA, y no introducir acceso a Aiven o producción.
