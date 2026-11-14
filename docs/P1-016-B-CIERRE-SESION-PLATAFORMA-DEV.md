# P1-016.B - cierre de regresion de plataforma sin tenant (DEV)

## Autoridad, identidad y dependencias
- Unica fuente ejecutable: Plan Comercial Inicial Unificado; PLAN_MAESTRO fila 60, dependencia P1-016.E.
- Task autora: Javier :20, ID 6adc2e4bd8a48191ac7d8641ed95a1a3. Rama unica work/solqaryn-p1-016-b; revision por otro TASK_ID.
- COLA!O60=SI permite DESARROLLO con predecesora en VALIDANDO, pero COLA!G60=NO PROHIBE MERGE/LISTO hasta P1-016.E LISTO. Comprobar G60 de nuevo antes de integrar.
- Recibos previos a consultar en vivo: P1-016.C y P1-016.D ya LISTO; P1-016.E en PR de revision (VALIDANDO al preparar). Estos estados son fotografia y no evidencia actual para el cierre.

## Regresion causal delimitada
- Prueba filtrable P1016BPlatformWithoutTenantClosureTests (5 hechos): superadmin con rol global y sin membresia accede pese a empresa piloto inactiva; admin empresarial no gana rol global por X-Empresa-Id; identidad sin rol global no entra; bloqueo revoca acceso aun con identidad JWT de prueba; ausencia de autenticacion HTTP se rechaza antes de auditar.
- Se reutiliza el fixture EF InMemory P1016APlatformWithoutTenantTests existente, no se duplican ni modifican las reglas productivas.
- Las aserciones heredadas comprueban HTTP controller Ok/Forbid, auditoria de ambito global sin EmpresaId, ausencia de membresia empresarial y Cache-Control privado para exito.
- TEST_LOCAL=NO_EJECUTADO; esta entrega NO acredita PASS hasta observar GitHub Actions con filtro FullyQualifiedName~P1016B (>0 ejecutados, 0 fallos), regresiones, Scope Lock y checks requeridos exact HEAD.

## Seguridad, limites y cierre
- Sin cambio de controlador, middleware, DTO, API, JWT firmado, RBAC, tenencia, datos/migraciones, secretos ni ambientes QA/main/PROD. La UI nunca confiere autoridad global.
- Fixture InMemory NO acredita autorizacion en HTTP remoto, JWT firmado, rollback MySQL o despliegue DEV vivo. No activar flags ni permisos como efecto de estos tests.
- Exigir review tecnica independiente sobre SHA definitivo, dependencia LISTO/COLA!G60=SI, PR CLEAN, 3 required checks SUCCESS del HEAD exacto, merge protegido dev y 3 required checks SUCCESS del SHA de merge.
- Solo entonces conciliar PLAN_MAESTRO H60 a LISTO, COLA calculada, BITACORA evento unico y readback.
- Ante fallo: mantener VALIDANDO, corregir en rama original sin force-push y solicitar nueva revision. Rollback proporcional mediante PR protegido que revierta solo test, acta y encabezado de CHANGELOG, preservando cambios ajenos.
