# P1-013.C — Preparar caso de gate de aislamiento de onboarding (DEV)

## Fuente y condicion medible

- Fuente ejecutable: STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V3, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; microtarea P1-013.C, orden 50, PLAN_MAESTRO!51 y COLA!G51=SI. Dependencia P1-013.A=LISTO.
- Task autora fisica: Javier :30, `6aeacb7820ac8191b7d768a6f33dfa09`; rama unica `work/solqaryn-p1-013-c`. El autor no realiza revision ni cierre independientes de esta entrega.
- **Una sola condicion negativa:** `MultiEmpresa:PlatformOnboardingEnabled=true` y `MultiEmpresa:CommercialIsolationCertified=false`. Aunque el operador del fixture tenga autoridad de plataforma, el endpoint no debe crear empresa ni membresia mientras falle cualquiera de ambos gates.

## Fixture y resultado esperado (no certificado)

Se reutiliza el fixture ya probado del proyecto `PlataformaEmpresasOnboardingSecurityTests.P1013A_AltaRechazadaPorCualquierGateApagado(true,false)`. Configura un controlador con `AppDbContext` EF InMemory y usuario global simulado, llama a `AltaControlada` y verifica `ObjectResult.StatusCode=503`; ningun nuevo `Empresa`, `UsuarioEmpresa`, `ConfigEmpresa` ni `RegistroAuditoria` en el contexto de prueba.

El wrapper `P1013COnboardingGateCaseTests.P1013C_AislamientoOffImpideAltaAunqueOnboardingEsteOn` permite filtro independiente `FullyQualifiedName~P1013C`. Ejecutar como prueba causal mediante CI o `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1013C`; **hasta recibir resultado verificable, PASS=NO_ACREDITADO**.

## Limites de seguridad y entrega

Este caso solo prepara y delimita una regresion negativa. No habilita flags, no altera backend, Angular, API/DTO, JWT, RBAC, MySQL, secretos, migraciones, datos reales o despliegues. EF InMemory no acredita rechazo HTTP real, aislamiento multiempresa remoto, identidad JWT real ni valores efectivos del entorno DEV.

El codigo, test y acta pueden entregarse a **VALIDANDO** con PR/SHA, auditoria y readback, aunque CI se ejecute de forma asincrona. VALIDANDO no equivale a aprobado: antes de merge/LISTO se requieren prueba dirigida realmente PASS, Scope Lock y CI exact-SHA, revision de otro TASK_ID, merge protegido, checks postmerge y conciliacion PLAN_MAESTRO/COLA/BITACORA. Rollback: revertir este changeset mediante PR protegido preservando otros scopes.
