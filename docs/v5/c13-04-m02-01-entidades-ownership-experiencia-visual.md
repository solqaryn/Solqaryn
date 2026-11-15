# SOLQARYN V5 FINAL — C13-04.M02.01: entidades visuales, ownership e invariantes

**Autor:** PRIMARY DEV 06 (`6af16bbad26881918c283abe06e1f32a`); **fecha:** 2026-11-15; **repo:** `solqaryn/Solqaryn@dev`; **PROJECT_SCOPE_LOCK=STRICT**. Artefacto **documental** de contratos y modelo; no modifica Angular, CSS, backend, DB ni runtimes.

## Autorización y dependencia

[STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) fijan la unidad. Tras `C13-04.M01.06` LISTO+A5 independiente sobre `c707bb8edfba59a6a5c90b45671ffd3a2df20bf6`, PLAN fila 1988 estaba PENDIENTE sin owner; COLA G/O=SI y DAG F=VALIDADO/G=SI. Se efectuó claim condicional con Task única y lectura posterior. `C13-04.M02.02` también está habilitada sin ser requisito técnico previo de esta unidad: ambas comparten M01.01–06+A5.

Se reutilizan como fuentes conceptuales [M01.01](./c13-04-m01-01-alcance-no-regresion-experiencia-unificada.md), [M01.02](./c13-04-m01-02-actores-entradas-salidas-superficies-ui.md), [M01.03](./c13-04-m01-03-inventario-codigo-brechas-coherencia-visual.md), [M01.04](./c13-04-m01-04-riesgos-supuestos-decisiones-experiencia-unificada.md), [M01.05](./c13-04-m01-05-criterios-aceptacion-experiencia-unificada.md) y la revisión independiente M01.06. Riesgos, supuestos y decisiones no se consideran cerrados por referencia.

## Entidades conceptuales U01–U14

| ID | Objeto lógico | Clave candidata | Autoridad/ownership | Restricción de diseño |
| --- | --- | --- | --- | --- |
| U01 | VisualSurface | surface_key | producto/área autorizados | No crear ruta o permiso por diseño |
| U02 | AudienceContext | actor_key | identidad backend real | Anónimo no hereda rol privado |
| U03 | TenantTheme | tenant_id | tenant autenticado | No reutilizar datos de A en B |
| U04 | BranchContext | branch_id + tenant_id | sucursal validada por servidor | No confiar en query param |
| U05 | ComponentContract | component_key + version | sistema de diseño autorizado | Sin código SCSS/HTML escrito aquí |
| U06 | ActionContract | action_id + resource_key | permiso backend | El botón visible no autoriza |
| U07 | NavigationContract | route_key + role_scope | router e IAM reales | Guard Angular no sustituye RBAC |
| U08 | FormState | form_key + state | componente padre | Error, disabled y success accesibles |
| U09 | FeedbackState | flow_key + state | flujo activo | Loading/empty/error no ocultan acción |
| U10 | FocusLifecycle | focus_origin + target | DOM real probado en etapa futura | Foco restituido tras modal |
| U11 | ResponsiveBreakpoint | viewport_key | matriz de diseño | Reflow sin pérdida de contenido |
| U12 | ContrastToken | token_key + mode | tokens de tema aprobados | Medición real antes de decir WCAG |
| U13 | EvidenceScenario | scenario_key + source_sha | revisor autorizado | No confundir caso futuro y ejecución |
| U14 | RollbackContract | surface_key + baseline_sha | versión Git y pruebas | No revierte datos comerciales |

Relaciones: `VisualSurface` puede contener `ComponentContract`, `ActionContract` y estados; `AudienceContext` y `TenantTheme` determinan presentación, nunca permiso de backend. `FocusLifecycle` y `ResponsiveBreakpoint` describen observables futuros por componente. `EvidenceScenario` enlaza oráculo y SHA de ejecución real; `RollbackContract` se refiere a Git, sin efectos sobre transacciones.

