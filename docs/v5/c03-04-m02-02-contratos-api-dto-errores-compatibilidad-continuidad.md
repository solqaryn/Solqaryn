# SOLQARYN V5 FINAL — C03-04.M02.02: contratos API/DTO de continuidad

**Microtarea:** C03-04.M02.02 — definir API, DTO, errores, validaciones y compatibilidad. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. **Repo:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT. **Naturaleza:** propuesta documental M02, no rutas HTTP ejecutables ni DTOs compilados.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, y PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. No alterar JWT/RBAC/tenant, scopes ni otras ramas.

## 1. Contratos existentes y frontera

C03-04.M01.01–06 está LISTO, con revisión A5 independiente. `C03-04.M02.01` ya documentó doce entidades propuestas y dieciséis invariantes, pero su A5 sigue pendiente: este contrato M02.02 es **documental independiente según su DAG**, no depende de usar M02.01 como autoridad técnica ejecutable.

Rutas observadas: `scripts/backup_dev.sh` (blob `d2e0ca8bf48dc8c21fcbfd5ab5256c770bdbdcf3`), `scripts/restore_dev.sh` (`9be58822ae5c6189e7a84cf785998bf42a1b576c`), workflow `.github/workflows/backup-restore-dev.yml` (`cee9cdd1e22d006c21d9a23cc40ead6223d7295c`), `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md` (`81e8b11557912fffcd741ff0f34faf69e0ed6f47`) y `docs/ROLLBACK_RUNBOOK.md` (`b7424464a5a79f0d5ff858a7ec4d2356a30355f0`). Estas fuentes son scripts/planes DEV, **no** endpoints de respuesta a incidentes ni pruebas de restore ya realizadas.

Los siguientes nombres describen **operaciones futuras propuestas**, sin implementar ni registrar controladores, servicios, DTO, validadores, workers o eventos. Cada request requiere usuario autenticado en backend, membresía/rol actual, empresa y recurso tenant-owned verificados, versionado y correlationId opaco generado por servidor.

## 2. Superficie lógica propuesta — no endpoints publicados

| Código | Operación futura | Solicitud mínima y resultado filtrado |
| --- | --- | --- |
| OP01 | Registrar incidente | Tipo allowlisted, alcance tenant/entorno autorizado, descripción saneada → ID opaco/version |
| OP02 | Leer incidente | ID opaco + contexto actual → solo incidente propio y estado |
| OP03 | Listar incidentes | Paginación y filtro cerrado por tenant → resultados autorizados sin PII ajena |
| OP04 | Proponer contención | Incidente + clase de medida y expectedVersion → propuesta pendiente, no acción real |
| OP05 | Aprobar contención | Permiso excepcional verificado y versión → decisión auditada, no reactivar JWT |
| OP06 | Anexar evidencia | Hash, clasificación y referencia opaca → manifiesto sin secretos |
| OP07 | Solicitar backup DEV | Entorno allowlisted, scope propio, propósito → solicitud pendiente, NO backup inmediato |
| OP08 | Consultar prueba backup | ID + tenant + SHA de job → evidencia verificada o PENDIENTE, nunca PASS inferido |
| OP09 | Solicitar ensayo restore | Hash de artefacto, destino descartable y permiso destructivo → plan NO ejecutado |
| OP10 | Consultar ensayo restore | ID + job exacto → hashes/esquema/errores y fecha medidos, sin archivos personales |
| OP11 | Proponer rollback | Cambio versionado, plan y dependencia → propuesta auditada, sin reversión inmediata |
| OP12 | Cerrar incidente | Estado, evidencias, aprobaciones y versión → cierre solo de alcance acreditado |

**Control de autoridad:** OP01–12 deben fallar cerrados si el solicitante no tiene rol/membresía vigente en el tenant o trata de usar IDs ajenos. El cliente nunca determina EmpresaId, host, privilegios ni política de seguridad por manipulación de headers. OP05, OP07, OP09 y OP11 requieren aprobación operativa específica que no aporta una Task técnica.

## 3. DTO y validaciones futuras

**Envelope propuesta `ContinuidadSolicitudV1`:** `schemaVersion`, `correlationId` (generado por servidor), `resourceId` opaco, `expectedVersion` entero positivo, `operation` enum cerrado y `payload` tipado por operación. No admitir `connectionString`, `jwt`, `password`, `privateKey`, `restoreHost` arbitrario ni un `empresaId` como fuente de autoridad.

**Respuesta propuesta `ContinuidadResultadoV1`:** `resourceId`, `state` permitido, `version`, `correlationId`, `proofRef` opaco y `error.code` genérico cuando aplique. Ni datos completos del cliente ni excepciones internas salen al consumidor.

