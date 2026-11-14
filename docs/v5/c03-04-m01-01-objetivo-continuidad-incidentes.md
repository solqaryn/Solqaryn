# SOLQARYN V5 FINAL — C03-04.M01.01: objetivo, exclusiones y no-regresión de continuidad operativa

**Alcance:** exclusivamente preparación DOCUMENTAL M01.01 de C03-04, sin cambios ejecutables ni operación sobre datos. **Autor:** PRIMARY DEV 04 — JAVIER, TASK_ID \`6aeacb7820ac8191b7d768a6f33dfa09\`; repositorio \`solqaryn/Solqaryn\`, rama \`dev\`, PROJECT_SCOPE_LOCK=STRICT; fecha 2026-11-13.

**Fuentes únicas:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA A:Z](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill \`solqaryn-project-governance\`, AGENTS y Scope Lock. DAG de C03-04.M01.01 \`VALIDADO\`/gate \`SI\`: 15 predecesoras M01.01–05 de C03-01, C03-02 y C03-03 comprobadas LISTO al reclamar. Esta independencia **solo habilita planificación documental M01.01–05**; NO libera M01.06/M02 ni concede permisos operativos.

## 1. Objetivo observable y resultado de fase

Definir el contrato futuro, verificable y reversible de **manejo de incidentes, revocaciones, respaldo, restauración y continuidad** que los bloques posteriores C10 y C12 deberán satisfacer antes de operar. Toda decisión debe identificar dueño humano autorizado, versión de configuración, impacto en empresas A/B, ámbito de datos, aprobaciones, controles de seguridad, evidencia de ejecución, señal de recuperación y mecanismo de rollback. El objetivo incluye runbook inicial **en forma de requisitos**, no un runbook productivo puesto en marcha.

El ciclo de incidente deseado comprende detección → clasificación con severidad → contención de credenciales/sesiones afectadas → preservación de evidencia minimizada → recuperación en entorno autorizado → validaciones de integridad y tenancy → cierre/postmortem y medidas preventivas. Backup y restore requieren manifest, checksum, cifrado, identidad del origen, fecha, restauración de prueba descartable, controles de redundancia y capacidad de revertir sin destruir datos legítimos.

**RPO, RTO, retención, frecuencia, custodia de claves, restauración de datos vivos y acceso de emergencia NO se inventan:** necesitan clasificación de datos, aprobación del propietario y pruebas reales del proveedor. Cero cambios por esta microtarea en infraestructura, secretos, usuarios, tokens, API, MySQL, repositorios, buckets, DNS o workflows.

## 2. Límites de confianza, fuentes vivas y sus brechas

| Fuente revisada en dev (blob Git) | Dato estático verificable | No demuestra / decisión pendiente |
| --- | --- | --- |
| \`.github/workflows/backup-restore-backend-smoke.yml\` (\`9ae22277208c78a13f6d7f81b8be740845a057fd\`) | Push dev con paths específicos **y** marcador \`[SOLQARYN-BACKUP-SMOKE]\`; job con MySQL 8.4 temporal, .NET 10 y backup DEV | No equivale a restore ejecutado para este SHA ni permite dispararlo en cambio documental sin autorización contextual |
| \`.github/workflows/backup-restore-topology.yml\` (\`43e3f3c64b5161e61520bf57f3d0650e5708aab1\`) | Push dev con paths restringidos **y** marcador \`[SOLQARYN-BACKUP-TOPOLOGY]\`; restore efímero y validación de topología | Un workflow existente no prueba RPO/RTO ni restore actual de archivos, permisos o proveedor vivo |
| \`docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md\` (\`81e8b11557912fffcd741ff0f34faf69e0ed6f47\`) | Describe backup cifrado/restore descartable DEV y secretos externos al código | Nombres de environment \`DEV\` y \`Desarrollo\` requieren conciliación contra jobs/configuración viva; no habilita migrar/rotar secretos |
| \`backend/tests/Solqaryn.Tests/I8009CRevocacionRenovacionTests.cs\` (\`cb79e9e1b0e78df46adb8fba5074e0b2773b0901\`) | Prueba de servicio con mocks rechaza renovar JWT de usuario bloqueado/inactivo | No demuestra revocación inmediata de todos los JWT, endpoints HTTP, A/B o proveedores |
| \`docs/v5/c03-03-m01-01-objetivo-no-regresion-auditoria.md\` (\`59a1e138fccf59c674bca3f6cc45ed1733ffa5bc\`) | Objetivo documental de auditoría, registros sin secretos y correlación; cierre M01.01 separado | No demuestra retención real ni monitorización/alertas productivas |

Los hashes identifican contenido del repositorio inspeccionado y **no son resultados de CI**. El control de Restore debe confrontar variables de entorno, rutas, exclusiones, permisos y SHA reales en el momento de ejecutar una prueba futura, sin copiar secreto alguno al repositorio. La coexistencia de nombres de environment y variables en textos de distinta época es una discrepancia a verificar, no licencia para cambiar la configuración.

## 3. Alcance fuera de esta entrega y fronteras estrictas

- No crear, iniciar, ejecutar ni reconfigurar workflows de backup, restore, health, migraciones, proveedores, Docker, cuentas, servicios ni infraestructura. No levantar MySQL ni descargar respaldos sensibles.
- No conceder permisos ni elevar JWT/RBAC, cambiar sesiones, eliminar empresas, registros o usuarios, modificar tenant A/B, habilitar cuentas de emergencia o aprobar un salto a QA/main/PROD.
- No establecer RPO/RTO, retención, recuperación automática, garantía de disponibilidad o resiliencia sin evidencia causal y dueño autorizado.
- Nunca restaurar en Aiven o sistemas productivos para validar un documento. Un restore de prueba en MySQL efímero solo se ejecutará con workflow y permisos apropiados en tareas de implementación posteriores.
- Identificadores de incidente y correlación no son grants. La evidencia de recuperación no contiene contraseñas, token, PII cruda, claves de respaldo ni connection strings.
- No usar planes archivados VAEP/Jules ni recursos retirados como fallback. No crear microtareas, Tasks, ramas ni PR comerciales.

## 4. Matriz falsable de no-regresión para implementación futura (NO ejecutada aquí)

| ID | Signo | Estímulo / precondición | Comprobación técnica futura exigida |
| --- | --- | --- | --- |
| CO01 | + | Incidente clasificado, owner y sistemas afectados identificados | Ticket/evento con severidad, alcance tenant, decisión, hora y evidencia desensibilizada |
| CO02 | - | Actor sin rol de incidente intenta contención o restore | 403/denegación; no cambio de credenciales, datos ni backups |
| CO03 | + | Usuario bloqueado intenta renovar sesión | Sin JWT nuevo, estado auditable; HTTP y backend reales proporcionales |
| CO04 | - | Token revocado/rol retirado intenta leer empresa B | Rechazo sin lectura/escritura cruzada, aunque UI/header afirme permiso |
| CO05 | + | Backup cifrado autorizado con manifest en DEV | Artefacto, hash, versión, fecha y origen verificables sin exponer llave |
| CO06 | + | Restore de ese artefacto a MySQL efímero permitido | Igualdad de checksum/topología/filas verificables y salud del backend |
| CO07 | - | Restore apunta a PROD, host desconocido o BD no descartable | Rechazo antes de efecto; ninguna escritura sobre origen ni destino no autorizado |
| CO08 | - | Checksum, metadata de migración o manifest no coinciden | Fallo cerrado, no declarar restauración exitosa |
| CO09 | - | Secretos/PII aparecen en log o artifact de incidente | Rechazo de evidencia contaminada, rotación/contención según permiso; sin persistencia abierta |
| CO10 | + | Incidente de empresa A con B ajena | Recuperación mantiene aislamiento A/B, permisos y registros de auditoría |
| CO11 | - | Falta proveedor, secreto o permiso de respaldo | No falsificar \`SUCCESS\`; registrar bloqueo y mantener servicios críticos seguros |
| CO12 | - | Job \`SKIPPED\`, \`CANCELLED\` o \`IN_PROGRESS\` | No contabilizar PASS de backup/restore ni RPO/RTO; verificar SHA causal |
| CO13 | + | Restauración falla tras aplicar cambios reversibles | Ejercitar rollback no destructivo y comprobar integridad con comparativa pre/post |
| CO14 | - | Reintentos simultáneos de recuperación | Un único lease dueño y control idempotente; sin doble escritor ni duplicación destructiva |
| CO15 | - | Documentos indican \`DEV\` y \`Desarrollo\` como environment | Revisar YAML y configuración efectiva; nunca inferir equivalencia ni cambiar secretos de oficio |
| CO16 | + | Responsable define RPO/RTO y ventana aprobados | Prueba cronológica medida, restauración real aislada y evidencia firmada; no aprobar con estimaciones |

**Regla de interpretación:** positivos y negativos son CONTRATOS PARA PRUEBAS POSTERIORES, no tests que esta tarea haya ejecutado. Checks futuros .NET/MySQL/Docker, backend y E2E solo cuentan con workflow real del SHA exacto y artefactos pertinentes, nunca por la simple presencia de YAML.

## 5. Decisiones pendientes de autorización y criterios de salida

| Decisión | Dueño / gate necesario | Evidencia futura |
| --- | --- | --- |
| Ventanas RPO/RTO por tipo de dato | Propietario + responsables del negocio | Objetivos aprobados; ensayo medido, no conjeturas |
| Retención y destrucción de backup | Propietario + seguridad/legal | Política y prueba de borrado irreversible segura |
| Restore de empresa con datos reales | Permiso explícito, evaluación de PII y proveedor | Backup verificable, destino y rollback autorizados |
| Revocación de sesiones/tokens | Identidad y seguridad | Tests JWT/RBAC/tenant A/B de servicio y HTTP |
| Permisos de entorno \`DEV\`/\`Desarrollo\` | Administrador de GitHub del proyecto | YAML+settings autenticados, sin copiar secretos |
| Promoción a QA/main/PROD | Orden del propietario separada + A5 independiente real | No derivable de un LISTO_DEV documental |

La salida M01.01 solo define el contrato inicial y los oráculos de aceptación. M01.06, M02 y trabajos de implementación o despliegue mantienen sus respectivos gates técnicos, responsabilidades y permisos.

## 6. Plan de auditoría documental de esta microtarea

- **A0**: revisar diff Markdown y CHANGELOG aditivo, referencias e integridad de enlaces, sin secrets ni edición de gobierno/arquitectura.
- **A1 positiva**: objetivo, 16 casos CO01–CO16, decisiones, límites, pruebas futuras y rollback expresados. **A1 negativa**: ningún YAML, script o prueba mock se presenta como prueba real de restore.
- **A2**: denegación de acceso sin permiso, tenancy A/B, no exposición PII/secretos, no privilegios de emergencia y no operación productiva.
- **A3**: fuentes GitHub, DAG VALIDADO, 15 prerequisitos LISTO, C10/C12 solo destinatarios contractuales. .NET, Node/Angular, MySQL, Docker y E2E \`NO_APLICA\` a cambio documental; Scope Lock \`SI\` al publicar.
- **A4**: commit directo FF dev, blobs/diff, SHA exacto, check-run \`verify-solqaryn-project-scope\` \`completed/success\`; readback PLAN/COLA/BITACORA A:Z y rollback normal \`git revert\`.
- **A5**: registrar PENDIENTE de un revisor independiente entre otras cinco Tasks JAVIER; nunca auto-A5_PASS ni autorización de QA/main/PROD.
