# SOLQARYN V5 — C10-01.M02.01: entidades, ownership y invariantes de storage neutral

**Alcance autorizado:** diseño documental M02 de C10-01, `dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autor:** PRIMARY DEV 03 — JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Plan único:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Fecha:** 2026-11-15.

## Estado observable, dependencia y límite

Este contrato define exclusivamente las **entidades conceptuales, relaciones, propiedad y reglas de invariantes** para una futura interfaz de almacenamiento neutral. No crea modelos EF, tablas, controladores, DTO compilados, endpoints, colas, eventos, buckets o adaptadores. M01.01–M01.06 están LISTO con A5 independiente, y el gate particular M02.01 tiene `DAG_TECNICO F=VALIDADO/G=SI` y `COLA G/O=SI` al preflight; no se extrapola este permiso a M02.06, M03 ni a entornos distintos de dev.

El inventario comprobado de `C10-01.M01.03` registra `IImageStorageService`, `ICompraDocumentoStorageService`, `StorageTenantContext`, `CloudinaryImageStorageService`, `CloudinaryCompraDocumentoStorageService` e `ImagenUploadSecurity`; sus contratos presentes son evidencia de compatibilidad que debe preservarse, **no prueba de que el objetivo neutral para video/exportación esté implementado**. Cloudinary continúa como integración vigente y R2/Hostinger no se activan.

## Modelo lógico propuesto (no persistido)

| Entidad conceptual | Identidad / ownership | Relaciones y restricciones propuestas |
| --- | --- | --- |
| RecursoAlmacenado | `ResourceId` opaco, `EmpresaId` resuelto por servidor | Pertenece exactamente a una empresa, un ámbito y una categoría; no se expone locator proveedor como ID de negocio |
| SujetoPropietario | Tipo de propietario y `OwnerId` dentro de la empresa | Relaciona el recurso con una compra, documento, imagen o exportación autorizada; propietario inexistente se rechaza |
| VersionRecurso | `ResourceId`, versión inmutable creciente | Huella SHA-256 de bytes verificados, tamaño, MIME inspeccionado y fecha; una sustitución crea versión, no sobrescribe auditoría |
| LocalizadorProveedor | ID interno vinculado a versión y ámbito | Referencia opaca a proveedor/adaptador vigente; nunca concede permiso ni determina empresa desde ruta/carpeta |
| PoliticaAccesoRecurso | Operación, empresa, actor y permiso RBAC verificables | Leer, crear, modificar, borrar y exportar se autorizan por separado desde backend |
| EstadoTransferencia | ID de intento idempotente por empresa/recurso/operación | Pendiente, validación, confirmado, fallido o compensado; prohibida publicación de parcial |
| EvidenciaIntegridad | Versión, hash, origen de medición y resultado | Hash de bytes calculado en borde autorizado; un hash declarado por navegador no se certifica |
| ReferenciaTemporal | Recurso, operación, expiración y revocación | Enlaces transitorios limitados a tenant/operación, sin secretos ni PII en logs |

Todas son **propuestas de contrato**: campos y nombres no son entidades C# ni migraciones existentes, y se ajustarán a los modelos reales al implementar M03.

## Invariantes verificables de diseño

1. **OWN01:** cada recurso pertenece a exactamente una empresa determinada por identidad/membresía del backend; nunca por `EmpresaId` del cliente.
2. **OWN02:** ningún locator físico, URL o nombre de carpeta funciona como autorización o como autoridad de tenant.
3. **OWN03:** permisos RBAC distintos para lectura, escritura, borrado y exportación; lectura no implica mutación.
4. **OWN04:** un actor sin sesión válida, permiso o membresía actual recibe denegación por defecto, sin fallback público.
5. **OWN05:** relaciones propietario/recurso/versiones no atraviesan empresas; referencias huérfanas no pasan a estado publicado.
6. **OWN06:** versiones confirmadas y sus checksums no pueden alterarse silenciosamente; cambios requieren nueva versión y auditoría.
7. **OWN07:** la huella SHA-256 se calculará de bytes reales en servidor; declaraciones externas no se consideran evidencia.
8. **OWN08:** MIME se verificará por firma y parser seguro, además de política por tipo; extensión o Content-Type no bastan.
9. **OWN09:** tamaño máximo, cuota y timeout se controlarán antes/durante streaming, también en operaciones reanudadas.
10. **OWN10:** un intento de carga incompleto o con checksum inválido nunca será legible ni consumible.
11. **OWN11:** borrar un recurso con consumidores activos requiere política explícita y efecto auditado, no eliminación implícita.
12. **OWN12:** enlaces temporales deben caducar, limitar operación, prevenir replay y no servir datos de otro tenant.
13. **OWN13:** la idempotencia se delimita por empresa + actor + operación + recurso + clave; no deduplicar entre tenants.
14. **OWN14:** respuestas y logs no filtran identificadores internos, firmas, secretos, tokens ni rutas privadas.
15. **OWN15:** la caída de proveedor no habilita proveedor alterno no aprobado, cambio de entorno ni migración automática.
16. **OWN16:** conversión/cutover exige manifiesto, conteos, hashes de bytes y restore/rollback verificados en un entorno efímero autorizado.
17. **OWN17:** las interfaces Cloudinary existentes conservan compatibilidad hasta contrato y regresiones dirigidas aprobadas.
18. **OWN18:** un LISTO documental y A5 sólo habilitan la revisión de sucesoras según DAG individual; nunca QA/main/PROD.

## Relaciones y transiciones futuras

`Empresa 1:N RecursoAlmacenado`; `RecursoAlmacenado 1:N VersionRecurso`; `VersionRecurso 1:N LocalizadorProveedor` (sin habilitar múltiples proveedores activos); `RecursoAlmacenado N:1 SujetoPropietario`; `VersionRecurso 1:N EvidenciaIntegridad`. Estas cardinalidades son **decisiones de diseño por validar** frente a relaciones existentes, no migraciones autorizadas.

Transiciones futuras permitidas: `SOLICITADO -> VALIDANDO -> CONFIRMADO`, o `SOLICITADO/VALIDANDO -> FALLIDO -> COMPENSADO`. El recurso solamente será visible después de validación de ownership, permisos, tipo, cuota e integridad. `CONFIRMADO -> REVOCADO` requiere permiso y auditoría. No se implementa máquina de estados en este cambioset.

## Oráculos positivos/negativos futuros; NO EJECUTADOS

| Positivo esperado (futuro) | Negativo a denegar (futuro) |
| --- | --- |
| P01 Actor A con permiso lee recurso A | N01 Actor A intenta recurso B |
| P02 Empresa A crea versión con MIME y bytes válidos | N02 Cliente modifica EmpresaId o ruta para suplantar |
| P03 Repetición con misma clave idempotente conserva resultado | N03 Reutilizar clave de empresa A para B |
| P04 Lectura verifica checksum de bytes conservados | N04 SHA-256 declarado no coincide con bytes reales |
| P05 Documento privado con permiso de lectura independiente | N05 Actor sólo lector intenta borrar/escribir |
| P06 URL temporal válida se limita a recurso y operación | N06 URL caducada, revocada o de otro recurso |
| P07 Falla del proveedor produce respuesta segura y auditada | N07 Fallback a proveedor/ambiente no autorizado |
| P08 Restore efímero reconstituye manifiesto y hashes | N08 Publicación de carga parcial o restore sin prueba |

## A0–A5, seguridad y reversibilidad

**A0 (documental):** confirmar diff limitado a este Markdown y sección autocontenida de `CHANGELOG_AI.md`, autor/owner y ausencia de secretos. **A1:** verificar presencia y consistencia de ocho entidades, 18 invariantes y ocho pares P/N, todos futuros; no atribuir tests ejecutados. **A2:** inspeccionar limitaciones JWT/RBAC, empresa A/B, checksum/MIME/tamaño y revocación fail-closed; no inferir seguridad HTTP/EF/Cloudinary en ejecución. **A3:** contrastar con cinco fuentes de código del inventario M01.03 y gate M01.06, manteniendo compatibilidad actual; compilar .NET/Angular, probar HTTP/MySQL o Playwright es NO_APLICA al diff sólo Markdown. **A4:** publicación por fast-forward contra HEAD exacto con Scope Lock y procedencia V5 `completed/success`; verificar PLAN/COLA/DAG/BITACORA después de cada transición. **A5:** revisión independiente por otra de las cinco Tasks Javier distinta del autor; no autoprobación.

**Rollback de este contrato:** `git revert` normal del commit documental; preservar BITACORA y no cambiar datos, credenciales, proveedores o ramas. Sin publicación a QA/main/PROD, sin compra ni activación de R2/Hostinger.
