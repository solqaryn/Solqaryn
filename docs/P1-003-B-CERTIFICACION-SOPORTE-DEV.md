# P1-003.B — Certificación real de soporte temporal en DEV

Estado actual: **PENDIENTE DE PR PROTEGIDO, CI LIVE y lectura posterior**. No declarar LISTO por haber creado esta prueba.

## Alcance

- GitHub Actions bajo environment DEV, fuente solqaryn/Solqaryn, ref dev y API Render DEV exacta.
- No se leen ni publican valores secretos; credenciales de prueba existentes y conexión MySQL DEV por variables protegidas.
- La credencial de operador en el slot heredado SOLQARYN_DEV_A_USERNAME debe poseer una asignación global de plataforma ACTIVA, verificada por SELECT real; si no, fail-closed sin otorgar roles, añadir membresías ni alterar contraseñas.
- Se comprueba que el operador no tenga membresías empresariales activas en A/B. Un administrador empresarial no puede abrir sesión de soporte.
- Solo dos sesiones de 1 minuto para las empresas A/B de prueba (target A=2, negativa B=3): Productos y Categorías, token de 256 bits en memoria, lectura solo GET, rechazo cross-tenant, cross-module, rutas de usuario y POST comercial, revocación y replay denegado, expiración cronológica.
- Lectura final SELECT MySQL valida sesiones revocadas, hash de token persistido sin secreto en claro y auditoría durable. Los artefactos de CI solo imprimen resultados booleanos sin PII/tokens.
- Efectos DEV autorizados: creación/revocación de dos sesiones efímeras y sus registros normales de auditoría. No se ejecutan INSERT/UPDATE/DELETE sobre empresas, roles, permisos, membresías ni datos comerciales.
- En caso de fallo posterior a una creación, intento de revocación en finally; si no es verificable, emitir NO_GO sin ocultar la incidencia.
- La prueba online se ejecuta únicamente desde código ya integrado en dev (push/dispatch), nunca desde código no revisado de PR.

## Aceptación

LISTO requiere: contrato offline PASS, test online PASS de JWT real y MySQL DEV para el SHA desplegado en Render, ejecución exacta con autorización del operador, rechazo empresarial, aislamiento A/B, expiración, revocación y auditoría, integración protegida del workflow con revisión independiente y readback de PLAN_MAESTRO, COLA y BITACORA. Si el secreto de la cuenta de soporte no corresponde a un administrador real de plataforma, conservar PENDIENTE/VALIDANDO y registrar el dato como impedimento de credenciales, sin inventar autoridad.
