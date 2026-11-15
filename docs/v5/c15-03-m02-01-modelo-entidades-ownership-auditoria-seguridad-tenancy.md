# SOLQARYN V5 FINAL — C15-03.M02.01: modelo conceptual de seguridad, ownership y aislamiento

**Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT (`6aeacb7820ac8191b7d768a6f33dfa09`). **Rama exclusiva:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Contrato **DOCUMENTAL** de M02.01; no nuevas entidades EF, migraciones, roles, endpoints, servicios ni permiso para desplegar.

## 1. Autoridad y alcance

Fuentes autorizadas: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [contrato previo M01.06](c15-03-m01-06-bloqueos-rollback-liberacion-auditoria-seguridad-tenancy.md) y AGENTS/Scope Lock/Skill canónica del HEAD vivo. M01.06 registra doce bloqueos y pruebas futuras, sin certificar una auditoría real.

Preflight por ID: `PLAN_MAESTRO!H2240=PENDIENTE`, I vacía; `COLA!G2240/O2240=SI`; `DAG_TECNICO!F2201=VALIDADO/G2201=SI`. La A5 independiente M01.06 de PRIMARY DEV 01 fue revisada por PRIMARY DEV 04 sobre SHA `05f4acc51ed6ca351b79c27136e2fbfb1a2130c5`, sin autoaprobación. Esta lectura es una condición de inicio, no un lock transaccional del Sheet.

El backend existente conserva autoridad real de JWT, RBAC y empresa/tenant, con Angular 22.2.1 como consumidor no confiable, API .NET 10, Domain/Application/Infrastructure y MySQL/EF 10.0.12. El presente esquema es un modelo de auditoría propuesto **sin DDL**. Los estados no se deducen de ausencia de hallazgos ni del texto de una Task.

## 2. Modelo lógico y dueño de verdad

| ID | Entidad conceptual | Identificador o llave | Fuente autoritativa | Invariante |
| --- | --- | --- | --- | --- |
| SEC01 | AuditCut | sourceRevision + observedHeadSha | GitHub + STANDARD | No fusionar evidencia de cortes incompatibles |
| SEC02 | TenantBoundary | tenantScope + authenticatedPrincipal | Backend autorizado | Empresa A jamás lee ni escribe datos B |
| SEC03 | PrincipalClaim | principalId + tokenSession | Validación JWT server-side | Datos de cabecera/slug no conceden permisos |
| SEC04 | PermissionDecision | principal + resource + action | RBAC relacional server-side | Denegar por ausencia o revocación |
| SEC05 | SensitiveResource | entityKey + verifiedTenantId | Almacenamiento comercial verificado | No emitir referencias cruzadas |
| SEC06 | EvidenceArtifact | sourceSha + blobSha + evidenceKind | GitHub/CI | Evidencia reproducible, sin secretos |
| SEC07 | SecurityControl | controlKey + scope + revision | Gobierno/contrato vigente | Ninguna prueba contractual equivale a ejecución |
| SEC08 | NegativeCase | controlKey + scenarioKey | Tests autorizados | Fallo de prueba preserva hallazgo |
| SEC09 | Finding | findingId + sourceCut | Auditoría futura | P0/P1 no se declara cero por falta de escaneo |
| SEC10 | Remediation | findingId + patchSha | GitHub, retest causal | No se cierra sin prueba nueva |
| SEC11 | DependencyGate | microtaskId + predecessorIds | DAG_TECNICO F/G | G=SI debe ser calculado y revalidado |
| SEC12 | WorkOwnership | microtaskId + ownerTaskId | PLAN_MAESTRO H/I | Un solo escritor autorizado por scope |
| SEC13 | IndependentReview | microtaskId + author + reviewer | PLAN K + BITACORA | A5 requiere dos TASK_ID Javier distintos |
| SEC14 | NativeTaskState | taskId + runId + observedUtc | Automations.peek | Autorreporte no reemplaza estado nativo |
| SEC15 | AuditLedgerEvent | eventId + idempotencyKey | BITACORA A:Z | Append-only, veintiséis columnas |
| SEC16 | ReleaseAuthority | env + explicitApproval | Gobernanza vigente | LISTO/A5 en dev no autorizan QA/main/PROD |

Relaciones conceptuales: SEC01 1:N SEC06/SEC09, SEC02 1:N SEC05, SEC03 N:M SEC04 según permisos vigentes, SEC07 1:N SEC08, SEC09 1:N SEC10, SEC11 N:N SEC12 por dependencias, SEC12 1:N SEC15 y 0:1 owner activo, SEC13 referencia commit original y revisor distinto. No se crean tablas, claves foráneas ni cachés de autorización por describir estas relaciones.

