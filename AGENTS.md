# SOLQARYN — Reglas actuales de trabajo

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
TASK_DESIGN=AUTONOMOUS_3_JAVIER_3_ALEX

## Autorizacion individual Javier :20 (condicionada al merge)

El permiso comercial DEV de PRIMARY_DEV_02 TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` se concede exclusivamente tras aprobar, fusionar y conciliar este changeset en dev/Sheet. GH contenido commit aaca89247d9aaf61eb21a308c2e43fd5fef24526 y BITACORA A77 verificadas. Permiso de GitHub ramas/PR, PLAN_MAESTRO y BITACORA solo con autoridad por identidad, WIP max 2, CI y revision independiente GitHub antes de LISTO. `CONFIG!B57=TRUE` no concede escritura a las otras cuatro Tasks no autorizadas. Nunca autoaprobar bajo jmejia31. No QA/main/PROD ni despliegues.

## Microfase 3.2 — inventario físico 3+3 y autorización operativa parcial

La arquitectura aprobada es de seis Tasks: tres Javier (:00/:20/:40) y tres Alex (:10/:30/:50), con capacidades simétricas de desarrollo, revisión, corrección y tramitación del cierre. El rol nominal no concede privilegio; cada operación requiere identidad, permisos, HEAD, exclusividad y CI reales. Las seis Tasks físicas están instaladas con IDs únicos y horarios :00/:10/:20/:30/:40/:50, conservando los dos IDs originales; véase `TAREAS_PROGRAMADAS!A2:Z7` y `docs/AUTOMATION_STATUS.md`. La instalación 6/6 NO acredita seis permisos operativos: `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=2` tras integración y readback de la concesión individual Javier :20; las otras cuatro Tasks adicionales conservan autorización comercial NO_VERIFICADA hasta readback desde sus propias ejecuciones. La microfase 3.2 sigue PARTIAL_NOT_CERTIFIED hasta que se cumplan los controles y se registre evidencia. La única autoridad de la nueva lógica está en `docs/AUTOMATION_3X3_OPERATING_CONTRACT.md` y el contador efectivo en `docs/AUTOMATION_STATUS.md`.

Una Task elige primero VALIDANDO de otra identidad y después PENDIENTE con TODAS las dependencias LISTO. El revisor corrige directamente en el mismo PR, conservando VALIDANDO. Sus commits requieren revisión independiente y CI del SHA final; jamás autoaprobar. Solo un merge protegido verificado en dev más readback Plan Maestro/Cola/Bitácora permite LISTO. No crear rama almacén ni duplicar ramas; máximo dos trabajos en paralelo. El runtime no es indefinido: registrar commits y punto de continuación ante interrupción.

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
4. `docs/AUTOMATION_AUTHORITY.md` regula el diseño 3+3 autorizado y `docs/AUTOMATION_3X3_OPERATING_CONTRACT.md` su operación futura. La autoridad real sigue sujeta a `docs/AUTOMATION_STATUS.md`, conexiones y permisos verificados: el inventario de seis Tasks físicas no concede escritura a las cinco sin permiso operativo certificado desde cada ejecución. El piloto v1.2 (una Primary escritora y una Secondary READ_ONLY) permanece documentado únicamente como antecedente. No autoriza QA/main/PROD ni restaura VAEP. Gobierno paritario: `PROPIETARIA_INSTITUCIONAL` SOLQARYN Platform; `EJECUTOR_PRINCIPAL_01` Javier (GitHub user ID `93171442`) y `EJECUTOR_PRINCIPAL_02` Alex (`morales35alex`) son pares humanos, sin jerarquía ni transferencia de permisos de Task.
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
