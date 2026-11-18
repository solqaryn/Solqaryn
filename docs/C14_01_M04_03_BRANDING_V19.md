# C14-01.M04.03 - Branding dinámico y respuesta tardía A/B

## A0 / riesgo R2 (identidad multiempresa)
Incremento de pruebas acotado al servicio frontend existente EmpresaIdentidadService: contrato de branding y configuracion dinamica. Se añaden E2E no, cinco tests unitarios Angular y workflow GitHub-hosted; no cambia implementacion de app, backend, DB, permisos, tokens ni UI de DEV05. Rollback git revert normal.

## A1 aserciones capaces de fallar
Defecto positivo/fallback: marca y logo B, mensaje y descripcion, copyright anual. Negativos: reset plataforma borra identidad A; respuesta demorada A no reemplaza B; fallo de proveedor deja plataforma neutral; guardar B revoca cualquier A pendiente y conserva politica copyright sin sustitucion. Cinco unitarias dirigidas en Angular runner real.

## A2 aislamiento
Las respuestas A/B son fixtures unitarios, NO integración HTTP ni certificación de JWT/RBAC backend, MySQL o tenant real. La regla de version de respuesta se ejecuta en el servicio genuino. No exponer secretos, grants ni guardar identidades de usuarios; condiciones negativas verifican no contaminación de branding.

## A3 integración pertinente
GitHub Actions instala dependencias fijadas, ejecuta el servicio Angular y five tests, lint/tipado y build producción. No se cambian HTTP/API/DB, por lo cual integración provider HTTP y MySQL son NO_APLICA motivado; no atribuir su PASS.

## A4/A5 y continuidad
Conservar VALIDANDO mientras CI externa siga QUEUED/IN_PROGRESS. Requiere check GitHub-hosted directed SUCCESS, Scope Lock, V5, SHA exacto, readback PLAN/COLA/DAG/BITACORA; no considerar SKIPPED PASS. A5 únicamente Task Javier distinta; no habilitar QA/main/PROD. El incremento no certifica la aceptación completa de M04.03 sin evidencia adicional de UI dinámica.
