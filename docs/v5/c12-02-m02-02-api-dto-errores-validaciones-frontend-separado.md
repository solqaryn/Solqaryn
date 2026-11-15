# SOLQARYN V16 — C12-02.M02.02 contratos API, DTO, errores y compatibilidad del frontend separado

**Microtarea:** C12-02.M02.02 · PLAN fila 1773 · **riesgo V15:** R0 documental sin runtime · **autor:** PRIMARY DEV 04 `6aeacb7820ac8191b7d768a6f33dfa09` · `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## 1. Antecedentes, alcance y límites

Fuente exclusiva STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet institucional PLAN_MAESTRO/COLA/DAG. M01.01–06 ya LISTO+A5 independiente. M02.01 documenta E01–E12 e INV01–INV16, blob `2cc6094228286a3da32569ef50476efa7429c3ec`: no representa API, tabla, clase C# ni interfaz TypeScript implementada. Angular 22.2.1, API .NET 10 y seguridad JWT/RBAC/tenancy de backend siguen sin cambios.

Las decisiones M01 D01–D10 de proxy, caché, DNS/TLS, headers, host por ambiente, SEO, manifests, observabilidad, presupuestos de UX y promoción continúan **PENDIENTES**. Este contrato propone posibles operaciones de lectura/validación futura, sin exponer rutas reales ni implementar endpoints, reescribir `app.routes.ts` o `frontend/vercel.json`, desplegar servicios o cambiar proveedores. En producción, backend decide tenant y roles; `tenantId`, host, slug y headers enviados por navegador nunca otorgan privilegios.

## 2. Ocho operaciones propuestas OP01–OP08 (sin endpoint real)

| ID | Operación futura conceptual |
| --- | --- |
| OP01 | Consultar manifiesto público de release con hashes y versión |
| OP02 | Consultar reglas públicas de robots/sitemap sin rutas internas |
| OP03 | Evaluar propuesta de proxy declarativo sin escribir configuración |
| OP04 | Consultar compatibilidad de build y versión cliente sin atribuir PASS |
| OP05 | Consultar política de caché y headers por ambiente autorizado |
| OP06 | Validar propuesta de dominio/host bajo allowlist sin tocar DNS |
| OP07 | Consultar puntero de rollback a release firmado y verificado |
| OP08 | Consultar evidencia resumida de CI por SHA, sin logs sensibles |

Todas necesitarán auth, recursos allowlisted, validación proporcional y autorización de implementación; las consultas públicas deberán excluir secretos/PII, rutas ERP y sesión. OP03, OP06 y OP07 no autorizan por sí mismas modificar edge, DNS o publicar rollback.

## 3. Ocho DTO propuestos D01–D08 (no tipos compilados)

| ID | DTO conceptual | Campos permitidos propuestos |
| --- | --- | --- |
| D01 | ReleaseManifestView | sha,version,assetHashes,fecha |
| D02 | RoutePolicyView | publicPaths,privatePathCount,policyRevision |
| D03 | ProxyRuleProposal | aliasTarget,methods,headerPolicy,revision |
| D04 | BuildCompatibilityView | sourceSha,buildSha,minimumClientVersion |
| D05 | CachePolicyView | publicTtl,privateNoStore,varyKeys |
| D06 | DomainBindingProposal | hostCanonical,environmentId,proof |
| D07 | RollbackPointerView | previousSha,manifestHash,approvedBy |
| D08 | TestEvidenceView | scenarioId,runId,testedSha,outcome |

Los DTO deberán versionarse y preservar compatibilidad hacia atrás. Se rechaza cualquier campo de token, contraseña, DSN, cookie, SQL, HTML/JS arbitrario o tenant arbitrario. La autorización se comprueba en backend, nunca mediante un DTO enviado por el cliente.

## 4. Doce contratos de errores ERR01–ERR12 (no handlers existentes)

| ID | Código/HTTP orientativo |
| --- | --- |
| ERR01 | IDENTITY_REQUIRED/401 |
| ERR02 | ROLE_FORBIDDEN/403 |
| ERR03 | TENANT_MISMATCH/403 |
| ERR04 | HOST_NOT_ALLOWED/400 |
| ERR05 | INVALID_REVISION/409 |
| ERR06 | PROXY_TARGET_REJECTED/400 |
| ERR07 | PRIVATE_CACHE_FORBIDDEN/422 |
| ERR08 | MANIFEST_MISMATCH/409 |
| ERR09 | MISSING_SHA_EVIDENCE/422 |
| ERR10 | CI_NOT_SUCCESS/409 |
| ERR11 | ROLLBACK_UNAUTHORIZED/403 |
| ERR12 | PROVIDER_NOT_AVAILABLE/503 |

Los códigos son propuestas a reconciliar con contratos backend reales, no garantía de handlers existentes. Para tenant ajeno, no revelar existencia de usuarios o datos. Describir disponibilidad 503 no debe enmascarar fallo 401/403.

## 5. Catorce validaciones V01–V14

| ID | Regla propuesta para futuro código y pruebas |
| --- | --- |
| V01 | Identidad real validada server-side por JWT |
| V02 | Tenant A/B nunca se decide por header, query o slug cliente |
| V03 | Roles y membresía revocados se rechazan en backend |
| V04 | Host y origen siguen allowlist; SSRF rechazada |
| V05 | Destino proxy debe ser alias tipado, sin URL arbitraria |
| V06 | CORS no acepta wildcard más credenciales |
| V07 | CSP restringe scripts, no se abre para acelerar build |
| V08 | Respuestas privadas no entran en caché compartida |
| V09 | Manifiesto de assets y release comparten SHA |
| V10 | TTL público acotado y privado no-store por defecto |
| V11 | Versión cliente anterior conserva compatibilidad o error definido |
| V12 | No se publica en QA/main/PROD sin autoridad nueva |
| V13 | PASS de CI requiere SHA causal y check completed/success |
| V14 | Observabilidad y errores no filtran PII, cookies o secretos |

Los checks CI required que estén queued, in_progress, failed, skipped o cancelled no acreditan PASS; un descendiente requiere hashes de blobs de scope idénticos, dependencias y job exitoso auditados.

## 6. Diez pares P/N T01–T10 — FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo/fallo futuro |
| --- | --- | --- |
| T01 | Manifest concuerda con SHA | Hash incorrecto se rechaza |
| T02 | Solo rutas públicas aparecen en sitemap | ERP privado indexable falla |
| T03 | Proxy autorizado usa alias seguro | Target externo arbitrario falla |
| T04 | Tenant A accede solo a sus recursos | Tenant B/rol revocado obtiene 403 |
| T05 | Caché pública no contiene sesión | Token/datos privados entre sesiones falla |
| T06 | CORS/CSP protegidos | Credenciales con wildcard/script arbitrario falla |
| T07 | Deep-link SPA conservando 401/403 | Fallback entrega contenido privado falla |
| T08 | Rollback restituyó manifest y reglas | SHA anterior no verificado se rechaza |
| T09 | Check requerido exact-SHA success | Cancelled/skipped/queued no equivale PASS |
| T10 | Errores tipados sin PII ni secretos | DSN/token/tenant ajeno en respuesta falla |

**Cobertura efectiva hoy: 0/10 ejecutados**. No hay HTTP real, API adicional, navegador ni tests de MySQL creados por esta unidad. Un build que termine success no mide LCP/INP/CLS ni prueba estado del proveedor.

## 7. Calidad A0–A5, rollback y puertas

- **A0:** diff exclusivo de este documento y sección autocontenida de `CHANGELOG_AI.md`; operaciones, DTO, validaciones y compatibilidad referidos a M01/M02.01.
- **A1:** 8 operaciones, 8 DTO, 12 errores, 14 reglas y 10 pares P/N documentados; 0/10 tests runtime ejecutados.
- **A2:** aislamiento tenant A/B, JWT/RBAC, revocación, CSP/CORS, SSRF, caché de sesiones y PII fail-closed definidos como restricciones, no controles implementados aquí.
- **A3:** integración de conceptos con Angular/build/routing/proxy descrita; .NET, MySQL, browser, Playwright y proveedores **NO_APLICA** al diff Markdown. No emitir API ni tareas programadas.
- **A4:** dev fast-forward/CAS, Scope Lock y procedencia V5 completed/success exact-SHA, PLAN/COLA/DAG/BITACORA readback. No atribuir FAIL/SKIPPED/IN_PROGRESS a PASS.
- **A5:** requiere revisor real de otra Task Javier distinta del autor, evidencia de SHA/blob/tests proporcional; el autor jamás se aprueba.
- **Rollback documental:** `git revert` normal del commit bajo HEAD y exclusividad verificados, preservando BITACORA. No representa rollback de edge o migración. Ninguna ruta a QA/main/PROD, DNS, proveedor o dato real está autorizada.

**Salida:** LISTO_DEV acredita un contrato documental R0, no un sistema de CDN/API ejecutable, migración, autorización productiva o certificación de accesibilidad/seguridad HTTP.
