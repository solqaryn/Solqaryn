# C02-01.M04.01 — Menú de contexto empresarial verificado

Autor DEV04; scope exclusivo solqaryn/Solqaryn@dev. Riesgo R2 por identidad, membresías y UI Angular 22.2.1.

La pantalla de credenciales nunca solicita empresa. Después de validar el JWT y el tenant por backend, el shell consulta GET /tenant-context/empresas una vez por contexto (usuario + empresa). «Cambiar empresa» solo aparece si la lista autenticada contiene más de una membresía con IDs únicos e incluye el tenant verificado. Sin respuesta o si llega una respuesta antigua, falla cerrado; localStorage no autoriza ni habilita el botón. El cambio borra contexto y permisos antes de regresar al gate de selección, donde el servidor verifica la nueva membresía. No concede JWT ni privilegios desde Angular.

A0: cambios acotados a servicio de contexto, shell, pruebas y documentación. A1: pruebas positivas N=2, N=1 y negativas duplicados, HTTP 403, ID ajeno y respuesta obsoleta. A2: tenant A/B, roles y revocación únicamente como control visual fail-closed; autorización real de backend intacta. A3: tipado, tests Angular y build real en GitHub-hosted CI; MySQL/Playwright no certificados por esta entrega, no simularlos. A4: Scope Lock y V5 exact-SHA con readback institucional. A5: revisión por Task Javier distinta de la autora DEV04.

Rollback seguro: revert normal de un solo commit; sin ramas, migraciones, cambios de backend, QA, main o producción.
