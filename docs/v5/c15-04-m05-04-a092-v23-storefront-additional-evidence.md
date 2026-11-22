# C15-04.M05.04 - V23 A0.92: 15 capturas publicas complementarias

Fecha: 2026-11-21. Repositorio solqaryn/Solqaryn, rama dev. PROJECT_SCOPE_LOCK=STRICT.
Task ejecutora DEV02: 6af16bb9492c8191b8e8c7d606f11507.
Microtarea existente: PLAN_MAESTRO!A2297=C15-04.M05.04, H=EN_PROGRESO, I=DEV02.
Candado preexistente: SQV17_C15_04_M05_04 / namedRangeId 2000002297; sin nuevo CLAIM.

## A0: analisis de impacto y riesgo
Entrega incremental test-only R1 dentro de auditoria UX global R2 no finalizada. Extiende cobertura V23 de A091 a /tienda/ofertas, /tienda/carrito y /tienda/cuenta. Navegador real con Angular servido en localhost GitHub-hosted; 320, 390, 768, 1280, 1440px por ruta (15 PNG fullPage). Controles positivos: destino main, h1 unico, skip-link y foco real con Tab. Negativos: no overflow del documento, 0 solicitudes HTTP mutantes. Archivo de test y workflow, sin cambios a Angular runtime, API, SQL, identidad, compras, roles o contratos. No se instala software externo, no se usan credenciales de clientes.

## A1-A4: puerta de evidencia verificable
El workflow v19-c15-04-m05-04-global-ux.yml ejecuta Angular npm lint + build y Playwright Chromium de la suite completa (27 casos tras agregar A092). Capturas deben guardarse como c1504-m0504-v23-a092-screenshots con 15 PNG no triviales; artefacto ausente => CI FAIL. Para acreditar A4 se requiere GitHub-hosted exact-SHA COMPLETED SUCCESS, Scope Lock y V5/procedencia success; no asumir PASS mientras QUEUED/IN_PROGRESS/SKIPPED/FAILED. Conservar A091 por separado como evidencia previa, no reemplazar ni atribuir comparacion antes/despues: no hubo cambio visual de runtime.

## A2-A3 proporcional / limites globales
El incremento modifica un test de UI publica, de modo que JWT/RBAC tenant A/B y EF/MySQL son NO_APLICA a este diff; estos requisitos SIGUEN ABIERTOS en el scope global C15-04.M05.04, junto a 160 rutas/estados por rol, WCAG integral, 200% zoom, field CWV/RUM, permisos backend y pantallas privadas. Capturas no certifican UX premium completa. No reabrir LISTO, no cambiar H/I ni habilitar sucesoras, no A5 propia, no QA/main/PROD. Mantener EN_PROGRESO hasta evidencia global completa; A5 posterior por otra Task Javier.

## Referencias originales condicionadas
SKREF-03 Impeccable: pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199, .agents/skills/impeccable/SKILL.md; guia foco, responsive y evidencia.
SKREF-05 Taste: Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37, skills/taste-skill/SKILL.md; solo rutas publicas, preservar diseno Angular existente.
SKREF-10 Angular: angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9, skills/dev-skills/angular-developer/SKILL.md; pruebas accesibles y tipado.
SKREF-04 Motion, 06 Humanizer, 11 Threat Model, 12 .NET Performance: NO_APLICA a este cambio test-only sin animacion, copy UI, threat model ni profiling .NET.
Rollback seguro: git revert normal del changeset, sin reset ni force-push y conservando BITACORA.
