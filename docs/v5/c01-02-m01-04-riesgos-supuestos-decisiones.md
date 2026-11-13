# C01-02.M01.04 — Riesgos, supuestos y decisiones preliminares de capabilities y permisos

**Tipo:** V5 FINAL, M01.04 de C01-02 (preparación documental). **Autor de este ID:** PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. Repositorio exclusivo `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuentes únicas: [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Resultado exigido de la fase:** modelar capacidades por rubro, plan, rol, tenant, sucursal y recurso, con una matriz de denegación por defecto. **Esta unidad solo registra riesgos, supuestos y decisiones pendientes**: NO implementa ni certifica la matriz, permisos, licencias, migraciones, UI o despliegue. M01.06 y M02+ conservan sus gates y la revalidación del contrato C01-01 final. No confundir preparación con autorización efectiva.

## Fuentes técnicas comprobadas para análisis

- `backend/src/Application/Services/PermisoService.cs` (blob `278a8fa90dec43609d20157208a143f5794dcc35`): matriz basada en roles y grants persistidos; revisión de rol, ámbito y empresa propietaria antes de mutar.
- `backend/src/Application/Common/CatalogoPermisosBase.cs` (blob `6060b4c54c0428902603f2157984b693a324e8f3`): catálogo de identificadores, no concede autorización por existir.
- `backend/src/Domain/Security/ContextoTenantActual.cs` (blob `7377d2d752985cc9baf31d08280f38ad94a6a114`): contexto desde membresía verificada; rechazo de recursos de otra empresa.
- `backend/src/Application/Security/PlatformSupportPolicy.cs` (blob `b103f0b0ec7b9169c060661f6a4456f8863152cf`): allowlist de soporte DEV restringida a lecturas revisadas, no bypass general.
- `backend/tests/Solqaryn.Tests/N04CatalogoPermisosRuntimeTests.cs` (blob `00a9044cd8d400cde58b7f4bce63ff98a7432e94`): prueba existente para catálogo vs permisos declarados por API; no se ejecutó en esta unidad.
- `docs/ERP_N0_4_RBAC_RELACIONAL.md` y `docs/v5/c00-01-m02-01-domain-ownership.md`: referencia de grants, ownership y riesgos históricos. Los resultados de certificación antiguos NO son prueba para código nuevo.

## Riesgos y tratamiento obligatorio

| ID | Riesgo / detonador | Control / evidencia requerida | Acción sin evidencia |
| --- | --- | --- | --- |
| R01 | Se deduce capacidad por rubro o plan sin permiso efectivo persistido | Intersección explícita de entitlement y grant RBAC, probada en API; ningún plan da acceso por sí solo | Denegar |
| R02 | Un usuario de empresa A presenta tenant, sucursal o recurso de empresa B | Membresía verificada, `ContextoTenantActual.ExigirEmpresa` y prueba negativa A→B | Denegar y auditar |
| R03 | Un rol administrador pretende omitir grants relacionales | Prueba de ausencia de bypass y presencia de grants explícitos | Denegar hasta tener grants |
| R04 | Cambio de matriz afectaría un rol global, del sistema o sin empresa propietaria | Validar `EsSistema`, `Ambito`, `EmpresaId` y autorización del actor | Rechazar mutación |
| R05 | Catálogo de capabilities se separa de acciones realmente exigidas por controllers | Matriz API/DTO → capability → permiso → seed; test de catálogo runtime | Bloquear implementación incompleta |
| R06 | Plan/rubro/sucursal cambia a mitad de una operación | Definir vigencia, transacción y revalidación server-side en M02/M03 | No cachear una aprobación irrevocable |
| R07 | Recurso legado con `EmpresaId` nulo se interpreta como público | Matriz de ownership y cuarentena de registros sin propietario | No conceder acceso |
| R08 | Selector Angular permite operación sin permiso backend | Probar 403 en API incluso con UI manipulada | Bloquear cambio de seguridad |
| R09 | Error de permisos expone PII, JWT, headers o detalle de autorización | Respuestas seguras, log minimizado y casos negativos | Bloquear cierre |
| R10 | PR/CI anterior, job SKIPPED o Scope Lock de otro SHA se usa como PASS | Checks `head_sha` exacto y controles según tipo de artefacto | Mantener VALIDANDO |
| R11 | Dos Tasks reclaman el mismo ID o revisor se hace pasar por autor | PLAN!H/I, COLA por fórmula, BITACORA A:Z con claves únicas y Task distinta para A5 | No cambiar propiedad ni promover |

## Supuestos sujetos a verificación posterior

| ID | Supuesto provisional | Qué lo invalida |
| --- | --- | --- |
| S01 | El RBAC actual se decide por grants persistidos y ámbito empresarial | Código nuevo introduce un bypass sin auditoría |
| S02 | Rubro y plan pueden convertirse en restricciones adicionales, nunca en grants implícitos | Diseño o negocio exige activación automática sin autorización |
| S03 | Sucursal y recurso tienen ownership verificable desde servidor | Alguna relación o FK carece de tenant inequívoco |
| S04 | La matriz `C01-01` definirá identidad/membresías antes de ejecutar `C01-02.M02+` | C01-01 final cambia el contrato provisional |
| S05 | Los 11 prerrequisitos de preparación documental figuran LISTO y DAG VALIDADO | Cualquier readback posterior contradice PLAN/COLA/DAG |
| S06 | No hay implementación ni cambios de esquema en esta M01.04 | Diff contiene C#, Angular, SQL, YAML o datos ejecutables |

## Registro de decisiones

| ID | Estado | Decisión y dueño |
| --- | --- | --- |
| D01 | FIJADA POR GOBERNANZA | Ausencia de grant = denegación; no reconocer `EsAdministrador` como bypass; responsable: backend/autorización |
| D02 | FIJADA POR SEGURIDAD | Tenant/sucursal/recurso se validan por contexto del servidor, nunca solo por header o UI; responsable: backend |
| D03 | PENDIENTE C01-02.M02 | Definir precedencia entre plan, rubro, rol, tenant, sucursal y recurso con tabla de verdad; responsable: microtarea de contrato |
| D04 | PENDIENTE C01-02.M02 | Definir modelo de expiración y revocación de capability y estrategia de caché; responsable: microtarea de contrato |
| D05 | PENDIENTE DE IMPLEMENTACIÓN | Establecer pruebas positivas/negativas de API y multiempresa con MySQL real cuando el resultado lo exija; responsable: autora futura |
| D06 | FUERA DE ESTA TAREA | No tocar datos, secretos, QA/main/PROD, integración con cobros, migraciones ni licencias sin autorización y A5 independiente |

## Criterios documentales de aceptación y rechazo

**Positivo P1:** se identifica cada riesgo con detonador, evidencia requerida y rechazo seguro; ninguna decisión pendiente se presenta como implementada. **Positivo P2:** se puede trazar la autoridad `RolPermiso → Permiso`, el contexto tenant y el control separado de plan/rubro, sin ampliar permisos.

**Negativos N1–N6:** rechazar acceso por plan sin grant; rechazar A→B aunque la pantalla lo permita; rechazar bypass administrador; rechazar rol global/sistema sin ownership; rechazar `SKIPPED` como PASS de prueba requerida; rechazar QA/main/PROD sin A5 independiente y permiso expreso. Son escenarios de revisión documental, **no** pruebas HTTP, MySQL o navegador ejecutadas por esta microtarea.

## Clasificación, validaciones y rollback

`requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`: documento sin código ejecutable. `requiere_security=SI` (revisión de riesgos y fronteras JWT/RBAC/tenant). `requiere_scope_lock=SI` (commit documental a dev).

A0: revisar diff, estructura, secretos y referencias; A1: P1/P2 y N1–N6 en documento; A2: denegación por defecto y no exposición de PII/secretos; A3: trazabilidad Plan V5 ↔ DAG ↔ arquitectura y código leído; A4: SHA, checks obligatorios, commit fast-forward y readback en PLAN/COLA/BITACORA. A5 solo por otro TASK_ID. **Rollback:** revert normal de commit directo, sin force-push, y rectificación institucional aditiva. No marcar implementación de capabilities como LISTO por este documento.
