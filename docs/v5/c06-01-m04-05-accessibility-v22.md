# C06-01.M04.05 — Verificar responsive, teclado, foco, labels y contraste

SOLQARYN V22, 2026-11-21, único repo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK STRICT. PLAN D888 manda la prueba concreta, E888 es meta acumulada de M04 y no obliga a certificar toda M04 con esta microtarea. Riesgo R1 test-only.

## Implementación y prueba real
Se incorpora Playwright/Chromium sobre la ruta pública Angular 22 /conoce-solqaryn: 320/375/768/1440 px sin overflow; landmarks/main/h1 y navegación etiquetada, links con nombre accesible; ningún formulario no autorizado; teclado Tab activa skip-link con borde visible, Enter lleva el foco a main sin abandonar la ruta; nueve clases de texto con contraste computado en el navegador >=4.5:1 WCAG AA. Casos negativos fallan ante overflow, nombres vacíos, foco oculto o contraste menor. Ejecución GitHub-hosted aislada, build/tipado Angular, skill/scope lock exact-SHA, 0 reintentos para no ocultar fallos.

## A0-A5 y limitaciones
A0: diff sólo de test, workflow, evidencia y changelog; no cambia landing ni runtime privado. A1: asserts positivos, negativos, móviles y límites de viewport. A2: ausencia de controles privados/inputs, JWT/RBAC/tenant HTTP NO_APLICA pues no cambia runtime ni D888 los exige. A3: Chromium real+Angular real; MySQL NO_APLICA. A4: check requerido debe terminar SUCCESS exact-SHA, no SKIPPED, CANCELLED ni IN_PROGRESS; Scope Lock y procedencia V5. A5 pendiente por otra Task Javier distinta de DEV05. LIMITACION: no certifica lector de pantalla humano, aparatos físicos, CWV de campo, configuración comercial, toda M04, QA/main/PROD. Rollback git revert FF, sin sobrescribir otras tareas.
