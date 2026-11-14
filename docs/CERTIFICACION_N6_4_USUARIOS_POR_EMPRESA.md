# ERP-N6.4 — Certificación canónica de Usuarios por empresa

Fecha de preparación: 2026-09-11
Autoridad operativa: `docs/VAEP_AUTHORITY.md`
Repositorio/rama: `jmejia31/Solqaryn` / `Desarrollo`
Estado de este documento al publicarse: **CANDIDATO DE CIERRE H / VALIDANDO**. Este archivo no marca por sí solo `N6.4.H=LISTO_REAL`; el cierre exige REVIEW_FIRST, checkpoint exact-head y reconciliación de `TASKS.md`, `CHANGELOG_AI.md`, COLA y CONFIG.

## 1. Dictamen y alcance

ERP-N6.4 materializa la capacidad de que un mismo usuario pueda estar asociado a una o varias empresas y mantener un rol distinto por cada empresa mediante la entidad relacional `UsuarioEmpresa`.

El alcance certificado por N6.4.A-G comprende:

- contrato de dominio de la membresía usuario-empresa y sus invariantes;
- persistencia EF/MySQL, relaciones, índices y unicidad por `(UsuarioId, EmpresaId)`;
- casos de uso, repositorio, DTOs y API para consultar/asignar empresas, cambiar rol empresarial y activar/desactivar una membresía;
- experiencia frontend para administrar membresías y roles por empresa;
- autorización, auditoría y controles de seguridad aplicables al punto;
- QA/regresión/CI de las capas aplicables, con P0=0 y P1=0 al cierre de G.

**Límite explícito:** N6.4 no certifica aislamiento anti-leakage completo. El enforcement de empresa activa/seleccionada, el scope de autorización tenant-aware y las pruebas de ausencia de fuga entre tenants pertenecen a **ERP-N6.5 — Aislamiento**. N6.4 crea la relación y la capacidad administrativa necesarias para que N6.5 pueda imponer ese aislamiento sin inventar otra autoridad.

## 2. Cadena A-G certificada

| Unidad | Resultado | Evidencia principal |
| --- | --- | --- |
| N6.4.A — Auditoría/preflight | `LISTO_REAL` | `vaep/evidence/fragments/N6.4.A_LISTO_REAL_20260911T1610Z.json`; REVIEW_FIRST `N6.4.A_ANTIG_RUNTIME_CONTRACT_REVIEW_FIRST_20260911T1608Z.json`; gate `34620117985` SUCCESS; admission `34620112514` SUCCESS; evidencia de cierre `fe5caf740a6c91c7bf323be363fe177f78795c79` |
| N6.4.B — Dominio/contratos | `LISTO_REAL` | `367be9d5f4e13d2984c5ec7805b6eeea2e611c2f`; `vaep/evidence/reviews/N6.4.B_REVIEW_FIRST_20260911T1621Z.json`; `vaep/evidence/fragments/N6.4.B_LISTO_REAL_20260911T1624Z.json`; run `34621302778` SUCCESS |
| N6.4.C — Persistencia/migración | `LISTO_REAL` | `bb108e9a37dad7fdd27086f4bbeaa475ea2dc338`; `vaep/evidence/fragments/N6.4.C_LISTO_REAL_20260911T1728Z.json`; REVIEW_FIRST PASS; gates causales PASS |
| N6.4.D — Aplicación/API | `LISTO_REAL` | functional head `37c7926a504f9ee3543a168383517b2f7567ffec`; `vaep/evidence/reviews/N6.4.D_REVIEW_FIRST_20260911T1829Z.json`; `vaep/evidence/fragments/N6.4.D_LISTO_REAL_20260911T1829Z.json`; aceptación `34631573663` SUCCESS; VAEP engine `34632804981` SUCCESS |
| N6.4.E — Frontend/UX | `LISTO_REAL` | functional head `810ce5d070a0871cc8ff4e8b63245746a8bdea0d`; certificación `a7d6ae7c1770fc6bba51030f34abab6a48077466`; REVIEW_FIRST `N6.4.E_REVIEW_FIRST_20260911T1856Z.json`; M10 `34635599759` SUCCESS; VAEP engine `34635599995` SUCCESS |
| N6.4.F — RBAC/auditoría/seguridad | `LISTO_REAL` | functional head `3f788ec78b03b820592c7a514fa3a63e345b3754`; certificación `72caf4ea47f2fe31c22e0f4cc3eedc2568a69515`; REVIEW_FIRST `N6.4.F_REVIEW_FIRST_20260911T1928Z.json`; RBAC `34638305008`, Priority4 `34638304864`, API/RBAC/auditoría/E2E `34638305000`, Fase2 `34638304858` SUCCESS |
| N6.4.G — QA/regresión/CI | `LISTO_REAL` | REVIEW_FIRST `5f8a25d23e71093cc3faedededa531724b8ea310`; receipt/certificación `b5f992691907199a3239c9cc0b2afc1891446322`; `vaep/evidence/reviews/N6.4.G_REVIEW_FIRST_20260911T1943Z.json`; `vaep/evidence/fragments/N6.4.G_LISTO_REAL_20260911T1943Z.json`; P0=0/P1=0 |

