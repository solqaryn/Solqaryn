# P5-001.C — Delimitación institucional de tokens en DEV

- Autoridad: P5-001.C del Plan Comercial Inicial, dependencia P1-016.B. Alcance: identificar un único archivo/escenario, registrar ruta y SHA y un cambio mínimo.
- Fuente real y única de producto: `frontend/src/styles.scss`, blob `2eb48562fd77c758662af6b9683649b4f9698a64` sobre HEAD original `7f2fa95211c3fea8a3fba935450afec8e5ab1aaf`.
- Escenario: tokens CSS de `:root`, alias semánticos, uso en body/botones Angular Material y foco de teclado. No cambia el runtime; solo prueba de delimitación, workflow CI y acta.
- La ejecución anterior de Alex DEV 07 registró un candidato SHA256 `b86180135d21e6e232bc397c168b6c784822d0c51d96512b95bea63311e8b7db` que **no pudo recuperarse**. Esta resolución es **nueva, realizada desde la cuenta Javier por petición expresa del propietario**; no se atribuye al artefacto original de Alex. Conservar su autoría previa en el historial comercial.
- A0: diff mínimo, Node built-in sin nuevas dependencias/secretos/API/DB/migraciones; `node --check` y revisión estática.
- A1: ocho pruebas en Node sobre el archivo real: positivo, eliminación de token, CSS peligroso, duplicación, alias, consumidores Material, foco teclado y archivo vacío.
- A2: las pruebas rechazan valores CSS sospechosos y protegen el foco visible; no sustituyen RBAC/JWT ni demuestran el contraste dinámico en navegador.
- A3: el job `.github/workflows/p5-001-c-tokens.yml` ejecuta test de contrato sobre el árbol exacto de GitHub; controles de Scope Lock, frontend y backend del SHA integrado deben revisarse antes de cerrar.
- A4: después de publicar, comprobar `dev` exact-SHA, job dirigido PASS y readback de PLAN/COLA/BITACORA; no declarar PASS antes de CI.
- A5: pendiente de revisión independiente de otro ejecutor autorizado, necesaria para QA/main/PROD.
- Rollback: revert normal de prueba, workflow, acta y entrada en CHANGELOG_AI. No revertir commits de otros, ni alterar código productivo.
