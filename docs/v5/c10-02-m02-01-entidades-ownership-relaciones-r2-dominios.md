# SOLQARYN V5 — C10-02.M02.01: modelo conceptual de entidades, ownership y relaciones para archivos privados

**Autor:** PRIMARY DEV 01 — JAVIER, Task `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Fecha:** 2026-11-15.

## Objetivo y no-alcance

Definir **sólo contratos conceptuales** para una posible evolución de almacenamiento privado, namespaces/prefijos por empresa, objetos, capacidades temporales, retención y dominios de archivos. **No activar Cloudflare R2**, no cambiar Cloudinary vigente ni DNS/CDN/CORS, ni comprar servicios, abrir cuentas, alterar credenciales, emitir URLs firmadas reales, crear endpoints, migraciones SQL, permisos JWT/RBAC o publicar QA/main/PROD.

El lote `C10-02.M01.01–05` consta LISTO con A5 de revisor independiente; `C10-02.M01.06` SHA `3c115e7f3dc9ea48edac2239e5515f9470468d99`, documento blob `bc33bb8e10455504c4deb69156f0acb4c816b697`, recibió A5 independiente DEV01 tras revisión causal. Sólo después `C10-02.M02.01` obtuvo DAG F=VALIDADO/G=SI, COLA G/O=SI y PLAN PENDIENTE sin propietario. La admisión significa **M02 documental**, no permiso para migrar archivos, publicar dominio o instalar un proveedor.

El diseño debe cotejar contratos actuales de Cloudinary, manejo de archivos, DTO, autenticación, control de inquilinos, retención y auditoría de `C10-02.M01.03`. Las decisiones D01–D08 registradas en M01 permanecen abiertas; no asumir proveedor, región, costos, contrato, firma, TTL ni configuración real por ambiente.

## Modelo conceptual (no crea tablas ni servicios)

| Entidad propuesta | Identidad/owner | Relación y límite de autoridad |
| --- | --- | --- |
| RecursoPrivado | Empresa efectiva + ID opaco + clasificación | Representa archivo lógico, no URL pública |
| VersiónRecurso | Recurso, versión/hash, tamaño verificado | Historia inmutable, rollback no borra fuente |
| LocalizadorProveedor | Recurso + proveedor aprobado + clave opaca | Nunca confundir bucket/key con autorización |
| ÁmbitoAlmacenamiento | Empresa, entorno, propósito y política | Separación A/B; prefijo no sustituye ACL |
| ObjetoPendienteIngreso | Recurso, actor, tamaño/tipo previsto, expiración | Cuarentena hasta inspección de bytes real |
| EvidenciaIntegridad | Versión, digest servidor, tamaño y verificación | No validar sólo por cabecera MIME cliente |
| CapacidadTemporal | Recurso, método, actor, TTL y nonce | Propuesta de enlace firmado, no token emitido |
| PublicaciónDeArchivo | Canal/host permitido, recurso y versión | Nunca hereda visibilidad desde dominio |
| PolíticaRetención | Empresa, clasificación, hold y expiración | Retención aprobada; no eliminar por fallback |
| RegistroAcceso | Actor, recurso, empresa, resultado y correlación | Redacción de tokens, PII y signed URL |
| PlanTraslado | Manifiesto, origen/destino y checkpoint | Migración reversible y sin borrar origen |
| EstadoReconciliación | Manifiesto, hash/contaje y diferencias | No declarar cutover sin readback real |

Son **proyecciones futuras**. Las relaciones conceptuales: Empresa 1:N RecursoPrivado; RecursoPrivado 1:N VersiónRecurso, 0:N LocalizadorProveedor, 0:N CapacidadTemporal y 0:N RegistroAcceso; VersiónRecurso 0:N EvidenciaIntegridad; PlanTraslado 1:N EstadoReconciliación. Revisar esquema y API existentes antes de proponer nuevas tablas.

## Invariantes fail-closed exigibles en una futura implementación

1. **FILE01:** empresa efectiva deriva de backend autenticado y membresía vigente, nunca de `EmpresaId`, host, key o prefijo cliente.
2. **FILE02:** permisos RBAC por acción leer/subir/listar/borrar/publicar, revalidados en servidor antes de firmar cualquier capacidad.
3. **FILE03:** A no puede leer, sobrescribir, enumerar ni borrar recurso B aunque conozca una URL o referencia opaca.
4. **FILE04:** key/prefijo normalizado y opaco; denegar traversal, doble codificación, prefijos ajenos y colisiones.
5. **FILE05:** URL temporal futura limita recurso, verbo, expiración, política y alcance, sin exponer secreto de firma.
6. **FILE06:** TTL/replay y revocación deben distinguir bearer temporal y token de un solo uso; no prometer revocación instantánea por TTL.
7. **FILE07:** caché/CDN se particiona por empresa y contexto; private/no-store donde corresponda, sin fuga A/B por host.
8. **FILE08:** verificar bytes/tamaño/checksum/MIME permitido en backend y pruebas, no por extensión del nombre.
9. **FILE09:** rechazo/timeout/parcial no publica recurso, no pierde estado ni habilita download de archivo truncado.
10. **FILE10:** logs, URLs, auditoría y errores no muestran JWT, firma, credencial, PII ni cuerpo privado.
11. **FILE11:** clasificación/hold/retención se consultan antes de lifecycle/borrrado, sin defaults irreversibles inventados.
12. **FILE12:** migración a proveedor nuevo requiere inventario, digest por versión, readback y plan de rollback ensayado.
13. **FILE13:** no borrar origen antes de conciliación de permisos, hashes, versiones, referencias y respaldo verificado.
14. **FILE14:** nombre de dominio no prueba tenant; host/certificado/política deben validarse en servidores autorizados.
15. **FILE15:** cuotas y streaming limitan tamaño, reintentos y concurrencia por empresa sin saturar otras empresas.
16. **FILE16:** Scope Lock, CI causal A0–A4 y A5 independiente no sustituyen aprobación contractual/infraestructura/QA/PROD.

## Ensayos futuros (NO EJECUTADOS)

| Positivo por implementar | Negativo que debe rechazarse |
| --- | --- |
| P01 Actor A autorizado lee recurso A | N01 Actor A consulta recurso B |
| P02 Prefijo se normaliza y valida | N02 Traversal cruza espacio ajeno |
| P03 Firma válida se limita a recurso/verbo/TTL | N03 Firma alterada o vencida sirve |
| P04 Upload pequeño válido confirma digest | N04 MIME falso o archivo demasiado grande |
| P05 Descarga privada respeta políticas | N05 CDN de A entrega caché B |
| P06 Revocación niega nuevas capacidades | N06 JWT/rol revocado firma nuevo enlace |
| P07 Registro auditado no revela datos sensibles | N07 Logs contienen secret o URL firmada |
| P08 Retención respeta hold aprobado | N08 Lifecycle elimina objeto protegido |
| P09 Reintento de ingreso es idempotente | N09 Timeout genera doble recurso |
| P10 Migración compara hash/versión | N10 Hash distinto se declara éxito |
| P11 Rollback conserva referencia y origen | N11 Cutover borra origen no conciliado |
| P12 Tenant B no accede a tenant A | N12 Host/subdominio cliente altera owner |

P01–P12/N01–N12 son **oráculos FUTUROS_NO_EJECUTADOS**; no existe en esta microtarea suite .NET, Angular, browser, R2, DNS, HTTP o MySQL ejecutada. Tampoco se proporciona prueba de restauración real, propiedad de dominio o costos aprobados.

## A0–A5, impacto y reversión

**A0:** corroborar Skill canónica, HEAD, Scope Lock, Plan/COLA/DAG, CAS/owner y diff exclusivo de este documento + sección autocontenida de CHANGELOG. **A1:** cotejo documental de doce proyecciones, dieciséis invariantes y doce pares P/N futuros. **A2:** verificar explícitamente los requisitos tenant A/B, JWT/RBAC, URLs temporales, replay, retención, integridad, privacidad y no activación. **A3:** compatibilidad conceptual con contratos existentes de archivos y Cloudinary; .NET/MySQL/Playwright/DNS/R2 `NO_APLICA` a diff Markdown, nunca PASS. **A4:** publicar por fast-forward/CAS sobre SHA vigente, leer blobs/diff y exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success en SHA aplicable; registro BITACORA/readbacks. CI requerida SKIPPED/CANCELLED/IN_PROGRESS/FAIL no acredita PASS.

**A5:** revisión independiente por otra Task Javier distinta de autora; registrar evidencia SHA/diff/seguridad en PLAN K y BITACORA, releer gates. M02.01 documental LISTO no libera R2/DNS, M03 runtime, migraciones, QA/main/PROD ni activa proveedor.

**Rollback R1 documental:** commit aditivo o `git revert` normal, sin sobrescribir otras Tasks ni reescribir BITACORA. **Rollback R2 futuro no ejecutado:** manifiesto de objetos/huellas, copia y restauración verificadas en ambiente efímero, cutover controlado y retorno al proveedor fuente; no mover ni borrar datos actuales por aprobar este documento.
