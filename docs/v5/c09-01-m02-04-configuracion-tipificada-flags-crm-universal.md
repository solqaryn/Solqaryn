# SOLQARYN V17 — C09-01.M02.04: configuración tipada y feature flags conceptuales del CRM universal

**Microtarea:** C09-01.M02.04 · **Autor:** PRIMARY DEV 04 / JAVIER_CHATGPT · **TASK_ID:** \`6aeacb7820ac8191b7d768a6f33dfa09\` · **Riesgo de esta entrega:** R0 documental · **Repositorio:** \`solqaryn/Solqaryn@dev\` · **PROJECT_SCOPE_LOCK:** STRICT.

**Fuente de autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`), PLAN_MAESTRO fila 1307. **Objeto:** definir opciones tipadas, ámbitos, precedencia, validación, seguridad y flags FUTURAS para historial, segmentación, actividades, oportunidades y consentimiento CRM. La entrega es **únicamente documental** y no modifica código, archivos de configuración ejecutables, variables de entorno, base de datos, feature flag providers ni las 6 Tasks.

**NO IMPLEMENTADO/NO EJECUTADO:** ningún flag o permiso CRM se activó, ningún endpoint/worker/campaña/exportación existe por este texto, no se ejecutaron xUnit, MySQL 8.4/EF, API/HTTP, Angular/Playwright ni simulaciones de rollout. La autorización de M03 y A5 requieren evidencia posterior. Cualquier valor citado es propuesta de contrato, no valor que ya se haya desplegado.

## 1. Baseline, dependencias y fronteras

