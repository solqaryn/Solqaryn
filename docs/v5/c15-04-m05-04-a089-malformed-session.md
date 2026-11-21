# C15-04.M05.04 — A0.89: contrato de sesión inválido en Chromium

Proyecto SOLQARYN, exclusivamente dev, PROJECT_SCOPE_LOCK=STRICT. Task DEV02: 6af16bb9492c8191b8e8c7d606f11507. Unidad existente en EN_PROGRESO; candado V17 SQV17_C15_04_M05_04 / 2000002297, sin segundo CLAIM.

## Hipótesis causal y riesgo

Una respuesta HTTP 200 no es prueba de autenticación: JSON roto o `success=true` con `data=null` no deben habilitar la superficie UX global, exponer `PRIVATE_ERROR_DO_NOT_RENDER` ni disparar escrituras. Esta adición obliga a interceptar una petición real de sesión durante Chromium y prueba cuatro combinaciones (2 cuerpos por 2 viewports de 375 y 1280 píxeles), con aserciones negativas observables. El proxy de sesión usa datos simulados: **NO prueba JWT firmado, RBAC, tenant A/B, HTTP backend real ni MySQL**.

## Estado y límites

A0 y diff limitado a una prueba Playwright y este documento; A1 dirigida pendiente de GitHub Actions al publicar SHA; A2/A3 seguridad e integración global NO_MEDIDO salvo evidencias separadas; A4 requiere checks completados SUCCESS del SHA causal, Scope Lock V5 y readback. No presentar como WCAG global ni auditoría completa de ~160 rutas. A5 independiente exclusivamente después de A0–A4. Si la prueba revela un defecto real, reparar bajo nuevo checkpoint, preservar historial.

No se reescribe CHANGELOG_AI.md: su contenido histórico de tamaño superior a 1 MB debe preservarse íntegro. Un append seguro requiere lectura completa verificada; nunca reemplazarlo por una respuesta truncada. Reversión: git revert normal sobre este commit, nunca force-push.