La promoción control-plane de H quedó materializada posteriormente en `207a43cbf379ec0b1edd42d38a2d9aeeb0b33cfd`, sin cambio funcional de producto.

## 3. Contrato funcional y de datos

### Dominio

`UsuarioEmpresa` representa una membresía tenant-aware con:

- `UsuarioId` positivo;
- `EmpresaId` positivo;
- `RolId` positivo e independiente por empresa;
- estado `Activa`;
- cambio de rol controlado;
- activación/desactivación explícita;
- resolución fail-closed cuando usuario, empresa o estado no corresponden.

La cobertura de N6.4.B demuestra que un usuario puede poseer roles diferentes en empresas diferentes sin colapsarlos en una única membresía.

### Persistencia

N6.4.C materializa `UsuarioEmpresa` en EF/MySQL con:

- FK hacia Usuario;
- FK hacia Empresa;
- FK hacia Rol;
- comportamiento restrictivo para evitar borrados cascada que destruyan trazabilidad;
- índice/constraint único `(UsuarioId, EmpresaId)` para impedir membresías duplicadas;
- índices de acceso aplicables;
- migración y snapshot EF reconciliados;
- backfill fail-closed: no se inventa una empresa ni un rol cuando los históricos son ambiguos.

### Aplicación y API

La superficie `UsuariosController` expone, bajo autenticación y permisos de `ModuloSistema.Usuarios`, los casos de uso de membresías:

- `GET /usuarios/{id}/empresas`;
- `POST /usuarios/{id}/empresas`;
- `PUT /usuarios/{id}/empresas/{empresaId}/rol`;
- `PUT /usuarios/{id}/empresas/{empresaId}/estado`.

El servicio valida usuario, empresa, rol, estado y duplicidad antes de confirmar cambios. La administración de membresías no sustituye la autorización contextual tenant-aware que se completa en N6.5.

### Frontend

La feature de Usuarios integra administración de membresías empresariales con selector de empresa/rol, activar/desactivar, loading, vacío, error, responsive y atributos de accesibilidad. El exact-head E fue validado por lint/build, Angular y Playwright en M10.

## 4. Seguridad, RBAC y auditoría

N6.4.F demostró:

- `[Authorize]` sobre el controller de Usuarios;
- permisos declarativos para lectura, asignación y cambio de rol;
- verificación dinámica `Activar`/`Desactivar` para el cambio de estado de membresía;
- auditoría de `UsuarioEmpresa` para asignación, cambio de rol y cambio de estado;
- persistencia del actor de actualización;
- ausencia de bypass o secreto nuevo detectado por los gates aplicables.

Durante F se detectó un defecto real en las pruebas nuevas: xUnit1031 por `GetAwaiter().GetResult()` bajo warnings-as-errors. Se corrigió en `3f788ec78b03b820592c7a514fa3a63e345b3754` convirtiendo las pruebas a `async Task` + `await`. El defecto no fue ocultado y los gates exact-head posteriores quedaron verdes.

## 5. QA, regresión y CI

N6.4.G reutiliza evidencia causal ya existente en lugar de duplicar suites:

- dominio/unit: contratos `UsuarioEmpresa` de B;
- persistencia/migración: C;
- aplicación/contratos/seguridad/auditoría: D/F;
- frontend/E2E: E;
- regresión exact-head: backend build/tests, MySQL/migración/integridad, RBAC, auditoría, frontend lint/build/unit y E2E.

`Performance=N/A` para N6.4 porque el Plan Maestro no define SLA, latencia, throughput ni volumen objetivo gobernado para este punto. No se inventa un umbral sintético; la persistencia sí mantiene los índices/constraint aplicables.

`CI adjustment=NOT_REQUIRED`: las suites vigentes ya cubren las capas aplicables, por lo que crear otro workflow sólo duplicaría ejecución.

### Eventos no causales preservados

- Vercel `solqaryn-desarrollo`: failure por deployment rate limit observado durante F. Se conserva como telemetría externa; no es fallo de código/test y desplegar Producción no forma parte del DoD de N6.4.F/H.
- `VAEP Jules Diagnostic / diagnose-jules`: `JULES_API_ERROR` sin manifest ni sesión durante G. Se conserva como telemetría; G fue ejecución directa `CHATGPT_VAEP`, sin offload Jules, por lo que no sustituye ni invalida las suites de aplicación exitosas.

## 6. Runbook operativo

### Alta de membresía

