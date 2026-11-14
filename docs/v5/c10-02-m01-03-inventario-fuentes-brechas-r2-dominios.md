# SOLQARYN V5 — C10-02.M01.03: inventario de fuentes y brechas para R2 y dominios de archivos

**Unidad:** C10-02.M01.03, PLAN_MAESTRO fila 1480; objetivo literal del STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL: «Leer contratos, código y evidencia disponibles sin asumir certificación». **Autor:** PRIMARY DEV 04, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Origen:** Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Corte de inspección:** 2026-11-14, commit `af43226f7451c1cf3d5db39b7669c44689d739ec`.

## Separar hechos observados de objetivos futuros

C10-02 propone buckets privados, prefijos tenant, URL firmada, expiración y lifecycle **sin cutover**; estos conceptos son alcance futuro, no evidencia de que exista R2, DNS ni backend R2 habilitado. C10-01 tiene contratos para almacenamiento Cloudinary y preparación documental provider-neutral. Esta microtarea inspecciona diez fuentes originales de Git, incluyendo interfaces, clases, pruebas y dos documentos M01 de C10-02; registra **brechas de evidencia**, no defectos explotables ni pruebas runtime ejecutadas.

## Inventario de diez blobs exactos de Git

| ID | Fuente verificada a SHA de dev | Blob SHA | Observación sustentada / límite |
| --- | --- | --- | --- |
| S01 | `backend/src/Application/Interfaces/IImageStorageService.cs` | `f74f496957dfa7cc1d1ec00a26444ec4befa24fa` | `IImageStorageService`, UploadAsync IFormFile; contrato actual menciona Cloudinary, no prueba R2 |
| S02 | `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs` | `454d588851ecc7db810259e63705852360172928` | `DocumentoAlmacenado`, upload documento; necesita cotejo futuro con DTO R2 |
| S03 | `backend/src/Application/Interfaces/StorageTenantContext.cs` | `716e8e205502d6300f555947c79edc2852fd63ba` | `TenantPrefix => empresas/{EmpresaId}` y `Desde(ContextoTenantActual)` server-side; no implica política R2 |
| S04 | `backend/src/Infrastructure/Services/CloudinaryImageStorageService.cs` | `2413e0b9a05859f7978a6c1708b05fbc2648d5bb` | Clase Cloudinary y configuración CloudName/ApiKey/ApiSecret; no exponer valores secretos |
| S05 | `backend/src/Infrastructure/Services/CloudinaryCompraDocumentoStorageService.cs` | `3bcc063a6396c41ed03d26f6f6c69e38acb4351f` | Adaptador Cloudinary de documento; integración existente debe preservarse |
| S06 | `backend/src/Infrastructure/Services/ImagenUploadSecurity.cs` | `456a7875f5f9b9ac475ed7ce83af3215beb2b408` | Rechaza imágenes mayores a 10MB, MIME/extensión/bytes inconsistentes y daños, sin concluir validez R2 |
| S07 | `backend/tests/Solqaryn.Tests/Application/Storage/StorageTenantContextTests.cs` | `760f6e851568cddae0c7282654e40fb3a965ef5b` | Fuente de xUnit para tenant/prefijo, inspeccionada, no ejecutada en esta M01 |
| S08 | `backend/tests/Solqaryn.Tests/ImagenUploadSecurityTests.cs` | `edd4f994c1aa5cc8b91b49eaf91e36b3768b57d2` | Fuente de tests PNG/content-type inconsistente, no equivale a prueba presigned URL |
| S09 | `docs/v5/c10-02-m01-01-alcance-r2-dominios-archivos.md` | `3359ab47c9d6a472d39c64a627da057ac01ddd1f` | NR01–NR14: objetivo/no-alcance, riesgos de replay, no cutover ni infraestructura |
| S10 | `docs/v5/c10-02-m01-02-actores-interfaces-r2-dominios.md` | `185471a4de72653b6e3a199ee7a8aaa34ef3ea88` | A01–A14, I/O01–12 y fronteras F01–F08, contratos conceptuales NO_IMPLEMENTADOS |

