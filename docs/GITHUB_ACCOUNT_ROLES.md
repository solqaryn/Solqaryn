# SOLQARYN — Cuentas y permisos efectivos para ejecución DEV

## Estado actual
- Repositorio oficial: solqaryn/Solqaryn, desarrollo en dev. Los rulesets de main y qa subsisten; DEV fue liberado de la exigencia de PR/checks obligatorios por ruleset. Un verificador antiguo de PR no añade un nuevo ruleset ni revoca el nuevo contrato.
- Las seis Tasks de Javier mantienen cuenta, ID y horarios; último readback nativo conocido: HABILITADAS. Las dos Tasks de Alex no se cambian desde Javier; el registro de pausa/activación de noviembre es histórico y el estado actual sólo puede verificarse desde la cuenta propietaria.
- Toda acción GitHub, Google Sheets, Render o Vercel usa la credencial efectiva conectada del actor. Nunca suplantar Alex, Javier o solqaryn-platform.
- En el flujo ordinario DEV no se exige formal APPROVED de GitHub ni PR; se exige cuatro auditorías funcionales y técnicas con evidencia real por microtarea y commit directo a dev. Una revisión externa de otro Scope es admisible sin transformarse en espera obligatoria.
- Seguridad, permisos y separaciones de poder siguen aplicándose a cambios críticos; los permisos de qa/main/prod no se infieren de dev. `PROJECT_SCOPE_LOCK=STRICT` protege identidad y límites, no reintroduce PR protegidos.

## Preparación de Alex, pendiente de titular
- Alex mantiene los TASK_ID 6adbbc9ad9e88191bea8eb69104afb25 (:10) y 6adcc98a80a48191b71a5156624c21df (:50), sin cambios físicos ejecutados. Para el nuevo grupo de ocho se proponen PRIMARY DEV 07 (:05) y PRIMARY DEV 08 (:35), **no aplicados**. Solo Alex puede aprobar y publicar los nuevos títulos, horarios, prompts V13 directos y habilitación. Ver docs/AUTOMATION_V13.md.

## Regla de integridad
- Actor original identificado por TASK_ID, evidencia de cambios y SHA. El ejecutor no emite revisiones en identidad ajena, ni afirma pruebas no realizadas. Conflictos de HEAD deben reconciliarse y probarse antes de publicar.
- Cualquier solicitud futura para volver a usar PR/APPROVED en DEV requiere una orden nueva del propietario; no aplicar por historia. Fuente operativa única: docs/AUTOMATION_V13.md.
