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

## V21 — Incremento de aceptación visual sobre JWT y MySQL efímeros (2026-11-19)

- Alcance R2 y A0: se integra un nuevo Playwright/Chromium real contra API .NET 10 con JWT firmado, membresías empresariales A/B y MySQL 8.4 exclusivamente en GitHub-hosted CI. No cambia la implementación productiva ni permisos. El test comprueba marca y logo inyectados en el shell real de Angular para A/B y neutralidad de consola global; incluye denegación backend 403/404 del tenant ajeno y responsive 375/1280.
- A1/A2: tres casos adversariales de interfaz con JWT real, negativos de membresía A/B y ausencia de contaminación visual de marca entre usuarios. La marca pública se intercepta de manera explícita y determinista: **NO** constituye prueba de proveedor de branding auténtico, DNS, integraciones externas o configuración empresarial persistida. Los tokens y contraseñas son efímeros; no hay secretos persistidos.
- A3: el runner reutiliza el laboratorio aislado MySQL/ASP.NET/Angular previamente validado por C15; test dirigido, logs TRX/JUnit, compilación Release, lint, build y Chromium. No toca MySQL productivo, Aiven, Cloudinary real ni QA/main/PROD.
- A4: exigir SHA publicado y check-runs GHA completed/success de este workflow, Scope Lock, V5, frontend, backend y seguridad pertinentes. Si en cola, fallido o cancelado, conservar VALIDANDO sin atribuir PASS; readback GitHub/PLAN/BITACORA antes de considerar cierre.
- A5: una tercera Task Javier (no DEV01 autor ni DEV02 reparador) debe revisar cambios y evidencia cuando A0–A4 de la **unidad completa** estén satisfechos. El proveedor real y la cobertura global de superficies M04 permanecen NO_CERTIFICADOS por este incremento; conservar historial P1/P2 y VALIDANDO hasta completar la aceptación. Rollback por git revert ordinario sin fuerza.
