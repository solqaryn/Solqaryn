# SOLQARYN V5 — C15-03.M03.03: autorización, tenancy y denegación por defecto en .NET 10

**Microtarea:** C15-03.M03.03, PLAN_MAESTRO fila 2248, autor PRIMARY DEV 03 JAVIER `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente institucional:** STANDARD V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fecha:** 2026-11-15.

## Alcance real del código publicado

Se implementa `backend/src/Application/AutomationV5/V5TenancySecurityAuthorizationGate.cs`: una frontera ejecutable de **preautorización segura** que recibe un snapshot de evidencia exclusivamente desde `IV5TenancySecurityVerifiedAuthorizationEvidenceReader`, interfaz para un futuro proveedor **confiable del backend**. No acepta como autoridad un identificador de tenant, permiso, recurso o identidad suministrado por el navegador.

La regla exige recurso > 0, superficie válida, SHA hexadecimal de 40 caracteres, autenticación/verificación por servidor, membresía y permiso vigentes, origen de evidencia confiable, tenant y actor consistentes, ID exacto de recurso y ownership derivado de persistencia. Reutiliza `V5TenancySecurityDomainRules.Evaluate` de M03.01 para sucursal, enlace de archivo, grant de job, contexto de control plane y denegación de bypass. Las solicitudes de escritura institucional, enumeración o bypass son rechazadas antes de invocar la fuente.

**Límite crítico:** si todo coincide, el resultado `AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED` sólo pide una integración/autenticación adicional. Todos sus indicadores de acceso a recurso, archivo, job, control plane, write y query bypass son siempre `false`. **No hay endpoint, consulta EF, middleware conectado, rol creado, token concedido ni operación externa** en este cambio. La fuente concreta de autoridad/ownership deberá implementarse y probarse con evidencia independiente en la microtarea de integración permitida. En particular, no corrige `TenantQueryBypassGuard.RespectCurrentTenant` ni transforma su fallback en una certificación de aislamiento: ese riesgo se mantiene abierto.

## Pruebas .NET 10/xUnit incorporadas

Archivo `backend/tests/Solqaryn.Tests/Application/V5TenancySecurityAuthorizationGateTests.cs`, nueve `[Fact]` que cubren:

1. Snapshot coherente tenant A/actor 11/recurso 101 sólo exige ejecución confiable posterior y **no concede** capabilities.
2. Request nula, recurso inválido, superficie no definida y SHA inválido sin consulta a proveedor.
3. Bypass de filtro, enumeración y escritura institucional bloqueados antes de leer evidencia.
4. SHA obsoleto o snapshot ausente denegados sin exposición de información.
5. Revocación, membresía inactiva, permiso ausente, origen no confiable o sesión no autenticada denegados.
6. Tenant B contra A, mismo tenant con recurso ajeno, parent tenant ajeno, ownership no persistido y actor distinto denegados.
7. Sucursal cruzada, link vencido, job sin grant y control-plane sin autoridad denegados.
8. Job con grant auditado e idempotencia no se ejecuta ni recibe permiso de escritura por aprobar preflight.
9. Cancelación y dependencia nula no autorizan acceso ni consultan la fuente.

Los casos son unitarios con fuente sintética, **no prueban HTTP, EF/MySQL, aislamiento end-to-end, JWT real, permisos de producción ni vulnerabilidad explotada**. GitHub Actions .NET backend y controles de seguridad son requeridos sobre el SHA publicado; no reportar PASS hasta `completed/success`. Si alguna prueba queda en curso o falla, conservar VALIDANDO con owner y corregir o reanudar después; no forzar LISTO.

## Dependencias, seguridad, integración futura y rollback

El código reutiliza la fuente `Domain/V5TenancySecurityDomainRules.cs` y no modifica la semántica previa de `V5TenancySecurityApplicationService` (M03.02), autenticación, infraestructura o API. La coexistencia de M03.01+M03.02+A5 fue verificada con PLAN/COLA/DAG antes de reclamar. Para obtener autorización real en una fase posterior debe existir adaptador respaldado por identidad, membresía, privilegios y recurso persistido confiables, integrado a API/EF con negativas A/B y A1/A2, comprobación de revocación, protección contra enumeración y tests MySQL efímeros. Ningún usuario/Task puede convertir esta validación parcial en permiso por sí mismo.

**A0:** revisión de tipos, ownership, fuentes y no-regresión. **A1:** nueve xUnit positivos y negativos en CI real. **A2:** checks de seguridad multiempresa y Authorization/files/tenant pertinentes exact SHA, negativas A/B. **A3:** build/release backend real, sin declarar integración HTTP/EF. **A4:** diff de dos .cs más este Markdown y `CHANGELOG_AI.md`, Scope Lock y procedencia V5 exact SHA, readback y estados institucionales. **A5:** revisión independiente por otro TASK_ID de seis Javier tras LISTO. Rollback normal `git revert` sin force-push; no borrar BITACORA histórica.

No hay cambios en QA/main/PROD, DNS, proveedores reales, secretos, MySQL/Aiven, webhooks, dispatchers ni cuentas.
