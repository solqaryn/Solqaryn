# SOLQARYN — Reglas actuales de trabajo

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET
TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY
CURRENT_REGISTERED_TASKS=6
AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=TRUE
GITHUB_CHAT_EXECUTOR=solqaryn-platform
GITHUB_CHAT_REVIEWER=jmejia31
GITHUB_CHAT_ROLE_SEPARATION=STRICT
GITHUB_CHAT_ROLE_POLICY=docs/GITHUB_ACCOUNT_ROLES.md

## Enmienda operativa propuesta — certificación automática CI en DEV (2026-10-30)

**Vigencia condicionada:** esta norma solo sustituye la revisión formal para futuros PR comerciales ordinarios cuando el changeset esté fusionado por PR protegido en `dev`, el job requerido `verify-solqaryn-project-scope` ejecute la certificación contra el SHA exacto y exista readback de CI. Durante la implantación/bootstrap de este PR normativo sigue exigida revisión formal realmente independiente. No se simula un `APPROVED` humano: `AUTO_CERT=PASS` significa exclusivamente certificación técnica de GitHub Actions.

Para PR comerciales de ramas deterministas `work/solqaryn-<microtarea>`, el gate automático comprueba identidad y repositorio originales, alcance/diff, base DEV, SHA exacto, ausencia de secretos añadidos en código productivo, tres checks requeridos (incluyendo el propio Scope Lock), pruebas específicas del scope, resultado de todos los checks aplicables y rechazo fail-closed de fallos, faltas de evidencia o cambios concurrentes de HEAD. El código del verificador se ejecuta desde el commit BASE confiable, no desde el PR que evalúa. El job de Scope Lock ya es obligatorio en el ruleset de dev; no se requieren botones ni aprobación humana para el PR comercial que supera toda la certificación y sea fusionado mediante permisos reales y el flujo protegido. Un PASS de una Task o de Sheets no sustituye este gate.

Los PR que cambian gobierno, Skill, reglas de seguridad, workflows requeridos o el propio verificador **siguen necesitando revisión formal independiente** del SHA final. El bootstrap no puede autocertificarse. Ningún cambio expande el allowlist comercial 5/6 ni habilita Alex :30; no altera QA/main/PROD, credenciales, Aiven, grants ni despliegues. Una Task no puede autoaprobarse ni usar otra identidad. El cierre `LISTO` exige merge protegido, CI postmerge aplicable y readback de `PLAN_MAESTRO`, `COLA` (fórmula intacta) y `BITACORA`. Revertir mediante PR protegido con nuevo CI, sin eliminar evidencia histórica.

## M3.4 — ampliación quirúrgica WIP comercial a 6 (25-10-2026)

**AUTORIDAD VIGENTE tras merge protegido y comprobación del HEAD:** `MAX_CONCURRENT_SCOPES=6` y `MAX_OPEN_WORK_BRANCHES=6` describen **microtareas comerciales distintas, no autorizaciones de Tasks**. Se preserva EXACTAMENTE `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=5` y su allowlist vigente: Javier :00/:20/:40 y Alex :10/:50. Alex :30 sigue sin permiso comercial. WIP actual se mide contando scopes con PR/ramas de trabajo comerciales realmente abiertos; una rama fusionada, un PR de gobierno, una prueba diagnóstica y una fila obsoleta de Sheet no ocupan un scope. El nuevo límite NO exige que estén ocupados los seis, ni permite simular LISTO.

**Control de concurrencia y continuidad:** una Task autorizada puede mantener como máximo **dos microtareas asignadas** y a la vez **un solo scope en edición activa**. Puede reclamar una segunda únicamente cuando su primera entrega esté en `VALIDANDO` con PR auténtico, no exista edición ni revisión correctiva en curso de esa rama, y haya cumplido `REVIEW_FIRST` sobre los PR `VALIDANDO` de otra cuenta. Las dos microtareas deben tener IDs, ramas, owners y auditorías distintos, todas las dependencias `LISTO`, prioridad P0 y toma exclusiva de GitHub con readback; no se comparten ramas ni se reescriben PR. Una Task con un scope `EN_PROGRESO` o dos ya asignados NO puede reclamar un tercero. La única revisión formal válida es de login GitHub diferente al autor y a los contribuyentes revisados. Ci exact-SHA 3/3, merge protegido a dev y readback PLAN/COLA/BITACORA siguen obligatorios. El techo global de seis aplica a scopes comerciales; permisos individuales siguen siendo 5/6.

