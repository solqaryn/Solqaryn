# C06-01.M06.01 — Matriz causal de pruebas reales frente a mocks

**Contrato:** SOLQARYN V5 FINAL, `PLAN_MAESTRO!D896` — revisar que las pruebas corresponden al alcance real y no a mocks insuficientes. `PLAN!E896` expresa la meta acumulativa de M06, no un permiso para certificar toda la landing ni el negocio. Gobierno `PROJECT_SCOPE_LOCK=STRICT`, sólo `solqaryn/Solqaryn@dev`; riesgo **R0 documental** (ningún cambio de runtime). Fecha de corte: 21 noviembre 2026. Este dictamen no crea pruebas, despliegues ni estados de negocio.

## Evidencia de ejecución causal y límites

| Unidad fuente V5 | Ejecución/observación verificada | Qué sí acredita (D de ese ID) | Qué **no** acredita |
| --- | --- | --- | --- |
| `C06-01.M05.01` | `PLAN!H890=LISTO`, evidencia CI Chromium 5/5, capturas de ancho/foco, revisión A5 independiente | Alcance observable de la página pública y estados de navegación local | Contratos privados, disponibilidad externa, planes/legales publicados |
| `C06-01.M05.02` | `PLAN!H891=LISTO`, evidencia registrada de HTTP real `/health`/`/health/ready`, Oracle EF MySQL 8.4 efímero; A5 por otra Task | Respuesta de salud/readiness y arranque aislado del proveedor que realmente fue ejercitado | Signed JWT multiempresa A/B, autorización de recursos, proveedor de negocio en producción |
| `C06-01.M05.03` | `PLAN!H892=LISTO`, evidencia registrada de servicio MySQL efímero, rollback y health, revisión A5 distinta | Pruebas aisladas del proveedor y reversión sobre banco efímero; no hacer equivalencia con producción | Credenciales productivas, persistencia en tráfico real, flujos autenticados |
| `C06-01.M05.04` | `PLAN!H893=LISTO`, Playwright real Angular/Chromium en cinco anchuras, CTA `/login`, estados públicos; A5 distinta | Caso **positivo** de navegación pública usando DOM/rutas Angular reales | Pasar de `/login` a dashboard autenticado ni resolución real de tenant |
| `C06-01.M05.05` | `PLAN!H894=LISTO`, Chromium 7/7 y PNG con respuestas sintéticas de error; revisión A5 independiente | **Negativos de la UI pública**: consulta no autorizada por query, no reflejar tokens, no invocar API privada, errores sanitizados, bloqueo de click duplicado | Ataques auténticos BOLA/IDOR contra API, revocación JWT, BD/propietario de tenant |
| `C06-01.M05.06` | `PLAN!H895=LISTO`; `5c54571785ad0fc89f74d579b808a85c7d31c5d7` [CI Chromium](https://github.com/solqaryn/Solqaryn/actions/runs/44956939907/job/137007041604) `completed/success`: **5/5**, diez PNG, cinco tamaños, Tab/Enter, 200% simulación CDP, 204/403/404/429/500, `Scope Lock` SUCCESS. [Artefacto](https://github.com/solqaryn/Solqaryn/actions/runs/44956939907/artifacts/14727862529) | Regresión y a11y básica de `/conoce-solqaryn` en navegador **real** GitHub-hosted, observaciones de tiempo **sólo de CI** | Core Web Vitals de usuarios, WCAG exhaustivo, pruebas en teléfonos físicos, APIs privadas; no se inspeccionaron visualmente los PNG de este job en la revisión independiente DEV04 |

**Resultado:** las suites Chromium sí ejecutan un navegador Angular real en GitHub-hosted, mientras que las respuestas de `/health` en varias suites provienen del **fixture HTTP local** `frontend/scripts/c0601-health-fixture.mjs`. Separar esos dos hechos es obligatorio: un navegador real no convierte un backend sintético en integración HTTP/JWT/MySQL de negocio. El documento del scope `C06-01.M05.06` enumera los cuatro archivos del diff, test `3116b76221ea9801cf5af1017f9df6b7d20099fb`, workflow `4bb6b5ebc20493c1319eb5126e756bff3a082f58`, documento `0e4b65361f1d513221c7a263e1fd0a2026777338`; las pruebas no modifican código de producción.

## Incidencias abiertas y decisión de alcance

1. **UI-03, severidad P1 y estado PARCIAL:** en `C06-01.M05.04` está documentado que la raíz `/` todavía redirige a `/login`. Las verificaciones sobre `/conoce-solqaryn` no la corrigen. No eliminar/ocultar la incidencia ni conceder PASS global de toda la experiencia.
2. **Ruta pública y permisos:** revisar separado acceso `/login` y rutas privadas con JWT firmado, RBAC real, pertenencia tenant A/B, HTTP servidor y MySQL aislado cuando el contrato de otra microtarea lo exija; ni el fixture local ni el `test-only` de la landing sustituyen esa evidencia.
3. **UX premium:** las imágenes de CI existen y los asserts de overflow/teclado son observables, pero la revisión de diseño integral, hardware real, zoom de navegador/sistema, Lighthouse completo, field CWV, contraste WCAG 2.x exhaustivo y experiencia de formularios externos siguen **NO_MEDIDO / NO_CERTIFICADO** según superficie.
4. **V5/A5:** conservar A5 aprobadas de M05 por terceros como evidencia específica de cada D890–D895; ninguna prueba .01–.05 obliga a cerrar el criterio acumulativo E. `C06-01.M06.01` es una **auditoría documental**, no un nuevo A5 ni una reapertura de tareas LISTO.
5. **CI causal:** `completed/success` de la prueba exact-SHA respalda sólo su alcance; checks globales `in_progress/skipped` nunca equivalen a PASS de una prueba requerida. No reutilizar un SHA descendiente sin verificar identidad de blobs o equivalencia.
6. **Rollback:** revertir únicamente este documento por commit normal en `dev` si se requiere corrección; sin reset/force-push. `BITACORA` histórica y owners quedan intactos, no tocar `QA/main/PROD`.

## Matriz A0–A5 del presente diff R0

- **A0:** diff exclusivo de la presente matriz de cobertura, contra PLAN D896 y fuentes técnicas fechadas; sin API, UI, datos, scripts o CI modificados.
- **A1:** fuentes cruzadas con CI real, limitaciones explícitas y casos positivos/negativos existentes; *nuevo test de ejecución: NO_APLICA* porque no se cambia runtime.
- **A2:** JWT/RBAC/tenancy de negocio **NO_APLICA** al diff R0; conservar el límite y las exigencias de cada microtarea R2.
- **A3:** MySQL/Chromium de integración **NO_APLICA** como nueva prueba de este diff documental; las suites previas se describen con exactitud.
- **A4:** exigir `verify-solqaryn-project-scope` exacto para el SHA de publicación y readback de `PLAN/COLA/BITACORA`; V5 provenance si aplica; cualquier check causal pendiente impide cierre.
- **A5:** revisión posterior independiente por otra Task Javier distinta del autor/reparador DEV04. No autoaprobar.

**V23, routing condicional:** SKREF-06 Humanizer de origen original `blader/humanizer@9862685f575c65a8247f90369951df1b3416e3d6` fue consultada para claridad sin alterar evidencia. SKREF-03 Impeccable, 05 Taste y 10 Angular se consultaron para la revisión precedente de UI pública, pero **NO_APLICA** imponer nueva implementación de UI ni generar capturas porque este diff sólo documenta cobertura; SKREF-04 motion, 11 threat model y 12 .NET perf son **NO_APLICA** en ausencia de cambio correspondiente. Mantener fuentes/pins según `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md`, sin importar librerías ni skills.

Dictamen limitado: existen pruebas reales para los contratos D890–D895 tal como consta en el Sheet y GitHub, pero faltan las evidencias identificadas para afirmar cumplimiento acumulativo de M05/M06, seguridad privada y producción. El documento registra brechas y siguiente evidencia, sin otorgar nuevas autorizaciones ni crear dependencias.
