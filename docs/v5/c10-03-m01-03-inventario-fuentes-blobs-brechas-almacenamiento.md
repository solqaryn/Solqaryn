# SOLQARYN V5 — C10-03.M01.03: inventario estático verificable de fuentes y brechas de almacenamiento

**Unidad:** C10-03.M01.03 · PLAN_MAESTRO fila 1516 · **Responsable:** PRIMARY DEV 04 / JAVIER_CHATGPT / TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09 · **Fecha:** 2026-11-14 · **Rama:** solqaryn/Solqaryn@dev · PROJECT_SCOPE_LOCK=STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Microtarea M01.03: «Leer contratos, código y evidencia disponibles sin asumir certificación». Antecedentes: C10-03.M01.01 (blob ca7ccfaf423ead017a127acde393d53cf6ab181b) y C10-03.M01.02 (blob dde15b96f610dcd0b7712129e69bcba04a8586bd).

## Dictamen de alcance y método

**Este es un inventario ESTÁTICO DE CÓDIGO, NO un inventario de objetos históricos.** Se inspeccionaron rutas y blobs Git del HEAD dev mediante lecturas de repositorio, no conexiones con Cloudinary, Aiven, R2, DNS ni datos reales. Se preserva INVENTARIO_HISTORICO=NO_VERIFICADO. El hecho de encontrar integraciones, tests o scripts en Git no acredita cantidad de medios; **NO_VERIFICADO no es cero** y tampoco demuestra que existan objetos. Sólo un inventario posterior autenticado, acotado por ambiente y reconciliado entre metadatos MySQL y objetos del proveedor podrá producir CERO_VERIFICADO o POSITIVO_VERIFICADO.

Si todas las fuentes autorizadas acreditan CERO_VERIFICADO, no hay exportación, copia ni checksum de históricos que ejecutar. Aun así, una implementación futura para nuevas cargas deberá probar fixtures efímeros, cuotas, MIME real, pertenencia tenant A/B, compensación y rollback. Si hay objetos históricos, se requerirá manifiesto y plan de migración adicional, nunca migración inferida desde este documento.

## Matriz reproducible de doce fuentes y blobs Git

| Código | Ruta en dev | SHA de blob verificado | Hallazgo limitado |
| --- | --- | --- | --- |
| F01 | backend/src/Application/Interfaces/IImageStorageService.cs | f74f496957dfa7cc1d1ec00a26444ec4befa24fa | Contrato legacy y overload tenant-aware; la implementación predeterminada sin soporte explícito falla cerrado |
| F02 | backend/src/Application/Interfaces/StorageTenantContext.cs | 716e8e205502d6300f555947c79edc2852fd63ba | Contexto de EmpresaId/UsuarioId derivado de identidad verificada, no de valores libres del cliente |
| F03 | backend/src/Infrastructure/Services/CloudinaryImageStorageService.cs | 2413e0b9a05859f7978a6c1708b05fbc2648d5bb | Adaptador Cloudinary con upload, descarga y borrado tenant-aware; existencia del código no demuestra objetos reales |
| F04 | backend/src/Infrastructure/Services/CloudinaryCompraDocumentoStorageService.cs | 3bcc063a6396c41ed03d26f6f6c69e38acb4351f | Persistencia de documentos de compras vía adaptador actual, separada de R2 futuro |
| F05 | backend/src/Infrastructure/Services/CloudinaryFolderResolver.cs | 5516d4186e61476084927cbf023db10d780d6197 | Prefijo por ambiente y comprobación de carpeta para borrado; no sustituye ownership ni permiso de aplicación |
| F06 | backend/src/Infrastructure/Services/ImagenUploadSecurity.cs | 456a7875f5f9b9ac475ed7ce83af3215beb2b408 | Reglas de firma binaria/MIME/extensión, tamaño 10 MiB, dimensiones 4096 y máximo 16 MP para imágenes |
| F07 | backend/tests/Solqaryn.Tests/Application/Storage/CloudinaryTenantOwnershipTests.cs | 6008a64b07b70c3bb6c2597adabd438f2a52d796 | Casos de rechazo de locator de tenant ajeno y URL no permitida, identificados en fuente; NO ejecutados en esta unidad |
| F08 | backend/tests/Solqaryn.Tests/ImagenUploadSecurityTests.cs | edd4f994c1aa5cc8b91b49eaf91e36b3768b57d2 | Casos de firma binaria inválida, MIME inconsistente, truncamiento, dimensiones y tamaño; NO ejecutados aquí |
| F09 | backend/tests/Solqaryn.Tests/API/CommercialFilesPilotGuardTests.cs | 4391974d78b525b4a6333a1c290adb3c55de9244 | Contratos de rutas/métodos/permisos piloto y pertenencia A/B, sin certificar tráfico de producción |
| F10 | scripts/audit_cloudinary_dev_locations.py | 5b24bcb8bdc322bdd02d66953af32ef07b2af8c5 | Script declarado read-only para localizadores Cloudinary de DEV; sólo se leyó el archivo, NO se ejecutó |
| F11 | docs/v5/c10-03-m01-01-alcance-inventario-real-y-almacenamiento-futuro.md | ca7ccfaf423ead017a127acde393d53cf6ab181b | Objetivo y regla estricta de distinguir inventario desconocido, cero y positivo |
| F12 | docs/v5/c10-03-m01-02-actores-entradas-interfaces-inventario-almacenamiento.md | dde15b96f610dcd0b7712129e69bcba04a8586bd | Actores, entradas/salidas y fronteras F01–F09 como contrato futuro, no interfaces desplegadas |