**Inventario configurable (enmienda 2026-10-30):** se elimina el techo interno de seis Tasks y la distribución obligatoria 3+3. El censo físico y su lista de IDs son una fotografía actual, no un máximo. La propietaria puede proponer otra cantidad o distribución mediante cambio de gobierno protegido, comprobación de capacidad efectiva del proveedor y readback de cada Task. Esta política no crea, elimina, activa ni autoriza Tasks. Se mantienen el allowlist comercial vigente de cinco IDs, Alex :30 excluido, `MAX_CONCURRENT_SCOPES=6`, máximo dos scopes asignados por Task y un escritor activo por Task, además de REVIEW_FIRST, CI exact-SHA 3/3, aprobación independiente, merge protegido y readback institucional.

**Riesgo y mitigación:** más PR puede aumentar conflictos en CHANGELOG_AI.md y cola de revisiones. Priorizar RECOVER/REVIEW_FIRST y cierres, reconciliar en la misma rama sin force-push, nunca tomar trabajo con editor ajeno activo. No se cambian ERP, migraciones, RBAC, QA/main/PROD, secretos ni proveedores. El WIP de seis scopes comerciales es independiente del inventario de Tasks.


## M3.3 — WIP histórico previo (sustituido por M3.4)

El límite vigente `MAX_CONCURRENT_SCOPES=3` del contrato 3+3 cuenta SOLO microtareas comerciales distintas del Plan Inicial con rama determinista y entrega abierta (máximo 1 scope por Task autorizada), NO PR normativos/de gobierno, ramas `test/` diagnósticas, auditorías, PR fusionados ni historias archivadas. Actualmente la allowlist es 5/6 (Javier :00/:20/:40; Alex :10/:50); Alex :30 sigue sin autorización comercial. Las verificaciones de exclusividad por rama, CI exacto, revisión independiente, merge protegido y readback no se reducen. Tras cada `LISTO`, recalcular el DAG y continuar dentro del mismo run; objetivo de medición 6 cierres LISTO/hora, sin declararlos por anticipado. Ante impedimento externo de un scope, avanzar otro P0 independiente si existe capacidad.

## Autorizacion adicional M3.2 — Javier :40 y Alex :10/:50

Se amplía a 5/6 exclusivamente después de merge protegido y conciliación de Sheets; detalles y pruebas en `docs/AUTOMATION_STATUS.md` y `docs/AUTOMATION_AUTHORITY.md`. GitHub commits de prueba individuales: Javier :40 `46e6a9822d89932697c93e77c96df239d73bed4f`, Alex :10 `37fb117a38959c99c5795130a1a40365693cc0b4` y Alex :50 `b0f5a77757847d623a17c0f1d9c5fde57fd909fd`. BITACORA filas 85, 76 y 81 corroboran. Pruebas consumidas, no repetir. Se excluye Alex :30 hasta evidencia propia contents:write y nueva autorización protegida. Se conservan independencia GitHub de revisores, CI 3/3, Scope Lock STRICT, WIP comercial máximo 6 según M3.4, excluidos QA/main/PROD.

## Autorizacion individual Javier :20 (condicionada al merge)

