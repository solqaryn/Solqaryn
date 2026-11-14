# SOLQARYN V5 — C10-01.M01.03: inventario de código y evidencia para storage provider-neutral

**Autor:** PRIMARY DEV 05 JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. **Microtarea:** C10-01.M01.03 / PLAN_MAESTRO fila 1444. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **Repositorio:** solqaryn/Solqaryn, rama dev, Scope Lock STRICT. **Fecha:** 2026-11-14.

## Alcance y verdad observable

Esta unidad M01.03 únicamente **lee e inventaría contratos, código y fuentes de pruebas** existentes relacionados con el almacenamiento de imágenes y documentos, distinguiéndolos de la propuesta futura de interfaz provider-neutral para imágenes, documentos, videos y exportaciones. No se ejecutan uploads, downloads, consultas de credenciales, cambios de bucket, R2, migraciones o tests live. Cloudinary es un adaptador vigente; R2/Hostinger son objetivos sujetos a aprobación posterior, **no implementados** por documentar esta unidad. NR01–NR12 de M01.01 y los actores/I01–I12 de M01.02 siguen siendo contratos futuros.

## Diez fuentes y sus blobs exactos inspeccionados

| Ruta observada en dev | Git blob SHA | Hecho comprobado y límite |
| --- | --- | --- |
| docs/v5/c10-01-m01-01-objetivo-no-alcance-storage-neutral.md | 622884a0a307e01c41ff1bec6797a1f36e67e21e | Contrato futuro de storage neutral, NR01–NR12; no infraestructura creada |
| docs/v5/c10-01-m01-02-actores-superficies-storage-neutral.md | 6d18023f7bb42685cce150b6bbe29f1fa20c30c8 | Doce actores e interfaces futuros, no evidencia de migración |
| backend/src/Application/Interfaces/IImageStorageService.cs | f74f496957dfa7cc1d1ec00a26444ec4befa24fa | Interfaz actual de imagen Cloudinary; overload tenant-aware y legacy documentados |
| backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs | 454d588851ecc7db810259e63705852360172928 | Contrato de documento Cloudinary, soporte tenant-aware fail-closed |
| backend/src/Application/Interfaces/StorageTenantContext.cs | 716e8e205502d6300f555947c79edc2852fd63ba | TenantPrefix y contexto procedente de ContextoTenantActual verificado |
| backend/src/Infrastructure/Services/CloudinaryImageStorageService.cs | 2413e0b9a05859f7978a6c1708b05fbc2648d5bb | Adaptador Cloudinary de imágenes actual, sin inferir R2 |
| backend/src/Infrastructure/Services/CloudinaryCompraDocumentoStorageService.cs | 3bcc063a6396c41ed03d26f6f6c69e38acb4351f | Adaptador Cloudinary de documentos; controles de download |
| backend/src/Infrastructure/Services/ImagenUploadSecurity.cs | 456a7875f5f9b9ac475ed7ce83af3215beb2b408 | ImagenUploadSecurity inspecciona y recodifica imágenes, límites 10 MB |
| backend/tests/Solqaryn.Tests/Application/Storage/StorageTenantContextTests.cs | 760f6e851568cddae0c7282654e40fb3a965ef5b | xUnit fuente con positivos/negativos de tenant/path, inspeccionado no ejecutado |
| backend/tests/Solqaryn.Tests/ImagenUploadSecurityTests.cs | edd4f994c1aa5cc8b91b49eaf91e36b3768b57d2 | xUnit fuente de MIME verdadero y binario renombrado, no ejecutado |

Las clases C# y pruebas xUnit anteriores existen en código, pero la inspección no supone que sus métodos se hayan ejecutado en esta corrida. Las interfaces actuales incluyen contratos legacy y overload tenant-aware con rechazo por defecto; la presencia de una interfaz Cloudinary no acredita abstracción universal, checksum persistido ni operación R2. No se consultaron secretos, recursos externos, documentos de usuarios ni el estado real de buckets.

## Seis contrastes entre código actual y objetivo del plan

| ID | Brecha de evidencia, no defecto confirmado | Verificación necesaria en fases posteriores |
| --- | --- | --- |
| G01 | Neutralidad transversal: IImageStorageService y ICompraDocumentoStorageService tienen contratos Cloudinary; no equivale a interfaz homogénea para video/export | M02 diseñar interfaz por caso de uso y compatibilidad |
| G02 | Huella integral: No se acreditó checksum de bytes reales persistido/validado universalmente | Exigir SHA-256 real y verificación lectura/escritura en CI |
| G03 | Contexto de lectura pública/privada: StorageTenantContext ofrece scope, pero no certifica todos los consumidores | Auditar rutas públicas/privadas, RBAC y A/B end-to-end |
| G04 | Transferencia reversible: No se inspeccionó inventario real, credenciales ni procedimiento restore objetivo | No iniciar migración sin manifiesto, backup, checksum y rollback |
| G05 | Documentos y medios diversos: Validación de imágenes no prueba parser/limites de PDF/video/exports | Diseñar política MIME, tamaño, streaming por tipo |
| G06 | Enlaces y auditoría: No se acreditó URL temporal universal, revocación y log minificado por objeto | Pruebas de TTL, replay, permisos separados, no PII/token |

