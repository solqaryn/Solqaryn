# I8-002.F — Caso preparatorio de recuperación empresarial (DEV)

**Autoría:** Task Javier :20 (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Fuente:** Plan Comercial Inicial V3, microfase I8-002.F, predecesora I8-002.A en VALIDANDO. **Estado de evidencia:** CASO PREPARADO; ninguna prueba de recuperación ejecutada.

## Condición medible y fixture sintético

Con un contexto de empresa autenticado/verificado como **empresa-a**, ensayar tres solicitudes de recuperación de acceso: (1) `ana@empresa-a.invalid`, miembro activo de A; (2) `nadie@empresa-a.invalid`, identidad inexistente; (3) `luis@empresa-b.invalid`, miembro exclusivamente de B con cabecera de A falsificada. Los dominios `.invalid` son deliberadamente no entregables. Ningún correo, clave, token ni base de datos real debe utilizarse.

**Oráculo público propuesto (no implementado):** los tres casos devuelven una misma respuesta neutra sin revelar existencia de usuario ni tenant, sin incluir token, hash, correo de destino ni enlace. La decisión privada de emitir notificación de prueba SOLO se permite para el miembro verificado de A; para desconocido y B debe ser falsa. Un encabezado de tenant suministrado por el cliente no otorga autorización.

## Protocolo de ejecución futuro (pendiente, NO PASS)

1. Preparar usuarios sintéticos A/B y un cliente de correo de prueba sin salida externa. Verificar empresa desde contexto server-side, no desde `X-Empresa-Id` no confiable.
2. Invocar la solicitud de recuperación **una vez que exista un endpoint real autorizado**, con las tres identidades y el mismo contexto A. Comparar código, cuerpo y campos públicos; deben ser indistinguibles y no enumerativos.
3. Comprobar efectos privados: una sola notificación de prueba para miembro A; cero para desconocido/B. Validar digest persistido (nunca token crudo), expiración, uso único, revocación, rate limiting, auditoría sin secretos, respuesta para token vencido y ausencia de enlaces de PROD desde DEV.
4. Ejecutar regresiones HTTP/JWT/middleware y MySQL/SMTP de prueba bajo CI dirigida con SHA exacto. Registrar PASS o NO_GO según evidencia. Nunca usar cuentas reales ni inventar envío de correo.

## Alcance presente y límites

`backend/tests/Solqaryn.Tests/I8002FRecuperacionCasoPreparadoTests.cs` verifica únicamente integridad y segregación de la matriz sintética **de casos esperados**. No invoca ni simula un endpoint de recuperación, no produce tokens y no afirma que el runtime satisfaga los resultados. `AuthController` actual expone login/renovar; el requisito de restablecimiento por token/email sigue sin certificación. Sin cambios de API, DTO, MySQL, JWT/RBAC, SMTP, frontend o configuración. No hay `dotnet test` local acreditado; CI pendiente. El rollback es revert protegido únicamente de este fixture, documento y nota `CHANGELOG_AI.md`, preservando trabajo ajeno.
