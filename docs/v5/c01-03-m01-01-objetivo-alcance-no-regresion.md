# SOLQARYN V5 FINAL — C01-03.M01.01: objetivo, no-alcance y no-regresión de configuración dinámica

**Clasificación:** TIPO 1, preparación documental M01 exclusivamente. **Estado de implementación:** no implementado ni certificado en runtime por esta unidad. **Fuente ejecutable:** Standard de Implementación de Automatizaciones V5 FINAL, PLAN_MAESTRO y COLA institucionales. **Puerta DAG:** C01-03.M01.01 F=VALIDADO/G=SI; sólo preparación documental independiente M01.01–05. Los seis C00-01.M02.01–06 y cinco C00-03.M01.01–05 aparecen LISTO al readback de esta unidad.

## 1. Objetivo delimitado

Registrar el objetivo y sus condiciones falsables para una futura configuración dinámica segura: **separar valores de configuración autorizados del código ejecutable** y definir su lectura y modificación con tipo, ámbito, identidad y versionado explícitos, sin convertir datos del cliente en privilegios. Las claves permitidas deberán tener propietario funcional, tipo (booleano, entero acotado, enumeración o texto seguro), restricción, default seguro, audiencia y política de auditoría. El contrato definitivo, API, MySQL y pruebas de inyección reales pertenecen a fases ejecutables posteriores y no se dan por terminados aquí.

El resultado atómico de **esta** microtarea es el acuerdo documental sobre objetivo, no-alcance y reglas de no-regresión. Los documentos hermanos C01-03.M01.02–05 desarrollan actores, contratos, riesgos y aceptación; su mera existencia no certifica que haya una solución implementada.

## 2. Superficies y fronteras de confianza

| Superficie | Objetivo futuro, no estado acreditado | Frontera obligatoria |
| --- | --- | --- |
| Backend .NET 10 / Application y API | Validación tipada, autorización por capacidad/rol, errores controlados y semántica de versión | JWT y membresía real resueltos por servidor; no confiar en EmpresaId, slug o cabecera aportados |
| Infrastructure / EF Core y MySQL | Lectura/escritura aislada por empresa y transacciones auditables | Empresa A no puede consultar ni editar B, ni acceder a secretos o configuración global |
| Frontend Angular 22 | Presentación de valores autorizados, formularios seguros y manejo de estado | UI y guards nunca sustituyen controles del backend |
| Workers o integraciones | Consumo de configuraciones autorizadas sólo si el caso real lo exige | Identidad de servicio, ámbito y fallback verificados; no se habilita un nuevo proveedor por este documento |
| Auditoría | Huella de actor, clave, versión, ámbito y resultado sin material sensible | No registrar JWT, contraseñas, PII ajena ni secretos |

Las rutas, DTO y entidades concretas se derivarán del código vigente y los contratos de C01-01 antes de programar. Ningún componente nuevo se presume existente por esta tabla.

## 3. Fuera de alcance de C01-03.M01.01

No crear ni modificar componentes Angular, endpoints, servicios, DTO, tablas, migraciones, Docker, workflows, permisos, roles, JWT, secretos, Cloudinary, SMTP ni infraestructura. No cambiar registros de configuración en base de datos ni activar flags, evaluadores de expresiones o valores que ejecuten JS, SQL, HTML/CSS arbitrario o URL `javascript:`. No migrar archivos de imágenes inexistentes. No realizar despliegues, promoción a QA/main/PROD, pruebas destructivas ni decisiones de proveedores. No declarar C01-03.M01.06, M02, M03, M04, M05 ni M06 liberados por adelantar esta preparación.

## 4. Condiciones de no-regresión para futuras entregas

| ID | Invariante / caso falsable | Evidencia futura necesaria |
| --- | --- | --- |
| NR-01 (+) | Administrador de empresa A con rol/capacidad válida cambia únicamente clave allowlisted de A, con valor del tipo/rango permitido | Prueba API + lectura de A y auditoría, cuando exista endpoint real |
| NR-02 (+) | Lectura autorizada aplica default/fallback y versión sin exponer campos sensibles | Caso positivo de contrato y persistencia, si hay DB |
| NR-03 (-) | Actor de A intenta forzar empresa B mediante ID, slug, cookie, cabecera o payload | 401/403/404 seguro según contrato, sin lectura ni mutación de B; test backend real |
| NR-04 (-) | Usuario sin membresía/capacidad, token inválido o trabajador fuera de ámbito intenta leer o cambiar configuración | Denegación por defecto, sin elevación, con auditoría no sensible |
| NR-05 (-) | Clave desconocida, tipo nulo/no permitido, número fuera de límites o versión obsoleta | Error seguro, no escritura parcial, no overwrite concurrente |
| NR-06 (-) | Valor contiene `<script>`, `javascript:`, SQL literal, ruta de archivo o payload grande | Rechazo mediante validación tipada, nunca ejecución/inyección |
| NR-07 (-) | Cambio de tenant o cierre de sesión durante respuesta asincrónica | UI descarta respuesta anterior; ningún dato o tema de A persiste en B |
| NR-08 (-) | Un feature flag o configuración de UI intenta deshabilitar RBAC, rate limiting, auditoría o Scope Lock | Control de backend y checks obligatorios conservados |
| NR-09 (control) | Reversión o pérdida del proveedor de configuración | Default seguro y rollback aditivo; no borrar datos comerciales legítimos |
| NR-10 (control) | Registro A5 o transición comercial apunta a evento incompleto | BITACORA A:Z, 26 campos, EVENT_ID e IDEMPOTENCY_KEY únicos; revisión A5 por TASK_ID distinto |

**Criterio de aceptación de M01.01:** este documento debe expresar objetivo delimitado, superficies, exclusiones y al menos un control positivo y uno negativo de tenant, inyección, concurrencia, cierre y rollback, con límites claros entre diseño y pruebas todavía pendientes. La revisión documental comprueba la presencia, coherencia y trazabilidad de esos criterios; **no representa PASS de tests API, MySQL, E2E o inyección en runtime**.

## 5. Fuentes, dependencias, gates y fases siguientes

- Fuentes: Google Doc V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN_MAESTRO H autoridad, COLA E/G/O fórmulas, DAG_TECNICO estado VALIDADO, BITACORA A:Z).
- Gobierno: `.agents/skills/solqaryn-project-governance/SKILL.md`, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_V13.md` en `solqaryn/Solqaryn@dev`; arquitectura viva prevalece sobre diseños futuros.
- Dependencias técnicas verificadas de DAG: C00-01.M02.01–06 y C00-03.M01.01–05 LISTO; el gate anticipa **documentación M01.01–05 únicamente**. Revalidar C00-01 final, C01-01 y nueva evidencia antes de diseñar contratos finales o implementar.
- Clasificación de pruebas para este cambio documental: `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` (fronteras JWT/RBAC/tenant, inyección y secretos analizadas), `requiere_scope_lock=SI` para el SHA exacto del commit. A0–A3 son revisiones documentales con casos positivos y negativos explícitos, no ejecuciones técnicas inexistentes.
- A4 sólo tras commit directo fast-forward en dev: diff/árbol final, SHA, check Scope Lock SUCCESS exact-SHA, readback PLAN/COLA/BITACORA y rollback por revert normal. A5_PENDIENTE de revisor distinto; ningún A5_PASS implícito. No editar QA/main/PROD.

**Rollback de esta entrega:** revertir solamente el commit documental con Git revert normal sobre dev, revisando HEAD y trabajos concurrentes; nunca reset ni force-push. Ningún dato de clientes, secretos, base de datos o configuración activa fue modificado por el alcance de este documento.
