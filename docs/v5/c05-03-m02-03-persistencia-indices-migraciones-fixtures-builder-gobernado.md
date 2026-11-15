# SOLQARYN V16 — C05-03.M02.03: persistencia conceptual, indices, migraciones y fixtures del builder gobernado

**ID:** C05-03.M02.03, PLAN_MAESTRO!A802. **Autor ejecutor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Repo/branch:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Clasificacion:** V15 R0 DOCUMENTAL, 2026-11-15.

**Fuente unica:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN/COLA/DAG institucional. **Resultado:** contrato de persistencia e integridad de paginas/secciones tipadas con rechazo de HTML/JS/SQL ejecutable, URL peligrosas y payloads excesivos. **Ningun objeto MySQL/EF fue creado ni migrado**.

## 1. Precedencia, seguridad y no-alcance

`C05-03.M01.06` conserva BL01–BL16, RB01–RB10, LG01–LG12 y D01–D08 PENDIENTES; `C05-03.M02.01` propone E01–E12 e INV01–INV16; `C05-03.M02.02` documenta contratos API/DTO/error sin endpoints ejecutables. DAG F763=VALIDADO/G763=SI y COLA G802/O802=SI habilitan ESTE R0, no migraciones ni builder real. Los conceptos siguientes se alinean con PageDraft, SectionPlacement, PageRevision, approvals, proyecciones y hosts ya descritos.

No alterar entidades EF existentes, DbContext, migraciones, Angular, rutas, permisos JWT/RBAC, negocio de inventario, hosts, secrets, servidores, CDN ni QA/main/PROD. El cliente nunca decide el tenant autoritativo: `EmpresaId` y autorización se resuelven del actor validado en servidor. La UI no sustituye control backend. No usar Aiven/productivo para pruebas.

## 2. Doce estructuras candidatas P01–P12 (NO CREADAS)

| ID | Estructura futura | Constraint de integridad requerido |
| --- | --- | --- |
| P01 | BuilderPageDraft | (EmpresaId, PageId) y OwnerUserId verificado, estado privado |
| P02 | BuilderSectionPlacement | (EmpresaId, PageId, SectionId), orden acotado y FK conceptual tenant-scoped |
| P03 | BuilderPageRevision | (EmpresaId, PageId, Revision), inmutable/monotona, hash y ETag |
| P04 | BuilderRevisionSection | copia append-only de la seccion de revision propia |
| P05 | BuilderComponentRegistry | ComponentKey+Version de allowlist precompilado |
| P06 | BuilderPropertySchema | props tipadas/limitadas, nunca HTML/CSS/SQL libre |
| P07 | BuilderTokenPreset | tokens y paleta versionados aprobados de plataforma |
| P08 | BuilderPublicationApproval | EmpresaId/PageId/Revision/ApproverId, estado revocable |
| P09 | BuilderPublicProjection | tenant+host verificados, solo revision publicada minima |
| P10 | BuilderHostBinding | hostname normalizado, propiedad comprobada server-side |
| P11 | BuilderPreviewLease | tenant+actor+revision+expiracion, preview read-only |
| P12 | BuilderAuditEvent | metadatos minimos de actor, tenant, operacion, resultado y correlacion sin PII |

Son contratos hipoteticos; no se aprueban tablas ni se valida schema real. Todas las consultas, relaciones, caches y eventos deberan filtrar por tenant autentico y recurso autorizado para evitar IDOR/BOLA. La revision obsoleta devuelve conflicto sin escrituras.

## 3. Diez indices/constraints IDX01–IDX10 (NO CREADOS)

| ID | Clave conceptual | Por que |
| --- | --- | --- |
| IDX01 | Draft(EmpresaId, PageId) UNIQUE | identidad aislada |
| IDX02 | Placement(EmpresaId, PageId, Position) UNIQUE | orden determinista |
| IDX03 | Revision(EmpresaId, PageId, Revision) UNIQUE | CAS/historial |
| IDX04 | RevisionSection(EmpresaId, PageId, Revision, SectionId) UNIQUE | pertenencia |
| IDX05 | Component(ComponentKey, Version) UNIQUE | allowlist versionada |
| IDX06 | Property(ComponentKey, Version, PropertyName) UNIQUE | props permitidas |
| IDX07 | Approval(EmpresaId, PageId, Revision, Status) | revocacion |
| IDX08 | Projection(VerifiedHost, EmpresaId, ActiveRevision) | host y cache segregada |
| IDX09 | Preview(EmpresaId, ActorId, ExpiresAt) | expiracion |
| IDX10 | Audit(EmpresaId, ResourceId, CreatedAt) | traza sin fuga |

