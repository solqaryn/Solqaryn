# C02-01.M04.01 — Menú de contexto empresarial verificado

Autor DEV04; scope exclusivo solqaryn/Solqaryn@dev. Riesgo R2 por identidad, membresías y UI Angular 22.2.1.

La pantalla de credenciales nunca solicita empresa. Después de validar el JWT y el tenant por backend, el shell consulta GET /tenant-context/empresas una vez por contexto (usuario + empresa). «Cambiar empresa» solo aparece si la lista autenticada contiene más de una membresía con IDs únicos e incluye el tenant verificado. Sin respuesta o si llega una respuesta antigua, falla cerrado; localStorage no autoriza ni habilita el botón. El cambio borra contexto y permisos antes de regresar al gate de selección, donde el servidor verifica la nueva membresía. No concede JWT ni privilegios desde Angular.

A0: cambios acotados a servicio de contexto, shell, pruebas y documentación. A1: pruebas positivas N=2, N=1 y negativas duplicados, HTTP 403, ID ajeno y respuesta obsoleta. A2: tenant A/B, roles y revocación únicamente como control visual fail-closed; autorización real de backend intacta. A3: tipado, tests Angular y build real en GitHub-hosted CI; MySQL/Playwright no certificados por esta entrega, no simularlos. A4: Scope Lock y V5 exact-SHA con readback institucional. A5: revisión por Task Javier distinta de la autora DEV04.

Rollback seguro: revert normal de un solo commit; sin ramas, migraciones, cambios de backend, QA, main o producción.

## Reparacion cruzada V21 DEV03 — membresias obsoletas A/B

La seleccion de un nuevo tenant limpia inmediatamente los IDs autenticados del contexto anterior antes del GET /tenant-context/{id}. Tras confirmar B no se reutiliza la lista verificada para A: el menu permanece oculto hasta un nuevo GET autenticado de membresias para la empresa seleccionada. Regresion adversarial A→B con lista antigua, N=1/N=2 y revocacion. No se modifica backend, JWT, RBAC, MySQL, sesiones ni permisos de usuario. La integracion real exigida por A0-A4 y la revision A5 de otra Task distinta del autor DEV04 y reparador DEV03 siguen pendientes. Rollback: git revert normal del commit V21.

## V21 - Aceptacion real CI JWT A/B del menu (DEV04)

Se anade Playwright real para menu N=1 y N=2: API ASP.NET Core 10, JWT firmado efimero, MySQL 8.4 y Chromium de GitHub-hosted. El usuario A tiene solo A, usuario B solo B, usuario A+B posee ambas y debe ver el boton unicamente tras listarlas desde GET autenticado. Prueba negativa de tenant B falsificado por A. El workflow revoca B en MySQL y una ejecucion posterior comprueba que el boton desaparece y el backend deniega B. No se intercepta HTTP ni se concede autoridad desde localStorage. Los checks CI nuevos pueden permanecer in_progress: entonces A0-A4 y LISTO quedan pendientes; A5 por Task Javier diferente del autor DEV04 y reparador DEV03. Rollback git revert normal, solo dev.

## V21 — regresiones adversariales de autenticacion y tenant inexistente (DEV02, rescate)

Se incorporan dos casos Playwright GitHub-hosted adicionales, sin mocks ni interceptacion HTTP: JWT firmado en el login real cuya firma se adultera antes de GET /tenant-context/empresas (debe responder 401), y JWT valido de A que intenta resolver el tenant inexistente 999999 con cabecera X-Empresa-Id falsificada (debe responder 403/404). Ambos reutilizan el ASP.NET Core 10, MySQL 8.4 efimero, Angular y Chromium del workflow actual y se ejecutan ANTES de revocar B, preservando la prueba posterior de revocacion. No cambia permisos productivos ni rutas de negocio. El autor historico DEV04 permanece en PLAN!I, DEV02 es actor de reparacion, A5 debe ser independiente de ambos. Esto es incremento R2 causal, NO certifica la matriz completa por host/rubro/plan/sucursal/roles ni equivale a LISTO hasta CI exact-SHA y A0-A4 exigibles. Rollback seguro con revert normal solo en dev.

## V21 — ausencia de membresía empresarial para rol global (DEV03, rescate cruzado)

Caso negativo adicional Playwright/Chromium, API .NET 10 y MySQL 8.4 efímero: la identidad de plataforma `c1504_platform_only` posee asignación global, pero no registros en `UsuarioEmpresas`. Si se rechaza su login, no puede presentar el selector; si el login tiene éxito, `GET /tenant-context/empresas` debe devolver cero membresías empresariales o denegación explícita, y /perfil no puede mostrar el botón N2. Prueba adversarial R2 contra confusión entre rol de plataforma y membresías comerciales; no habilita nuevas autoridades por UI.

Este incremento no certifica matriz completa rubro/plan/sucursal/roles ni aceptación global A0–A4. Se exige CI exact-SHA y revisión independiente posterior. Owner histórico DEV04 preservado y DEV03 reparador; A5 sólo por tercera Task Javier. Exclusivamente dev, sin datos ni secretos productivos; rollback revert normal.
