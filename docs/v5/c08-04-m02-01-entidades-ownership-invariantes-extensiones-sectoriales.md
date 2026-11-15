# SOLQARYN V5 — C08-04.M02.01: entidades, ownership e invariantes de extensiones sectoriales

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Repositorio:** `solqaryn/Solqaryn`, rama `dev`, Scope Lock STRICT.

**Naturaleza y frontera:** contrato DOCUMENTAL de M02.01. No se crea entidad C#, tabla, índice, API, permiso, flag, componente Angular, evento, migración ni interfaz operativa. Ninguna prueba runtime de módulo sectorial está acreditada. A5 por otra Task Javier; QA/main/PROD excluidos.

## 1. Fuentes reales y dependencia

- `docs/v5/c08-04-m01-01-alcance-extensiones-por-rubro.md` (blob `41dd2a10d667124c021791278d43ceab3dc02c35`): NR01–NR12 y fronteras de no duplicación.
- `docs/v5/c08-04-m01-03-inventario-codigo-contratos-extensiones.md` (blob `f34c6b59328d9d5a9e106e55eedeb250d50fb900`): once fuentes reales y diez brechas, no diez vulnerabilidades demostradas.
- `docs/v5/c08-04-m01-05-criterios-aceptacion-extensiones-sectoriales.md` (blob `9ad425e66e7839d38a2416cf97615b20e0e129a2`): AC01–AC14/N01–N14 futuros NO_EJECUTADOS.
- `docs/v5/c08-04-m01-06-bloqueos-rollback-modulos-sectoriales.md` (blob `009a4190314f89ec1516d0a8c4ddc7fe08cb4a96`): bloqueos BL01–BL16 y rollback futuros, sin permisos para desplegar.
- Entidades existentes documentadas en M01.03: `Empresa.cs` (`c44940df7c30983a4c23b93338e0cb9367962069`), `UsuarioEmpresa.cs` (`f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c`), `Producto.cs` (`19632ce1aebe6a9cbc4379523367531852ee3e7e`), `Sucursal.cs` (`8746b11fa30cdac0b97241b6221100bedd813b35`), `PublicStoreBinding.cs` (`dae03bcd963281d74e1d02e460934ab0783d39d5`).

El DAG vivo autoriza definir contratos M02 sin activar el motor de capabilities. Las seis M01 son documentación y condiciones de salida; no son evidencia de API, R2, pagos ni membresía operacional.

## 2. Modelo lógico propuesto, NO implementado

| Objeto futuro | Relación o llave conceptual | Propietario y restricción |
| --- | --- | --- |
| `CapabilityDefinition` | `capabilityKey + schemaVersion` | Catálogo global allowlisted; su existencia NO concede permiso |
| `TenantCapabilityAssignment` | `EmpresaId + capabilityKey + version` | Empresa efectiva validada en servidor, estado denegado por defecto |
| `SectorCapabilityPolicy` | `EmpresaId + capabilityKey + ruleKey` | Preferencias tipadas sólo dentro de restricciones RBAC |
| `SectorResourceBinding` | `EmpresaId + resourceKind + resourceId` | Reutilizar Producto/Sucursal/Cliente; prohibido resolver NULL como global |
| `CapabilityTransitionAudit` | `EmpresaId + operationId + expectedVersion` | Actor autorizado, tiempo, causa, versión, resultado saneado |
| `CapabilityScopeProjection` | `serverContextVersion + capabilityKey` | Proyección filtrada para Angular, nunca autoridad de autorización |

**No crear identidades paralelas.** `UsuarioEmpresa` expresa membresía/rol dentro de Empresa; una membresía comercial de gimnasio es otro concepto. `Empresa` es raíz del tenant; `PublicStoreBinding` puede resolver publicación pública pero host/slug no otorgan privilegio privado. Cada tabla futura requiere modelado EF/MySQL, índices por consulta real y migración reversible autorizada en microtarea de implementación; aquí sólo se fijan claves y relaciones de diseño.

## 3. Invariantes INV01–INV14

| ID | Invariante verificable futuro | Rechazo seguro requerido |
| --- | --- | --- |
| INV01 | Empresa activa, obtenida por contexto autenticado backend | Empresa inactiva o desconocida |
| INV02 | Tenant A/B aislados en lectura, mutación y exportación | ID de B con credencial A |
| INV03 | RBAC recurso/acción validado después de capability | Flag habilitado con rol revocado |
| INV04 | Assignment tipado, versionado y allowlisted | Clave desconocida, código HTML/SQL/JS |
| INV05 | Estado disabled/suspended no permite nuevas altas | Caché obsoleta reactiva operación |
| INV06 | Desactivación conserva historia y trazabilidad | Borrado transversal de reservas, cobros o productos |
| INV07 | Sucursal y recurso pertenecen a Empresa | Recurso de otra sede/empresa |
| INV08 | EmpresaId legado nullable exige resolución probada | NULL tratado como acceso multiempresa |
| INV09 | Operaciones versionadas resisten doble escritura | CAS vencido o replay con payload distinto |
| INV10 | Auditoría append-only, evidencia y actor saneados | Tokens, PII y datos de B en logs |
| INV11 | Host/binding público verificado en servidor | Host manipulado habilita ruta privada |
| INV12 | Angular guards sólo afectan experiencia | UI decide autorización backend |
| INV13 | Error o ausencia de proveedor causa NO_GO | Fallback permisivo o grant silencioso |
| INV14 | Reversión conserva tenants independientes | Rollback parcial sin evidencia o cruces de estado |

## 4. Casos sintéticos de aceptación P/N (NO_EJECUTADOS)

- P01/N01: A con RBAC y capability consulta su reserva; ID de reserva B es denegado sin metadatos.
- P02/N02: A cambia estado de módulo con versión vigente; versión vieja o doble petición no duplica grant.
- P03/N03: apagar capability impide altas y mantiene histórico; nunca elimina datos previos ni activos de B.
- P04/N04: recurso con ownership verificable permanece de A; `EmpresaId=NULL` exige resolución y no da acceso global.
- P05/N05: membresía comercial se representa sin alterar `UsuarioEmpresa`; revocación de rol deniega.
- P06/N06: host público validado resuelve catálogo público; host forjado no autentica backoffice.
- P07/N07: configuración allowlisted aplica solo tras autorización; entrada no tipada se rechaza.
- P08/N08: auditoría redactada identifica transición; secreto/PII/correlation ajena se rechaza.

Estos pares son criterios escritos para xUnit/API/EF/MySQL/Playwright futuros y NO son ejecución de esas pruebas. A0 revisa diff e invariantes; A1 compara modelo y pares positivos/negativos documentales; A2 exige aislamiento y denegación como límites de diseño; A3 vincula entidades existentes y M01 sin crear integración; A4 requiere commit fast-forward/CAS, Scope Lock y procedencia V5 `completed/success` del SHA exacto, más PLAN/COLA/BITACORA readback. A5 independiente tras LISTO.

**Reversión:** revert Git normal de este Markdown y entrada correspondiente de CHANGELOG sin reescribir trabajos ajenos. M02.06 sólo con LISTO+A5 de M02.01–05, DAG F=VALIDADO/G=SI y COLA G/O=SI; M03 requerirá código real y CI dirigida. Nada en este documento ejecuta migraciones, eventos o QA/main/PROD.
