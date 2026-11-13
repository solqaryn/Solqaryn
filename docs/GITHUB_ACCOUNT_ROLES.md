# SOLQARYN — Cuentas y permisos efectivos para ejecución DEV

## Estado actual
- Repositorio oficial: solqaryn/Solqaryn, desarrollo en dev. Los rulesets de main y qa subsisten; DEV fue liberado de la exigencia de PR/checks obligatorios por ruleset. Un verificador antiguo de PR no añade un nuevo ruleset ni revoca el nuevo contrato.
- Las seis Tasks de Javier mantienen cuenta, ID y horarios; último readback nativo conocido: HABILITADAS. Las dos Tasks de Alex no se cambian desde Javier; el registro de pausa/activación de noviembre es histórico y el estado actual sólo puede verificarse desde la cuenta propietaria.
- Toda acción GitHub, Google Sheets, Render o Vercel usa la credencial efectiva conectada del actor. Nunca suplantar Alex, Javier o solqaryn-platform.
- En el flujo ordinario DEV no se exige formal APPROVED de GitHub ni PR; se exige A0 de calidad estática más A1–A4 con evidencia para LISTO en dev y A5 independiente antes de promover a QA/PROD por microtarea y commit directo a dev. Una revisión externa de otro Scope es admisible sin transformarse en espera obligatoria.
- Seguridad, permisos y separaciones de poder siguen aplicándose a cambios críticos; los permisos de qa/main/prod no se infieren de dev. `PROJECT_SCOPE_LOCK=STRICT` protege identidad y límites, no reintroduce PR protegidos.

## Alex: incorporación 6→8 notificada como completada

Según certificación comunicada por Alex el 12-nov-2026, ambas Tasks fueron leídas directamente desde SU cuenta y tienen is_enabled=true, V13 A0–A5, vigilancia mutua, producción sin techo artificial y preferencia máxima. Sus últimas corridas constan completed y requires_user_action=false. Esta información es evidencia transmitida por el propietario; NO es una lectura independiente desde Javier. El modelo/razonamiento real y un rescate mutuo durante una pausa aún no están comprobados.

- PRIMARY DEV 07 — ALEX, TASK_ID 6adbbc9ad9e88191bea8eb69104afb25, FREQ=HOURLY;BYMINUTE=5;BYSECOND=0, America/Tegucigalpa
- PRIMARY DEV 08 — ALEX, TASK_ID 6adcc98a80a48191b71a5156624c21df, FREQ=HOURLY;BYMINUTE=35;BYSECOND=0, America/Tegucigalpa

No editar Tasks físicas desde la cuenta de Javier; las verificaciones futuras del estado de Alex pertenecen a Alex. Mantener A0–A5 y las restricciones de qa/main/PROD.

## Renombrado por minuto y continuidad
- Identidad de Tasks determinada por TASK_ID real y cuenta, no por título. Javier conserva seis IDs y RRULE horarios; los títulos se normalizan PRIMARY DEV 01 :00, 02 :10, 03 :20, 04 :30, 05 :40 y 06 :50. La plataforma puede suspender por acción pendiente o error; un prompt no anula esa protección. Registrar incidencia, mantener habilitada cuando la plataforma permita, nunca esconderla.
- Alex mantiene sus dos IDs y no se renombra físicamente desde Javier; PRIMARY DEV 07 :05 y 08 :35, según certificación transmitida por Alex desde su cuenta.

- Un Scope de Javier puede reanudar una hermana del mismo conjunto físico de seis mediante is_enabled=true, con un intento por corrida y readback. No puede operar las dos Alex. La preferencia de razonamiento máximo requiere selección nativa del propietario; ni prompt ni conector determinan el nivel efectivo.
## Regla de integridad
- Actor original identificado por TASK_ID, evidencia de cambios y SHA. El ejecutor no emite revisiones en identidad ajena, ni afirma pruebas no realizadas. Conflictos de HEAD deben reconciliarse y probarse antes de publicar.
- Cualquier solicitud futura para volver a usar PR/APPROVED en DEV requiere una orden nueva del propietario; no aplicar por historia. Fuente operativa única: docs/AUTOMATION_V13.md.
