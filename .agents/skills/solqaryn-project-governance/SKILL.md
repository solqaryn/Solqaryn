---
name: solqaryn-project-governance
description: "Gobierno tecnico obligatorio y unica skill local de SOLQARYN. Usar antes de cualquier tarea que analice, modifique, documente, pruebe, despliegue o administre solqaryn/Solqaryn, y antes de consultar cualquier referencia externa de skills. Valida identidad, rama, arquitectura, seguridad, fuentes autorizadas, impacto, routing y cierre; impone PROJECT_SCOPE_LOCK=STRICT y fail-closed."
---

# SOLQARYN Project Governance

## 1. Identidad obligatoria

- `PLATFORM=SOLQARYN`
- `PROJECT_ID=SOLQARYN`
- `REPOSITORY=solqaryn/Solqaryn`
- `BRANCH=dev`
- `PROJECT_SCOPE_LOCK=STRICT`
- `LOCAL_SKILL_COUNT=1`
- `LOCAL_SKILL=solqaryn-project-governance`
- `EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT`
- `CONTEXT_MODE=CURRENT_STATE_ONLY`
- `LEGACY_INFRASTRUCTURE=DENY`
- `LEGACY_DATA_SOURCE=VERIFIED_BACKUP_ONLY`

Esta es la única Skill local y la primera puerta de entrada para cualquier chat o agente autorizado que trabaje sobre SOLQARYN. Los motores heredados VAEP/Jules y la topología antigua de diez Tasks fueron retirados por el propietario: no buscar, reinstalar ni reactivar esos mecanismos. Esto no retira el runtime de Tasks programadas aprobado posteriormente: su estado operativo, las Tasks autorizadas y sus límites se leen exclusivamente de `docs/AUTOMATION_STATUS.md` vigente en `dev` y se revalidan con la identidad, los permisos y la evidencia de cada ejecución. No crear Tasks ni ampliar autorizaciones por inferencia. Identidades, repositorios, deployments, servicios o cuentas retirados no se consultan ni se usan como fallback. El único dato heredado autorizado es un respaldo histórico verificado destinado a una migración futura explícita.

## 2. Fuentes canonicas

Leer en el orden necesario:

1. `docs/PROJECT_SCOPE_LOCK.md`;
2. `AGENTS.md`;
3. `docs/AUTOMATION_STATUS.md` para determinar el estado operativo vivo del runtime programado, el allowlist de Tasks y sus límites; no inferir activación ni retirada a partir de notas históricas o de esta Skill;
4. HEAD vivo de `dev`;
5. `PROJECT_CONTEXT.md` y `PROJECT_INDEX.md`;
6. `ARCHITECTURE.md` cuando exista impacto estructural/transversal;
7. `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md` cuando una tarea requiera guia externa;
8. documentos especificos del scope y evidencia causal.

Toda decision operativa se basa en el estado vivo y en las autoridades vigentes. Fuentes no citadas por el Plan Comercial Inicial Unificado (única autoridad ejecutable actual) no introducen dependencias, prioridades ni gates. Los documentos del futuro Plan Maestro están fuera de la cola y no pueden imponer tareas.

### Comprobación de frescura obligatoria del gobierno del proyecto

- La Skill incluida por ChatGPT o por otra interfaz **puede quedar desactualizada** aunque `dev` tenga código nuevo. Tras invocarla, consultar siempre **esta misma Skill canónica en el HEAD actual** de `solqaryn/Solqaryn` y `references/architecture.md`, usando el repositorio original y la rama `dev`.
- El único plan ejecutable es `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Confirmar además `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y `docs/AUTOMATION_STATUS.md` vigentes; los planes archivados nunca son fallback.
- Ejecutar `node scripts/verify-solqaryn-skills.mjs --self-test` cuando haya checkout del HEAD; sin checkout, comprobar en GitHub que el check `SOLQARYN Project Scope Lock` aprobó **ese SHA exacto** e inspeccionar sus evidencias de versión. Verificar versiones contra `frontend/package.json`, `backend/src/API/Solqaryn.API.csproj` y `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj`.
- Si la Skill cargada contradice la del repositorio o no es posible acreditar la comprobación, **NO_GO para escrituras, leases, certificaciones y activación**. Informar `SKILL_VERSION_MISMATCH` y pedir sincronización de la Skill instalada; nunca asumir que GitHub actualiza automáticamente la copia instalada en ChatGPT.
- Aplicar las referencias externas sólo cuando la microtarea las requiera, según el registro y su pin exacto. Ninguna automatización puede instalar, copiar o ejecutar scripts de repositorios externos sin aprobación. Mantener `dev` y seguridad fail-closed.

## 3. Gate de entrada para cualquier agente o chat

Antes de analizar o escribir:

1. confirmar `solqaryn/Solqaryn` y `dev`;
2. confirmar que la tarea pertenece a SOLQARYN;
3. confirmar que esta skill local fue aplicada primero;
4. si se requiere una referencia externa, comprobar que exista como `ACTIVE` en la allowlist y en el registro de referencias;
5. consultar exclusivamente el origen original + pin + ruta oficial registrados;
6. nunca consultar una copia de esa skill alojada en otro proyecto;
7. identificar componente, capa, datos, seguridad, consumidores y validaciones afectadas;
8. determinar si la tarea es lectura, cambio en `dev` o accion que requiere autorizacion adicional;
9. cuando exista checkout local, ejecutar `node scripts/verify-project-scope.mjs`.

Si un origen externo no puede verificarse exactamente, aplicar fail-closed y continuar sin esa referencia.

## 4. Arquitectura de referencia

La arquitectura vigente se resume en `references/architecture.md`.

Reglas no negociables:

