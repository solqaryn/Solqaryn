# P1-013.D — Regresion negativa dirigida de gates de onboarding (DEV)

## Alcance y dependencia

- Fuente ejecutable: STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES (ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), orden 61, PLAN_MAESTRO!62.
- Autor: Task fisica Javier :20, `6adc2e4bd8a48191ac7d8641ed95a1a3`, GitHub `jmejia31`.
- Dependencia P1-013.C en VALIDANDO en el inicio; `COLA!O62=SI` permite preparar el desarrollo, pero `COLA!G62=NO` **prohibe merge y LISTO** hasta que P1-013.C este LISTO.
- Una condicion: `MultiEmpresa:PlatformOnboardingEnabled=false` y `MultiEmpresa:CommercialIsolationCertified=true`; ni un operador global autorizado puede registrar una nueva empresa.

## Caso y evidencias

La prueba `P1013D_OnboardingOffRechazaAltaAunqueAislamientoEsteCertificado` llama al fixture integrado `PlataformaEmpresasOnboardingSecurityTests.P1013A_AltaRechazadaPorCualquierGateApagado(false, true)`.
El fixture exige `ObjectResult.StatusCode == 503` y ausencia de `Empresa`, `UsuarioEmpresa`, `ConfigEmpresa` y `RegistroAuditoria` creados. Solo utiliza EF InMemory y datos sinteticos; no consume JWT, MySQL ni valores efectivos de Render DEV.

Comando dirigido para validador autorizado:
```bash
dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1013D
```
En el runtime de esta Task no hay dotnet disponible; por ello **test local NO_EJECUTADO, resultado NO_ACREDITADO**. No inferir PASS de compilacion ni de checks no relacionados. La ejecucion causal requiere salida de CI o un runner autorizado. Ningun codigo productivo, dato real ni variable de ambiente fue alterado.

## Reglas de entrega y cierre

Se permite entregar codigo y PR a VALIDANDO sin esperar CI; esto NO certifica la prueba. Task revisora fisica distinta del autor debe inspeccionar el diff y obtener test causal PASS, checks requeridos exact HEAD SUCCESS y `COLA!G62=SI` antes de fusionar. Tras merge protegido, exigir CI postmerge exact SHA y readback PLAN_MAESTRO/COLA/BITACORA antes de LISTO. No autoaprobar, no duplicar rama/PR, no inferir HTTP real ni habilitacion de onboarding. Rollback por PR protegido que revierta solo los cambios P1-013.D.
