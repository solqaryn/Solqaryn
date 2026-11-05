# P1-011.A — Alta atómica de administrador nuevo (DEV)

## Autoridad, alcance y reuso

- Plan Comercial Inicial Unificado V3, microtarea P1-011.A, orden 45, dependencias P0-012.B=LISTO, COLA!G46=SI.
- Propietario individual: Javier :40, TASK_ID `6adc2e55f47c81919f6374e561687076`, GitHub `jmejia31`; rama `work/solqaryn-p1-011-a` desde `dev@4ac3fe76cc7f98c40a2aa1160c92560e1aab6bb2`. WIP 6/1/1 y `PROJECT_SCOPE_LOCK=STRICT`.
- Implementación de referencia **ya integrada**, no reimplementada: [PR #3683](https://github.com/solqaryn/Solqaryn/pull/3683), HEAD `82764d5dc7c86b7f38e704ea56312bd7b045c3ca`, merge `7b7178bd366bbedc1e395ffa12c8a0200780d0e3`.
- Contrato vigente: `POST /plataforma/empresas/alta-controlada` acepta **exactamente una** modalidad: ID de administrador existente o `AdministradorNuevo` con usuario, nombre y contraseña. No cambia el contrato de API ni activa el onboarding.
- Este changeset es evidencia de reutilización y direccionamiento de la regresión **ya existente**. No introduce un segundo circuito de alta, nuevos datos o una suite MySQL duplicada.

## Impacto arquitectónico y de seguridad

- Frontend Angular: comportamiento existente conservado. No se cambia interfaz ni componentes.
- Backend: `PlataformaEmpresasController` ya exige usuario JWT autenticado, `PlataformaScopeService` vigente y doble configuración `MultiEmpresa:CommercialIsolationCertified` + `MultiEmpresa:PlatformOnboardingEnabled`; ambas condiciones continúan obligatorias.
- Persistencia: EF Core y MySQL, **una transacción** que incluye `Usuario`, `Empresa`, `ConfigEmpresa`, `UsuarioEmpresa` y `RegistroAuditoria`. No se añaden migraciones ni se crean datos sobre Aiven real.
- Autorización: `Usuario.RolId` conserva rol empresarial base no administrador. El rol ADMIN de plantilla empresarial reside únicamente en la membresía; no se crea `UsuarioRolPlataforma` ni se concede autoridad global.
- Seguridad de credenciales: BCrypt workFactor 12 para contraseña nueva. Ni contraseña ni hash se incluyen en auditoría/respuesta.
- Sin cambios a QA, main, PROD, credenciales, secretos, roles de proveedor ni reglas protegidas.

## Dos carriles de pruebas existentes, sin duplicar ni debilitar cobertura

**Diagnóstico PR #3851, SHA c65885ae:** el check `SOLQARYN - Directed commercial scope` ejecutó xUnit sin servicio MySQL ni `ConnectionStrings__DefaultConnection`. La prueba `Category=Integration` falló con `MySQL efímero CI no configurado`, mientras el job Oracle/MySQL de `dev-ci` sí proporciona MySQL efímero y terminó SUCCESS. Scope Lock rechazó correctamente el SHA anterior. Este fallo de preparación no acredita fallo funcional de transacciones.

- **Prueba dirigida de PR, sin MySQL externo:** prefijo `P1011A_` en tres pruebas existentes de `PlataformaEmpresasOnboardingSecurityTests.cs`, sin alterar cuerpos ni aserciones. Comprueban BCrypt, rol empresarial base, membresía admin, configuración/auditoría, contraseñas débiles, duplicidad, modalidades mezcladas, doble gate y falta de autoridad global. El filtro `FullyQualifiedName~P1011A` exige TRX con al menos un PASS y cero FAIL sobre HEAD exacto.
- **Integración MySQL efímero real:** mantener nombre original y cuerpo íntegro de `PlataformaEmpresasMySqlTransactionIntegrationTests.AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal` con `Category=Integration`. El job `Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4` de `dev-ci` usa servicio MySQL 8.4 y cadena de conexión temporales, y ejecuta `dotnet test ... --filter Category=Integration`. Incluye commit atómico y rollbacks ante ConfigEmpresa, UsuarioEmpresa y auditoría.
- **Gates separados:** un PASS InMemory no prueba transacciones MySQL, y un PASS de migraciones no sustituye el check comercial dirigido. Ambos deben acreditar éxito sobre el SHA final, junto con Scope Lock y seguridad aplicables. No se cambia el workflow, ni DB externa, ni código productivo.

## Gates y limitaciones

- La presencia de tests en el repositorio **no demuestra una corrida exitosa**. Exigir log/TRX dirigido `P1011A` con al menos 1 PASS y 0 FAIL sin MySQL, **y** job MySQL efímero `Category=Integration` SUCCESS con regresión transaccional existente, ambos sobre SHA exacto, además de Scope Lock `AUTO_CERT=PASS` confiable y todos los checks aplicables.
- El merge protegido a `dev` y los workflows/checks `push` sobre SHA de merge son condiciones posteriores independientes. `LISTO` únicamente por **otro TASK_ID habilitado**, con revisión técnica cruzada, `PLAN_MAESTRO/COLA/BITACORA` leídos de vuelta y fórmulas preservadas.
- No afirmar prueba HTTP/JWT o MySQL vivo DEV remoto por el resultado de un fixture; estos gates, si el alcance final lo exige, se certifican separadamente.
- Reversión: revert mediante PR protegido del cambio documental/etiqueta del test. Nunca borrar empresas o usuarios legítimos ni forzar el branch.