**Reglas de validación:** V01 versión de esquema reconocida; V02 ID opaco no vacío; V03 actor y tenant actuales; V04 permiso por operación; V05 recurso tenant-owned; V06 clasificación de evidencia permitida; V07 cadena/hash con formato estricto; V08 expectedVersion exacta; V09 entorno allowlisted CI/dev; V10 aprobación destructiva para restore; V11 referencia a job SHA exacto; V12 prohibición de secretos y URLs arbitrarias en DTO; V13 paginación acotada; V14 deduplicación por actor+tenant+operación+versión sin fuga cross-tenant.

## 4. Errores contractuales y compatibilidad

| HTTP futuro (no ejecutado) | Código público genérico | Condición |
| --- | --- | --- |
| 400 | INVALID_INPUT | Falta campo requerido, enum desconocido o versión inválida |
| 401 | AUTH_REQUIRED | No hay identidad autenticada válida |
| 403 | ACCESS_DENIED | Permiso/membresía/tenant insuficiente sin filtrar detalles |
| 404 | NOT_FOUND | Recurso inexistente o de otro tenant, misma respuesta indistinguible |
| 409 | VERSION_CONFLICT | expectedVersion o claim perdió CAS |
| 409 | DUPLICATE_OPERATION | Mismo idempotencyKey con payload incompatible |
| 412 | PRECONDITION_NOT_MET | No existe manifiesto, aprobación o control exigido |
| 422 | UNSAFE_TARGET | Destino fuera de allowlist CI/DEV |
| 429 | RATE_LIMITED | Cuota autorizada agotada, sin reintentos infinitos |
| 500 | INTERNAL_ERROR | Error inesperado, respuesta y log saneados |
| 503 | DEPENDENCY_UNAVAILABLE | MySQL/storage/CI no confirmó evidencia, sin falso PASS |

La semántica anterior **no modifica la API actual** ni garantiza que los controladores existentes emitan estos códigos. Versionar ruta/controlador futuro y publicar adaptador de compatibilidad es otro scope con prueba HTTP negativa y autorización correspondiente. Angular solo proyecta estados, jamás concede permisos. No integrar `EmpresaConfiguracion`, IAM, outbox ni colas por este contrato.

## 5. Oráculos FUTUROS_NO_EJECUTADOS

| ID | Clase | Expectativa verificable una vez implementado |
| --- | --- | --- |
| P01 | + | Operador válido A consulta incidente propio con información saneada |
| P02 | + | Evidencia con hash validado queda vinculada solo al incidente/tenant |
| P03 | + | Repetición idempotente devuelve mismo resultado a mismo actor A/version |
| P04 | + | Un ensayo restore efímero válido conserva checksum y muestra job exacto |
| N01 | - | Usuario A intenta consultar/reabrir incidente B → 404/403 sin fuga |
| N02 | - | Rol legacy admin sin RolPermiso actual → DENY |
| N03 | - | Membresía revocada después del JWT → DENY |
| N04 | - | DTO transporta secreto o host externo arbitrario → rechazo |
| N05 | - | Destino PROD en solicitud de ensayo → NO_GO, sin ejecución |
| N06 | - | expectedVersion viejo o escritor concurrente → 409 |
| N07 | - | Hash o manifiesto no coincidente → bloqueo sin certificar restauración |
| N08 | - | IdempotencyKey de tenant A presentada en B → DENY |
| N09 | - | CI queued, cancelled, skipped o failed → PENDIENTE/ERROR, nunca PASS |
| N10 | - | RPO/RTO no medidos → no mostrar cumplimiento |
| N11 | - | Paginación ilimitada o recurso inexistente → error/paginación acotada |
| N12 | - | Consumidor intenta activar worker/webhook por diseño → NO_GO |

No se ejecutó ninguno de los P/N anteriores como prueba real en esta microtarea; son contratos para código futuro.

## 6. A0–A5, impacto y rollback

- **A0**: artefacto y sección CHANGELOG contiguos, rutas actuales leídas, cero cambios .NET/Angular/SQL, cero secretos y rollback definido.
- **A1**: revisión documental de OP01–12, V01–14, 11 errores y P01–04/N01–12, sin atribuir resultados HTTP.
- **A2**: revisión de restricciones JWT/RBAC/tenant A/B, IDOR/BOLA, PII, versiones y objetivos fail-closed.
- **A3**: coherencia de M01.01–06 con A5, M02.01 como diseño no requerido por gate, y scripts/runbooks vigentes; tests runtime NO_APLICA a este diff.
- **A4**: commit en dev fast-forward/CAS, Scope Lock y V5 provenance `completed/success` exact SHA más readback PLAN/COLA/BITACORA.
- **A5**: revisión independiente otra Task Javier, nunca autoría propia, previo a cierre institucional que requiera ese gate.

**Impacto y rollback:** dos archivos Markdown únicamente; no cambios a controllers, DI, auth, RBAC, MySQL, workers, eventos, archivos reales ni infraestructura. Sin QA/main/PROD, compra ni secretos. Revertir con `git revert` normal, sin force-push ni reescribir BITACORA.