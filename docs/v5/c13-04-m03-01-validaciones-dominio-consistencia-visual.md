# SOLQARYN V5 - C13-04.M03.01: validaciones de dominio de coherencia visual

Alcance: reglas reales C#/.NET 10 y xUnit en dev, autor DEV06 Javier. Autoridad Standard V5 / PLAN / COLA / DAG, PROJECT_SCOPE_LOCK=STRICT.

## A0: arquitectura, riesgos, dependencias

Dominio introduce V5VisualConsistencyDomainRules como validador puro de tenant, permisos, evidencia de pantalla/estado, viewport, teclado, texto de error, privacidad, contraste y SHA. Caller debe suministrar contexto autentificado en backend y catalogo aprobado por servidor. Esta microtarea no conecta fuente real JWT/RBAC, no registra DI/API, no renderiza Angular, ni modifica datos, migraciones, proveedores, eventos, webhooks, tareas ni produccion. Domain no depende de Infrastructure.

## A1-A3: comportamiento real y limites

Resultado READY_FOR_BROWSER_REVIEW significa solamente que la evidencia es formalmente elegible para revision independiente; no equivale a accesibilidad WCAG, permiso tenant, PASS browser o release. Pruebas positivas y negativas en xUnit cubren: contexto legitimo sin privilegios nuevos; A contra B; membresia revocada y permiso insuficiente; SHA obsoleto o incompleto; superficie forjada o no autorizada; estado/viewport no admitido; referencia con inyeccion y PII; teclado y reduced motion; feedback de errores; contraste inferior al umbral; inputs de autoridad nulos. Ningun escenario xUnit demuestra experiencia real en Chromium o aislamiento del backend conectado a JWT/MySQL.

Solo estados LOADING, EMPTY, ERROR, SUCCESS, FORBIDDEN y anchos 320,390,768,1280 son pistas del contrato, no mediciones runtime. Futura integracion UI requiere Playwright y lector/teclado reales con evidencia y SHA. Rollback git revert normal sin reescritura de historia.

## A4-A5: CI, registros y separacion

El cierre requiere GitHub Actions backend .NET10/xUnit, seguridad multiempresa y Scope Lock/V5 completed/success sobre SHA aplicable con blobs preservados, PLAN/COLA/DAG/BITACORA readback. Si checks requeridos cancelados, en progreso o fallidos, queda VALIDANDO con owner DEV06. A5 posterior por otra Task Javier distinta del autor; no main/QA/PROD ni activacion de servicios.
