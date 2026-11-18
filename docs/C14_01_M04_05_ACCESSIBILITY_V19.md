# C14-01.M04.05 — Accesibilidad responsive y teclado

## A0 / riesgo R1
Cuatro E2E Playwright de login publico y rechazo anonimo de QA privado. Nuevos archivos de test, workflow, evidencia y changelog. Sin cambiar codigo UI compartido en revision DEV05, API, JWT/RBAC/tenant, MySQL, secretos ni QA/main/PROD. Reversion git revert normal.

## A1 funcional positivo, negativo, frontera y error
Anchos 320/375/768/1280 sin overflow; nombres accesibles para campos; submit invalido bloqueado. Tab real desde inicio, foco visible, hitbox al menos 44 px, Space muestra/oculta contrasena sin POST. Contraste del titulo >=4.5 WCAG AA con cota inferior conservadora calculada contra fondo negro/blanco si el panel es translúcido. Un color CSS no reconocido causa fallo (no SKIP). Ruta QA privada anonima redirige sin revelar panel.

## A2 seguridad / no-alcance
Sin mocks HTTP, JWT sintético, credenciales ni datos tenant. El rechazo de Angular no certifica autoridad backend; comprobar JWT/RBAC/tenant vivo es NO_APLICA a este changeset (no cambia esa capa). No se aprueba QA/main/PROD.

## A3 integración causal
CI GitHub-hosted Angular 22.2.1 + Chromium real usando frontend en 127.0.0.1:4200 y cuatro pruebas. MySQL/EF y API autenticada NO_APLICA: no se modifican. Este incremento no pretende certificacion global de todos los flujos ERP.

## A4/A5 y rollback
Exigir Scope Lock y V5 y job Chromium completed/success exact-SHA; QUEUED/IN_PROGRESS/SKIPPED/CANCELLED no PASS. H=VALIDANDO hasta pruebas y aceptación suficientes; A5 revisión independiente de otra Task Javier. Readback en PLAN/COLA/DAG y BITACORA, liberar V17 solo tras prueba causal. Git revert de dev sin force-push.

## Reparacion causal CI V19 — 17 noviembre 2026 (DEV01)
En SOURCE_SHA fd0f4002772f59bb9e21e8cb9a73dc324ea9daad, run 44234634696 job 134567687275, Chromium termino FAILURE con 2/4 aprobadas: la reversibilidad de contrasena no reobtuvo el control tras cambio de tipo y el contraste consulto el DOM antes de esperar el encabezado. Se preserva evidencia QA_DEFECT_FOUND de pruebas y no se afirma defecto de produccion ni PASS. La correccion mantiene teclado real desde inicio, Tab para volver a alcanzar el boton sin focus programatico, aserciones positivas de dos estados de aria-pressed, espera de encabezado visible y el calculo conservador AA >= 4.5. Exigir CI dirigida 4/4 y controles Scope Lock/V5 exact-SHA antes de A0-A4 o LISTO; A5 distinto del autor. Mantener riesgos de contraste real sin suprimir controles, y rollback git revert normal.

## Ampliacion R1 de navegacion y superficies — 18 noviembre 2026 (DEV01)
Se agregan dos casos E2E Chromium sin mocks ni credenciales: cuatro rutas privadas ERP/plataforma verifican rechazo anonimo y cero mutaciones HTTP; tres rutas publicas de tienda verifican montaje real y ausencia de overflow horizontal a 320/768/1280 px. El alcance suma seis pruebas dirigidas; no habilita JWT/tenant positivo ni certifica backend o conversion comercial. Exigir job dirigido completed/success exact-SHA, Scope Lock y V5 antes de acreditar A1-A4; conservar VALIDANDO por cobertura global P1 y solicitar A5 independiente. La prueba original fallida en fd0f4002 y su reparacion ca6e61fc permanecen en BITACORA. Rollback con git revert normal.
