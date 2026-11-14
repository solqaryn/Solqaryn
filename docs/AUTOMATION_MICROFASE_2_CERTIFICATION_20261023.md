# SOLQARYN — Microfase 2: certificación de la estructura operativa Google Sheets

**Fecha:** 23-10-2026  
**Resultado físico Google Sheets:** `M2_SHEETS_READBACK=PASS`  
**Alcance del commit:** evidencia, auditoría y actualización documental; **no** activa Tasks ni runtime.  
**Fusión a `dev`:** requiere PR protegido, checks sobre SHA vigente y aprobación independiente elegible antes de considerar el cierre integrado.

## Autoridades verificadas

- Repositorio: `solqaryn/Solqaryn`; base de preflight `dev` en `f567b7d1d8b5b4530508abebed2900e2930894f2`; rama protegida.
- Skill instalada y `references/architecture.md` comparadas contra el HEAD canónico: **coinciden**. Angular 22.2.1, net10.0 y EF Core 10.0.12 verificados.
- Contrato: `docs/AUTOMATION_AUTHORITY.md` v1.2, con grupo de aprobación `JAVIER,ALEX,SOLQARYN_PLATFORM`, sujeto a permiso real e independencia cuando se exija.
- Estado real en `docs/AUTOMATION_STATUS.md`: `AUTOMATION_RUNTIME=RETIRED`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, `VAEP_DISPATCH_AUTHORIZED=FALSE`, `JULES_WORKERS_AUTHORIZED=0`.
- Los tres checks obligatorios sobre el HEAD de preflight estaban en **SUCCESS**: `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend`. Ese resultado no sustituye CI del futuro PR de cierre.

## Destino y permisos

