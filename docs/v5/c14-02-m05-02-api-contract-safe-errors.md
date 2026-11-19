# C14-02.M05.02 — contrato API/DTO y errores seguros (incremento V20)

**ID:** C14-02.M05.02 · **Riesgo R2:** autorización, BOLA/IDOR, host, archivos y jobs · **Repositorio:** solqaryn/Solqaryn@dev · **Owner DEV05:** 6adc2e55f47c81919f6374e561687076 · **Scope Lock:** STRICT.

## A0: alcance, impacto y límites

El STANDARD V5 exige ejecutar el contrato API/DTO con errores seguros para la aceptación de seguridad. Este incremento añade una **suite xUnit ejecutable contra el código Application real** `C1402SecurityAcceptanceAuthorizationGate` y un workflow .NET 10 dirigido. No crea controlador, endpoint HTTP, DTO público, DI de autorización real, acceso a MySQL, migración, JWT, grants, secretos ni llamadas a proveedores. No se presume que la API futura propuesta en C14-02.M02.02 esté implementada.

El cambio afecta exclusivamente pruebas, workflow, este documento y CHANGELOG. El gate Domain/Application subyacente permanece idéntico. Consumidores API/Angular y esquema de datos no cambian. Rollback: git revert normal del commit, sin force-push ni alterar historia institucional.

## A1-A3: pruebas adversariales pertinentes

Ocho métodos xUnit nuevos cubren: prevalidación positiva que **sigue sin conceder acceso**, petición nula, identificadores/traversal/longitud/formato/SHA/superficie inválidos sin lectura externa, override tenant/RBAC y restore, recurso extranjero y SHA obsoleto, membresía revocada/tenant B/RBAC/JWT inválidos, host no confiable, enlace de archivo vencido, job sin grants/idempotencia, lector fallido con código estable y cancelación. El oráculo exige códigos exactos y banderas de autorización falsas, no resultados textuales autoafirmados. La ejecución GitHub-hosted es la única fuente de PASS.

A2 prueba denegación en el preflight Application con snapshots sintéticos. **No acredita autenticación JWT firmada, middleware HTTP real, sesión revocada de MySQL ni aislamiento tenant A/B de extremo a extremo**. A3 xUnit real Domain→Application y error del lector; MySQL/EF/Playwright/proveedores son NO_APLICA para el **diff test-only**, pero el requisito de aceptación comercial integral permanece **NO_ACREDITADO**.

## A4-A5: gates institucionales

Publicar en dev por fast-forward y revalidar SHA de commit, `verify-solqaryn-project-scope`, V5/procedencia, backend Release, seguridad multiempresa, autorización y job dirigido. `QUEUED`, `IN_PROGRESS`, `CANCELLED`, `SKIPPED` y `FAILURE` no constituyen PASS requerido. La microtarea permanece VALIDANDO hasta completar A0–A4 para el alcance exigido y contar con pruebas pertinentes de HTTP/JWT/RBAC/tenant y cero P0/P1 sin mitigación. A5 debe efectuarla otra Task Javier independiente del autor y del reparador, mediante lectura del código, requisitos y logs. Nunca habilitar QA/main/PROD ni abrir dependencias por una suite parcial.
