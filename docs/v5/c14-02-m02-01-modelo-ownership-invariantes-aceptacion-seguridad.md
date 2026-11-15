# SOLQARYN V5 — C14-02.M02.01: modelo conceptual, ownership e invariantes de aceptación de seguridad

**Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14 (America/Tegucigalpa). **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** contrato documental de entidades, relaciones e invariantes. No cambia JWT, RBAC, MySQL, middleware, frontend, tareas programadas ni datos reales; **NO es pentest ni certificación de cero P0/P1**.

## 1. Autoridad, dependencias y alcance

La única fuente ejecutable es [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) con [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El preflight de `C14-02.M02.01` exige PLAN_MAESTRO H2060=PENDIENTE, I2060 vacío; COLA G2060/O2060=SI; DAG_TECNICO ID exacto F=VALIDADO/G=SI. El antecedente `C14-02.M01.06` está LISTO y su A5 documental independiente por DEV01 se acredita sobre commit `e9b73c625b30d9b1ae91a1eaf29e4e50622eadeb` y documento blob `6cb629af7993a6f4eb7cb8518f538f7ed196326f`, preservando al autor DEV05. Ese A5 no equivale a pruebas HTTP o cobertura empírica P0/P1.

Contratos M01.01–M01.06 delimitan alcance, actores, fuentes, riesgos, aceptación y bloqueos. `docs/v5/c14-02-m01-06-gates-bloqueo-rollback-liberacion-seguridad.md` define B01–B12 sobre dependencias, CI, JWT/RBAC, BOLA/IDOR, tenant, sucursal, propiedad, hosts, archivos, jobs, backups y revisión independiente. Este M02.01 modela las responsabilidades para una **aceptación futura** de seguridad en C14-02. Es preparación documental; no adelanta una implementación M03 ni elimina precondiciones de otros lotes.

## 2. Entidades lógicas SE01–SE14 (no nuevas tablas)

| ID | Entidad conceptual | Owner de autoridad | Relación / restricción |
| --- | --- | --- | --- |
| SE01 | `SecurityAcceptanceScope` | Plan autorizado por ID y versión | Ámbito dev / sistema / tenants que se pretende evaluar; prohibido ampliarlo desde texto |
| SE02 | `SubjectIdentity` | JWT validado por backend | Identidad y claims autenticados; un header de cliente no sustituye firma |
| SE03 | `Membership` | Relaciones reales usuario/empresa/sucursal | Revocación efectiva antes de recurso protegido |
| SE04 | `PermissionGrant` | RBAC relacional y políticas backend | Permiso por módulo/acción y scope, deny by default |
| SE05 | `TenantBoundary` | Empresa dueña del dato | A/B aisladas, sin lookup cruzado por ID directo |
| SE06 | `OwnedResource` | Persistencia bajo empresa/sucursal | FK y filtros validados por backend, no por Angular |
| SE07 | `HostBinding` | Registro de host/storekey autorizado | Comparación exacta y procedencia confiable |
| SE08 | `ProtectedMedia` | Registro de objetos bajo propietario | Acceso de enlace firmado TTL/replay/tenant verificables |
| SE09 | `AsyncEffect` | Aplicación e integración autorizadas | Correlación e idempotencia sin efectos cruzados |
| SE10 | `SecurityFinding` | Revisión de evidencia reproducible | Severidad P0–P3 según criterio documentado, no intuición |
| SE11 | `ReproductionEvidence` | Job CI o prueba autorizada | SHA, entorno, datos sintéticos, pasos y resultado |
| SE12 | `MitigationRecord` | Propietario real de cambio | Diff, responsable, prueba negativa y rollback |
| SE13 | `IndependentReview` | Otra Task Javier autorizada | TASK_ID revisor distinto del autor, SHA y evento trazables |
| SE14 | `ReleaseDecision` | Gobernanza de ambientes | DEV LISTO/A5 no concede QA/main/PROD |

`SubjectIdentity` (SE02) 1→N `Membership` (SE03), pero cada membresía requiere verificación backend antes de recurso SE06; SE05 1→N SE06 restringe propiedad, y SE04 valida la acción. `SecurityFinding` SE10 1→N SE11 y 0→N SE12, con revisión SE13 ligada al SHA de corrección. No se puede computar cero vulnerabilidades a partir de ausencia de registros: `NO_EVALUADO` no significa `PASS`. La existencia conceptual de SE01–SE14 no crea entidades EF ni política de retención.

## 3. Invariantes SI01–SI16 para futuras pruebas

| ID | Garantía objetivo | Contraejemplo negativo exigible |
| --- | --- | --- |
| SI01 | Tenant de recurso verificado en backend para cada lectura | Usuario A accede a dato B con ID conocido |
| SI02 | Escritura compara owner/sucursal y permiso efectivo | POST de A modifica entidad B |
| SI03 | JWT se valida firma, audiencia, emisor, expiración y revocación según contrato real | Token expirado o alterado obtiene HTTP 200 |
| SI04 | RBAC es efectivo tras cambio o revocación de membresía | Permiso revocado continúa permitiendo acceso |
| SI05 | Host/storekey se resuelve por igualdad exacta desde origen confiable | Host manipulado selecciona tienda ajena |
| SI06 | Recursos media se entregan por autorización y TTL | URL de otro tenant o replay concede acceso |
| SI07 | EF filtra empresa y respeta FK compuesta cuando corresponda | Consulta sin tenant scope publica datos de B |
| SI08 | Background job sólo opera con contexto de pertenencia explícito | Worker sin contexto cruza empresa |
| SI09 | Caché y rate limit separan identidad/empresa cuando aplique | Respuesta cacheada de A se entrega a B |
| SI10 | Logs minimizan PII, secretos y tokens | Authorization Bearer aparece en auditoría |
| SI11 | Manejo de error no revela stack ni detalles de otra empresa | 404 vs 403 permite enumerar objeto ajeno |
| SI12 | CI obligatoria concluye completed/success exact-SHA | Cancelled/skipped/in_progress se cuenta PASS |
| SI13 | Evidencia de hallazgo conserva reproducción y versión | Severidad cambia sin nueva prueba |
| SI14 | Rollback preserva consistencia y no reescribe historia | Force-push borra pruebas previas |
| SI15 | A5 requiere revisión independiente con blob original | Autor aprueba su propio scope |
| SI16 | Promoción a otros ambientes necesita permiso explícito | LISTO_DEV se convierte automáticamente en PROD |

Los invariantes SI01–SI16 son **condiciones futuras a demostrar**, no resultados de pruebas ya ejecutadas. El contrato distingue `DENEGADO_VERIFICADO`, `PERMITIDO_VERIFICADO`, `FALLO` y `NO_EJECUTADO`; ninguna observación omitida se cuenta positiva.

## 4. Matriz positiva/negativa SA01–SA12, FUTUROS_NO_EJECUTADOS

| Escenario | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| SA01 | Tenant A sólo lee sus recursos | A obtiene recurso de B por ID |
| SA02 | Admin legítimo posee permiso exacto | Rol sin permiso usa ruta protegida |
| SA03 | Revocación invalida operación protegida | Membresía revocada conserva acceso |
| SA04 | Host válido selecciona dominio propio | Host similar/spoof redirige al vecino |
| SA05 | Link firmado vigente autoriza archivo | TTL expirado/replay/archivo de otro tenant |
| SA06 | EF y FK compuesta preservan propietario | FK referenciada de B se acepta en A |
| SA07 | Worker idempotente no duplica efecto | Reintento duplica notificación/efecto |
| SA08 | JWT y headers inválidos producen rechazo | Token sin firma válida obtiene 200 |
| SA09 | Logs redactados permiten diagnóstico | Se expone PII, access token o secreto |
| SA10 | Hallazgo P1 corregido tiene retest con SHA | Marcar mitigado sin prueba causal |
| SA11 | A5 de revisor distinto comprueba archivo original | Mismo TASK_ID firma A5 |
| SA12 | CI exact-SHA se respeta antes de LISTO | Check cancelled se declara success |

Estas pruebas exigirían un entorno CI controlado con fixtures sintéticos para tenant A/B, permisos de GitHub Actions, servicios efímeros MySQL donde aplique, validación manual de vulnerabilidades y lectura de logs. **Ninguna SA01–SA12 se ejecutó en esta microtarea documental.** No se afirmará pentest, BOLA/IDOR cubiertos, cero P0/P1, backup real ni seguridad de producción.

## 5. Seguridad, procedencia, rollback y límites

La implementación futura deberá revalidar la arquitectura ASP.NET Core 10 y Angular 22.2.1, el proveedor EF Core 10 + MySQL, controles JWT/BCrypt/RBAC y fronteras tenant. La UI no sustituye autorización backend. La evidencia de CI asociará SHA exacto, `status`, `conclusion`, logs, nombre del job y tiempo de observación; Scope Lock y la procedencia V5 son obligatorios en cualquier commit de `dev`. Efectos asíncronos, event dispatcher o modificación de Tasks no están autorizados por este documento.

**A0:** correspondencia documental SE01–SE14, SI01–SI16, SA01–SA12 y fuentes M01. **A1:** coherencia entre escenarios positivo/negativo, sin runtime. **A2:** amenazas tenant A/B, JWT/RBAC/PII y denegación conceptual, sin test HTTP. **A3:** vínculos con M01.06, PLAN/COLA/DAG/BITACORA; .NET/Angular/EF/Playwright `NO_APLICA_DOCUMENTAL` a dos Markdown. **A4:** diff limitado a documento y `CHANGELOG_AI.md`, fast-forward expected SHA, checks Scope Lock y V5 `completed/success` en SHA exacto o equivalencia de HEAD demostrada, readback. **A5:** otra Task Javier independiente del autor, nunca autoaprobación.

**Rollback documental:** `git revert` normal sobre dev, revalidar Scope Lock/HEAD y agregar evento BITACORA, sin force-push, sin reescribir registros históricos, sin tocar DB o QA/main/PROD. Una implementación posterior requiere scope específico, pruebas de aislamiento y rollback adicional. M02.02 definirá contratos de API, M02.03 persistencia, M02.04 flags, M02.05 eventos conceptuales y M02.06 puertas finales; sólo DAG/PLAN revalidado permite el siguiente claim.