- \`C09-01.M01.01–06\` define no regresión, riesgos, decisiones pendientes D01–D08, aceptación futura y gates. M01.06 tiene A5 independiente según las fuentes institucionales; esta M02.04 tiene disponibilidad propia verificada en COLA G/O y DAG F/G al CLAIM.
- \`C09-01.M02.01\` define 16 conceptos E01–E16, invariantes INV01–INV20 y seguridad tenant A/B; \`C09-01.M02.02\` define 8 operaciones API propuestas, 8 DTO, 12 errores y 16 validaciones; \`C09-01.M02.03\` documenta contratos futuros de persistencia, índices y migración. **Estos son contratos conceptuales**, no endpoints, permisos JWT, campos EF ni tablas existentes.
- Tres entidades base inventariadas en M01: \`Empresa\`, \`Cliente\` y \`UsuarioEmpresa\`. \`Cliente.EmpresaId=NULL\` legado se trata como propietario NO VERIFICADO; ninguna flag puede transformarlo en acceso global ni completar backfill por sí sola.
- Arquitectura autorizada: Angular 22.2.1 standalone/Signals (visualización solamente); ASP.NET Core 10 Application/Domain (autoridad y reglas); Infrastructure con proveedor Oracle EF Core 10.0.12/\`MySql.EntityFrameworkCore\` 10.0.9 (persistencia futura). La UI, host, cookie, cabeceras o \`EmpresaId\` solicitado **no conceden** permisos.

## 2. Registro de opciones conceptuales y tipadas (O01–O16)

Los tipos son futuros contratos de backend, no variables actualmente disponibles. \`OFF\` es el valor seguro inicial de cualquier capacidad con efecto comercial. Los números son **límites propuestos**, revisables por métricas/criterios aprobados antes de código.

| ID | Opción futura, tipo y valor inicial seguro | Ámbito y validación prevista |
| --- | --- | --- |
| O01 | \`CrmEnabled: boolean = false\` | Global AND Empresa habilitada; false cancela funciones CRM sin quitar seguridad |
| O02 | \`TimelineReadEnabled: boolean = false\` | Empresa; exige membresía activa, \`crm.actividad.read\` real y ownership validado |
| O03 | \`TimelinePageSizeMax: int = 50\` | 1..100, límite servidor; nunca aceptar 0/negativo/valor fuera de rango |
| O04 | \`TimelineLookbackDays: int = 90\` | 1..3650; retención efectiva queda sujeta a privacidad y hold |
| O05 | \`ActivityWriteEnabled: boolean = false\` | Empresa; exige autorización backend y write idempotente |
| O06 | \`OpportunityPipelineEnabled: boolean = false\` | Empresa; solo etapas allowlisted y CAS; nunca genera venta/asiento |
| O07 | \`SegmentPreviewEnabled: boolean = false\` | Empresa; preview limitado y sin exportación implícita |
| O08 | \`SegmentPreviewRowsMax: int = 100\` | 1..1000, resultado minimizado y paginado; cuota/costo backend |
| O09 | \`SegmentAllowedFields: enum[] = []\` | Allowlist por esquema/versionado, nunca SQL/JS arbitrario |
| O10 | \`ConsentRequired: boolean = true\` | Invariante de seguridad no relajable por Empresa/branch |
| O11 | \`ContactDispatchEnabled: boolean = false\` | Global AND Empresa; **no crea** worker/campaña ni revierte opt-out |
| O12 | \`ExportEnabled: boolean = false\` | Global AND Empresa + permiso separado \`crm.export\`; default deny |
| O13 | \`ExportRowsMax: int = 100\` | 1..1000; solo columnas allowlisted y finalidad autorizada |
| O14 | \`IdempotencyTtlMinutes: int = 60\` | 1..1440; scope por Empresa+actor+operación+hash de payload |
| O15 | \`AuditRetentionDays: int = 365\` | 30..3650, requiere política legal/hold; jamás borrar por config sola |
| O16 | \`LegacyUnownedAccessAllowed: boolean = false\` | **Invariante fija false**; nunca sobreescribir desde configuración de tenant |

**Precaución de diseño:** O10 y O16 son controles invariantes de seguridad, NO flags operativas habilitables por usuarios o administradores de Empresa. D01–D08 sobre consentimientos, esquema, índices, retención, límites, auditoría y transacciones requieren aprobación independiente antes de fijar valores de producción. Si se descubre un override que invierta un invariante, la configuración se rechaza completa y queda \`OFF\`.

## 3. Jerarquía, resolución y cambios (R01–R12)

- **R01:** una capacidad con efectos exige \`GlobalGate AND TenantGate AND RBAC AND MembershipActive AND ResourceOwnership\`; jamás \`OR\`.
- **R02:** un request puede indicar Empresa deseada, pero backend determina la identidad y membresía válidas; no se confía en \`X-Empresa-Id\`, host, slug, cookie, ruta o frontend.
- **R03:** configuración global autoriza techo máximo; tenant solo restringe dentro del techo; sucursal solo puede reducir; ninguna rama puede elevar rol o permiso.
- **R04:** opción ausente, nula, desconocida, inválida, caducada o no verificable = default deny/valor seguro, con evento de auditoría minimizado. No permitir \`true\` implícito.
- **R05:** se versiona cada snapshot con \`ConfigVersion\` opaca y timestamp UTC; cambios exigen actor autorizado, razón, diff allowlisted y comparación de versión esperada.
- **R06:** cachear solo con clave \`EmpresaId+version+option\` y TTL acotado; se invalida inmediatamente ante revocación o cambio de política de seguridad; un cache miss no implica allow.
- **R07:** no guardar JWT, secretos, credenciales o PII en registros/flags; logs incluyen actor/tenant opacos, versión, outcome, motivo y correlación.
- **R08:** límites \`int\` se validan en servidor con rango y valor máximo global; overflow, negativos, falsos booleanos textuales, arrays no allowlisted o duplicados fallan.
- **R09:** habilitar preview no concede escritura, exportación ni envío; cada operación requiere permiso backend separado.
- **R10:** \`ConsentRequired\` y opt-out prevalecen siempre sobre \`ContactDispatchEnabled\`; falta de consentimiento por finalidad/canal bloquea envío.
- **R11:** poner una flag a \`false\` bloquea nuevas operaciones, no borra historial transaccional, audit log o jobs existentes; la reversión se verifica.
- **R12:** ninguna modificación de CONFIG habilita migraciones, eleva claims V17, crea Tasks, cambia RRULE, elimina A5_FAIL, fuerza COLA/DAG o autoriza QA/main/PROD.

## 4. Compatibilidad, seguridad y rollout FUTURO

1. Preparación: inventariar opciones efectivamente existentes y rutas consumidoras; usar nombres de contrato distintos para propuestas sin implementarlas en esta unidad. Verificar no duplicar scopes JWT existentes.
2. Gate de seguridad: futuras opciones R2 que influyan en acceso exigen pruebas negativas tenant A/B, rol lector/escritor, revocación, \`EmpresaId=NULL\`, consentimiento/opt-out, export sin permiso, y bypass por frontend.
3. Despliegue gradual futuro: \`CrmEnabled=false\` global y por Empresa inicialmente; activar primero lectura mínima para tenant piloto autorizado, previa revisión A5 y pruebas API/DB; después funcionalidades cada una por permiso, no todas de golpe.
4. Validación de performance: medir latencia, LCP/INP/CLS solo en UI afectada, costos de consultas y cuotas, segmentación por tenant, con valores y denominadores; mientras tanto \`NO_MEDIDO\`.
5. Reversión: si falla un flujo, desactivar la flag concreta en ámbito global, invalidar caché y verificar que la denegación persista; nunca migración destructiva ni borrado de auditoría. La habilitación efectiva de proveedores/SMTP exige consentimiento, credenciales autorizadas y prueba causal separada.

## 5. Oráculos de aceptación FUTUROS: 12 pares P/N no ejecutados

| Par | Positivo futuro (en entorno autorizado) | Negativo o frontera que debe fallar |
| --- | --- | --- |
| P01/N01 | A habilitado lee su timeline A con JWT/membresía vigente | Empresa B o ID de Cliente B es denegado aunque O02=true |
| P02/N02 | O03=50 y Limit=20 producen una página válida | Limit=0, -1, 101 u overflow son rechazados |
| P03/N03 | O04=90 restringe la ventana temporal | lookback negativo o fecha incoherente se rechaza |
| P04/N04 | actor A con permiso escribe una actividad idempotente | mismo idempotency key distinta carga falla sin duplicar |
| P05/N05 | oportunidad A cambia de etapa allowlisted con versión vigente | etapa B, versión CAS obsoleta o rol lector falla |
| P06/N06 | preview de segmento permitido muestra agregados mínimos A | SQL/JS libre, segmento B o escaneo sin cuota se bloquea |
| P07/N07 | consentimiento A activo autoriza canal/finalidad específica | opt-out/membresía revocada bloquea aun O11=true |
| P08/N08 | permiso \`crm.export\` y O12=true permiten export mínimo A | permiso de lectura solo o columnas PII no permitidas fallan |
| P09/N09 | snapshot versionado se actualiza con CAS correcto | dos editores con misma versión dejan un solo ganador |
| P10/N10 | tenant A usa cache key A+versión sin contaminar B | cache key sin tenant o política revocada devuelve deny |
| P11/N11 | flag false corta nuevas actividades sin perder historial | fallo de cache/config no convierte un deny en allow |
| P12/N12 | legacy Cliente con owner reconciliado puede leerse autorizado | \`EmpresaId=NULL\` nunca se expone aunque O16 forjada true |

**Ejecución actual de estos 12 pares: NO_MEDIDO.** Estos son criterios de prueba para futuras microtareas R1/R2, no resultados de xUnit, Playwright, MySQL ni HTTP. En M03 habrá que comprobar calidad A0–A4 auténtica y A5 de otro Task: pruebas negativas JWT/RBAC/tenant, integración MySQL 8.4 **efímera si se toca EF/DB**, pruebas de navegador si cambia UI, Scope Lock exact-SHA y readback institucional.

## 6. Auditoría R0 y cierre acotado de esta unidad

- **A0**: alcance único \`C09-01.M02.04\`, autorización PLAN/DAG/COLA y V17 lock, revisión de fuentes M01/M02; 16 contratos O01–O16, 12 reglas R01–R12, 12 pares P/N FUTUROS y rollback documental; solo docs/v5 y CHANGELOG.
- **A1**: coherencia y fronteras de opciones tipadas, valores permitidos, casos de rechazo y transición; las pruebas futuras **no fueron ejecutadas** en R0.
- **A2**: inspección estática de JWT/RBAC/tenant A/B, revocación, consentimiento, \`Cliente.EmpresaId=NULL\`, IDOR, PII, CAS e idempotencia; NO_CERTIFICA_SEGURIDAD_RUNTIME.
- **A3**: \`NO_APLICA_R0\` a compilación .NET, Angular, API/HTTP, MySQL/EF, proveedores reales y Playwright porque no hay cambio ejecutable.
- **A4**: tras publicar solo los dos Markdown en dev, exigir \`verify-solqaryn-project-scope\` y \`V5 - Sin encabezados vacios ni atribucion cruzada\` \`completed/success\` en SHA exacto (o descendiente equivalente con blobs idénticos), readback H/I, BITACORA y diff, sin usar check \`in_progress\` como PASS.
- **A5**: \`PENDIENTE\` de revisión por otro TASK_ID entre las cinco Tasks Javier, distinto de \`6aeacb7820ac8191b7d768a6f33dfa09\`; LISTO_DEV de documento no equivale a A5_PASS.
- **Rollback**: \`git revert\` normal del commit específico, con HEAD y escritores revalidados; nunca force-push, reset, cambios destructivos ni borrar BITACORA.

**Resultado de esta entrega:** especificación de configuración y flags **conceptuales** preparada para futura implementación de CRM universal. Sin permisos nuevos, sin migración, sin mutar tenancies, sin QA/main/PROD.
