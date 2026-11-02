# P1-003.B — Certificación real de soporte temporal en DEV

Estado actual: **PENDIENTE DE PR PROTEGIDO, CI LIVE y lectura posterior**. No declarar LISTO por haber creado esta prueba.

## Alcance

- GitHub Actions bajo environment DEV, fuente solqaryn/Solqaryn, ref dev y API Render DEV exacta.
- No se leen ni publican valores secretos; credenciales de prueba existentes y conexión MySQL DEV por variables protegidas.
- La credencial del operador es la cuenta real `javier`, cuya autoridad administrativa de plataforma debe estar ACTIVA y verificada por SELECT. Su contraseña de prueba debe estar configurada en el secreto de GitHub Environment DEV `SOLQARYN_DEV_PLATFORM_PASSWORD` (nunca se imprimirá ni almacenará en el repositorio). La cuenta empresarial de contraste `admin_prueba_a` usa legítimamente el secreto histórico `SOLQARYN_DEV_B_PASSWORD`; los nombres de los secretos empresariales siguen el contrato A/B/C existente y no deben intercambiarse. Si falta la contraseña de plataforma o no corresponde al operador, fail-closed sin otorgar roles, añadir membresías ni alterar contraseñas.
- Se comprueba que el operador no tenga membresías empresariales activas en A/B. Un administrador empresarial no puede abrir sesión de soporte.
- Solo dos sesiones de 1 minuto para las empresas A/B de prueba (target A=2, negativa B=3): Productos y Categorías, token de 256 bits en memoria, lectura solo GET, rechazo cross-tenant, cross-module, rutas de usuario y POST comercial, revocación y replay denegado, expiración cronológica.
- Lectura final SELECT MySQL valida sesiones revocadas, hash de token persistido sin secreto en claro y auditoría durable. Los artefactos de CI solo imprimen resultados booleanos sin PII/tokens.
- Efectos DEV autorizados: creación/revocación de dos sesiones efímeras y sus registros normales de auditoría. No se ejecutan INSERT/UPDATE/DELETE sobre empresas, roles, permisos, membresías ni datos comerciales.
- En caso de fallo posterior a una creación, intento de revocación en finally; si no es verificable, emitir NO_GO sin ocultar la incidencia.
- La prueba online se ejecuta únicamente desde código ya integrado en dev (push/dispatch), nunca desde código no revisado de PR.

## Prueba dirigida de CI

El filtro `FullyQualifiedName~P1003B` ejecuta `P1003BSupportRouteCertificationTests`, con aserciones reales de allowlist y contexto verificado por servidor, sin MySQL porque el gate dirigido no dispone de ese servicio. La integración MySQL original sigue en `SupportSessionMySqlIntegrationTests`, sin cambio de aserciones, ejecutada por los jobs que sí levantan MySQL; el escenario LIVE es otro gate posterior al merge.

## Aceptación

LISTO requiere: contrato offline PASS, test online PASS de JWT real y MySQL DEV para el SHA desplegado en Render, ejecución exacta con autorización del operador, rechazo empresarial, aislamiento A/B, expiración, revocación y auditoría, integración protegida del workflow con revisión independiente y readback de PLAN_MAESTRO, COLA y BITACORA. Si el secreto de la cuenta de soporte no corresponde a un administrador real de plataforma, conservar PENDIENTE/VALIDANDO y registrar el dato como impedimento de credenciales, sin inventar autoridad.
