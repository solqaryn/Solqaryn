# P1-013.E — regresion positiva dirigida de gates de onboarding (DEV)

## Autoridad, alcance e independencia
- Documento rector: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Google Doc ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.
- Microtarea P1-013.E, PLAN_MAESTRO!66, depende de P1-013.D.
- Desarrollo iniciado por Task física Javier :40 `6adc2e55f47c81919f6374e561687076` vía FALLBACK_PENDING_V4; rama única `work/solqaryn-p1-013-e`. Revisión independiente requiere un TASK_ID distinto.
- `COLA!O66=SI` autoriza desarrollar con P1-013.D VALIDANDO; `COLA!G66=NO` **prohíbe integrar o cerrar** hasta que la predecesora esté realmente LISTO.

## Regresión proporcional
`P1013EOnboardingPositiveRegressionTests.P1013E_DobleGateOnPermiteAltaControladaConAuditoria` reutiliza dos tests ya integrados:
1. `P1013A_DirectorioNoAnunciaAltaSinDobleGate(true,true,true)` exige que la disponibilidad positiva sea visible y que la consulta no cree nuevas entidades.
2. `P1012A_PlataformaCreaEmpresaConAdminRegistradoSinEscaladaGlobal()` prueba `CreatedResult`, una nueva empresa, única membresía empresarial, configuración y registro de auditoría, sin agregar rol global al administrador empresarial.

Filtro CI recomendado: `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj -c Release --filter FullyQualifiedName~P1013E`; exigir total de pruebas mayor que cero y cero fallidas. **NO_TEST_LOCAL_EXECUTED**: el PASS debe venir de ejecución causal sobre el SHA exacto. No se afirma todavía CI exitosa.

## Límites y cierre
Prueba EF InMemory con autoridad de plataforma simulada. No acredita flags efectivos en DEV, JWT de red, MySQL productivo, endpoint desplegado ni transacciones remotas. No modifica API, datos, migraciones, seguridad, credenciales, flags, QA/main/PROD. Para LISTO se requiere P1-013.D LISTO (G66=SI), revisión técnica real por otra Task, checks exact SHA requeridos SUCCESS, merge protegido, CI postmerge y readback PLAN/COLA/BITACORA. Reversión: PR protegido exclusivamente de este test/acta/changelog.
