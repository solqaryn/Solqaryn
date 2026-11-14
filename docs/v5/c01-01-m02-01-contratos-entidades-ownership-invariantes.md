# SOLQARYN V5 FINAL — C01-01.M02.01: entidades, ownership, relaciones e invariantes de identidad universal

**Clase:** diseño contractual documental de M02.01. **Autor:** PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio:** `solqaryn/Solqaryn@dev`. **Gobierno:** `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido:** definir entidades, propiedad, relaciones e invariantes para la identidad universal de empresa, sucursal, recurso, usuario, membresía y contexto. Esta unidad especifica el contrato y la matriz de pertenencia; **NO** implementa entidades nuevas, endpoints, reglas activas adicionales, migraciones, datos, integraciones ni permisos. Los siguientes alcances M02.02–06 y M03–06 requieren preflight y pruebas propios; no se liberan por redactar este documento.

**Fuentes rectoras:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA. El cierre documental M01.06 `docs/v5/c01-01-m01-06-gates-bloqueo-rollback-identidad.md` exige evaluación por ID y nunca autoriza automáticamente IAM/JWT/RBAC/DB.

## 1. Preflight y fronteras de la entrega

Al autorizar la **evaluación** de esta M02.01: seis `C01-01.M01.01–06` estaban LISTO en PLAN H146:H151; `C00-01.M06.06`, `C00-03.M01.06` y `C00-03.M06.06` estaban LISTO en H37, H79 y H109. Para `C01-01.M01.06` se comprobó revisión A5 documental independiente por J00, evento de BITACORA fila 3001 sobre SHA `009d412176429266eee6574ac632bca333f3b0b2`, aunque la anotación histórica en PLAN!J151 siguiera indicando A5_PENDIENTE. No reescribir eventos históricos ni inferir otro A5. El DAG específico de esta M02.01 requiere esas nueve predecesoras y `F=VALIDADO/G=SI`; COLA `G/O=SI` es un prefiltro, no un permiso general.

Esta tarea no modifica contratos ejecutables actuales ni sustituye la revisión de seguridad de un consumidor de API, EF o Angular. Todo dato del navegador (EmpresaId, SucursalId, recurso, rol, header, claim, URL o cookie) es **selección solicitada**, nunca prueba de autorización. El backend debe validar la membresía y ownership en el momento de operar.

## 2. Inventario de fuentes y distinción entre existente y futuro

| Fuente actual en dev y blob Git | Entidad o interfaz observada | Invariante efectiva y límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` — `c44940df7c30983a4c23b93338e0cb9367962069` | `Empresa`: ID heredado, `Nombre`, `Activa`, identidad comercial | Nombre no vacío; estado activo no concede por sí solo acceso tenant |
| `backend/src/Domain/Entities/Sucursal.cs` — `8746b11fa30cdac0b97241b6221100bedd813b35` | `Sucursal.EmpresaId?`, `AsignarEmpresa`, `ReasignarEmpresa` | Resolver empresa rechaza nulo/no positivo; asignación ordinaria no reasigna dueño distinto; reasignación explícita no comprueba permisos por sí misma |
| `backend/src/Domain/Entities/Usuario.cs` — `cfe319bb61e405379fa632ed18d3183cd2df1a68` | Identidad `Usuario`; `RolId` y `PasswordHash` legacy | Usuario no equivale a membresía ni a administrador de todos los tenants; hashes jamás a DTO/log |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` — `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Relación usuario–empresa con rol tenant `RolId`, estado `Activa` | IDs positivos; rol efectivo solo con coincidencia y membresía activa; unicidad lógica (UsuarioId, EmpresaId), constraint física se verifica aparte |
| `backend/src/Domain/Security/ContextoTenantActual.cs` — `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto tenant construido por `DesdeMembresia` | `ExigirEmpresa` rechaza empresa inválida o ajena; no construir autoridad desde entrada arbitraria |
| `backend/src/Application/Interfaces/IUsuarioScopeService.cs` — `5345ee2de1b1e942bff8eded658f5dc27c0a6745` | Scope legacy y scope tenant, empresas seleccionables | 0 o múltiples membresías no generan empresa única; implementaciones no migradas fallan cerrado (null/lista vacía) |
| `backend/src/Application/Services/PermisoService.cs` — `278a8fa90dec43609d20157208a143f5794dcc35` | Permisos de rol/empresa y grants relacionales | Rol inactivo/sistema/sin owner empresarial no puede editar matriz personalizada; revisar consumidores antes de afirmar cobertura global |

**Lectura estática del código:** las propiedades y métodos anteriores están presentes en los blobs; no se ejecutaron llamadas HTTP, .NET, SQL, EF Core, Angular ni navegador como parte de esta unidad de diseño. Una prueba futura debe confirmar que los consumidores y restricciones físicas siguen estos contratos.

## 3. Grafo de propiedad propuesto (no nuevas tablas autorizadas)

```text
Usuario (identidad global, NO autoridad tenant)
  └── UsuarioEmpresa [UsuarioId, EmpresaId, RolId, Activa]
        └── Empresa [Id, Activa]
              ├── Sucursal [EmpresaId positivo, estado]
              │     └── Recurso operativo / almacén / ubicación (ownership verificable)
              └── Roles, permisos y recursos pertenecientes a Empresa

