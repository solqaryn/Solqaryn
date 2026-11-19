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

## V21 — Reparacion dirigida de CI por instalacion Chromium (19 noviembre 2026)

- Diagnostico causal: el workflow `v21-c14-01-m04-03-real-branding.yml`, SHA fuente `90ae8a8b80b88b3dd6888a4d6ef2525c6301a687`, run `44523489248`, job `135557880732`, concluyo FAILURE en el paso **Instalar Chromium**, antes de ejecutar los tres casos de aceptacion reales. No fue un PASS ni evidencia de que los tests hayan fallado.
- Hipotesis verificable P2: fallo temporal de resolucion/descarga de dependencias Ubuntu APT. Reintentos maximos tres, espaciados 15/30 segundos, sin omitir `--with-deps`, sin desactivar controles; agotados los intentos el job falla. El nuevo SHA exige readback de CI exacta para saber si se corrigio. Esta reparacion **NO** certifica proveedor real de branding, alcance global M04, A0–A4 ni A5.
- Sin modificaciones a runtime productivo, endpoints, JWT, RBAC, EF, MySQL, secretos o QA/main/PROD. Rollback: revert Git normal del commit de reparacion tras verificar HEAD.

## V21 - A0.1 proveedor publico sin interceptacion (DEV05 rescate)
- Playwright Chromium consulta el endpoint autentico /empresa-configuracion/publica de ASP.NET Core 10 con MySQL 8.4 efimero; compara respuesta con identidad Angular tras validar tenant A. No se inyecta ni intercepta el proveedor en este nuevo caso.
- La configuracion publica es global: esta evidencia no certifica un proveedor de marca tenant-especifico ni marca propia para empresas A/B. Los tres casos previos siguen simulando exclusivamente el proveedor visual; no contarlos como integracion real de marca.
- Requiere ejecucion dirigida COMPLETED SUCCESS del SHA exacto, Scope Lock, V5 y controles relevantes. Mantener VALIDANDO hasta completar A0-A4 del alcance atomico; A5 independiente. Sin QA/main/PROD, secrets ni datos reales.
- Rollback aditivo mediante git revert ordinario del commit.

## V21 - A0.2 reparacion causal de asercion P0 (DEV05)
- SHA previo 79b56088: run 44602189955, job 135836250513, 3/4 Chromium; el nuevo GET real /empresa-configuracion/publica devolvio **HTTP 503**, no 200. Los otros 3 escenarios A/B con proveedor simulado aprobaron.
- Diagnostico: el P0 del backend niega la configuracion PUBLICA GLOBAL ambigua al coexistir dos empresas en MySQL efimero, antes de exponer datos. NO desactivar guard, ni cambiar runtime, tenant/JWT/RBAC o HTTP para fabricar 200.
- Contrato corregido: el cuarto test ejecuta HTTP autentico y exige 503 fail-closed, ausencia de identidad A/B en la respuesta, login JWT real y tenant-context/1 200, pero UI neutral SOLQARYN; no afirma proveedor de branding tenant-especifico positivo.
- Sigue faltando integracion real del proveedor por tenant, aceptacion atomica completa A0-A4 y nueva CI exact-SHA; no LISTO ni A5. Rollback mediante git revert normal.

