# C09-04.M04.02 — Estados privados de mensajería multicanal (V19)

**Alcance y propietario:** SOLQARYN dev, PROJECT_SCOPE_LOCK=STRICT; microtarea C09-04.M04.02, TASK_ID DEV03 6adc2e4bd8a48191ac7d8641ed95a1a3. Riesgo R2 por contexto de mensajería y tenancy.

## A0: unidad aislada y dependencias

Entrega Angular 22.2.1 standalone en `c09-04-m04-02-estados.component.ts`; TestBed dirigido y GitHub Actions exact-SHA. Los estados autorizados son `cargando`, `vacio`, `error`, `exito`, y denegación derivada **exclusivamente** de sesión global `Plataforma` con `esAdministrador=true`. Ninguna bandera de rol empresarial concede acceso. Fallo o excepción de sesión = denegación. Mensajes opacos; no interpolar errores externos.

El dueño DEV04 de `C09-04.M04.01` tiene pendiente la ruta/pantalla protegida. Esta unidad se desarrolla en archivos nuevos y **no modifica su ruta ni integra componentes mientras haya escritor**. El componente no es todavía una pantalla navegable; su conexión a esa ruta requiere revisión separada, guard del Router y backend. Ningún estado `exito` acredita entrega a proveedor.

## A1/A2/A3/A4 con límites explícitos

- A1: ocho tests dirigidos positivos, negativos, frontera de estados, revocación, error/entrada hostil y excepciones. Requieren ejecución real en runner; resultados **no acreditados antes de CI**.
- A2: comprobación de sesión Plataforma admin vs anónimo, Empresa, no admin y revocación; sin envío, formularios, datos de tenant, operaciones mutantes ni logs privados. La UI jamás sustituye backend JWT/RBAC.
- A3: integración Angular TestBed + compilación en GitHub-hosted para el componente aislado; Playwright/Chromium se difiere hasta su montaje real en la ruta de M04.01. No hay proveedor, MySQL, SMTP, WhatsApp, webhooks ni JWT firmado; **NO_APLICA a este diff**, no certificar runtime inexistente.
- A4: `verify-solqaryn-project-scope`, V5 changelog/procedencia, Angular tests, lint, build y CI requerida en SHA exacto deben finalizar SUCCESS antes de LISTO_DEV. `SKIPPED/CANCELLED/QUEUED/IN_PROGRESS` nunca son PASS. PLAN/COLA/DAG y BITACORA requieren readback.
- A5: revisión independiente por otra de cinco Tasks Javier, jamás autor DEV03.

**Rollback:** revert normal del commit publicado (sin force-push), con comprobación de HEAD, preservando cambios posteriores y la historia de BITACORA. No modificar QA, main, PROD, Aiven, secretos ni configuración de otras Tasks.
