# SOLQARYN V17 — C12-01.M02.01: entidades, ownership, relaciones e invariantes de topología objetivo

**ID:** C12-01.M02.01 · **PLAN_MAESTRO:** fila 1736 · **Autor:** PRIMARY DEV 03 JAVIER (TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3) · **Riesgo:** R0, exclusivamente documental · **Ámbito:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT.

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO/COLA/DAG_TECNICO vigentes. **Antecedente directo:** C12-01.M01.06 y M01.01–M01.05, con A5 independiente y elegibilidad documental M02 recalculada. No se crea servidor, cuenta, contrato de proveedor, recurso externo, secreto, migración, desplegable, endpoint ni cambio de infraestructura real. **Estado vivo no modificado:** Angular 22.2.1/Vercel, ASP.NET Core 10/Render, MySQL Aiven y Cloudinary. Hostinger y cualquier DNS de destino son opciones por evaluar, no un ambiente autorizado o probado.

## A0. Alcance y criterio verificable

Definir un **modelo conceptual versionado** de la topología objetivo para estudiar más adelante VPS, red privada, contenedores, API, frontend, workers, persistencia, medios, secretos, respaldos y reversión. Su finalidad es permitir que M02.02–M02.05 especifiquen DTO, controles, persistencia eventual y límites, sin confundir esquema conceptual con implementación. Se preservan las dieciséis barreras G01–G16 y las decisiones todavía abiertas de M01.06; M02 documental no equivale a autorizar Hostinger, Cloudflare, traslado de MySQL, compras ni cutover.

**Consumidores previstos:** decisiones/ADR de plataforma, responsable de seguridad, equipo de datos, verificador CI y documentación de rollback. **Capas productivas alteradas:** ninguna. **DTO/API existentes alterados:** ninguno. **Datos/migraciones/transacciones:** ninguno. **Auth/JWT/RBAC/tenant:** solo invariantes de diseño, ningún permiso efectivo. **Rollback de este commit:** git revert aditivo tras revalidar HEAD, sin eliminar BITACORA.

## E01–E16: entidades conceptuales y dueño de decisión

| ID | Concepto futuro | Clave conceptual y ownership | Relación propuesta |
| --- | --- | --- | --- |
| E01 | TopologyRevision | topologyRevisionId, aprobación del propietario | Agrupa E02–E16; nunca se activa por mera creación |
| E02 | EnvironmentBoundary | environmentKey DEV/QA/PROD, control institucional separado | E01 contiene fronteras; no se comparten secretos ni estados |
| E03 | WorkloadDefinition | workloadKey, custodio de arquitectura | Describe Angular/API/worker; asigna E04, sin crear contenedores |
| E04 | PlacementProposal | placementId, equipo de infraestructura | Une E03 con una E02 y posible proveedor aún sin verificar |
| E05 | PrivateNetworkZone | networkZoneId, responsable de red | E04 propone ubicación; E06/E07 deben respetar segmentación |
| E06 | IngressRoutePlan | ingressId, dueño de dominio/red | Solo ruta propuesta hacia E07; sin DNS/TLS efectivo |
| E07 | ServiceEndpointContract | serviceKey, dueño de aplicación/API | Une workload E03 y zona E05; HTTP no queda acreditado |
| E08 | AccessPolicyReference | policyRef, responsable de seguridad | Restringe E07/E09 por identidad, rol, ambiente y revocación |
| E09 | SecretReference | secretRef opaca, custodio IAM | Se vincula E02/E03/E08; nunca almacena valores secretos |
| E10 | DatabasePlacementPlan | databasePlanId, responsable de datos | Relaciona E02/E05/E08; motor actual Aiven no cambia |
| E11 | VolumeRetentionPlan | volumePlanId, responsable de datos/operación | Referencia E03/E10 y retención aprobada futura |
| E12 | BackupRestoreContract | backupPlanId, responsable de continuidad | Cubre E10/E11 y verificación de restauración aislada |
| E13 | MediaBindingPlan | mediaBindingId, responsable de medios | Referencia servicios E03 y custodia tenant; Cloudinary vivo no cambia |
| E14 | ObservabilityContract | observabilityId, responsable de operación | Señales de E03/E07 sin PII, tokens ni credenciales |
| E15 | MigrationWaveProposal | waveId, responsable del cambio | Solo contempla E01/E02/E04/E10 y sus precondiciones |
| E16 | RollbackCheckpoint | checkpointId, responsable de recuperación | Depende de E12/E15; no afirma restore ejecutado |

**Cardinalidad conceptual:** E01 contiene múltiples E02 y propuestas E03; E03 puede tener varias E04 por revisión, cada E04 pertenece a una E02; E07 corresponde a un servicio y zona; E10 pertenece a una frontera E02; E12 ampara uno o más recursos E10/E11, siempre con responsabilidad explícita. E15 **no** se convierte en despliegue hasta que los gates G01–G16 sean comprobados y exista autorización independiente. Los IDs no son claves de tabla existentes ni implican esquema EF.

## I01–I20: invariantes fail-closed del contrato FUTURO

