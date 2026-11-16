# C12-04.M03.04 — adaptador aislado de manifiestos sintéticos de cutover

**Alcance:** SOLQARYN V17, dev exclusivamente, R2 por tenant/RBAC/concurrencia, PROJECT_SCOPE_LOCK=STRICT. Implementa en Infrastructure un adaptador en memoria de checkpoints sintéticos, no migración EF, DDL ni respaldo real. Predecesora C12-04.M03.03 con A5 independiente.

## A0: impacto y límites

- Clase C1204IsolatedManifestAdapter depende sólo del contrato C1204VerifiedActor de Application. Ninguna ruta API/HTTP, DI, job, migración, conexión MySQL, secreto ni servicio externo se modifica.
- El tipo Manifest es independiente de C1204VerifiedEvidence: no implementa IC1204CutoverReadinessEvidenceReader y no puede satisfacer el contrato del lector de evidencia confiable.
- Los únicos datos almacenados son actor/tenant opacos, clave sintética, SHA de commit, número de revisión y cuatro marcadores enum cerrados; ningún checksum, conteo ni resultado real de respaldo/restore se afirma.
- Una identidad verificada se recibe sólo como contrato in-process; el adaptador NO autentica JWT real. No registrar el adaptador como endpoint ni aceptar C1204VerifiedActor serializado desde clientes.
- IsAuthoritativeEvidence, CertifiesMySqlRestore, GrantsSqlOrMigration, AuthorizesCutover y AuthorizesQaOrProduction son falsos incluso en el camino positivo.

## A1 y A2: criterios ejecutables

- Positivo restringido: registro y lectura sintética, mismo actor y tenant, SHA exacto, revisión 0 a 1.
- Negativos: tenant B, actor diferente, membresía o permiso revocados, falta JWT/MFA, identificador inseguro, SHA distinto, clave malformada, marcador desconocido/duplicado y cancelación.
- Fronteras: máximo 4 marcadores enum, 32 manifiestos por tenant, revisión optimista monotónica y 8 escritores concurrentes sobre la misma revisión con un único ganador.
- Snapshot defensivo: copiar la lista mutable del llamante y devolver una colección de sólo lectura.

## A3 y A4: evidencia y no-aplicabilidad

Las pruebas xUnit son in-process; NO_APLICA MySQL/EF/HTTP/Chromium para este diff sin base de datos ni UI. Un check MySQL genérico en CI no convierte este adaptador en integración real. Exigir GitHub-hosted .NET 10 Release/xUnit, seguridad/autorización pertinentes, Scope Lock y V5 SUCCESS sobre SHA exacto o equivalencia demostrada. Nada queued, failed, cancelled o skipped requerido equivale a PASS.

## A5 y reversión

A5 por Task Javier distinta de DEV06. No libera migración real, nuevos GRANT, hosts, QA/main/PROD ni restauraciones. Rollback con git revert normal del commit y readback institucional, sin force-push ni alteración de BITACORA histórica.
