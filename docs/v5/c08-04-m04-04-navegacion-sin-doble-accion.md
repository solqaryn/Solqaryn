# SOLQARYN V18 — C08-04.M04.04 verificación de navegación, mensajes y no duplicación

**Autor**: PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **PLAN** fila 1283, candado V17 SQV17_C08_04_M04_04 / 2000001283. **Scope** solqaryn/Solqaryn@dev, STRICT. **Riesgo** R1 UI y pruebas.

## A0 — Alcance y ausencia justificada de formularios

Se añaden regresiones reales Playwright/Chromium para la ruta privada informativa /extensiones-sectoriales, sin mutar implementación ni afirmar conectividad de módulos. Esta pantalla intencionalmente **no tiene formularios, botones de activación, POST ni doble-submit**. Se valida esa ausencia en vez de fabricar un formulario o un endpoint inexistente. El único enlace informativo Volver al panel debe alcanzarse mediante pulsaciones Tab desde el foco inicial y responder a Enter; no se aplica .focus() artificial. Se conservan authGuard y los contratos de C08-04.M04.01-.03 sin alterar cliente/servidor, tenants, JWT, RBAC, MySQL, infraestructura o CSS.

## A1/A2/A3 — Pruebas que pueden fallar

Caso positivo: sesión sintética autorizada, navegación de teclado, link /dashboard, Enter efectivo. Casos negativos: tenant sintético 403 redirige a login y nunca renderiza ámbitos; los mensajes no atribuyen activación ni autoridad; formulario/botón/control operativo ausente; ninguna solicitud a API de módulos y cero mutaciones HTTP. La inexistencia de controles operativos impide doble acción en este scope; no se certifica idempotencia del backend ni se reclama ejecución de un submit inexistente. El navegador es GitHub-hosted Chromium real; la identidad/tenant HTTP son fixtures sintéticas, NO JWT/RBAC/aislamiento A/B reales.

El workflow instala Chromium y exige Vitest previo de los cinco estados, typecheck/lint/build de Angular y los tres casos E2E. MySQL/EF/proveedores reales no son pertinentes porque no se modifican ni se consultan. No se ejecuta QA/PROD.

## A4/A5

Exigir SHA dev publicado fast-forward, Scope Lock/V5 y controles pertinentes completed/success, más Chromium causal. Mientras estén queued/in_progress dejar VALIDANDO/owner/RUN, nunca LISTO. BITACORA A:Z idempotente y readback PLAN/COLA/DAG; A5 exclusivamente por otra Task Javier. Rollback normal git revert, sin cambiar historia.