No suponer comportamiento de UNIQUE/NULL, FK ni planes de consulta del proveedor Oracle sin integration real de MySQL 8.4. Indices no reemplazan las verificaciones de roles, revocacion, propiedad y transacciones.

## 4. Seis fases futuras MIG01–MIG06 — TODAS NO EJECUTADAS

1. **MIG01 inventario:** verificar compatibilidad de EF Core 10.0.12/Oracle MySql.EntityFrameworkCore 10.0.9 y baseline, sin alterar BD.
2. **MIG02 expansion:** migrador unico, grants minimos y tablas aditivas con rollback documentado.
3. **MIG03 integridad:** constraints y FKs con tenant A/B cruzado denegado; preservar historial.
4. **MIG04 backfill:** fixtures sinteticos, checkpoints/idempotencia, ninguna carga productiva.
5. **MIG05 indices:** planes EXPLAIN y cargas reproducibles, sin bloqueo transversal ni perdida.
6. **MIG06 cutover:** solo tras Category=Integration MySQL 8.4 efimero, xUnit A/B, CI, reversa, controles y autorizacion separada.

Esta lista no ordena ejecutar ninguno; D01–D08, LG01–LG12, runtime, QA/main/PROD siguen NO_GO segun gate causal.

## 5. Doce fixtures futuros FX01–FX12: P/N, 0 ejecutados

| ID | Positivo futuro | Negativo/limite futuro |
| --- | --- | --- |
| FX01 | Actor A crea draft A | tenant B no lee ni edita A |
| FX02 | Seccion A pertenece a pagina A | Seccion B en pagina A se rechaza |
| FX03 | ComponentKey/version permitidos | version desconocida no interpreta codigo |
| FX04 | Prop tipada segura | script/handler/SQL ejecutable no se procesa |
| FX05 | URL HTTPS allowlisted | javascript:, file:, IP privada/SSRF denegados |
| FX06 | Revision actual guarda con CAS | ETag stale: conflicto sin lost update |
| FX07 | Publicador con rol y approval valido | rol revocado: no publica |
| FX08 | Preview propio sin efectos | no activa worker, correo, webhook ni BD |
| FX09 | Host propio proyecta minimo | host B no filtra drafts/cache A |
| FX10 | Rollback autorizado versiona | rollback sin permiso no elimina historia |
| FX11 | Carga bajo limites | exceso bytes/profundidad rechaza acotado |
| FX12 | Auditoria redactada | PII/secrets/IDs B nunca se registran |

IDs `EmpresaA=101`, `EmpresaB=202` puramente ficticios. **12 pares de pruebas especificados, 0 ejecutados**: no certificar HTTP, SQL, xUnit ni Playwright. En implementacion R2 deberan acreditarse positivos, negativos, fronteras, revocacion y pruebas MySQL reales Category=Integration en servicio efimero.

## 6. A0–A5, impacto, evidencia y reversa

- **A0:** ID, contratos M01.06/M02.01/M02.02, P01–P12/IDX01–IDX10/MIG01–MIG06/FX01–FX12; cambios solo a este Markdown y seccion nueva autocontenida de CHANGELOG.
- **A1:** consistencia documental de relaciones, pertenencia tenant, version, errores y 12 pares futuros NO EJECUTADOS.
- **A2:** JWT/RBAC, empresa A/B, revocacion, XSS/SSRF, CAS, PII y host/caches definidos como salvaguardas futuras, NO_APLICA ejecucion seguridad al diff R0.
- **A3:** cotejo de fuentes existentes; .NET/Angular/HTTP/MySQL/EF/Playwright NO_APLICA por no introducir runtime.
- **A4:** publicacion fast-forward/CAS `dev`, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact SHA; PLAN/COLA/DAG y BITACORA A:Z con readback.
- **A5:** Task Javier distinta del autor debe revisar independientemente; no se presume PASS ni se libera M03/QA/PROD.

**Rollback documental:** `git revert` no destructivo tras validar HEAD y concurrencia; BITACORA historica intacta. Las decisiones D01–D08, RG/BL/LG y requisitos de seguridad permanecen vigentes.