## Invariantes I01–I14 y fronteras de confianza

| ID | Propiedad exigida |
| --- | --- |
| I01 | Tenant A/B conserva tema y permisos aislados al cambiar de sesión |
| I02 | Toda acción privada necesita autorización backend independiente del estado visual |
| I03 | Los componentes interactivos exponen nombre accesible y foco visibles |
| I04 | Estados loading/empty/error/success son distinguibles sin color |
| I05 | Un modal restaura foco y conserva navegación por teclado |
| I06 | Responsive 320/390/768/1280 mantiene CTA y errores disponibles |
| I07 | Reduced-motion conserva información y flujo |
| I08 | Los tokens de contraste requieren medición real futura, no promesa por CSS |
| I09 | El componente no renderiza PII o datos de otro tenant por caché |
| I10 | El frontend no persiste JWT/secretos en telemetría ni logs de prueba |
| I11 | Contratos de ruta no elevan acceso cuando una membrecía se revoca |
| I12 | Oráculos positivos y negativos se versionan con SHA de código |
| I13 | CANCELLED/SKIPPED/queued/in_progress no son PASS de CI |
| I14 | A5 independiente y permisos por ambiente no son transferibles a QA/PROD |

La autoridad de IAM/JWT/RBAC, scopes empresa/sucursal y revocación permanece en backend .NET; un flag de Angular, un tenant en URL o un color de tema no puede conceder capacidades. Los estados visuales propuestos no sustituyen medición WCAG ni accesibilidad testada.

## Casos positivos/negativos P01–P08 y N01–N08 FUTUROS_NO_EJECUTADOS

| Positivo | Oráculo futuro | Negativo | Denegación futura |
| --- | --- | --- | --- |
| P01 | Login válido muestra flujo y foco claro | N01 | JWT revocado o inválido no habilita ruta privada |
| P02 | Tenant A visualiza su tema y datos | N02 | Navegación a tenant B no muestra recursos A |
| P03 | Formulario anuncia mensaje de error textual | N03 | Error sólo cromático no supera aceptación |
| P04 | Modal responde a teclado y retorna foco | N04 | Focus trap o foco perdido bloquea |
| P05 | CTA visible en 320/390/768/1280 | N05 | Desborde oculta CTA indispensable |
| P06 | Spinner anuncia progreso y salida recuperable | N06 | Carga eterna sin recuperación bloquea |
| P07 | Preferencia reduced-motion no omite contenido | N07 | Movimiento obligatorio impide usar flujo |
| P08 | Evidencia exact-SHA documenta test real futuro | N08 | Archivo spec sin job PASS no certifica |

Para ejecutar en M03–M05 serán necesarios ruta y rol reales, tenant A/B sintético, viewport, navegador, fixture, resultado esperado/observado, test Playwright/HTTP causal, SHA y check `completed/success`. No existe PASS funcional en este documento.

## Pruebas documentales A0–A5, riesgos y reversión

**A0:** archivo Markdown + CHANGELOG autocontenido, 14 entidades y 14 invariantes; no se escriben Angular ni CSS. **A1:** ocho pares positivos/negativos documentados como oráculos futuros. **A2:** tenant A/B, membresía revocada, RBAC backend y privacidad de evidencias, sin declarar cobertura runtime. **A3:** M01.01–06 LISTO+A5 y contratos M02 independientes; sólo revisión de fuentes, sin navegador/HTTP. **A4:** fast-forward/CAS y diffs de dos Markdown (o lote documental independiente permitido), Scope Lock/V5 `completed/success` en SHA pertinente, BITACORA y readback. **A5:** revisor distinto de esta Task; no autoprobación.

Rollback: `git revert` normal en dev, nunca force-push o modificación de historia. No migraciones, cambios de tema operativo, cuentas, QA/main/PROD, webhooks ni pagos.