**Procedencia:** cada SHA anterior identifica un blob de código o documento observado en el árbol Git de dev durante esta revisión. Los archivos de pruebas son evidencia de cobertura disponible, no resultados nuevos; las lecturas estáticas no certifican la ejecución, el número de archivos, el estado del proveedor ni una ruta de migración. Cambios posteriores requieren revalidar blobs y HEAD antes de reutilizar esta matriz.

## Brechas y condiciones de cierre futuro

| Brecha | Falta demostrar | Prueba o acción futura, bajo autorización específica |
| --- | --- | --- |
| G01 | Conteo real de objetos históricos | Inventario autenticado por ambiente y fecha; cero sólo con evidencia completa |
| G02 | Conciliación metadatos MySQL ↔ objetos proveedor | Comparación de referencias, hashes, huérfanos y duplicados sin publicar URLs/PII |
| G03 | Estado de todas las superficies de media | Cubrir productos, compras, perfiles y otros consumidores identificados, no sólo una tabla |
| G04 | Ownership y retención por objeto | Manifiesto con tenant, tipo, ubicación opaca, clasificación y hold |
| G05 | Integridad de históricos si el conteo es positivo | Checksum y manifiesto reproducible con idempotencia antes/después |
| G06 | Nuevas cargas y límites | Fixture nuevo efímero validando bytes, MIME real, cuota y reversión |
| G07 | URL temporal, TTL/replay y métodos | Pruebas de expiración, host, verbo, alcance y filtración negativa |
| G08 | Borrado y compensación por fallo parcial | Ensayo con proveedor simulado/efímero, sin datos productivos |
| G09 | Política de errores y auditoría saneada | Correlación sin secretos, key cruda, firma, PII ni acceso transversal |
| G10 | Gate migración Cloudflare R2/Hostinger | Autorización expresa, rollback probado y controles infra; no desplegado aquí |

La matriz describe brechas de **evidencia**, no declara vulnerabilidades explotables ni incumplimientos comprobados del sistema vigente. Ninguna de ellas habilita permisos, ampliación de infraestructura o cambios de datos.

## Oráculos de pruebas futuras — NO_EJECUTADOS

| Escenario | Positivo esperado | Negativo que debe rechazarse |
| --- | --- | --- |
| P01/N01 | Inventario completo con cero acreditado | Interpretar un fallo o una sola fuente vacía como cero |
| P02/N02 | Conteo positivo conciliado, con manifiesto | Copiar datos de proveedor sin conciliar ownership |
| P03/N03 | Usuario autorizado A consulta medios A | Token A conoce locator o EmpresaId B y obtiene sus medios |
| P04/N04 | Imagen de fixture válida dentro de cuota | MIME mentido, contenido truncado o bytes fuera de límite |
| P05/N05 | Descarga temporal sólo del objeto y verbo permitidos | URL caducada, replay o método no permitido |
| P06/N06 | Retry de nueva carga no duplica efectos | Timeout deja objeto parcial o doble escritura |
| P07/N07 | Hold protege histórico hasta autorización | Borrado de objeto con hold o dueño ajeno |
| P08/N08 | Rollback restaura metadatos/objetos de fixture | Activar R2, DNS o cutover sólo por haber completado M01 |

Todos estos P/N son **criterios futuros** y no equivalen a pruebas xUnit, integración MySQL, Playwright ni ejecución del inventario. Tampoco existe evidencia de llamada a proveedor, carga, eliminación, exportación, checksum histórico o recuento real en esta M01.03.

## Impacto por capas, controles y reversión

- **Dominio/Application:** contratos de storage y pertenencia inspeccionados; ningún cambio a modelos, DTO ni casos de uso.
- **Infrastructure/API:** adaptadores existentes y sus pruebas inspeccionados; sin endpoint nuevo, configuración de Cloudinary/R2, token, secreto ni cliente adicional.
- **Frontend:** sin cambios Angular, UI, páginas ni rutas. El browser no decide tenant, ownership ni permiso de archivo.
- **Datos y seguridad:** sin SQL, migración, transacción, lectura de objetos o cambio JWT/RBAC. Toda prueba futura exige aislamiento A/B, denegación por defecto y retención comprobada.
- **Observabilidad:** BITACORA conserva eventos idempotentes de gobierno; los localizadores/secretos reales no se registran.
- **A0:** ruta exclusiva del plan, fuente única, blobs inspeccionados, DAG F=VALIDADO/G=SI, COLA G/O=SI y claim CAS único.
- **A1–A3:** revisión **documental** de 12 fuentes, 10 brechas, ocho pares de oráculos y fronteras de confianza; suites runtime NO_APLICA al diff Markdown, sin atribuir PASS funcional.
- **A4:** exigir publicación serializada fast-forward en dev y checks Scope Lock / procedencia V5 completed-success sobre SHA exacto, diff sólo Markdown y readback PLAN/COLA/BITACORA.
- **A5:** revisión independiente por una Task Javier distinta de la autora. LISTO_DEV de este contrato no permite QA/main/PROD.
- **Reversión:** git revert normal del commit documental y revalidación institucional, sin force-push ni operaciones sobre medios.