Solicitud autenticada -> usuario válido -> membresía activa de Empresa
                     -> rol efectivo de esa Empresa -> permiso/acción
                     -> ownership del recurso/sucursal -> operación permitida
```

| Relación | Cardinalidad conceptual | Owner autoritativo | Comprobación obligatoria |
| --- | --- | --- | --- |
| Usuario ↔ Empresa | N:M mediante UsuarioEmpresa | Identidad de la membresía | Par usuario/empresa positivo y activo; rol propio de ese par |
| Empresa → Sucursal | 1:N | `Sucursal.EmpresaId` | ID positivo, empresa activa, owner coherente antes de lectura/escritura |
| Empresa → Rol empresarial | 1:N en el ámbito empresa | `Rol.EmpresaId` cuando corresponda | Rol activo y del tenant, no rol sistema/legacy como superpermiso |
| Sucursal → recurso operativo | 1:N conceptual; verificar esquema real | FK/relación del recurso y sucursal propia | Cadena de ownership completa sin saltos A→B; huérfanos en cuarentena |
| UsuarioEmpresa → contexto | Un contexto por par verificado | Membresía activa | Factory `DesdeMembresia` y guard `ExigirEmpresa`; sin cache de privilegios revocados |

Estas cardinalidades son contratos de dominio y deben cotejarse contra mappings EF y constraints de MySQL en tareas de persistencia; no se certifican constraints físicas a partir de este dibujo.

## 4. Invariantes falsables de diseño

| ID | Invariante y condición de aceptación | Caso negativo / denegación |
| --- | --- | --- |
| INV-01 | Empresa válida con nombre no blanco; su activación no otorga membresías | Empresa inactiva o nombre vacío no se convierte en tenant utilizable sin validación |
| INV-02 | Solo `UsuarioEmpresa` activa vincula usuario/empresa/rol para una operación | Usuario sin membresía, membresía revocada o del tenant B → DENY |
| INV-03 | Rol efectivo se lee de la membresía correcta y grants de empresa | `Usuario.RolId` legacy, `EsAdministrador` o flag de UI → nunca grant cross-tenant |
| INV-04 | `ContextoTenantActual` se forma desde membresía validada y exige Empresa propia | Encabezado, claim o URL con Empresa B → DENY, aunque el token sea sintácticamente válido |
| INV-05 | Sucursal nueva tiene EmpresaId positivo, persistente y autorizado | `EmpresaId=null/0` o sucursal B para actor A → DENY |
| INV-06 | Reasignación de Sucursal requiere servicio autorizado, auditoría y validación de destino | Llamar método de dominio no puede sustituir autorización de app ni trasladar datos B |
| INV-07 | Recurso operativo tiene cadena Empresa–Sucursal–recurso coherente | FK huérfana, almacen o ubicación de B, unión cruzada → DENY |
| INV-08 | Cambiar de tenant invalida la reutilización del rol/contexto/resultado anterior | Respuesta tardía de A tras pasar a B no puede mostrar datos de A en sesión B |
| INV-09 | Desactivar membresía/usuario/empresa revoca acceso en verificación siguiente | Cache, sesión antigua o doble acción no debe retener grants revocados |
| INV-10 | Índices únicos propuestos por par y códigos normalizados no mezclan empresas | Duplicado (UsuarioId,EmpresaId), mismo código de sucursal en empresa propia sin control → rechazar según constraint |
| INV-11 | Datos personales, PasswordHash y JWT no se exponen en contratos o logs | Error 403/404, trazas y exportes no contienen token ni PII ajena |
| INV-12 | M02.01 solo produce especificación, no evidencia de implementación ni promoción | Markdown/fixtures no acreditan MySQL vivo, API, aislamiento end-to-end, A5 ni QA/PROD |

**Matriz mínima de pertenencia:** usuario U1 con membresía activa A y rol RA puede intentar recursos A dentro de sus grants; U1 sin membresía B no accede a recursos B; U2 con membresía B no accede A; sucursal sin owner verificable falla cerrado; rol legacy sin membresía no permite acceso a ninguna empresa. La aceptación definitiva exige pruebas reales al implementar consumidores, no ejemplos de papel.

## 5. Impacto, verificaciones y reversión

- **Componente:** contrato de dominio de identidad universal C01-01/M02.01. **Archivos intencionales:** únicamente este Markdown y sección autocontenida de `CHANGELOG_AI.md`. **Consumidores:** planificación posterior de Domain/Application/API/Infrastructure/Angular; ninguna interfaz runtime cambia hoy.
- **Datos/seguridad:** no escribe empresas, usuarios, membresías, permisos, JWT ni datos productivos; no toca EF, migraciones, índices físicos ni servicios. Conserva reglas de tenant A/B, RBAC, revocación y privacidad.
- **Clasificación:** `requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`.
- **A0** revisar diff, origen/sha de siete blobs, secretos, vínculos y encabezados de CHANGELOG, sin error de procedencia.
- **A1** comprobar los contratos INV-01–INV-12 mediante casos positivos/negativos documentales; distinguir aceptación futura de prueba ejecutada.
- **A2** auditar separación U/A/B, revocación, rol tenant, PII y ausencia de nueva concesión por documento.
- **A3** comprobar los nueve predecesores, DAG individual, contrato M01.06, PLAN/COLA y consumidor M02.02; no inventar pruebas de integración.
- **A4** publicar commit FF a dev; revisar diff/blobs, Scope Lock `verify-solqaryn-project-scope` y gate V5 CHANGELOG `completed/success` para el SHA exacto; readback institucional.
- **A5** corresponde exclusivamente a otra de las cinco Tasks Javier, separada de la autora; `A5_PENDIENTE` al cerrar DEV, nunca autorizar QA/main/PROD automáticamente.
- **Rollback:** `git revert` ordinario del commit documental y evento compensatorio aditivo tras comprobar el HEAD; no force-push, cambios de reglas, borrado de BITACORA, nuevos PR/ramas o migraciones.

**Salidas a verificar posteriormente:** M02.02 definirá API/DTO y errores; M02.03 persistencia/índices/migraciones; M02.04 configuración; M02.05 eventos/idempotencia solo como diseño futuro; M02.06 dependencias. M03 implementará únicamente lo que corresponda tras nuevas auditorías. Ninguna de estas unidades queda reclamada o liberada por M02.01.
