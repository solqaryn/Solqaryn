# SOLQARYN V5 FINAL — C03-04.M02.01: ownership e invariantes de respuesta y recuperación

**Unidad:** C03-04.M02.01, "Definir entidades, ownership, relaciones y reglas de invariantes". **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID \`6adc2e55f47c81919f6374e561687076\`. **Rama:** \`solqaryn/Solqaryn@dev\`, \`PROJECT_SCOPE_LOCK=STRICT\`. **Naturaleza:** contrato M02 DOCUMENTAL, sin entidades EF, tablas ni operación runtime.

**Autoridades:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`); PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA del Sheet \`1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU\`; Skill canónica, AGENTS.md y Scope Lock de HEAD dev. Este contrato NO autoriza cambiar IAM, JWT, RBAC, tenants, backups reales ni QA/main/PROD.

## 1. Base de evidencia y procedencia

Las cinco entregas anteriores C03-04.M01.01–05 están LISTO con A5 independiente; C03-04.M01.06 [blob \`5a761e1b7e206809cfa94eed5966e87e1e97df30\`] también obtuvo A5_PASS independiente, posterior a su publicación. Las seis se verifican por ID al seleccionar esta microtarea, no por un contador congelado.

| Fuente verificada de la arquitectura/operación | Blob SHA | Límite de prueba |
| --- | --- | --- |
| \`scripts/backup_dev.sh\` | \`d2e0ca8bf48dc8c21fcbfd5ab5256c770bdbdcf3\` | Reglas de host y entorno; NO acredita backup ejecutado |
| \`scripts/restore_dev.sh\` | \`9be58822ae5c6189e7a84cf785998bf42a1b576c\` | Requiere permiso destructivo; NO acredita restore |
| \`.github/workflows/backup-restore-dev.yml\` | \`cee9cdd1e22d006c21d9a23cc40ead6223d7295c\` | Define ensayos CI efímeros; YAML no equivale a resultado PASS |
| \`docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md\` | \`81e8b11557912fffcd741ff0f34faf69e0ed6f47\` | Procedimiento DEV; no autoridad PROD |
| \`docs/ROLLBACK_RUNBOOK.md\` | \`b7424464a5a79f0d5ff858a7ec4d2356a30355f0\` | Procedimiento de rollback; no prueba restauración exitosa |

## 2. Entidades conceptuales y propietario efectivo

Las siguientes son **proyecciones de diseño, NO clases, tablas, endpoints ni DTO existentes**. Cada entidad futura tendrá identificador opaco, versión y evidencia causal; ninguna fila suelta del Sheet es una autorización operativa.

| Concepto propuesto | Owner/autorización necesaria | Relaciones y límite |
| --- | --- | --- |
| IncidenteSeguridad | operador autorizado de empresa/ámbito afectado y autoridad de plataforma verificada | Tiene evidencias saneadas y decisiones de contención; no atribuye automáticamente derechos de plataforma |
| AlcanceIncidente | empresa tenant, sucursal, recurso y entorno explícitos | Asociación a incidente; una empresa A no recibe identificadores ni restauraciones de B |
| IdentidadOperador | membresía actual y rol empresarial relacional | Referencia a decisión; jamás almacenar JWT, contraseña, secreto ni rol legacy como grant |
| EventoIncidenteConceptual | operación + sujeto autorizado + correlationId opaco | Sólo contrato de trazabilidad; **no** dispatcher, webhook, worker ni transmisión |
| EvidenciaIncidente | owner del incidente y clasificación de sensibilidad | Manifiesto/hash sin credenciales ni datos personales en logs públicos |
| PlanContencion | aprobación y versión verificadas antes de acción | Pasos reproducibles; no ejecutar revocaciones por el mero registro del plan |
| SolicitudRespaldo | owner de datos, entorno descartable o dev autorizado | Origen, alcance, timestamp, checksum; no incluye material secreto |
| ArtefactoRespaldo | mismo tenant/ámbito y custodia autorizada | Referencia opaca a manifiesto y hash; no implica que exista objeto real |
| SolicitudRestauracion | operador autorizado + destino efímero/development allowlisted | Relaciona artefacto y aprobación destructiva específica; nunca PROD implícito |
| EnsayoRestauracion | entorno CI aislado y ejecución comprobable | Resultado medido/rollback con SHA de job y hash, no solo documentación |
| AccionRollback | misma autorización operativa y versión origen | Referencia a incidente/cambio y prueba de reversibilidad; historia append-only |
| DecisionCierre | autoridad independiente y controles causales PASS | Cierra únicamente ámbito evaluado; no sustituye A5 ni autorización QA/PROD |

**Owner de datos vs owner de tarea:** el propietario del incidente es autoridad empresarial/operacional verificada, no se confunde con el TASK_ID que reclama trabajo técnico. Task Javier solo puede modificar el scope DEV permitido y nunca adquiere privilegios sobre datos de empresas por reclamar una microtarea.

## 3. Invariantes de no-regresión y dependencias

- I01. Cada caso mantiene empresa/ámbito/entorno inequívocos; toda identidad cruzada A→B termina DENY antes de I/O.
- I02. Una membresía revocada, rol inactivo o ausencia de permiso relacional impide decisiones de contención, restauración y auditoría sensible.
- I03. El rol global legacy y la visibilidad UI no conceden acceso a backups ni datos del tenant.
- I04. Cualquier restauración destructiva exige autorización explícita y allowlist de destino CI/DEV; el documento no la suministra.
- I05. Un artefacto sin hash y manifiesto comprobables nunca habilita restauración ni permite marcar RPO/RTO como cumplidos.
- I06. La ruta de origen, el destino y los IDs del ámbito se validan por fuente servidor; no aceptar ruta/host suministrado sin control.
- I07. Un replay de solicitud exige clave idempotente asociada a actor+tenant+operación+versión, nunca deduplicación global cruzada.
- I08. Una operación parcial, timeout o CI CANCELLED/SKIPPED/FAIL conserva estado no certificado y evidencia para compensación.
- I09. BITACORA institucional permanece append-only; una corrección agrega evento, nunca reescribe evidencia pasada.
- I10. El estado de PLAN H/I y el gate calculado COLA/DAG se revalidan tras cada transición, no se fuerzan a SI.
- I11. Log y error públicos no exponen tokens, contraseñas, PII, SQL, identificadores ajenos ni excepciones internas.
- I12. RPO, RTO y objetivos de retención requieren mediciones aprobadas y evidencia, no cifras inferidas.
- I13. Migraciones futuras EF/MySQL precisan pruebas efímeras A/B, reversibilidad y permisos propios, sin usar Aiven productivo.
- I14. No hay mecanismo server-side de ejecución perpetua: la Task programada termina según límites reales, sin llamar a otras Tasks.
- I15. A5 de otra Task Javier es obligatoria para certificación independiente; ninguna Task aprueba su propia autoría.
- I16. Toda acción fuera de dev (QA/main/PROD, DNS, secretos, proveedores de pago) exige orden adicional y controles diferenciados.

Las relaciones entre incidencia, respaldo, evidencia y restauración son conceptuales. La implementación de persistencia, índices, transacciones, estados y APIs pertenece a M02.02–06/M03 con preflight propio; este documento no modifica ni valida esos componentes.

## 4. Casos de aceptación FUTUROS_NO_EJECUTADOS

| ID | Tipo | Oráculo contractual |
| --- | --- | --- |
| P01 | Positivo | Operador con membresía y permiso actual en A puede preparar un incidente solo en A |
| P02 | Positivo | Backup CI autorizado conserva manifiesto y checksum medido con dueño verificado |
| P03 | Positivo | Restore sobre entorno efímero autorizado concilia hashes y emite evidencia verificable |
| P04 | Positivo | Rollback documental reversible conserva enlaces a evidencias previas |
| N01 | Negativo | Usuario de A intenta abrir/restaurar recurso de B: DENY antes de consultar B |
| N02 | Negativo | Membresía revocada o rol legacy administrador sin permiso relacional: DENY |
| N03 | Negativo | Host fuera de allowlist, PROD o destino ambiguo: NO_GO sin mutación |
| N04 | Negativo | Falta aprobación destructiva de restore: DENY |
| N05 | Negativo | Hash distinto, copia ausente o manifiesto truncado: error seguro, sin falso PASS |
| N06 | Negativo | JWT, secreto SMTP o PII en log público: omitir/redactar y no certificar evidencia |
| N07 | Negativo | Reintento concurrente con idempotencyKey de otro tenant: DENY sin efecto cruzado |
| N08 | Negativo | Check queued/in_progress/cancelled/skipped/fail presentado como PASS: rechazar |
| N09 | Negativo | CAS propietario/HEAD cambiado: abortar y releer, nunca pisar commits |
| N10 | Negativo | RPO/RTO sin medición: queda NO_ACREDITADO |
| N11 | Negativo | Intento de promover QA/main/PROD por cierre documental: NO_GO |
| N12 | Negativo | Solicitud de registrar webhook, dispatcher o worker por este scope: NO_GO |

Los escenarios anteriores son **requisitos de pruebas futuras**, no pruebas HTTP/MySQL/Docker ni restauraciones realizadas en esta microtarea.

## 5. Auditorías, secuencia y rollback

**A0** inspección estática de fuente Git, CHANGELOG autocontenido, exclusión de secretos, diff exclusivamente Markdown. **A1** revisión de cuatro positivos/doce negativos documentales, no ejecución de runtime. **A2** límites de JWT/RBAC/tenant A/B, PII, allowlist de entornos y reversibilidad por diseño. **A3** trazabilidad con M01.01–06 LISTO+A5 y cinco rutas reales, sin afirmar integraciones. **A4** publicar dev fast-forward CAS, revisar SHA exacto Scope Lock y V5 procedencia SUCCESS y readback del PLAN/COLA/BITACORA. **A5** una Task Javier de distinto TASK_ID debe revisar blobs, pruebas pertinentes y ownership, sin autoaprobación. **Rollback:** git revert normal del changeset documental, con un evento compensatorio aditivo si fuera necesario.

**Impacto:** solo contrato de datos conceptuales C03-04 y CHANGELOG_AI.md. Sin nuevas entidades reales, contratos API, EF/migraciones, secretos, triggers, jobs runtime, alteración IAM, Aiven real, QA/main/PROD ni costes.