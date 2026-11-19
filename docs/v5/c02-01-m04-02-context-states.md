# C02-01.M04.02 — estados seguros para selección empresarial

Repositorio: solqaryn/Solqaryn, rama dev. TASK_ID ejecutora: 6adc2e55f47c81919f6374e561687076. Riesgo R2: identidad/tenant; PROJECT_SCOPE_LOCK=STRICT.

## Comportamiento y alcance

- 0 empresas autorizadas: rechazo explícito y controles deshabilitados.
- 1 empresa procedente de listado HTTP autenticado: preselección sin intersticial manual, pero **no** concede acceso hasta recibir confirmación independiente `GET /tenant-context/{id}` del backend actual.
- 2 o más empresas sin preferencia presente en la lista actual: selección manual; preferencia previa obsoleta queda descartada. Si una preferencia sigue en lista, se revalida igualmente contra el servidor.
- 403, lista duplicada o respuesta malformada: falla cerrado, no habilita contexto ni acceso, sin propagar información de un tenant ajeno.
- No modifica autoridad de JWT/RBAC, rutas API, MySQL, secretos, scopes globales, ni el shell de C02-01.M04.01 que gestiona otra Task.

## Evidencia A0–A5

A0: diff localizado en TenantSelector + TestBed, workflow dirigida, documentación y CHANGELOG; sin migraciones ni cambios de autorización.

A1/A2: pruebas de cero/una/dos empresas, preferencia obsoleta, 403, lista duplicada, y verificación de que no se emite `confirmado` antes del resultado HTTP. Las pruebas usan HttpTestingController y no simulan un JWT real.

A3: integración Angular TestBed + TenantContextService del producto real, ejecutada en GitHub-hosted Node 24; JWT firmado y aislamiento MySQL A/B extremo a extremo **NO ACREDITADOS** por este conjunto y siguen como requisito de aceptación transversal.

A4: GitHub Actions en SHA causal (TestBed, lint/Angular production build) y checks de Scope Lock/V5, security/frontend requeridos; si quedan QUEUED/IN_PROGRESS, no son PASS. PLAN/COLA/BITACORA readback y candado V17.

A5: revisión independiente de Task Javier distinta de DEV05, sin autopase. Rollback `git revert` no destructivo; QA/main/PROD no autorizados.

## Reparación causal V21 (TestBed CI)

El job 135567891157 del SHA c4422c80 detectó un defecto real: el `<select>` quedó habilitado en DOM tras devolver cero membresías, pese a que la autorización y el botón de envío estaban cerrados. `ngModel` pudo restablecer el estado nativo de `disabled`. Se vincula el atributo `disabled` directamente a los estados de carga/lista/ausencia de empresas y se comprueba tanto la propiedad como el atributo en TestBed. No se relajan validaciones de backend ni se concede contexto. El SHA reparado requiere una nueva CI dirigida y A5 independiente; no se convierte en LISTO sin A0–A4.
