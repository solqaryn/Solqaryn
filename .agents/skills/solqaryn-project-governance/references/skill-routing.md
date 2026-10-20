# SOLQARYN Skill Reference Routing

SOLQARYN mantiene una sola skill local: `solqaryn-project-governance`.

Las demas entradas se consultan como referencias externas originales segun `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md`.

| Referencia | Activar cuando | Regla de consulta |
| --- | --- | --- |
| SKREF-01 Agent Skills Specification | Crear/revisar estructura de la skill local | Leer especificacion oficial en origen/pin registrado |
| SKREF-02 Skill Creator | Crear/editar/validar la skill local | Usar la implementacion oficial integrada de ChatGPT/OpenAI |
| SKREF-03 Impeccable | Dashboard, app shell, forms, tablas, settings, onboarding, polish | Leer skill original en origen/pin |
| SKREF-04 Motion Skills | Motion, microinteracciones, revision/mejora de animaciones | Leer solo las skills de motion necesarias en origen/pin |
| SKREF-05 Taste Skill | Home, landing, tienda publica, CMS visual, marketing | Leer skill original en origen/pin |
| SKREF-06 Humanizer | UX copy, docs, PRs, informes | Leer skill original en origen/pin |
| SKREF-07 Napkin | Runbooks/lecciones recurrentes | Leer skill original en origen/pin; no crear autoridad paralela |
| SKREF-08 Token Optimizer | Contexto extenso, progressive disclosure, duplicacion | Leer skill original en origen/pin; no eliminar controles |
| SKREF-09 Caveman | Resumen interno efimero de bajo riesgo | Leer las rutas registradas en origen/pin; no usar para evidencia critica |

## Uso por lane del Plan Comercial Inicial

| Lane VAEP | Skill local | Referencias externas condicionales |
| --- | --- | --- |
| 01_SEGURIDAD | Governance antes de toda operación | SKREF-01/02 sólo al actualizar Skills; otras sólo si el cambio afecta su superficie |
| 02_COMERCIO | Governance y aislamiento de empresa | SKREF-03 para formularios/tables administrativos; SKREF-05 cuando afecta presentación pública |
| 03_UX | Governance, frontend/backend y accesibilidad | SKREF-03 UX institucional, SKREF-04 si hay animación, SKREF-05 para tienda/landing, SKREF-06 para textos |
| 04_RUTAS | Governance, HMAC, RBAC y rutas verificadas | No hay referencia externa obligatoria; utilizar sólo una pertinente a UX/routing que esté autorizada |
| 05_QA_OPERACION | Governance y evidencias exact-head | SKREF-01/02 al validar Skills; SKREF-07/08 para runbooks/contexto; SKREF-09 sólo compresión efímera no crítica |

El ejecutor debe usar **primero la Skill local del repositorio en el HEAD de dev**, confirmar que la copia instalada coincide y pasar `scripts/verify-solqaryn-skills.mjs` o el check de CI exact-head. Si hay discrepancia, `NO_GO`. No es válido alegar que una referencia fue aplicada por existir en el registro: registrar qué referencia, pin/ruta y por qué fue necesaria.

Cuando apliquen varias referencias, usar primero governance, luego referencias de superficie y por ultimo referencias de redaccion/contexto.
