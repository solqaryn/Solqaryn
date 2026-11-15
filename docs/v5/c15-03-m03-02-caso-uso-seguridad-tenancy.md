# SOLQARYN V5 FINAL — C15-03.M03.02: caso de uso Application de seguridad y tenancy

**Ámbito:** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT.
**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09.
**Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN_MAESTRO/COLA/DAG_TECNICO por ID exacto y Skill canónica de dev.

## Cambio de código ejecutable y fronteras

Se agrega backend/src/Application/AutomationV5/V5TenancySecurityApplicationService.cs, compuesto sobre la regla Domain V5TenancySecurityDomainRules de M03.01. La aplicación obtiene autoridad autenticada exclusivamente de IV5TenancySecurityAuthoritySource y propiedad persistida de IV5TenancySecurityResourceSource, pasando un tenant de backend verificado a la lectura scoped. La API de la segunda interfaz obliga por contrato a retener filtros tenant y negar fallback no acotado. No se construye autoridad desde host, URL, cabecera, cuerpo, cookie, slug ni tenant aportado por cliente.

Un preflight sin SHA de procedencia coherente, prueba saneada, identidad/rol/membresía activos o recurso con pertenencia verificada retorna denegación. Bypass de filtros y enumeración cruzada se rechazan antes de cualquier lectura. Domain niega propietario/parent tenant B o nulos, sucursal ajena, archivo revocado/caducado, job sin grant+auditoría+idempotencia y control plane sin permiso independiente. El resultado positivo únicamente exige un adaptador autenticado adicional: ninguno de sus indicadores concede acceso a datos, descarga, job, mutación o privilegio.

## Pruebas y riesgo causal

Pruebas xUnit en backend/tests/Solqaryn.Tests/Application/V5TenancySecurityApplicationServiceTests.cs: casos positivos y negativos para SHA, prueba, identidad, membresía, permisos, tenant A/B, parent/propiedad, filtro/enumeración, sucursal, archivo, job e idempotencia, control plane y constructores. CI causal .NET 10 backend, seguridad multiempresa, Scope Lock exact-SHA y procedencia V5 requerida antes de LISTO_DEV. Un job queued/in_progress, cancelled, skipped o failed no acredita PASS. A5 sólo por otra Task Javier distinta del autor.

**Limitación explícita:** interfaz y servicio Application reales, pero aún sin implementación confiable de adaptadores, sin DI activa ni endpoint HTTP, sin consultas EF, sin acceso MySQL ni ejecución de archivos/jobs/control plane. La observación previa TenantQueryBypassGuard.RespectCurrentTenant cuando AuthorizedReadEmpresaId es nulo sigue ABIERTA: este servicio no repara ni certifica esa ruta Infrastructure. Requiere tarea autorizada para integración y negativos HTTP/EF/MySQL 8.4 con tenants A/B, incluido contexto nulo. No se declara aislamiento multiempresa end-to-end ni seguridad del despliegue por esta entrega.

## A0–A5, rollback y exclusiones

A0: diff y dependencias Domain/Application; sin alterar arquitectura principal. A1: XUnit dirigido positivo y negativo; A2: vectores de seguridad A/B y fail-closed; A3: compilación/test backend real en GitHub Actions; A4: SHA exacto, Scope Lock, procedencia, publicación fast-forward y readback PLAN/COLA/DAG/BITACORA antes de LISTO_DEV. A5: revisión independiente posterior con SHA original. Rollback: git revert normal y revalidación de controles, sin force-push ni edición de historial.

No se modifica infraestructura, seguridad de APIs existentes, JWT/RBAC activo, EF, migraciones, MySQL, secretos, frontend, DNS, QA/main/PROD ni programaciones de Tasks.
