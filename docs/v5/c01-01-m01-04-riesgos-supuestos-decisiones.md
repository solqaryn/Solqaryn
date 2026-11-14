# SOLQARYN V5 FINAL — C01-01.M01.04: riesgos, supuestos y decisiones pendientes de identidad universal

**Estado del entregable:** preparación documental de C01-01/M01. No implementa ni certifica control nuevo. **Alcance:** identidad de empresa, sucursal, usuario, membresía, recurso y contexto, preservando límites JWT/RBAC/multiempresa. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y PLAN_MAESTRO/COLA/DAG_TECNICO (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`). **Ejecutor:** JAVIER :50, Task `6af16bbad26881918c283abe06e1f32a`.

**Resultado atómico M01.04:** registrar riesgos, supuestos verificables y decisiones abiertas antes de implementación. **Resultado mayor de M01, todavía no certificado:** contratos de dominio y matriz de pertenencia para empresa, sucursal, propiedad, recurso, usuario, membresía y contexto. El avance documental de M01.01–05 no libera M01.06/M02 ni reemplaza las pruebas de runtime.

## 1. Fuentes leídas y límites de afirmación

| Fuente viva en dev / blob Git | Observación verificable | Lo que NO demuestra |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` — `c44940df7c30983a4c23b93338e0cb9367962069` | `Empresa` posee identidad y estado `Activa` | Aislamiento global y cobertura de consumidores |
| `backend/src/Domain/Entities/Sucursal.cs` — `8746b11fa30cdac0b97241b6221100bedd813b35` | `EmpresaId` nullable por compatibilidad; `ObtenerEmpresaIdTenant` rechaza nulo/no positivo | Que toda operación actual rechace sucursal huérfana |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` — `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía activa e identidad par usuario/empresa; rol efectivo condicionado | Que no existan consumidores legacy de `Usuario.RolId` |
| `backend/src/Application/Interfaces/IContextoTenantService.cs` — `97d0a05456c66d069a56b9743bbc286af07502b1` | Empresa solicitada es selección, no prueba de autoridad; interfaz devuelve null si no verifica | Que cualquier endpoint llame a la interfaz |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` — `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Camino tenant con membresía; camino legacy sin empresa conserva compatibilidad condicional | Que el camino legacy sea seguro en todos los consumidores |
| `backend/src/Infrastructure/Services/CurrentUserService.cs` — `b6e3570a9cbd002527a64358bbdd32f4be229feb` | Claim `esAdministrador` se documenta como informativo, no debe otorgar permiso por sí solo | Que todo controlador/servicio ignore el claim |
| `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` — `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | Filtra por `AuthorizedReadEmpresaId` cuando hay empresa autorizada; ruta sin marca requiere revisión de cuarentena | Que una consulta sin marca sea segura en producción multitenant |
| `docs/v5/c01-01-m01-01-objetivo-identidad-no-regresion.md` — `90665746bd27a7680d0d92d6e3c8ebfaf58ae79b` | Objetivo, no-alcance y casos NR positivos/negativos ya definidos | Que NR se haya ejecutado sobre HTTP/MySQL |

**Gate DAG documental:** `DAG_TECNICO` identifica esta microtarea `VALIDADO/SI`; exige 11 antecesoras LISTO (`C00-01.M02.01–06` y `C00-03.M01.01–05`). Preflight contra PLAN_MAESTRO y COLA: estado de la fila libre, `G=SI/O=SI` previo al claim. El DAG permite **solamente preparación M01.01–05**. Una excepción de DAG no confiere autorización para activar backend ni promocionar un SHA.

## 2. Registro de riesgos trazables

| Riesgo / prioridad | Condición observable y superficie | Mitigación / prueba futura exigible | Decisión o responsable pendiente |
| --- | --- | --- | --- |
| R-ID-01 / CRÍTICO | Un token válido para usuario A se usa con `EmpresaId=B` sin membresía activa | HTTP/DB negativo A→B debe negar y no devolver PII; verificar `UsuarioEmpresa` activo | Responsable de seguridad/API debe elegir política de denegación uniforme |
| R-ID-02 / ALTO | `Usuario.RolId` legacy se interpreta como autorización en más de una empresa | Prueba negativa rol de A no permite función en B; inventario de consumidores | Responsable backend define migración gradual, sin retirar fallback inadvertidamente |
| R-ID-03 / ALTO | Sucursal con `EmpresaId` nulo/0 conserva lectura o write nuevo | Caso negativo de resolución, alta y reasignación; migración/backfill sólo con autorización separada | Responsable de persistencia determina invariantes y compatibilidad |
| R-ID-04 / CRÍTICO | `IgnoreQueryFilters` más `AuthorizedReadEmpresaId` ausente deja resultados fuera de ámbito | Revisar consumidores y middleware de cuarentena; prueba con 2 empresas; denegar si no hay scope | Revisión conjunta seguridad/persistencia antes de habilitar multitenant |
| R-ID-05 / ALTO | Claim de administrador se usa como elevación sin grants MySQL vigentes | Prueba token `esAdministrador=true` sin permiso: 403; casos positivos grant vigente | Responsable de RBAC comprueba siempre `IPermisoService` |
| R-ID-06 / ALTO | Usuario con varias membresías entra por contexto ambiguo o cambia empresa sin revalidar | Denegar acceso ambiguo y confirmar cambio de ámbito contra membresía vigente; auditoría de cambio | Contrato de selección de tenant en API/frontend pendiente |
| R-ID-07 / ALTO | Reasignar sucursal/recurso a otra empresa sin verificación y transacción | Negativo reasignación no autorizada; identidad de propietario pre/post y rollback | Definir quién autoriza reasignación excepcional y auditoría |
| R-ID-08 / ALTO | JWT revocado/expirado o usuario bloqueado conserva autorización cacheada | HTTP negativo token inválido/inactivo; consulta de usuario/membresía al resolver | Revisar cachés, claims, latencia y política de revocación |
| R-ID-09 / MEDIO | Filtración de PII y de ID internos en errores/bitácora | Revisar respuestas 401/403/404 y redacción de logs; observabilidad por ID correlativo sin PII | Seguridad define política de redacción y conservación |
| R-ID-10 / ALTO | Host público, slug o header se usa como identidad sin resolver tenant confiable | Negativos host/slug manipulado; controles server-side y claves por tenant | Responsable de storefront y API acuerdan frontera pública |
| R-ID-11 / MEDIO | Los contratos documentales se presentan como cobertura runtime | Exigir diff, suites .NET/HTTP/MySQL, casos A/B, CI exact-SHA en tareas ejecutables | Dueño de cada microtarea ejecutable debe aportar artefacto y prueba real |
| R-ID-12 / MEDIO | Colisión de claims o A5 independiente atribuido al autor | PLAN!H/I, COLA!E/G/O y BITACORA!A:Z (26) con EVENT_ID/IDEMPOTENCY_KEY; check de autor separado | Task autora no puede autopromover QA/main/PROD |

## 3. Supuestos explícitos, invalidadores y decisiones

| ID | Supuesto operativo, NO certificado | Evidencia necesaria para aceptarlo | Acción si es falso |
| --- | --- | --- | --- |
| S-01 | Todas las rutas HTTP privadas usan scope empresarial del backend | Inventario de endpoints, filtros, consumidores y pruebas A/B reales | Poner gate de seguridad y corregir rutas antes de liberar |
| S-02 | La pertenencia `UsuarioEmpresa` es la única fuente del rol empresarial efectivo | Pruebas contra claims legacy y todos los servicios consumidores | Reconciliar DTO/servicios, sin usar roles globales como parche |
| S-03 | La ruta sin `AuthorizedReadEmpresaId` sólo puede ejecutarse bajo cuarentena monoempresa | Evidencia middleware + integration test con 2 empresas | Bloquear ruta de consulta multitenant y abrir corrección |
| S-04 | La persistencia legacy de sucursales sin propietario no autoriza writes | Inventario de datos existentes + pruebas de creación/actualización | Plan aditivo de saneamiento, no ejecutar migración aquí |
| S-05 | La selección de tenant de UI no se interpreta como autorización | Pruebas de endpoint, DTO y manipulación de `EmpresaId`/header | Corregir backend primero y añadir negativos |
| S-06 | Los controles A0–A5 se aplicarán por alcance, no por conteo de CI global | Check-runs `head_sha` exacto, gate requerido y BITACORA idempotente | No declarar LISTO cuando una prueba causal falta |

**D-01 — Decisión tomada:** mantener este cambio documental; ningún cambio de JWT, RBAC, servicios, interfaces, EF, Angular, configuración ni datos de usuario. **D-02 — Decisión tomada:** fallar cerrado ante prueba ambigua, no inferir pertenencia del cliente. **D-03 — Pendiente:** formalizar política de 401/403/404 sin filtrar existencia de recursos; requiere aprobación técnica e implementación futura. **D-04 — Pendiente:** definir ruta de migración segura de legacy `Usuario.RolId` y sucursales con owner nulo, con rollback y compatibilidad. **D-05 — Pendiente:** cobertura de pruebas positivas y negativas HTTP/DB multiempresa para futuras tareas de ejecución, con evidencia CI del SHA exacto. **D-06 — Pendiente:** matriz de derechos plataforma/empresa/sucursal/recurso y administración excepcional con registro auditable.

## 4. Criterios de aceptación DOCUMENTALES de M01.04

- **Positivos:** existe un registro identificable R-ID-01–12; cada riesgo tiene condición observable, mitigación concreta y propietario/decisión por resolver; S-01–06 separa supuestos de pruebas; referencias a blobs existentes y a estado institucional.
- **Negativos:** no convertir `EmpresaId`/JWT/host/slug en autoridad por documento; no declarar que bypass, compatibilidad legacy o RBAC están probados en runtime; no usar estado de CI ancestor; no tratar `SKIPPED` como PASS; no completar M01.06 ni activar permisos/servicios.
- **Seguridad:** sin secretos, tokens, PII, nombres de clientes ni grants reales; los riesgos A→B son escenarios de prueba pendientes, no violaciones verificadas.
- **Integración documental:** fuentes V5/Scope Lock/DAG y código coinciden con rutas y hashes citados; `BITACORA` requiere 26 campos A:Z; `PLAN_MAESTRO!H` autoridad para estado; `COLA!E` fórmula derivada.
- **Subpruebas NO_APLICA:** dotnet, Node/Angular, MySQL, Docker y E2E: no se altera ejecutable, migración, workflow ni contrato runtime. Scope Lock sí aplica al SHA de cualquier commit dev publicado.

## 5. Rollback y siguientes puertas

Si se revierte el documento, usar commit nuevo `git revert <SHA_PUBLICADO>` no destructivo y registrar evidencia, sin reset/force-push. C01-01.M01.02/.03/.05 son unidades independientes y mantienen sus propietarios. No proceder a M01.06 o implementación M02–M05 por este registro. A5 revisará de forma independiente la concordancia de fuentes y un caso positivo/negativo documental sobre SHA publicado; ninguna promoción a QA/main/PROD queda autorizada.