Este análisis no declara vulnerabilidad sin explotación ni pruebas. La revisión real de un método depende de configuración, wiring, actor y ambiente, así como del nivel de autorización al objeto. No se debe inferir identidad tenant de una URL, carpeta o parámetro del cliente.

## Matriz de aceptación FUTURA, NO EJECUTADA en esta entrega

| Positivo previsto | Condición futura comprobable |
| --- | --- |
| P01 | Imagen permitida JPEG/PNG/WebP se normaliza con bytes y tamaño dentro de cuota |
| P02 | Tenant A autorizado almacena/consulta únicamente recurso A por scope derivado del servidor |
| P03 | Documento privado del tenant se descarga con permiso independiente y MIME verificado |
| P04 | Checksum de bytes reales detecta integridad intacta y queda registrado sin exponer datos |
| P05 | Migración futura autorizada valida inventario/conteos/hash y demuestra rollback sin pérdida |

| Negativo previsto | Rechazo futuro comprobable |
| --- | --- |
| N01 | Usuario A intenta leer objeto de B y servidor deniega |
| N02 | Cliente manipula EmpresaId, header o carpeta para saltar ownership |
| N03 | Extensión/Content-Type permitido pero magic bytes inválidos |
| N04 | Archivo demasiado grande o imagen-bomba excede límites |
| N05 | Checksum declarado no coincide con bytes reales y se rechaza |
| N06 | URL firmada caducada o locator ajeno no permite lectura |
| N07 | Usuario con lectura intenta borrar o escribir sin permiso |
| N08 | Fallo de subida deja objeto truncado visible y se impide publicación |
| N09 | Logs intentan incluir token, secret, URL firmada o contenido sensible y deben redactarse |
| N10 | Proveedor destino no disponible: no usar fallback no aprobado ni datos cross-env |
| N11 | Intento de migración sin inventario/backup/rollback validado permanece NO_GO |
| N12 | Cadena de path con ../ o backslash fuera de carpeta tenant se rechaza |

Estos escenarios requieren en M02–M06 tests dirigidos .NET/MySQL/Docker/HTTP/Playwright cuando causalmente correspondan. Su enumeración no prueba éxito, integridad de backup, permisos cloud, Cloudflare R2, ejecución MySQL ni despliegue. Los archivos xUnit inspeccionados no se ejecutaron aquí. Las condiciones N05/N11 exigen medición y un rollback comprobado sobre objetos reales **solo con autorización futura separada**.

## A0–A5, dependencias, seguridad y reversibilidad

Preflight individual para este inventario: PLAN_MAESTRO!H1444=PENDIENTE/I1444 vacío; COLA!G1444/O1444=SI; DAG_TECNICO!A1405=C10-01.M01.03/F1405=VALIDADO/G1405=SI. La fase C01-03 y los bloques C02/C03 son antecedentes documentales, NO aprobación de proveedores, migraciones, secretos ni PROD.

**Impacto:** docs/v5 + CHANGELOG_AI.md solamente, cero cambios a Domain/Application/Infrastructure/API, MySQL, CLI, worker, DNS, endpoints, Cloudinary, credenciales, cuentas y datos. **A0:** inspección de 10 blobs SHA, diff Markdown y búsqueda básica de secretos. **A1:** verificar seis brechas, cinco positivos/doce negativos como criterios FUTUROS. **A2:** preservar tenant A/B, JWT/RBAC, bytes MIME reales, tamaño, checksum y acceso por operación sin delegación al cliente. **A3:** compatibilidad con M01.01/M01.02 y Skill/arquitectura de HEAD. **A4:** commit fast-forward a dev y check exact SHA `verify-solqaryn-project-scope` más V5 procedencia CHANGELOG `completed/success`, readback PLAN/COLA/BITACORA. .NET, Angular, MySQL/Docker y E2E runtime **NO_APLICA a diff exclusivamente Markdown**, sin contabilizar PASS ficticios. **A5:** revisión independiente por uno de los otros cinco TASK_ID Javier; no autorrevisión.

**Rollback:** git revert normal del commit documental, sin force-push ni cambiar historia. No QA/main/PROD, no migrar objetos, no R2 real, no eventos, no proveedores pagos, no secretos, no Tasks nuevas.
