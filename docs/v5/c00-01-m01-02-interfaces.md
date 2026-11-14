# C00-01.M01.02 — Entradas, salidas, actores y superficies

**Proyecto**: `solqaryn/Solqaryn` · **Rama**: `dev` · **Contexto**: `PROJECT_SCOPE_LOCK=STRICT`.
**Microtarea V5 FINAL**: identificar entradas, salidas, actores y superficies afectadas dentro del baseline `C00-01.M01`. Este inventario es documental; no activa conectores, modifica datos ni confirma despliegues.

## Inventario de contratos y actores

| Actor / frontera | Entrada o autoridad comprobable | Salida esperada | Superficie / capa | Regla de confianza y prueba de regresión |
| --- | --- | --- | --- | --- |
| Propietario de V5 | Documento Google Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, versión V5 FINAL | Requisito y dependencia de ID `C00-01.M01.02` | Plan Comercial Inicial | Nunca convertir texto aspiracional en despliegue o permiso vigente |
| Task Javier :10 | TASK_ID `6af16bb9492c8191b8e8c7d606f11507` en allowlist y Automations.peek | Evidencia A0–A4; A5_PENDIENTE para otro Scope | Scheduled Tasks y trazabilidad | Verificar permiso nativo; no tocar Tasks Alex |
| Repositorio autorizado | GitHub `solqaryn/Solqaryn` rama `dev`, Skill canónica y Scope Lock | Commit directo fast-forward y SHA posterior | Código, pruebas y CI exact HEAD | No PR, rama nueva, force, qa/main/PROD ni recuperación de V3 |
| Seguimiento operativo | Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` y `PLAN_MAESTRO!H` | Estado y EVENT_ID único en BITACORA; COLA fórmula | PLAN_MAESTRO/COLA/BITACORA | No escribir COLA E/G/O; no inventar LISTO o fórmulas |
| Navegador público | Host, ruta y formularios públicos tipados | UI accesible; request explícito a API | Angular 22.2.1 / frontend | Host, slug, EmpresaId o `X-Empresa-Id` no dan permisos |
| Usuario autenticado | Credenciales y JWT verificado por backend | Respuesta autorizada / denegación sin fuga | ASP.NET Core 10 API; guards Angular | JWT/BCrypt/RBAC y pertenencia tenant A/B se imponen en servidor |
| Casos de uso de aplicación | DTO validado y contexto tenant autorizado | Resultado de negocio o fallo seguro | Application / Domain | Autoridad nunca deriva de un campo controlado por cliente |
| Repositorio de datos | Comando autorizado; UnitOfWork/EF Core | Lectura o commit transaccional trazable | Infrastructure; MySQL EF Core 10 / Oracle | Tenant A/B aislados; migraciones con rollback separado |
| Medios y documentos | Referencia a archivo del tenant autorizado | URL temporal / PDF / archivo protegido | Cloudinary/QuestPDF; R2 solo objetivo V5 | Inventario histórico de imágenes cero: migración NO_APLICA |
| CI y QA independientes | SHA Git y pruebas proporcionales A0–A4 | A5_PASS o A5_FAIL por Scope distinto | GitHub Actions y revisión | A5_PASS no autoriza por sí solo QA/main/PROD |

## Entradas documentales y salidas del baseline

- **Entradas primarias:** Google Doc V5 FINAL, Sheet operativo, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md`, `docs/AUTOMATION_V13.md`, `.agents/skills/solqaryn-project-governance/SKILL.md`, `references/architecture.md`, `frontend/package.json`, proyectos .NET y código presente en `dev`.
- **Salidas de esta microtarea:** esta matriz versionada, una prueba Node positiva y negativa de sus límites, entrada aditiva en `CHANGELOG_AI.md`, y los readbacks de estado/auditoría en fuentes institucionales. No produce UI, endpoint, esquema ni entidad nuevos.
- **Control de conflicto:** se deriva del inventario anterior `docs/v5/c00-01-m01-01-scope.md`; si fuentes vivas difieren, se debe actualizar la matriz en un cambio posterior con nueva evidencia, no asumir paridad. Documento V5 describe Hostinger y Cloudflare como objetivo, frente a Vercel, Render, Aiven y Cloudinary documentados en la infraestructura actual; no es un cutover.

## No-regresión y rollback

- No alterar JWT/BCrypt/RBAC, `X-Empresa-Id`, permisos tenant A/B, secretos, migraciones, datos ni deploys. Las pruebas sintéticas del contrato son documentales; **no prueban HTTP/MySQL/Playwright ni disponibilidad real**.
- Validar que toda fila contiene un actor, entrada, salida, superficie y regla de confianza. Cambios o borrados de límites críticos deben fallar cerrado.
- Si el documento necesita corrección, usar commit aditivo y pruebas. Para revertir este changeset de documentación/pruebas, `git revert` normal del commit puntual después de verificar HEAD y el impacto concurrente; nunca reset/force. Preservar BITACORA apendicular y las seis Tasks Javier.

**Alcance certificado:** inventario de superficies, no implementación o promoción. Auditorías A0–A4 y readback pueden certificar solo esta microtarea; revisión A5 independiente queda pendiente para promoción.
