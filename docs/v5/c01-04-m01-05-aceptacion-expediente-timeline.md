# SOLQARYN V5 FINAL — C01-04.M01.05: aceptación observable del expediente y timeline universal

**ID:** C01-04.M01.05; **tipo:** TIPO1, diseño documental de criterios de aceptación funcional y técnica, no prueba ni implementación. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`; `solqaryn/Solqaryn@dev` Scope Lock STRICT. **Fuente:** Standard de Implementación V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO. Fase futura debe unificar cronología, decisiones, observaciones, adjuntos y versiones, incluyendo contrato de timeline y consulta aislada por ámbito.

**Relación con preparatorias:** M01.01 objetivo/no regresión, M01.02 siete actores/diez entradas/salidas, M01.03 lectura de diez blobs reales, M01.04 12 riesgos/seis supuestos/ocho decisiones. Ninguna microtarea documental constituye cierre M01.06, certificación de M02 ni autorización QA/main/PROD.

## 1. Tabla de aceptación funcional futura

Cada criterio define **estímulo, resultado verificable y prueba requerida cuando exista implementación**. No es un test ejecutado en M01.05.

| ID | Situación / entrada | Salida obligatoria observable | Evidencia futura |
| --- | --- | --- | --- |
| AC-01 (+) | Miembro con rol legítimo de A abre expediente de recurso propio A | Sólo hechos, decisiones y observaciones de A con su propietario y estado, sin mezclar B | API autenticada + consulta MySQL sobre A |
| AC-02 (+) | Eventos de A con mismo timestamp y diferentes IDs | Orden total determinista, paginación sin duplicados/huecos por empate | Tests backend .NET + integración/fixture DB determinista |
| AC-03 (+) | Actor autorizado agrega observación en A | Se registra actor, UTC, recurso, motivo y versión, sin modificar hecho histórico previo | API + persistencia real con comparación pre/post |
| AC-04 (+) | Actor con permiso registra una decisión formal | Requiere estado, rol, justificación y trazabilidad separados de texto libre | Prueba autorizada y readback de estado/versiones |
| AC-05 (+) | Usuario obtiene adjunto privado del recurso propio | Bytes y metadatos se entregan por vía validada y temporal, nunca por publicId sin ownership | Endpoint + storage de pruebas privado y permisos |
| AC-06 (+) | Reintento con misma idempotency key y mismo recurso | Una sola operación material, respuesta coherente y correlación rastreable | Test integración transaccional + reintento |
| AC-07 (+) | Dos cambios legítimos concurrentes con versión esperada | Un cambio conserva la versión ganadora y otro obtiene conflicto seguro | Test de concurrencia de DB y API |
| AC-08 (+) | Frontend muestra timeline y cambia página, estado o filtro permitido | Experiencia accesible, navegación estable, sin saltos de contexto | Lint/build y Playwright Chromium dirigido |
| AC-09 (-) | JWT válido de empresa A intenta expediente, evento o adjunto de B | Rechazo seguro, cero datos/bytes B, ninguna mutación | Negativo A/B HTTP + DB + almacenamiento |
| AC-10 (-) | Token revocado, membresía inactiva o rol sin permiso | 401/403/404 según contrato y sin filtrar existencia de recurso | Tests de revocación/autorización backend |
| AC-11 (-) | ID de expediente sin owner, nulo o huérfano | Fail-closed, no fallback a ámbito por cabecera | API negativa + consulta de datos |
| AC-12 (-) | PublicId/URL de adjunto legacy o B enviado por A | Ni descarga ni metadatos cruzados, sin revelar URL firmada | HTTP storage y tenant-aware tests |
| AC-13 (-) | HTML/script, JSON malicioso, campo desconocido, tamaño excesivo | Validación estricta, no ejecución de script, sin PII ni stacktrace | Tests DTO/API + Angular HTML/UX |
| AC-14 (-) | Versión stale o repetición con payload distinto | Conflicto documentado o error estable; sin duplicar ni reescribir historia | .NET + MySQL transaction test |
| AC-15 (-) | Usuario cambia A→B/logout con petición A aún pendiente | Respuesta stale A se descarta, no aparece en UI B ni cache | Playwright navegación + red demorada |
| AC-16 (-) | Evento de proveedor/worker fallido o duplicado | Sin pérdida ni duplicación de fuente, rollback/reintento idempotente | Job de integración y simulación de fallo causal |
| AC-17 (control) | Un check necesario está SKIPPED, pending o sha distinto | No pasa auditoría exigible ni se declara LISTO funcional | GitHub check-runs exact-SHA y logs |
| AC-18 (control) | A5 procede del mismo TASK_ID autor o falta registro completo | A5_FAIL/NO_GO, sin promoción a QA/main/PROD | BITACORA 26 columnas + identidad autenticada |

**Separación de autoridad:** AC-01 a AC-18 son compromisos de diseño para la implementación posterior. La existencia actual de `AuditoriaController`, `AuditoriaRepository`, `CompraDocumento` o `ContextoTenantActual` no prueba que un expediente universal satisfaga estos criterios.

## 2. Gates técnicos por superficie afectada futura

| Superficie y riesgo | Debe validar | No debe confundirse con |
| --- | --- | --- |
| Dominio, DTO, API .NET | Compilación y tests reales de validación, contratos, ID/versión y RBAC | Documento de aceptación sin implementación |
| JWT, membresía, tenancy A/B | Negativos en API, revisión repo, filtro de EmpresaId, revocación y PII | Header client-side o controller admin solamente |
| EF Core/MySQL 8.4 | Consulta tenant filtrada, orden estable, paginación, rollback transaccional | Test unitario con mocks o MySQL job SKIPPED |
| Storage e integración externa | ownership, firma/expiry, tamaño/MIME, auditoría de errores y fallos parciales | URL PublicId de compra legacy |
| Frontend Angular/Node | Build/lint, accesibilidad y actualización de contexto seguro | UI bonita sin control backend |
| E2E Chromium | Navegación y descarte de respuesta stale A→B con backend o fixture causal apropiado | Captura sintética como certificación live |
| Scope Lock V5 | Skill/identidad/dev y check-run completed/success sobre SHA publicado | Estado combinado de commit que omite check runs |
| Auditoría institucional | PLAN H, COLA E fórmula, BITACORA 26 columnas con A5 de otra Task | Eventos de 21 columnas o PASS inventado |

## 3. Evidencia observada en dev para diseñar aceptación, no certificarla

- `backend/src/API/Controllers/AuditoriaController.cs` (blob `82057f2acc571263ed7ed530eae0f81359b2f81a`) protege lectura de auditoría con permiso y rol admin, pero no expone un expediente.
- `backend/src/Infrastructure/Repositories/AuditoriaRepository.cs` (blob `8fc9417b88da91ad1a5777ea77d9e31756230384`) muestra filtros y orden por Fecha, sin `EmpresaId` explícito en método visible: **riesgo a investigar**, no fuga confirmada.
- `backend/src/Domain/Entities/RegistroAuditoria.cs` (blob `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462`) permite registrar actor, empresa nullable, correlación y valores, pero no versión universal de expediente.
- `backend/src/Domain/Entities/CompraDocumento.cs` (blob `7472b6418b5da34c58b5f7572f3fd6a858abac86`) registra documento de compra y empresa nullable legacy: no autoriza acceso transversal.
- `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs` (blob `454d588851ecc7db810259e63705852360172928`) tiene contratos tenant-aware fail-closed que requieren implementación explícita.
- `backend/src/Domain/Security/ContextoTenantActual.cs` (blob `7377d2d752985cc9baf31d08280f38ad94a6a114`) exige membresía real y EmpresaId coincidente, no claims arbitrarios.
- `frontend/src/app/services/auditoria.service.ts` (blob `e7d51d65833bcfb7dbeac24a42751c9ed229f34e`) utiliza GET auditoría, no pantalla universal de expediente ni prueba de descarte stale.

## 4. Comprobaciones documentales de M01.05 y pendientes

**Pruebas documentales positivas:** P-D01 matriz completa 18 criterios identificables con input/output/evidencia; P-D02 cobertura de cronología, decisiones, observaciones, adjuntos, versiones, aislamiento tenant A/B; P-D03 requerimientos reales mapeados a herramientas CI cuando se implementen.

**Pruebas documentales negativas:** N-D01 rechazar declarar `AC-09` probado sólo por un controlador administrativo; N-D02 rechazar convertir publicId o URL en autorización; N-D03 rechazar `SKIPPED` de MySQL o Playwright como PASS si esos jobs son exigibles; N-D04 rechazar liberar M01.06/M02 por el solo cumplimiento documental M01.01–05; N-D05 no permitir A5 del mismo TASK_ID ni eventos incompletos; N-D06 no permitir cambio a QA/main/PROD sin autorización expresa.

**Clasificación de esta entrega:** TIPO1 documento, requiere_dotnet=NO, requiere_node_angular=NO, requiere_mysql=NO, requiere_docker=NO, requiere_e2e=NO porque no toca código; requiere_security=SI en análisis JWT/RBAC, tenancy, adjuntos y PII; requiere_scope_lock=SI por commit documental. **A0** inspección estática de diff/fuentes/secretos, **A1** matriz con positivos/negativos, **A2** seguridad documental y denegaciones, **A3** coherencia con V5, DAG, código real y no-alcance, **A4** FF dev SHA/diff, check Scope Lock exacto y readback PLAN/COLA/BITACORA; **A5** independiente por otra Task.

**Prerequisitos:** DAG C01-04.M01.05 F=VALIDADO/G=SI, 13 LISTO (C00-01.M02.01–06, C00-03.M01.01–05 y C01-01.M01.01–02), PLAN H=PENDIENTE y COLA G/O=SI libres al reclamar. Sólo preparación M01.01–05; revalidar C00 final antes de cerrar M01.06 o implementar M02.

**Rollback e impacto:** solo Markdown V5 + CHANGELOG_AI.md + BITACORA/PLAN; no API/DTO/DB/Angular, despliegues ni secretos. Retorno por `git revert` normal del commit específico, tras releer HEAD, nunca reset ni force-push ni borrar eventos. Imágenes históricas cero = migración NO_APLICA; archivos futuros privados requieren gates.