La verificación consistió en leer **contenido y blob SHA** de estas diez rutas sobre `af43226f...` desde GitHub. No se inspeccionaron secretos, conexiones a proveedores reales ni datos productivos. S01–S08 son fuentes actuales y S09–S10 son documentos nuevos del lote; los criterios derivados no confieren permiso para migrar.

## Brechas G01–G12: hechos ausentes o por demostrar

| ID | Evidencia aún requerida antes de implementación/certificación |
| --- | --- |
| G01 | Identificar configuración real por ambiente de provider, sin consultar valores de secreto |
| G02 | Diseñar y validar adapter R2/SDK con permisos limitados por tenant y ambiente |
| G03 | Verificar política bucket privado y CDN/domain/trusted proxy, sin inferir seguridad por nombre de dominio |
| G04 | Confirmar traducción segura de TenantPrefix interno a key física, sin traversal ni colisiones A/B |
| G05 | Establecer contrato de URL firmada con objeto/método/TTL/reloj y firma correctamente validada |
| G06 | Modelar expresamente replay: bearer URL reutilizable durante TTL vs mecanismo one-time con estado |
| G07 | Diseñar revocación/caché segura tras cambiar empresa o rol sin exponer objetos antiguos |
| G08 | Definir MIME real, limites de streaming y checksums de objetos distintos de imágenes actuales |
| G09 | Precisar ciclo de vida, retención/hold legal y política de restauración con evidencias |
| G10 | Elaborar inventario real de objetos existentes, respaldo/hashes y rollback antes de cutover |
| G11 | Especificar trazabilidad y logs sin signed URLs, tokens, PII ni nombres privados |
| G12 | Generar integración con servicio efímero, casos positivos/negativos A/B y CI por SHA exacto |

Estas brechas son **preguntas de evidencia o trabajo futuro, no fallos funcionales comprobados**. No se ha ejecutado un test R2, una migración, una prueba de caducidad ni un ensayo de rollback real.

## Escenarios de verificación futura — NO_EJECUTADOS

**P01:** tenant A obtiene únicamente recurso A permitido; **P02:** un enlace válido tiene TTL/objeto/verbo limitados; **P03:** bytes permitidos se almacenan con MIME y checksum verificados; **P04:** conmutación A→B invalida caché de A; **P05:** lifecycle respeta retención; **P06:** migración autorizada comprueba hashes y reversión. **N01:** A intenta key B; **N02:** cliente falsifica EmpresaId; **N03:** rol de solo lectura intenta delete; **N04:** URL expirada; **N05:** URL bearer se reproduce dentro de TTL y se evalúa decisión de un solo uso; **N06:** path traversal; **N07:** MIME falso; **N08:** stream supera tamaño; **N09:** CDN cache entre tenants; **N10:** token aparece en log; **N11:** borrar objeto bajo retención; **N12:** activar R2, DNS o cutover sin autorización.

Ni P ni N son PASS. Son futuros oráculos para .NET/Angular/MySQL/servicio efímero. La fuente `ImagenUploadSecurity` sí contiene reglas, pero **su inspección no sustituye pruebas ejecutadas** ni permite extender implícitamente controles a todos los tipos de archivo.

## Impacto, controles y reversión

**Impacto real:** solo este documento y `CHANGELOG_AI.md`; cero cambios de Domain/Application/Infrastructure/API, Angular, MySQL, DNS, secrets, proveedores, migraciones, cuentas, tareas o entorno. **A0:** cotejar autoridad, alcance, hashes y DAG individual F VALIDADO/G SI. **A1:** inspección de diez blobs y G01–G12; sin falsificar certificación. **A2:** frontera JWT/RBAC/tenant, URL bearer/replay, MIME, logs, prefijos, retención y rollback. **A3:** consistencia con C10-01.M01.03 y C10-02.M01.01/.02; compilaciones runtime **NO_APLICA** a este diff Markdown. **A4:** fast-forward en `dev`, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` SUCCESS exact-SHA, bitácora y readback. **A5:** revisión independiente por otra Task Javier distinta de la autora antes de cualquier QA/main/PROD.

**Rollback:** `git revert` aditivo del commit Markdown, sin force-push ni manipular objetos reales (no tocados). C10-02.M01.06 y operaciones R2 continúan sujetos a sus propios gates.