El permiso comercial DEV de PRIMARY_DEV_02 TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` se concede exclusivamente tras aprobar, fusionar y conciliar este changeset en dev/Sheet. GH contenido commit aaca89247d9aaf61eb21a308c2e43fd5fef24526 y BITACORA A77 verificadas. Permiso de GitHub ramas/PR, PLAN_MAESTRO y BITACORA solo con autoridad por identidad, WIP comercial máximo 6 conforme a M3.4, CI y revision independiente GitHub antes de LISTO. `CONFIG!B57=TRUE` no concede escritura a las otras cuatro Tasks no autorizadas. Nunca autoaprobar bajo la cuenta GitHub autenticada de Javier (ID 93171442). No QA/main/PROD ni despliegues.

## Registro histórico M3.2 — seis Tasks instaladas y autorización parcial

En aquel registro el conjunto físico constaba de seis Tasks distribuidas tres por cuenta. Es una fotografía histórica, no un techo ni una distribución obligatoria. Las pruebas individuales de permisos y la autorización comercial se mantienen ligadas a IDs concretos, no a cuotas de cuenta. El cambio vigente de esta enmienda concede `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=5` tras integración protegida y readback del Sheet: tres Tasks Javier (:00/:20/:40) y dos Tasks Alex (:10/:50), cada una con contenido GitHub y auditoría nativos verificados. Alex :30 permanece sin autorización comercial al carecer de prueba propia de contents:write (intento previamente bloqueado por seguridad); la microfase global sigue parcial y el conteo 5/6 nunca equivale a seis permisos. Ninguna Task nueva se habilita por simplemente existir. La única autoridad de la nueva lógica está en `docs/AUTOMATION_3X3_OPERATING_CONTRACT.md` y el contador efectivo en `docs/AUTOMATION_STATUS.md`.

Una Task elige primero VALIDANDO de otra identidad y después PENDIENTE con TODAS las dependencias LISTO. El revisor corrige directamente en el mismo PR, conservando VALIDANDO. Sus commits requieren revisión independiente y CI del SHA final; jamás autoaprobar. Solo un merge protegido verificado en dev más readback Plan Maestro/Cola/Bitácora permite LISTO. No crear rama almacén ni duplicar ramas; máximo seis scopes comerciales distintos globales, hasta dos por Task autorizada con un único editor activo y solo segunda entrega tras VALIDANDO, conforme a M3.4. El runtime no es indefinido: registrar commits y punto de continuación ante interrupción.

Las reglas históricas del piloto 1+1 que aparecen abajo son archivo de trazabilidad y no limitan las capacidades v2.0. VAEP y Jules siguen retirados; QA, main, PROD y secretos fuera de alcance.

## Archivo histórico v1.2 — 2026-10-23 — PILOTO DEV PRIMARY AUTORIZADO POR EL PROPIETARIO (reemplaza gates historicos de microfase 3)

El propietario ordena reanudar las microtareas DEV ahora. PRIMARY_01 existente de Javier es el unico escritor automatizado autorizado, SECONDARY_02 de Alex permanece revisor READ_ONLY. Se limita a solqaryn/Solqaryn, base dev, rama especifica por microtarea del Plan Comercial Inicial y PR contra dev. Prohibido merge autonomo, QA, main, PROD, Render, Vercel, secretos o cambios productivos.

El RUN_ID, RUN_URL, TASK_ID_NATIVO_DEL_RUN, correo ChatGPT, RUN_COMPLETED_NATIVO, REVIEWED_RUN_ID y EVIDENCE_REF_AUTENTICADO no son condiciones de elegibilidad ni certificacion de trabajo DEV cuando ChatGPT no los expone. NO inventar campos. Evidencia operativa suficiente: TASK_ID configurado, ID de microtarea, HEAD base real, rama y PR/commit GitHub verdaderos, checks de CI y evidencia de readback. El 4/4 de ejecuciones de diagnostico READ_ONLY deja de ser requisito del piloto DEV.

Control de colisiones: un solo escritor Primary con WIP=1, comprobar ramas/PR activos del mismo ID antes de iniciar; continuar el PR existente en vez de duplicar. GitHub proporciona HEAD y proteccion de PR. Sin CAS ni leases en Sheets. Alex revisa un PR/SHA concreto, sin REVIEWED_RUN_ID de ChatGPT ni aprobacion ficticia. Se mantienen Project Scope Lock, permisos reales, seguridad multiempresa, CI exact-head y revision humana independiente cuando GitHub lo exija.

Esta habilitacion se vuelve efectiva unicamente al fusionar con checks obligatorios este contrato a dev y comprobar el HEAD nuevo. Los prompts programados deben validar la autoridad viva antes de escribir.
## Autoridades vigentes

1. `docs/PROJECT_SCOPE_LOCK.md` regula la identidad de SOLQARYN y el acceso a sus fuentes.
2. `docs/AUTOMATION_STATUS.md` declara si existe un motor programado autorizado.
3. El documento `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` es la fuente actual del alcance comercial. Ninguna hoja, manifiesto, bitácora o plan histórico es ejecutable por inferencia.
4. `docs/AUTOMATION_AUTHORITY.md` y `docs/AUTOMATION_3X3_OPERATING_CONTRACT.md` regulan un inventario configurable por la propietaria, con permisos comprobados individualmente. El censo de seis Tasks y la distribución actual 3+3 son fotografía, no techo ni autorización. Se preservan el allowlist comercial vigente de cinco Tasks y el WIP máximo seis scopes. El piloto v1.2 permanece como antecedente. No autoriza QA/main/PROD ni restaura VAEP. Gobierno paritario: `PROPIETARIA_INSTITUCIONAL` SOLQARYN Platform; `EJECUTOR_PRINCIPAL_01` Javier (`93171442`) y `EJECUTOR_PRINCIPAL_02` Alex (`morales35alex`) son pares humanos, sin jerarquía ni transferencia de permisos.
5. La única Skill local permitida sigue siendo `solqaryn-project-governance`; las referencias externas son opcionales y se consultan únicamente para una necesidad técnica real.

## Archivo histórico — estado de automatización al 22-10-2026 (sin vigencia operativa)

El propietario informó que eliminó las diez Tasks de ChatGPT y la hoja operativa antigua. **En aquel corte no había Tasks nuevas autorizadas, y se habían retirado las diez Tasks anteriores, workers Jules, coordinadores y cola VAEP.** No crear o reconstruir Tasks, activar cron, restaurar Sheets, reclamar leases, realizar despachos comerciales ni interpretar evidencias históricas como instrucciones vigentes. Todo motor nuevo necesita diseño y autorización explícitos; esta regla no impide el trabajo manual solicitado.

## Archivo histórico — enmienda v1.2 previa al piloto DEV

Inicialmente se diseñaron dos Tasks independientes, una de Javier y otra de Alex, sin permiso de operación. En aquel momento `AUTOMATION_RUNTIME=RETIRED` y `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0` eran los valores vigentes. Esa fotografía quedó sustituida por el piloto `ACTIVE_DEV` autorizado el 23-10-2026. No se trasladan permisos de cuenta, ni jerarquías humanas, ni autorizaciones de QA/PROD.

## Aprobaciones delegables con controles intactos (23-10-2026)

La norma única de selección flexible reside en `docs/AUTOMATION_AUTHORITY.md`, sección 7.1. Una aprobación pendiente de GitHub, Render, Vercel, Google Sheets/Drive u otra plataforma podrá dirigirse a **Javier, Alex o SOLQARYN Platform**, según disponibilidad y permisos individuales **efectivamente comprobados**, sin prioridad fija. Ninguna cuenta puede autoaprobar un cambio cuando se exige revisión independiente; la cuenta institucional necesita representante humano identificable e independiente si el proveedor exige esa independencia. Respetar las aprobaciones nativas, checks, controles del proveedor, límites de entorno y evidencia; ninguna disponibilidad habilita Tasks o PROD.

## Archivo histórico — microfase 3 READ_ONLY, superada como puerta de ejecución DEV

El 23-10-2026 se constataron dos Tasks físicas, Primary Javier (`6adbb9fff1548191a1bd5b30d9e9984a`) y Secondary Alex (`6adbbc9ad9e88191bea8eb69104afb25`). En aquel corte la certificación individual estaba en `0/4` y el runtime `RETIRED`. Esa evidencia se conserva en `docs/AUTOMATION_STATUS.md` e Issue #3730 exclusivamente como historia; ni sus horarios anteriores ni `RUN_ID`/`EVIDENCE_REF` condicionan el piloto DEV vigente. El modo y permisos actuales están únicamente en las claves de `docs/AUTOMATION_STATUS.md`, y la revisión de Alex exige evidencia GitHub del PR/SHA, no acceso a chats privados.

## Trabajo de desarrollo

- Trabajar en `solqaryn/Solqaryn`, rama `dev`, por PR y fusión protegida. Consultar el HEAD actual y no sobrescribir trabajo concurrente.
- Cambios de código tienen pruebas proporcionales, revisión causal, diffs auditables y rollback. Un plan histórico no puede imponer hitos ni gates no relacionados.
- Conservar JWT, BCrypt, RBAC, aislamiento A/B por empresa, auditoría, seguridad multiempresa, integridad de MySQL, respaldos, controles de secretos y prevención de fugas; no rebajarlos para ahorrar tiempo.
- Frontend Angular 22, backend .NET 10, MySQL EF Core, Cloudinary, SMTP y QuestPDF permanecen bajo la arquitectura vigente.
- Las pruebas de producto de CI son distintas de las automatizaciones que coordinaban Tasks. No usar un job llamado `automatizacion` como criterio único para borrarlo: verificar sus consumidores.
- No promover a QA/main/PROD, alterar datos productivos, secretos, dominios ni pagar servicios sin autorización nueva y explícita.
- `docs/VAEP_AUTHORITY.md` ya no existe en el árbol vivo `dev` (solo puede consultarse en historia Git); su autoridad antigua quedó retirada y jamás es fallback.

## Cierre

Mantener `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `ARCHITECTURE_CHANGELOG.md` y `CHANGELOG_AI.md` cuando el cambio lo amerite. Documentar cualquier acción humana pendiente con enlace e instrucciones concretas.