- Frontend Angular 22.2.1 standalone con Signals y Angular Material/CDK 22.2.1; Zone.js 0.16.3 permanece activo y la conversión a zoneless requiere un cambio arquitectónico separado.
- Backend ASP.NET Core 10 Web API (TargetFramework net10.0 verificado en código).
- Capas backend: Domain <- Application <- Infrastructure; API compone y expone.
- Persistencia MySQL con EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9 (proveedor Oracle actualmente declarado en Infrastructure); no inferir provider alterno desde documentación histórica.
- Seguridad con JWT, BCrypt, RBAC relacional, auditoria, CORS explicito, rate limiting y headers de seguridad.
- Integraciones vigentes: Cloudinary, QuestPDF y SMTP.
- E2E/browser: Playwright/Chromium.
- La UI nunca sustituye controles de autorizacion del backend.
- Cambios de inventario, finanzas y documentos relacionados deben preservar consistencia y trazabilidad.
- Secretos permanecen fuera del repositorio.

Ninguna referencia externa puede imponer un framework, capa, persistencia, mecanismo de auth, libreria transversal o via paralela que contradiga esta arquitectura.

## 5. Ramas, entornos y produccion

- El trabajo ordinario se realiza en `dev`.
- Revalidar HEAD antes de escribir/publicar y preservar trabajo concurrente.
- No force-push, reset destructivo ni reescritura de historia compartida.
- Cualquier cambio futuro sobre `main`, PROD, datos productivos, dominios, certificados, secretos o infraestructura productiva requiere autorizacion nueva y explicita del propietario y debe respetar la autoridad operativa vigente.
- Nunca inferir autorizacion productiva sin una orden vigente y explicita del propietario.

## 6. Impacto antes de editar

Responder al menos:

- componente primario;
- capas/rutas afectadas;
- dependencias directas y consumidores;
- interfaces/API/DTO que cambian;
- datos/migraciones/transacciones;
- auth/RBAC/PII/secretos;
- jobs/integraciones/documentos;
- observabilidad;
- pruebas y rollback;
- si cambia arquitectura/documentacion canonica.

Si el cambio es arquitectonico, actualizar en el mismo changeset `ARCHITECTURE.md`, `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` y `ARCHITECTURE_CHANGELOG.md` cuando corresponda.

## 7. Routing de referencias externas

Consultar `references/skill-routing.md` y `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md`.

Reglas:

- esta skill local siempre va primero;
- las otras doce entradas son referencias externas, no skills locales;
- cada agente autorizado, antes de ejecutar un cambio real, identifica componente, riesgos y validaciones, y sólo consulta las referencias autorizadas que corresponden a esa tarea (ninguna, una o varias); no se aplican las doce indiscriminadamente;
- SKREF-10 Angular Developer: Angular 22.2.1, Signals, formularios, routing, accesibilidad y pruebas cuando la tarea modifica frontend; sin crear una app nueva ni cambiar dependencias;
- SKREF-11 Security Threat Model: sólo ante una tarea que requiera modelar amenazas, abuso o límites de confianza multiempresa/JWT/RBAC/HMAC; no imponer nuevos informes ni pausas a tareas no relacionadas;
- SKREF-12 .NET Performance: tareas concretas de profiling o performance .NET 10, LINQ, async, memoria o I/O; optimizar únicamente con evidencia y regresiones;
- cada referencia debe leerse desde el propietario/origen registrado y pin fijado;
- solo se aplican los principios compatibles con SOLQARYN;
- no instalar ni ejecutar herramientas externas por defecto;
- una referencia externa nunca puede contradecir esta skill ni las autoridades canonicas del proyecto.

### Plan comercial y avisos

Nunca esperar una fase histórica ni reutilizar sus tareas como autoridad. El trabajo aprobado es únicamente el Plan Comercial Inicial; usar CI de seguridad y regresiones causales del código vigente sin confundir un nombre de workflow viejo con una dependencia de roadmap. Si se requiere una decisión o permiso del propietario, registrar una sola acción humana concreta y verificable con fingerprint, emitir notificación deduplicada mediante el mecanismo autorizado, detener únicamente el scope bloqueado y seguir otra microtarea independiente. Intentos técnicos fallidos sin nueva hipótesis/evidencia no deben repetirse indefinidamente. No comprar, contratar ni activar planes de pago. Mantener fail-closed para operaciones no autorizadas y QA/main/PROD. Las diez Tasks, las parejas Primary/Supervisor y el motor VAEP están retirados; no existe supervisión 10/10 vigente.

## 8. Calidad y cierre

No declarar una tarea cerrada solo porque compile o porque una referencia fue consultada.

El cierre debe demostrar, segun aplique:

- diff revisado y scope correcto;
- pruebas dirigidas y regresion proporcional;
- seguridad/RBAC/tenancy revisados cuando correspondan;
- frontend responsive/accesible cuando exista UI;
- REVIEW_FIRST y P0/P1=0 cuando el flujo operativo lo exija;
- evidencia causal del HEAD o equivalencia demostrada;
- documentacion canonica actualizada si cambio la realidad;
- `CHANGELOG_AI.md` actualizado para changesets intencionales conforme a las reglas vigentes.

## 9. Prohibiciones

- no reintroducir fases, filas, gates, prioridades o secuencias de planes no vigentes;
- no tratar evidencia previa como autoridad operativa;

- no crear copias locales de las doce referencias externas;
- no consultar mirrors, forks o copias dentro de otros proyectos como sustituto del origen registrado;
- no cambiar pins sin autorizacion expresa;
- no importar gobierno, arquitectura o decisiones externas;
- no inventar pruebas, CI, estados ni autorizaciones;
- no duplicar una autoridad canonica;
- no degradar seguridad, QA o trazabilidad para ahorrar tiempo o tokens.