## 3. Invariantes para una implementación posterior

| Regla | Obligación | Condición negativa |
| --- | --- | --- |
| INV01 | Identidad y tenant provenientes de backend autenticado | EmpresaId, URL o header del cliente como autoridad |
| INV02 | RBAC decidido por recurso/acción, no solo rol en UI | Guard Angular presentado como permiso real |
| INV03 | Revalidar membresía y revocación en lectura/escritura | Token válido pero membresía retirada admitida |
| INV04 | Denegar filtrado cruzado aun con query filter omitido | Acceso A→B mediante IgnoreQueryFilters |
| INV05 | Propietarios y colaboradores verificables por objeto | Identificador conocido permite acceso ajeno |
| INV06 | A5 por Task permitida distinta del autor y SHA original | Autor firma su propia revisión |
| INV07 | Checks requeridos completed/success exact SHA | CANCELLED/SKIPPED/FAIL como PASS |
| INV08 | Correlacionar evidencia y corte temporal exacto | Hallazgo viejo se mezcla con SHA nuevo |
| INV09 | Minimizar PII, tokens y trazas | Log de excepción filtra secretos |
| INV10 | Mantener registros históricos idempotentes | Reescritura de BITACORA o borrado de hallazgo |
| INV11 | CAS exclusivo y preservación de cambios concurrentes | Two writers ganan mismo scope |
| INV12 | Reversión aditiva con nueva validación | Force push o bypass de Scope Lock |

Las doce invariantes son reglas de diseño verificables por tests **futuros**. No son certificación de seguridad, pentest, cobertura o ejecución de políticas.

## 4. Casos positivos y negativos a validar posteriormente

| Caso | Positivo esperado | Negativo obligatorio |
| --- | --- | --- |
| T01 | Tenant A lee solamente A con membresía vigente | Header B no amplía el acceso |
| T02 | Admin permitido obtiene recurso propio | Usuario sin permiso recibe 403 sin enumerar B |
| T03 | Membresía revocada invalida operación posterior | JWT sin revalidación abre recurso |
| T04 | Modelo exige relación propietario-recurso | ID de objeto ajeno bypass ownership |
| T05 | CI exact-SHA cubre cambio real backend | Check cancelado se cuenta aprobado |
| T06 | A5 independiente usa commit/blobs originales | A5 autopublicado o cuenta fuera de allowlist |
| T07 | Hallazgo P0 mantiene estado hasta retest | Documento declara cero vulnerabilidades sin prueba |
| T08 | Evento repetido no duplica contador ni borra historia | Mismo event ID distinto payload sobrescribe |
| T09 | Un claim tiene owner exclusivo | Dos leases solapadas crean escrituras rivales |
| T10 | Rollback normal conserva concurrentes y CI nueva | Reset forzado elimina commit ajeno |

**T01–T10 = FUTUROS_NO_EJECUTADOS.** Las referencias no implican que se haya ejecutado MySQL, HTTP, Playwright, prueba de seguridad o revisión de todos los 2.340 IDs. Si un control produce hallazgo, conservar evidencia mínima anonimizada, owner y corrección causal, sin inventar PASS.

## 5. Límite de impacto, auditorías y rollback

- **A0 estática:** fuente V5, antecedente M01.06, SEC01–SEC16, INV01–INV12 y T01–T10, correspondencia de referencias, ausencia de encabezados vacíos y secretos; cambios sólo Markdown.
- **A1 funcional documental:** comprobar que cada caso tiene positivo y negativo falsables; ejecuciones de casos marcadas FUTUROS_NO_EJECUTADOS.
- **A2 seguridad:** no modificar código JWT/RBAC, autenticación, filtros tenant, PII, CORS, permisos, DB ni configuración; análisis estático no es pentest ni test funcional.
- **A3 integración:** cotejar el plan, Skill, DAG/PLAN/COLA y precedentes M01; no instalar runtime nuevo ni dar por realizadas pruebas HTTP/MySQL/Playwright.
- **A4 publicación:** fast-forward con expected HEAD, verificar diff de documento+CHANGELOG, SHA final, Scope Lock y procedencia V5 `completed/success`, registrar eventos y readback.
- **A5:** revisión posterior de otra Task Javier distinta de PRIMARY DEV 04. A5_PENDIENTE hasta tal evidencia.

Consumidores: M02.02 contratos API propuestos, M02.03 esquema conceptual, M02.04 configuración, M02.05 eventos como diseño, M02.06 gate terminal. Una especificación no libera M03, QA/main/PROD ni modifica G/O. **Rollback:** git revert normal en dev con CI, BITACORA aditiva y sin reset/force-push.
