# P1-013.A — Gates de onboarding: certificación causal (DEV)

## Fuente y autoría

- Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V3 (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); microtarea `P1-013.A`, orden 49, `PLAN_MAESTRO!50` y `COLA!G50=SI`.
- Task autora física `6adbbc9ad9e88191bea8eb69104afb25` (`SECONDARY_REVIEW_01`, ALEX_CHATGPT); GitHub autenticado `morales35alex`. Una sola microtarea propia bajo WIP 6/1/1, rama determinista `work/solqaryn-p1-013-a`, iniciada de `dev@89b3687d14336734788bda26e2e187f0eea763c2`.
- Dependencia `P0-012.B=LISTO`. La reclamación fue protegida por creación nativa de rama inexistente, verificación GitHub posterior y readback institucional de `PLAN_MAESTRO`, `COLA` calculada y `BITACORA`. No se adquirió propiedad de las revisiones ajenas.

## Código y alcance

Se **reutiliza el doble gate de código existente**; no se habilitan flags desde este PR:

1. `PlataformaEmpresasController.AltaControlada` rechaza HTTP 503 si `MultiEmpresa:CommercialIsolationCertified` o `MultiEmpresa:PlatformOnboardingEnabled` no es verdadero, antes de cualquier creación. Tras ambos gates, verifica autenticación y autoridad global real.
2. `UnscopedCommercialTenantGuardMiddleware` solo permite el POST exacto `/plataforma/empresas/alta-controlada` con ambas banderas y principal autenticado; rutas alteradas y verbos distintos no se desbloquean.
3. `PlataformaEmpresasController.Listar` calcula `puedeCrear` como conjunción de ambas banderas, sin declarar autoridad de plataforma por el simple valor visual de UI.

La suite dirigida `P1013A` reutiliza y etiqueta los casos existentes de negativos de controller y middleware, y añade una matriz de cuatro combinaciones de flags para el valor `puedeCrear`; comprueba que consultar el directorio no crea empresas, membresías ni `ConfigEmpresa`. Requiere `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1013A` con recuento >0 y 0 FAIL. No sustituye aislamiento JWT/RBAC ni pruebas relacionales.

## Estado real de configuración y límites

En el árbol fuente `backend/src/API/appsettings.json` y `appsettings.Development.json` **no se declaran** estas dos banderas: `IConfiguration.GetValue<bool>` adopta falso cuando no se suministran. Esto describe **solo los archivos versionados**, NO demuestra los valores del servicio DEV desplegado, donde proveedor y variables podrían inyectarlos. **NO_CERTIFICADO** estado real de entorno remoto ni comportamiento externo HTTP/MySQL; no se consultaron secretos ni infraestructura. Las pruebas xUnit son de controlador/middleware con EF InMemory y configuración explícita de fixture. Un estado remoto sin readback no se declara ON ni OFF.

## Gates de transición y reversión

- PR protegido hacia `dev` con Scope Lock `AUTO_CERT=PASS`, test dirigido real `P1013A`, CI/security y checks exact SHA. Si existen fallos o pending, conservar `EN_PROGRESO`.
- `VALIDANDO` únicamente después de código y pruebas dirigidas satisfactorias, con readback de propietario/evidencia en Sheets.
- `LISTO` únicamente tras merge protegido, CI push postmerge SHA exacto, revisión técnica de un TASK_ID distinto y readback institucional con auditoría. Nunca emitir GitHub APPROVED propio ni autocertificar independencia.
- Si la aceptación de P1-013.A exige el valor **real** remoto de estos flags, mantener el scope sin LISTO hasta obtener readback de configuración **no secreta** mediante operación individualmente autorizada.
- Rollback exclusivo por PR protegido que revierta test, acta y CHANGELOG; no activar/desactivar flags, no modificar tenant real, DB, main, QA, PROD, secretos, horarios ni permisos.
