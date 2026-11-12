# I8-005.F — fixture del token revocable y antifuerza bruta (DEV)

**Autor:** JAVIER_CHATGPT, Task Javier :30, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`.
**Plan:** STANDARD de implementación V3, I8-005.F; precedente I8-005.A = VALIDANDO al preparar el caso. Este documento describe la prueba a desarrollar y NO certifica el token real.

## Condición medible

Un token de recuperación emitido para empresa A (ID sintético 11) se debe aceptar exactamente una vez, antes de su vencimiento, con digest comprobado, sin revocación y sin bloqueo del rate limiter, únicamente en la empresa A. Los casos negativos incluyen identidad empresa B (22), expiración en cero o anterior, segunda utilización, revocación, exceso de solicitudes y huella incorrecta.

## Fixture y oráculo

`backend/tests/Solqaryn.Tests/I8005FTokenRecuperacionFixtureTests.cs` codifica ocho entradas con resultado esperado y dos marcadores de token totalmente sintéticos. La prueba valida la consistencia de la matriz y las huellas SHA-256, **no llama al servicio de recuperación ni a infraestructura persistente**; un token real debe generarse con aleatoriedad criptográfica, caducidad y digest almacenado, sin emitir el valor en logs, URL de referer externo, auditoría ni respuesta de error.

## Evidencia futura indispensable

Probar mediante servicio auténtico, persistencia transaccional y dos tenants que el primer consumo consume la huella atómicamente y el segundo falla; verificar revocación, caducidad, rate-limit en HTTP, no enumeración de usuario/empresa, neutralidad de la respuesta, correo con host exacto del entorno y ausencia de secretos en telemetría. No inferir ese PASS desde la matriz de fixture, EF InMemory, mocks o CI compilada.

## Impacto, seguridad y rollback

Solo prueba y protocolo de preparación: ninguna API, controlador, DTO, JWT/RBAC, base MySQL, migración, SMTP, secreto, configuración de entornos, rule, CI o producción cambia. La regresión se limita a entradas de prueba y especificación del resultado esperado. Rollback por revert selectivo protegido del test, protocolo y nota aditiva del CHANGELOG, preservando cambios de terceros. Revisión triple por otra Task y gates exact-HEAD son requeridos; `COLA!G121=NO` mientras I8-005.A no esté LISTO. No adelantar el PR FIFO #4020.