- Google Sheets original: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), ID `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. No se creó un segundo maestro.
- Propietaria institucional: `solqaryn.platform@outlook.com`; Javier `javiermejia3112@gmail.com` y Alex `morales35alex@gmail.com` tenían rol `writer` comprobado individualmente.
- Escrituras de microfase 2 mediante conexión Drive autorizada de Javier, desde la sesión interactiva de ChatGPT; **no** desde una Task programada.
- Último readback documental del archivo en esta certificación: revisión Drive `67` (2026-10-23T17:13:14.590Z). El campo `lastModifyingUser` de una revisión es atribución de Drive, no prueba individual de cada solicitud API.

## Estructura 8/8

Se conservan las seis pestañas originales `DASHBOARD`, `PLAN_MAESTRO`, `COLA`, `CONFIG`, `LEYENDA`, `BITACORA`, junto con las dos posiciones preparadas `TAREAS_PROGRAMADAS` y `AUTOMATIZACIONES`. No quedaron pestañas de QA creadas para pruebas anteriores.

- `TAREAS_PROGRAMADAS!A1:Z3`: 26 campos, **dos** slots `01=PRIMARY_01/JAVIER_CHATGPT` y `02=SECONDARY_02/ALEX_CHATGPT`; `TASK_ID` vacío, `NO_CREADA`, `NO_VERIFICADA`, permisos de Task no verificados y escritura no autorizada.
- `AUTOMATIZACIONES!A1:Q6`: 17 campos, exactamente cinco capacidades lógicas `AUTHORITY_GUARD`, `PLAN_SELECTOR`, `DEV_OPERATOR`, `REVIEW_GATE` y `AUDIT_REPORTER`. Todas `NO_ACTIVA` y `NO_AUTORIZADA` para escritura. No son cinco Tasks físicas.
- `CONFIG!A27:C75`: 49 parámetros informativos de contrato, roles, paridad, aprobaciones flexibles, ACL humanas observadas, runtime, readback e integridad; `CONFIG` no prevalece sobre GitHub ni concede acceso o autoridad.
- `DASHBOARD!A30:M54`: panel adicional y rutas de trabajo; ocho fórmulas comerciales originales preservadas, 17 fórmulas nuevas para dos slots, estado de Tasks y auditoría. Readback: **2 reservadas, 0 verificadas, 0 autorizadas, 0 ejecuciones, 5 capacidades, runtime RETIRED**, sin errores de fórmula.
- `LEYENDA!A30:C61`: 32 entradas de guía v1.2; las seis pestañas originales conservan su propósito.
- `BITACORA!A1:Y3`: 25 columnas de auditoría para eventos futuros y dos eventos reales de esta implementación; identificadores `SOLQARYN-M2-20261023-CERT-001` y `SOLQARYN-M2-20261023-PROTECTIONS-002`. No se inventaron ejecuciones de Tasks. La hoja comenzó sin eventos históricos.

## Integridad de datos comerciales y fórmulas (readback completo)

| Control | Resultado |
| --- | --- |
| `PLAN_MAESTRO!A2:O1124` | 1.123 filas y 1.123 IDs distintos |
| Estados de microtarea | 1.123 `PENDIENTE`, sin cambios |
| Dependencias | 0 inexistentes; 0 ciclos |
| `COLA!A2:N1124` | 1.123 filas / 15.722 fórmulas |
| Hash FNV64 de campos inmutables | `e393ce0da1d189e7` — coincide con línea base |
| Hash FNV64 de 15 columnas comerciales | `bad7c771a874f927` — coincide |
| Hash FNV64 de fórmulas `COLA` | `0e485b82e6b2eb72` — coincide |

Las huellas FNV64 son controles diagnósticos de igualdad, no firmas criptográficas. La SHA256 comercial registrada anteriormente en `CONFIG` permanece preservada.

## Protecciones, pruebas y límites

- **16 validaciones nativas `strict:true`** verificadas por lectura directa sobre slots, vinculación de cuentas/roles, estados sin evidencia, Task ID duplicado, tercera posición, sexta capacidad, permiso de escritura, bitácora y duplicados de evento.
- **38/38 casos negativos aislados** superados con un modelo de validación *en memoria*: tercera Task, identidad duplicada, permisos, aprobación independiente, cambio comercial, fórmula, runtime, VAEP, Jules, bitácora incompleta, lectura posterior y HEAD obsoleto, entre otros. **No constituyen 38 intentos de escritura rechazados por la API real.**
- Las advertencias de rangos protegidos se añadieron con API nativa; el evento `PROTECTIONS-002` documenta una deduplicación posterior de advertencias superpuestas. Este certificado **no confunde `warningOnly` con bloqueo de permiso ACL** ni certifica una garantía atómica de exclusividad.
- `PLAN_MAESTRO` y `COLA` no se reescribieron. El gobierno de microfase 2 es planificación, no motor de claims, CAS, lease ni aprobación GitHub.
- La comprobación visual se hizo mediante valores, formatos, fórmulas y dimensiones de la API; no hay certificación visual independiente en navegador.
- Ningún cambio a código del ERP, `main`, QA, PROD, Render, Vercel, secretos o credenciales. No se creó ni activó una Task física por esta microfase.

## Controles de cierre y alcance

**Resultado:** `M2_SHEETS_READBACK=PASS`; la microfase 2 queda preparada para cierre documental por vía protegida. La presencia de `MICROFASE_2_STATUS=CERTIFICADA` en `CONFIG` es una etiqueta de seguimiento basada en readback, **no** una fusión GitHub ni autoridad para ejecución.

La fase 3 —dos Tasks reales, una por cuenta— **no se inicia por este documento**. Sigue exigida autorización individual, verificación de capacidades de cada cuenta y pruebas programadas de lectura. `AUTOMATION_RUNTIME` y `SCHEDULED_CHATGPT_TASKS_AUTHORIZED` permanecen intactos.

**Reversión:** conservar historial Drive y huellas, restaurar únicamente rangos añadidos/modificados (`TAREAS_PROGRAMADAS!A1:Z3`, `AUTOMATIZACIONES!A1:Q6`, `CONFIG!A27:C75`, `DASHBOARD!A30:M54`, `LEYENDA!A30:C61` y ajustes puntuales de las 6 hojas, `BITACORA!A1:Y3`, validaciones y advertencias). Revalidar las 1.123 microtareas y 15.722 fórmulas. Rollback documental GitHub exclusivamente mediante PR protegido, sin force-push, reset ni degradación de seguridad.
