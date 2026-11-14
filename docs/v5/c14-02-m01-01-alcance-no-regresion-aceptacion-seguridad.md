# SOLQARYN V5 FINAL — C14-02.M01.01: alcance y no-regresión de aceptación de seguridad

**Unidad:** C14-02.M01.01 — fijar objetivo, no-alcance y criterio de no-regresión. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** preparación documental M01; no es una certificación de seguridad ni un release.

**Autoridad:** [Standard de Implementación V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) y gobierno vigente `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `.agents/skills/solqaryn-project-governance/SKILL.md`. En el preflight, PLAN fila 2054 estaba PENDIENTE sin owner; COLA G/O=SI y DAG_TECNICO fila 2015 F=VALIDADO/G=SI. El gate habilita **sólo** la preparación documental individual; no libera fases terminales ni autoriza QA, main o PROD.

## 1. Objetivo y resultado observable futuro

Definir un programa reproducible de aceptación de seguridad del SaaS multitenant que compruebe, mediante evidencia real sobre un SHA exacto, que usuarios, sesiones, recursos, APIs, archivos, jobs, cachés, backups e integraciones mantienen límites de autorización y propiedad por tenant, incluso ante entradas maliciosas y fallos parciales. La meta del lote C14-02 es abordar BOLA/IDOR, spoofing de host/tenant, exposición de archivos, procesamiento asíncrono, contaminación de caché, recuperación/backup y sesiones. Su evidencia mínima final exige **cero P0/P1 sin mitigación aprobada**; este documento no afirma que dicho criterio se haya medido ni satisfecho.

Una futura prueba de aceptación debe registrar: `CASE_ID`, actor/rol, tenant y recurso propio/ajeno, precondición, request u operación, resultado esperado, resultado observado, SHA de frontend/backend/infra, CI job/run y evidencia redactada sin secretos. Cualquier hallazgo P0/P1 se conserva abierto y bloquea la liberación de seguridad hasta mitigación verificable e independiente. No se falsificará un PASS por enumerar casos o por ejecutar un check no causal.

## 2. Estado conocido, superficies y fronteras

- El frontend es Angular 22.2.1 y su navegación/guards ofrecen experiencia, **nunca autorización final**.
- La API usa ASP.NET Core net10.0, JWT Bearer y validaciones del backend; las capas son Domain, Application, Infrastructure y API. La persistencia actual declarada es EF Core 10 con proveedor MySQL Oracle. No se sustituye el proveedor ni se introduce una migración.
- Los servicios de Cloudinary, QuestPDF y SMTP son integraciones existentes documentadas; su seguridad y permisos reales por ambiente requieren evidencia propia. No se asumen R2/Hostinger activos ni se consultan datos productivos.
- Las fronteras fundamentales son navegador→API, JWT→identidad/roles, tenant A→tenant B, request→repositorio/DB, API→archivo/servicio externo, trabajo síncrono→job, caché→contexto de tenant y copia de seguridad→restauración autorizada.
- El ámbito C14-02 consume criterios de C03 (seguridad/privacidad), C10 (archivos) y C12 (arquitectura/operación). Las dependencias de fase en el plan son referencias de preparación: no son permiso de ejecutar producción ni de aceptar módulos no implementados.

## 3. Invariantes NR01–NR18 a preservar durante el desarrollo futuro

| ID | Contrato de no-regresión | Caso negativo que impide aceptación |
| --- | --- | --- |
| NR01 | Todo recurso se consulta y modifica con autorización de usuario **y** pertenencia a tenant/sucursal aplicable | JWT válido de A lee/modifica ID de B |
| NR02 | El backend rechaza BOLA/IDOR aunque el cliente manipule IDs, rutas, cuerpo o query | UI oculta opción, pero API expone el recurso |
| NR03 | Roles/permisos se verifican en servidor y permanecen efectivos tras revocación | Token/rol obsoleto permite acción privilegiada |
| NR04 | El contexto de empresa proviene de fuente de identidad confiable y validada | Header/host/body inventado suplanta tenant |
| NR05 | El host público y CORS mantienen allowlist y tratamiento de orígenes inválidos | Host spoofing cambia callback, tenant o URL absoluta |
| NR06 | Sesiones y refresh/revocación no prolongan privilegios fuera de su vigencia | Logout, usuario bloqueado o rotación no invalidan acceso esperado |
| NR07 | Rate limiting y respuestas de error no revelan tokens, datos ni enumeración sensible | 401/403/404 expone existencia/datos de otro tenant |
| NR08 | Upload, descarga y URL firmada requieren ownership, tipo, tamaño y TTL apropiados | Objeto de B se obtiene con ID/link de A o TTL vencido |
| NR09 | Documentos exportados/PDF heredan permisos de la fuente y no mezclan datos | Exportación incluye registros ajenos o PII no autorizada |
| NR10 | Jobs/colas reproducen identidad y tenant con autorización antes de efecto | Trabajo diferido reusa contexto de otro tenant |
| NR11 | Reintentos/idempotencia preservan propiedad y no duplican efectos sensibles | Replay produce doble operación o escalamiento de acceso |
| NR12 | Cache keys y invalidación separan tenant, permisos y datos sensibles | Resultado/cache de A servido a B o usuario revocado |
| NR13 | Backups, restauraciones y archivos históricos preservan permisos, cifrado y trazabilidad | Restore mezcla datos de empresas o replica secretos |
| NR14 | Auditoría incluye identidad/correlación suficientes sin guardar credenciales/PII innecesaria | Log contiene JWT, contraseña o URL secreta completa |
| NR15 | Timeout/fallo del proveedor externo es fail-closed y no autoriza bypass | Fallo SMTP/Cloudinary/rate limiter convierte 403 en 200 |
| NR16 | Pruebas negativas usan tenants A/B aislados y fixtures no productivos | E2E usa datos reales, rompe integridad o no comprueba B |
| NR17 | CI causal obligatoria es `completed/success` sobre el SHA publicado | queued/in_progress/skipped/cancelled se cuenta como PASS |
| NR18 | La aprobación de dev y A5 independiente no promueven automáticamente QA/main/PROD | La documentación o el check de Scope Lock se trata como autorización de release |

## 4. Casos de prueba futuros — definidos, NO ejecutados

| Caso | Positivo esperado | Negativo exigido |
| --- | --- | --- |
| SEC-01 BOLA/IDOR | Usuario A opera recurso A permitido | A intenta ID de B y recibe rechazo sin fuga |
| SEC-02 RBAC | Rol habilitado ejecuta acción permitida | Rol retirado o limitado no accede |
| SEC-03 Host/tenant | Host/identidad válida resuelve contexto correcto | Host/header manipulado no cambia tenant |
| SEC-04 Sesión | JWT válido dentro de política accede | Token expirado/revocado falla |
| SEC-05 Archivos | Owner autorizado obtiene archivo propio | URL ajena/caducada falla |
| SEC-06 Job y replay | Job autorizado e idempotente causa efecto único | Replay/cambio de tenant no duplica ni escala |
| SEC-07 Caché | Clave correcta devuelve sólo datos propios | Permisos/tenant cambian y no reciben caché ajena |
| SEC-08 Backup/restore | Fixture autorizado conserva pertenencia | Restauración de fixture cruzado se rechaza |
| SEC-09 Errores | Error controlado deja auditoría suficiente | Error no publica JWT, secretos, PII ni stack privado |
| SEC-10 CI | Check de seguridad pertinente al SHA termina SUCCESS | Cualquier obligatorio pendiente, cancelado o fallido bloquea cierre |

Los diez pares son **oráculos propuestos** y no resultados de ejecución. Los casos que impliquen MySQL, red, secretos, proveedores o datos reales necesitarán entorno efímero o autorización independiente de su propio scope.

## 5. Exclusiones y controles de salida

**No incluido en M01.01:** implementar endpoints, middleware, claims, RBAC/ABAC, workers, colas, dispatcher de eventos, webhooks, cache, migraciones, cambio de proveedor, R2, Hostinger, reglas DNS, compra de servicios, lectura de datos productivos, tests E2E/HTTP ejecutados, cierre de hallazgos P0/P1, certificación WCAG, publicación QA/main/PROD. Ningún hallazgo se considera resuelto por este documento.

**Secuencia:** M01.02 mapeará actores/superficies; M01.03 inspeccionará código y pruebas reales; M01.04 registrará riesgos y supuestos; M01.05 fijará aceptación mensurable; M01.06 deberá validar su gate terminal sin adelantarse. M02–M06 tienen sus propios requisitos DAG, A0–A4, CI causal, A5 independiente y permisos.

**Criterio de aceptación documental de esta unidad:** sección de objetivo, límites, fronteras, NR01–NR18 y SEC-01–SEC-10 íntegros; no afirmaciones de runtime aprobado; autoridad documental trazable; cambios limitados a este Markdown y `CHANGELOG_AI.md`; Scope Lock y verificador V5 de procedencia completados en el SHA exacto. A0–A3 son revisión documental de estructura, coherencia, casos positivos/negativos definidos y seguridad conceptual; A4 exige GitHub diff/HEAD/CI y readback institucional. A5 queda pendiente de una Task Javier distinta.

**Rollback de este artefacto:** `git revert` normal del commit dev publicado, con revisión de efectos concurrentes y sin reescribir historia. No modifica estado histórico de BITACORA ni borra una revisión A5.
