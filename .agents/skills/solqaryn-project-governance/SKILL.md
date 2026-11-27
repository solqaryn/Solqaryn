---
name: solqaryn-project-governance
description: "Gobierno tecnico obligatorio y unica skill local de SOLQARYN. Usar antes de cualquier tarea que analice, modifique, documente, pruebe, despliegue o administre solqaryn/Solqaryn, y antes de consultar cualquier referencia externa de skills. Valida identidad, rama, arquitectura, seguridad, fuentes autorizadas, impacto, routing y cierre; impone PROJECT_SCOPE_LOCK=STRICT y fail-closed."
---

# SOLQARYN Project Governance

## 1. Identidad obligatoria

- `PLATFORM=SOLQARYN`
- `PROJECT_ID=SOLQARYN`
- `REPOSITORY=solqaryn/Solqaryn`
- `BRANCH=dev` para automatizaciones y trabajo ordinario DEV.
- `BRANCH=qa` para intervenciones manuales QA con autorización explícita del propietario.
- `PROJECT_SCOPE_LOCK=STRICT`
- `LOCAL_SKILL_COUNT=1`
- `LOCAL_SKILL=solqaryn-project-governance`
- `EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT`
- `CONTEXT_MODE=CURRENT_STATE_ONLY`
- `LEGACY_INFRASTRUCTURE=DENY`
- `LEGACY_DATA_SOURCE=VERIFIED_BACKUP_ONLY`

Esta es la única Skill local y la primera puerta de entrada para cualquier chat o agente autorizado que trabaje sobre SOLQARYN. Los motores heredados VAEP/Jules y la topología antigua de diez Tasks fueron retirados por el propietario: no buscar, reinstalar ni reactivar esos mecanismos. Esto no retira el runtime de Tasks programadas aprobado posteriormente: su estado operativo, las Tasks autorizadas y sus límites se leen exclusivamente de `docs/AUTOMATION_STATUS.md` vigente en `dev` y se revalidan con la identidad, los permisos y la evidencia de cada ejecución. No crear Tasks ni ampliar autorizaciones por inferencia. Identidades, repositorios, deployments, servicios o cuentas retirados no se consultan ni se usan como fallback. El único dato heredado autorizado es un respaldo histórico verificado destinado a una migración futura explícita.

### Separación obligatoria de modos operativos

- `DEV_AUTOMATION`: aplicar íntegramente las reglas, cola, leases, R1, plan comercial, comprobación de frescura y Scope Lock de DEV. Ninguna orden QA modifica esas reglas ni autoriza a una automatización a salir de DEV.
- `QA_MANUAL_OWNER`: ejecutar únicamente en `solqaryn/Solqaryn`, rama `qa`, cuando el propietario lo ordene expresamente. Usar exclusivamente las cuentas personales y servicios QA identificados y autorizados; no usar MAIN, producción ni otro workspace por inferencia. La autorización manual QA no altera el runtime de automatizaciones DEV.
- No usar el estado de un workflow, una microtarea o el Scope Lock de DEV como prerrequisito operativo para corregir, probar o desplegar QA manual. Conservar `PROJECT_SCOPE_LOCK=STRICT` en ambos modos y todos los controles de identidad, seguridad, trazabilidad, datos y CI propios de la rama objetivo. No sustituir pruebas de QA por evidencias de DEV ni viceversa.
- Una intervención QA puede corregir los fallos de CI de su propio HEAD: no exigir que los tests funcionales que se van a reparar estén verdes antes de publicar la corrección. Exigir identidad, procedencia, alcance y control de seguridad propios de QA; no aplicar migraciones ni certificar un despliegue con pruebas causales pendientes o fallidas.
- Si una autorización no identifica el entorno, conservar el modo ordinario y no realizar operaciones de otro entorno por inferencia. Un cambio de gobierno requiere autorización explícita y sincronización de la versión canónica y la instalada antes de entrar en vigor. No aplicar un borrador de esta Skill como excepción al gobierno vigente.

## 2. Fuentes canonicas

Leer en el orden necesario:

1. `docs/PROJECT_SCOPE_LOCK.md`;
2. `AGENTS.md`;
3. `docs/AUTOMATION_STATUS.md` para determinar el estado operativo vivo del runtime programado, el allowlist de Tasks y sus límites; no inferir activación ni retirada a partir de notas históricas o de esta Skill;
4. HEAD vivo de la rama objetivo; consultar `dev` como origen de gobierno y estado de las automatizaciones, y `qa` como origen operativo de una intervención manual QA autorizada;
5. `PROJECT_CONTEXT.md` y `PROJECT_INDEX.md`;
6. `ARCHITECTURE.md` cuando exista impacto estructural/transversal;
7. `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md` cuando una tarea requiera guia externa;
8. documentos especificos del scope y evidencia causal.

Toda decision operativa se basa en el estado vivo y en las autoridades vigentes. Fuentes no citadas por el Plan Comercial Inicial Unificado (única autoridad ejecutable actual) no introducen dependencias, prioridades ni gates. Los documentos del futuro Plan Maestro están fuera de la cola y no pueden imponer tareas.

### Comprobación de frescura obligatoria del gobierno del proyecto

Para `DEV_AUTOMATION` conservar las comprobaciones siguientes sin excepciones:

- La Skill incluida por ChatGPT o por otra interfaz **puede quedar desactualizada** aunque `dev` tenga código nuevo. Tras invocarla, consultar siempre **esta misma Skill canónica en el HEAD actual** de `solqaryn/Solqaryn` y `references/architecture.md`, usando el repositorio original y la rama `dev`.
- El único plan ejecutable es `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Confirmar además `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y `docs/AUTOMATION_STATUS.md` vigentes; los planes archivados nunca son fallback.
- Ejecutar `node scripts/verify-solqaryn-skills.mjs --self-test` cuando haya checkout del HEAD; sin checkout, comprobar en GitHub que el check `SOLQARYN Project Scope Lock` aprobó **ese SHA exacto** e inspeccionar sus evidencias de versión. Verificar versiones contra `frontend/package.json`, `backend/src/API/Solqaryn.API.csproj` y `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj`.
- Si la Skill cargada contradice la del repositorio o no es posible acreditar la comprobación, **NO_GO para escrituras, leases, certificaciones y activación**. Informar `SKILL_VERSION_MISMATCH` y pedir sincronización de la Skill instalada; nunca asumir que GitHub actualiza automáticamente la copia instalada en ChatGPT.
- Aplicar las referencias externas sólo cuando la microtarea las requiera, según el registro y su pin exacto. Ninguna automatización puede instalar, copiar o ejecutar scripts de repositorios externos sin aprobación. Mantener `dev` y seguridad fail-closed.

Para `QA_MANUAL_OWNER`:

- Consultar la Skill canónica y sus referencias para verificar su versión instalada; conservar la autoridad de gobierno de DEV como fuente de lectura, sin exigir el resultado operativo de sus workflows. Leer los contratos de la rama `qa` y su HEAD exacto; comprobar sus versiones de stack, repositorio, identidad, archivos de alcance y entornos autorizados.
- Acreditar el Scope Lock propio de QA mediante `node scripts/verify-project-scope.mjs` sobre un checkout real del HEAD de QA o mediante el paso equivalente aprobado en GitHub Actions para ese SHA exacto. Para un commit sólo documental, probar equivalencia de los archivos pertinentes respecto a la evidencia causal anterior. Si falta evidencia, realizar inspección de solo lectura y recuperar el control de alcance mediante un mecanismo autorizado, sin cambiar los controles de seguridad. Un fallo funcional posterior al Scope Lock no bloquea su propia corrección.
- Si la versión instalada no coincide con la versión canónica, solicitar sincronización. Si no puede acreditarse la identidad o el alcance QA, aplicar `NO_GO` a ese entorno; un fallo ajeno de DEV no constituye por sí solo falta de identidad ni de alcance QA.
- Antes de aplicar migraciones QA, exigir respaldo verificable, ensayo exacto sobre copia restaurada, preservación de datos e IAM, pruebas de regresión y aislamiento, plan de recuperación, SHA fijo y autorización del propietario. Antes de desplegar, comprobar el servicio y rama de QA, sus dependencias y compatibilidad de esquema; después comprobar SHA LIVE, readiness y pruebas funcionales auténticas. No declarar certificaciones por mera ejecución o compilación.


## 3. Gate de entrada para cualquier agente o chat

Antes de analizar o escribir:

1. confirmar `solqaryn/Solqaryn`, modo operativo y rama objetivo (`dev` ordinario/automatizado o `qa` manual autorizado);
2. confirmar que la tarea pertenece a SOLQARYN;
3. confirmar que esta skill local fue aplicada primero;
4. si se requiere una referencia externa, comprobar que exista como `ACTIVE` en la allowlist y en el registro de referencias;
5. consultar exclusivamente el origen original + pin + ruta oficial registrados;
6. nunca consultar una copia de esa skill alojada en otro proyecto;
7. identificar componente, capa, datos, seguridad, consumidores y validaciones afectadas;
8. determinar si la tarea es lectura, cambio DEV, intervención QA manual autorizada o acción que requiere autorización adicional;
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

- El trabajo ordinario y las automatizaciones se realizan en `dev`. El trabajo manual expresamente autorizado por el propietario para QA se realiza únicamente en `qa`, con los gates de QA y las cuentas QA autorizadas.
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

Nunca esperar una fase histórica ni reutilizar sus tareas como autoridad. El trabajo aprobado es únicamente el Plan Comercial Inicial; usar CI de seguridad y regresiones causales del código vigente sin confundir un nombre de workflow viejo con una dependencia de roadmap. Si se requiere una decisión o permiso del propietario, registrar una sola acción humana concreta y verificable con fingerprint, emitir notificación deduplicada mediante el mecanismo autorizado, detener únicamente el scope bloqueado y seguir otra microtarea independiente. Intentos técnicos fallidos sin nueva hipótesis/evidencia no deben repetirse indefinidamente. No comprar, contratar ni activar planes de pago. Mantener fail-closed para operaciones no autorizadas, para QA sin autorización manual y para MAIN/PROD sin autorización nueva y explícita. No transferir los bloqueos operativos de DEV a QA manual. Las diez Tasks, las parejas Primary/Supervisor y el motor VAEP están retirados; no existe supervisión 10/10 vigente.

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