| Regla | Invariante exigible cuando exista implementación |
| --- | --- |
| I01 | Toda propuesta tiene E01 versionada; una revisión rechazada no se convierte en activa por defecto |
| I02 | E02 DEV, QA y PROD no comparten referencias de secretos, identidades de servicio ni base de datos |
| I03 | No se deduce tenant a partir de hostname, cabecera enviada por cliente ni nombre de contenedor |
| I04 | Ningún recurso de E05/E10 permite entrada pública MySQL; solo rutas y principals aprobados |
| I05 | Una E07 nunca concede autorización backend por pasar guards de Angular |
| I06 | E08 conserva JWT, RBAC, pertenencia vigente, revocación y tenant A/B de extremo a extremo |
| I07 | E09 contiene **solo** referencias opacas; ni documento ni Git conservan passwords, llaves o tokens |
| I08 | Una misma secretRef no atraviesa E02 sin aprobación explícita y rotación comprobable |
| I09 | E10 mantiene compatibilidad EF Core 10/Oracle MySQL 8.4 antes de cualquier traslado de datos |
| I10 | Ninguna migración destructiva o en PROD se desprende de un contrato documental |
| I11 | E11/E12 preservan integridad, retención, cifrado y ownership; restore real exige ensayo aislado |
| I12 | E13 mantiene separación de medios por empresa y reglas de acceso firmadas cuando corresponda |
| I13 | E14 redacta PII/secretos y aplica cuotas y retención aprobadas, sin seguimiento global por defecto |
| I14 | Un E03 worker no procesa mensajes de tenant B con contexto/credenciales de tenant A |
| I15 | Toda ruta E06 requiere TLS, DNS, CORS y cabeceras de seguridad comprobados antes de cutover |
| I16 | Configuración Render/Aiven vigente no se elimina por incorporar un modelo objetivo alternativo |
| I17 | Una E15 exige ventana autorizada, plan de abortar, pruebas negativas y lectura de dependencias |
| I18 | E16 no afirma rollback si no hay datos de hashes, tiempos, responsables y prueba efectiva |
| I19 | Valores RTO/RPO, p95, costes y capacidades no medidos quedan expresamente NO_MEDIDO |
| I20 | En fallo de identidad, secreto, versión, integridad o aislamiento el resultado es NO_GO sin fallback inseguro |

**Límite esencial:** las invariantes describen criterios futuros, no controles ya implementados en VPS/Hostinger. No sustituyen G01–G16 de M01.06 ni se heredan como certificados para M03.

## P01/N01–P12/N12: escenarios FUTUROS_NO_EJECUTADOS

| Par | Positivo a ejecutar en un laboratorio autorizado | Negativo que debe ser rechazado |
| --- | --- | --- |
| P01/N01 | Crear revisión de topología versionada y aislada | Revisión sin owner publicada como activa |
| P02/N02 | Resolver E02 DEV sin identidad QA/PROD | Reutilizar credencial DEV en PROD |
| P03/N03 | API privada accesible desde zona autorizada | Entrada MySQL desde red pública |
| P04/N04 | Tenant A opera exclusivamente su servicio | Tenant B accede por host o header manipulado |
| P05/N05 | E09 resuelve una referencia autorizada fuera de Git | Valor secreto aparece en documentación o log |
| P06/N06 | JWT/RBAC/tenant backend rechazan principal revocado | Sesión revocada se mantiene autorizada tras proxy |
| P07/N07 | Migración sintética EF/MySQL 8.4 con datos íntegros | Cambio de esquema no revertible o sin transacción |
| P08/N08 | Restore aislado coteja hashes y conteos | Backup corrupto se declara restaurado |
| P09/N09 | TLS/CORS/routing preservan contrato y errores | DNS o ruta no aprobada reciben tráfico |
| P10/N10 | Medio del tenant A conserva ACL y firma adecuada | URL de tenant A entrega objeto de B |
| P11/N11 | Worker aplica duplicados de manera idempotente | Reintento duplica transacción o ignora revocación |
| P12/N12 | Rollback ensayado cumple umbrales previamente aprobados | Valor NO_MEDIDO se reporta como SLO PASS |

**Resultados de esta entrega:** 0/12 pares ejecutados; no se ejecutaron Playwright, MySQL, .NET, proveedores, Docker, VPS, DNS ni restauraciones. La prueba A1 de esta microtarea R0 es comprobación documental de E01–E16, I01–I20 y P/N01–12, correspondencia con M01.06, coherencia de relaciones y ausencia de permisos o funciones añadidas. A2 es revisión estática de fronteras de seguridad; A3 runtime **NO_APLICA** por falta de cambios ejecutables.

## Matriz de dependencias y gates

- **Entrada M02.01:** PLAN_MAESTRO H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI antes de claim; candado V17 por fila con CAS H y owner. Antecesor C12-01.M01.01–06 LISTO/A5 independiente acreditado. Revalidar versiones y propietario antes de cada transición.
- **Salida M02.01:** solo contrato documental revisable A0–A4, A5_PENDIENTE hasta aprobación de otra Task Javier. M02.02–05 podrán ser reevaluadas por fórmulas y gates reales, no por edición manual de COLA/DAG.
- **Futuro diseño E04/E10/E15:** sigue NO_GO para despliegue real mientras M01.06 G01–G16 permanezcan sin acreditación y falte permiso explícito de proveedor, datos, secrets, QA/main/PROD.
- **Evidencia A4 exigible:** commit en dev mediante fast-forward CAS con documento y sección autocontenida de CHANGELOG; check Scope Lock y procedencia V5 completed/success en SHA, o equivalencia estricta y readback PLAN/COLA/BITACORA.
- **A5:** lector autónomo distinto del autor revisará reglas, fuentes, casos negativos y alcance; podrá rechazar sin que el autor se autoapruebe.

## Handoff y rollback

Si CI requerida del SHA queda queued/in_progress, mantener VALIDANDO con SHA, job y owner hasta REVIEW_FIRST. Si FAIL, reparar la causa y conservar el rechazo; canceled/skipped nunca PASS. No extrapolar los escenarios futuros a una certificación de implementación. Revertir este documento y su CHANGELOG mediante git revert normal, después de comprobar HEAD y lectores dependientes; no sobrescribir trabajo concurrente, no modificar historial BITACORA ni desactivar pruebas.
