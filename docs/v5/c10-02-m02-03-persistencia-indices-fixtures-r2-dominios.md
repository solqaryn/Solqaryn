# SOLQARYN V5 — C10-02.M02.03: persistencia, índices y fixtures conceptuales de archivos privados

**Autor:** PRIMARY DEV 01 JAVIER, Task `6adbb9fff1548191a1bd5b30d9e9984a`. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente:** STANDARD V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet PLAN/COLA/DAG `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Fecha:** 2026-11-15.

## Objeto del diseño y gates

Esta unidad M02.03 **solo documenta** proyecciones de persistencia, posibles claves, índices y fixtures sintéticos para archivos privados, objetos/versiones, capacidades temporales, retención, integridad y eventual reconciliación de proveedores. **No crea ni modifica SQL, EF, tablas, bucket, R2, DNS, Cloudinary, API, endpoint, URL firmada ni archivo real.**

El antecedente `C10-02.M01.06`, SHA `3c115e7f3dc9ea48edac2239e5515f9470468d99`, obtuvo A5 independiente DEV01. El gate exacto de M02.03 constaba DAG F=VALIDADO/G=SI, COLA G/O=SI y PLAN PENDIENTE sin owner antes de su claim. M02.01 y M02.02 son preparaciones conceptuales publicadas por DEV01 con A5 de otras Tasks todavía pendiente; **no** se trataron como prerrequisitos liberados, ya que el DAG de M02.03 admite trabajo independiente.

Las fuentes `C10-02.M01.03` y contratos existentes de archivos/Cloudinary son puntos de compatibilidad, no prueba de disponibilidad R2 ni de autorizar DNS/CORS/lifecycle. Decisiones D01–D08 de proveedor, región, retención, costos, gestión de firmas y dominio permanecen abiertas.

## Modelo de datos futuro — NO MATERIALIZADO

| Proyección propuesta | Clave y relación | Límite de seguridad |
| --- | --- | --- |
| RecursoPrivado | EmpresaId efectiva, recurso opaco, clasificación | JWT/RBAC y membresía servidor |
| VersiónRecurso | Recurso, versión, digest, tamaño | Append-only; no sobreescribir historia |
| LocalizaciónObjeto | Recurso, proveedor aprobado, key opaca | Key/prefijo no concede acceso |
| NamespaceDeEmpresa | Empresa, entorno, propósito, versión | No inferir tenant por ruta |
| IngresoPendiente | Recurso, operación, expiry, expectedVersion | Sin publicación antes de inspección |
| EvidenciaChecksum | Recurso, versión, digest confiable | Validar bytes y flujo completo |
| CapacidadEmitida | Recurso, actor, método, expiración, nonce | Sin almacenar firma/secretos |
| PublicaciónRecurso | Canal/host verificado, versión, estado | Cache privada por empresa |
| RetenciónRecurso | Recurso, clasificación, hold, regla | Denegar borrado sin autoridad |
| RegistroAccesoSaneado | Empresa, actor, recurso, outcome | Sin tokens ni URL firmada |
| ManifiestoTraslado | Origen, destino propuesto, versiones | No activar migración |
| ReconciliaciónDeTraslado | Manifiesto, conteos, diferencias | No certificar cutover sin pruebas |

Cardinalidades **hipotéticas**: Empresa 1:N RecursoPrivado, RecursoPrivado 1:N VersiónRecurso, VersiónRecurso 0:N LocalizaciónObjeto/Checksum, RecursoPrivado 0:N CapacidadEmitida/PublicaciónRecurso/RegistroAcceso, Manifiesto 1:N Reconciliación. No duplicar modelos actuales sin consultar EF y el proveedor actual.

## Índices y constraints candidatos — NO CREADOS

1. `(EmpresaId,Clasificacion,Estado,Id)` paginado, nunca cross-tenant.
2. `(EmpresaId,RecursoId,Version)` unique por versión sin borrado histórico.
3. `(Proveedor,NamespaceAutorizado,KeyCanonica)` con validación de ámbito, no autoridad por key.
4. `(RecursoId,Version,Digest)` permite validar integridad, sujeto a longitud de hash.
5. `(EmpresaId,Operacion,IdempotencyKey,PayloadDigest)` conflicto si key igual y contenido distinto.
6. `(RecursoId,ActorId,Verbo,ExpiresAtUtc,Nonce)` TTL/replay acotado.
7. `(EmpresaId,CanalVerificado,RecursoId,Estado)` evita publicación/caché cruzada.
8. `(RecursoId,RetentionPolicyId,HoldUntilUtc)` comprueba retención antes de eliminar.
9. `(EmpresaId,RecursoId,OccurredAtUtc,Outcome)` trazas saneadas.
10. `(ManifiestoId,RecursoId,Version)` checkpoints sin duplicar traslados.
11. `(EmpresaId,TotalBytes,CuotaVersion)` proyección de control de cuota y concurrencia.
12. `(RecursoId,ExpectedVersion,EstadoIngreso)` CAS para parciales/timeout.

Son **alternativas**: antes de EF10/MySQL8.4 evaluar claves reales, duplicados, soft delete, colación, nulos legados, límites de índices, transacciones y costo de escritura. No suponer que un índice garantiza firma, ACL, permisos de dominio o exactly-once distribuido.

## Invariantes DB01–DB12

**DB01** empresa efectiva proviene del backend autenticado, no prefix/host/EmpresaId cliente. **DB02** ID de recurso y versión son opacos y no autorizan acceso. **DB03** registros B nunca se listan o firman bajo A. **DB04** key canonizada rechaza traversal y colisiones. **DB05** ingreso parcial no se publica ni se certifica por longitud declarada. **DB06** digest se calcula con bytes reales y se coteja con manifiesto/versionado. **DB07** versión anterior se preserva en rollback y auditoría. **DB08** firma/TTL/nonce se restringen por recurso, método y policy sin secretos en BD de aplicación. **DB09** retención/hold impide purga incluso en fase de traslado. **DB10** mismo idempotencyKey con digest diferente falla y nunca recrea objeto. **DB11** migración futura conserva origen hasta hash/ACL/versión y backup verificados. **DB12** logs y DTO no contienen firma, JWT, PII ni URL privada.

## Fixtures sintéticos FUTUROS_NO_EJECUTADOS

| Positivo proyectado | Negativo a rechazar |
| --- | --- |
| F01 Recurso A pertenece sólo a A | N01 B obtiene recurso A |
| F02 Versión 2 conserva versión 1 | N02 Overwrite borra historia |
| F03 Prefijo válido mantiene ámbito | N03 Traversal lee namespace B |
| F04 Digest verificado confirma objeto | N04 MIME falso/truncado se publica |
| F05 Reintento igual preserva idempotencia | N05 Key igual/digest distinto duplica |
| F06 Firma temporal válida respeta verbo | N06 Firma expirada o alterada permite lectura |
| F07 Política hold conserva recurso | N07 Lifecycle borra protegido |
| F08 Cache separa host y empresa | N08 Host B revela objeto A |
| F09 Upload parcial se recupera | N09 Timeout publica parcial |
| F10 Manifiesto lista versiones verificadas | N10 Un objeto ausente declara éxito |
| F11 Migración reversible conserva origen | N11 Cutover elimina objeto fuente |
| F12 Trazas saneadas preservan correlación | N12 Logs guardan URL firmada/JWT |

No se implementaron fixtures, pruebas SQL, restauración, HTTP, xUnit, Playwright, proveedores, costes ni bases de datos. Ningún caso de esta matriz equivale a PASS runtime.

## Evidencia A0–A5 y reversión

**A0:** Skill canónica, HEAD/Scope Lock, fuente única, DAG/COLA, CAS/owner y diff de este documento + sección íntegra `CHANGELOG_AI.md`. **A1:** validar documentalmente 12 proyecciones, 12 índices candidatos, 12 invariantes y 12 pares de fixtures futuros. **A2:** separación tenant A/B, JWT/RBAC, canonización de key, integridad, idempotencia, TTL/replay, retención y saneamiento. **A3:** cotejo conceptual con Cloudinary y contratos API/EF existentes; .NET/MySQL/HTTP/R2 `NO_APLICA` al diff Markdown, no PASS. **A4:** fast-forward/CAS sobre HEAD vivo, comparar blobs y CI `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success SHA exacto y readbacks PLAN/COLA/DAG/BITACORA. Checks requeridos en progreso/cancelados/skipped/fallidos no son PASS.

**A5:** otra Task Javier distinta del autor debe revisar diff/SHA/evidencia de seguridad y registrar PASS/FAIL real; LISTO_DEV documental no autoriza R2, DNS, secretos, migraciones, M03, QA/main/PROD.

**Rollback documental:** corrección aditiva o `git revert` normal preservando BITACORA y commits ajenos. **Rollback operativo futuro NO REALIZADO:** inventario de versiones/hashes y ACL antes/después, restore ensayado en entorno efímero y conservación del proveedor origen hasta conciliación; sin cambios sobre datos reales por este documento.
