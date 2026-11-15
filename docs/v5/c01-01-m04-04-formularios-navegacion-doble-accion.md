# SOLQARYN V5 — C01-01.M04.04: formularios, navegación y doble acción

**Repositorios:** solqaryn/Solqaryn, rama dev; PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente:** STANDARD V5 FINAL, PLAN_MAESTRO fila 167.

## Alcance real
Cobertura de regresión sobre el formulario existente de edición de usuarios en ruta privada. Se utilizan formularios reactivos, navegación existente, señales de guardado y la autorización por acción implementada por el backend. No se crea endpoint, ruta nueva, privilegio, permiso ni nueva fuente de identidad. Se conserva control de tenant y RBAC del servidor; el test de frontend nunca los sustituye.

## Pruebas dirigidas (estado: pendiente de resultado real del runner)
1. Formulario inválido no emite escritura y no navega.
2. Sin permisos de asignar rol o restablecer contraseña, la llamada conserva el rol previo y omite la contraseña, incluso si el control deshabilitado recibe valores programáticos.
3. Dos invocaciones durante una petición pendiente generan una sola llamada; la navegación y el mensaje de éxito ocurren únicamente después de la respuesta.
4. Rechazo 403 no navega ni reintenta por sí mismo; permite un segundo intento explícito tras volver a estado editable.
5. ID de ruta inválido no consulta ni escribe usuarios y redirige a la lista.

## A0–A5 y rollback
A0: cambios aditivos y acotados a test Vitest, workflow dirigido, este documento y CHANGELOG; sin modificaciones a runtime, API, Domain, Application, EF o MySQL.
A1: las cinco pruebas unitarias deben obtener completed/success en GitHub Actions para el SHA exacto; no se atribuye PASS por publicación.
A2: se verifican límites de UI no autorizada y no-envío; no son pruebas de bypass HTTP, penetración, tenant A/B real ni de backend.
A3: Vitest Angular, lint y build de producción dirigidos; sin acceso a secretos ni servicios de producción.
A4: Scope Lock, V5 provenance y CI causal completed/success sobre SHA válido, diff y PLAN/COLA/BITACORA readback antes de LISTO_DEV.
A5: revisión independiente por otra Task Javier; jamás autoaprobación ni pase implícito a QA/main/PROD.
Rollback: git revert normal; ningún force-push, ninguna migración.
