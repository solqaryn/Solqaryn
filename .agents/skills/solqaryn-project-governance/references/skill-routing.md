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
| SKREF-10 Angular Developer | Cambios de Angular 22, componentes, Signals, formularios, Angular Router, accesibilidad o pruebas | Consultar sólo su SKILL.md original de angular/angular en pin; no migrar framework ni crear apps |
| SKREF-11 Security Threat Model | Amenazas o límites de confianza explícitos A/B, JWT, RBAC, HMAC o sesiones | Consultar sólo su SKILL.md original de openai/skills en pin; analizar evidencia específica del repositorio |
| SKREF-12 .NET Performance | Cuellos de botella demostrados, profiling, problemas LINQ, async, memoria o I/O .NET 10 | Consultar sólo su SKILL.md original de dotnet/skills en pin; no optimizar sin medición |

## Orientación técnica opcional por tipo de cambio

| Área del producto | Skill local | Referencias externas condicionales |
| --- | --- | --- |
| 01_SEGURIDAD | Governance, autenticación y tenancy | SKREF-11 si se requiere modelado de amenazas/límites de confianza; SKREF-01/02 sólo al modificar Skills |
| 02_COMERCIO | Governance y aislamiento de empresa | SKREF-10 para Angular, SKREF-03 para UX administrativa, SKREF-05 para tienda, SKREF-11 ante amenaza explícita, SKREF-12 para performance .NET demostrada |
| 03_UX | Governance, frontend y accesibilidad | SKREF-10 para Angular; SKREF-03 UI, SKREF-04 movimiento, SKREF-05 tienda, SKREF-06 textos, sólo según componente |
| 04_RUTAS | Governance, HMAC, RBAC y rutas verificadas | SKREF-11 para modelado de abuso/tenancy, SKREF-10 para Angular Router, SKREF-12 para rutas .NET con perf medido |
| 05_QA_OPERACION | Governance, pruebas y evidencias exact-head | SKREF-12 sólo si la tarea aborda rendimiento .NET; SKREF-07/08 runbooks, SKREF-09 resumen efímero, SKREF-01/02 para validar Skills |

El ejecutor debe usar **primero la Skill local del repositorio en el HEAD de dev**, confirmar que la copia instalada coincide y pasar `scripts/verify-solqaryn-skills.mjs` o el check de CI exact-head. Si hay discrepancia, `NO_GO`. No es válido alegar que una referencia fue aplicada por existir en el registro: registrar qué referencia, pin/ruta y por qué fue necesaria.

Cada agente autorizado debe seleccionar referencias únicamente ante un cambio técnico concreto del Plan Comercial Inicial: consultar primero la Skill local canónica y comprobar frescura, clasificar el alcance de la microtarea, seleccionar 0..N referencias, validar fuente original + pin + ruta registrada, registrar cuáles aplicó y ejecutar las pruebas causales. Si ninguna aplica, basta Governance; si el origen fijado falla, no usar forks o mirrors ni simular su consulta. Bloquear sólo el scope que la necesita.

No se instalan Skills externas como copias locales, no se ejecutan herramientas externas ni se importan agentes, dependencias, migraciones o defaults de otros repositorios. No se crean microtareas, automatizaciones, Tasks ni informes no requeridos. El sistema Primary/Supervisor anterior está retirado.

Cuando apliquen varias referencias, usar primero governance, luego referencias de superficie y por ultimo referencias de redaccion/contexto.