1. Verificar que el usuario exista y esté habilitado para administrar una membresía activa.
2. Verificar que la empresa exista y esté activa.
3. Verificar que el rol exista, esté activo y no eliminado.
4. Rechazar si ya existe la clave `(UsuarioId, EmpresaId)`.
5. Persistir la membresía y registrar auditoría.

### Cambio de rol empresarial

1. Verificar permiso `Usuarios/AsignarRol`.
2. Resolver usuario, empresa, rol y membresía.
3. Rechazar rol inválido/inactivo/eliminado.
4. Actualizar sólo el `RolId` de esa membresía y auditoría correspondiente.

### Activar/desactivar membresía

1. Resolver permiso dinámico `Usuarios/Activar` o `Usuarios/Desactivar`.
2. Al activar, validar usuario, empresa y rol activos.
3. Actualizar la membresía y el actor.
4. Registrar evento de auditoría `UsuarioEmpresa`.

## 7. Estrategia de rollback y recuperación

N6.4.H **no ejecuta Producción** ni un rollback de datos.

Ante un defecto de aplicación/UI, la estrategia preferida es corrección forward en `Desarrollo` sobre el mismo contrato de datos. No se debe borrar la tabla ni reconstruir membresías para resolver un defecto de capa superior.

Ante un defecto de persistencia:

1. congelar nuevas mutaciones de membresía;
2. exportar/resguardar las filas `UsuarioEmpresa` y validar conteos/unicidad;
3. identificar si el problema es schema, datos o consumidor;
4. preferir migración correctiva forward;
5. sólo considerar `Down` destructivo si se demuestra que no existe información que deba preservarse o si hay un respaldo/restauración explícitamente validado;
6. nunca inferir empresa/rol durante recuperación cuando exista ambigüedad.

La constraint única `(UsuarioId, EmpresaId)` no debe relajarse como mecanismo de emergencia.

## 8. Aplicabilidad de artefactos documentales

- **OpenAPI estático separado:** N/A para N6.4. La API está definida por controllers/DTOs y la infraestructura Swagger existente; no se introdujo un contrato OpenAPI versionado independiente que requiera una segunda autoridad documental.
- **ADR separado:** N/A para N6.4. La decisión local es la relación `UsuarioEmpresa` ya materializada y probada. La política de aislamiento contextual entre tenants es una decisión de N6.5 y no debe adelantarse aquí.
- **ERD separado:** N/A como archivo adicional. Este certificado registra la relación suficiente para el punto: `Usuario 1..* UsuarioEmpresa *..1 Empresa` y `UsuarioEmpresa *..1 Rol`, con unicidad `(UsuarioId, EmpresaId)`. La configuración EF/migración sigue siendo la autoridad ejecutable.
- **Runbook separado:** no requerido; el runbook proporcional y rollback quedan contenidos en este certificado para evitar documentación duplicada.

## 9. Riesgo residual y frontera N6.5

El riesgo que queda **no es deuda oculta de N6.4** sino el siguiente punto explícito del roadmap: impedir tenant leakage de extremo a extremo. El preflight dirigido ya observa que la autorización efectiva histórica continúa resolviendo `Usuario.RolId` global y que no existe todavía un concepto de empresa actual dentro de `IUsuarioScopeService`/JWT/permiso efectivo. Asimismo, listados/repositorios generales de Empresa no están todavía restringidos por la membresía activa del usuario.

Por ello, N6.5.A debe partir de estas preguntas fail-closed:

- ¿cómo se selecciona y transporta la empresa actual sin confiar en un claim obsoleto?
- ¿cómo se resuelve el rol efectivo desde `UsuarioEmpresa` para esa empresa?
- ¿qué repositorios/consultas requieren scope obligatorio de empresa?
- ¿qué endpoints deben rechazar acceso cross-tenant incluso con IDs válidos de otro tenant?
- ¿qué cache/claim debe invalidarse al desactivar o cambiar la membresía?
- ¿qué pruebas automatizadas negativas demuestran fuga cero entre Empresa A y Empresa B?

N6.5 no debe crear una segunda entidad de membresía ni reutilizar `Usuario.RolId` como autoridad tenant-aware.

## 10. Condición de cierre H

Este documento queda listo para el checkpoint H. Para promover `N6.4.H` a `LISTO_REAL` deben cumplirse conjuntamente:

1. `TASKS.md` y `CHANGELOG_AI.md` reconciliados de forma aditiva/history-preserving;
2. REVIEW_FIRST documental con P0=0/P1=0;
3. exact-head/checkpoint aplicable terminal y sin fallo causal atribuible;
4. receipt de H con SHA exacto/equivalencia demostrada;
5. COLA H=`LISTO` con evidencia `LISTO_REAL`;
6. parent N6.4 cerrado sólo después de A-H;
7. promoción de `N6.5.A` únicamente después del cierre real de H.

Hasta entonces: `N6.4.H=EN_PROGRESO/VALIDANDO`, sin false `LISTO_REAL`.
