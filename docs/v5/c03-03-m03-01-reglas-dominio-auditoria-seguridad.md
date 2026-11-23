# C03-03.M03.01 — Reglas de dominio reales de auditoría de seguridad (V24)

Repositorio exclusivo: solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK STRICT. PLAN D518: implementar reglas de dominio y validaciones server-side, no certificar toda M03/M06.

## Incremento de código
- `backend/src/Domain/Security/C0303SecurityAuditDomainRules.cs` añade un preflight puramente de dominio con catálogo cerrado de acciones/resultados, actor verificado por adaptador, membresía vigente, tenant A/B, owner de recurso leído desde persistencia, revocación, correlación ASCII limitada a 64 caracteres, ámbito plataforma separado y requisito de transacción estricta.
- La salida candidata minimizada no admite payload libre, old/new, JWT, contraseña, cabeceras ni PII. Ni siquiera un resultado positivo otorga permiso de escritura. No ejecuta HTTP, EF, SQL ni registra eventos autónomamente.
- `backend/tests/Solqaryn.Tests/Domain/C0303M0301SecurityAuditDomainRulesTests.cs` incorpora positivos, negativos, fronteras, errores, revocación de roles, A/B y grants de plataforma. Pruebas causales xUnit en GitHub-hosted para el SHA publicado son requisito de A1/A2/A4, no se inventan localmente.

## Impacto y límites
- Domain -> Application/API consumidor futuro; no se modifica DI, DTO, persistencia, migraciones, endpoints ni políticas RBAC en esta microtarea. No se añade autoridad desde headers/cookies. `RegistroAuditoria`, `AuditoriaService`, `AuditoriaController` y EF deben tener integración autenticada real en M03.02–M03.06 y M05, sin equiparar mocks con MySQL 8.4/JWT.
- Riesgo R2 de seguridad, sin efecto de producción. A3 HTTP/MySQL/Chromium NO_APLICA **a este diff solo Domain+xUnit**, no se certifica la fase completa ni se elimina la brecha de sink durable/retención/exportación. La implementación de casos HTTP/JWT/MySQL es una dependencia futura explícita, no un PASS.
- Rollback: `git revert` normal del SHA cuando no haya otro escritor; conservar registros institucionales y revalidar descendientes.
- A5 posterior por Task Javier distinta de DEV06, con examen independiente de código, pruebas, CI exact-SHA y limitaciones. No mover QA/main/PROD ni forzar DAG/COLA.

## Evidencia de publicación
Se registrará SHA exacto y URL CI real en PLAN/BITACORA cuando GitHub confirme publicación. Si CI obligatoria está queued/in_progress, estado VALIDANDO y A5_PENDIENTE, sin LISTO ficticio.
