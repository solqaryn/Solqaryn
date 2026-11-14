# SOLQARYN V5 FINAL — C01-02.M02.06: dependencias tecnicas y contrato de salida de capabilities

**Naturaleza:** cierre de DISEÑO de la fase M02 de C01-02, no implementación de un evaluador de capabilities. **Autor:** SOLQARYN PRIMARY DEV 02 — JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Destino:** solqaryn/Solqaryn, exclusivamente dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

**Autoridad ejecutable:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). La unidad exacta exige: **fijar dependencias técnicas y contrato de salida del lote**. No agrega IDs, filas ni habilitaciones ficticias.

## 1. Evidencia del lote M02, con identidad de contenido inmutable

Los siguientes blobs de documentos existen en el árbol Git de dev al preflight f8aed3cc35930573294f6380c9801933494e1ce7. Los seis antecedentes constan como LISTO en PLAN_MAESTRO y sus revisiones A5 independientes están registradas en las columnas J/K/L; la condición es revalidable y puede cambiar posteriormente.

| ID y antecedente | Blob Git SHA-1 del documento de alcance | Propósito y límite |
| --- | --- | --- |
| C01-02.M01.06 | a00aaa117f26c567a995a3e3a2e6017d17f09686 | Bloqueos, rollback y puerta documental anterior |
| C01-02.M02.01 | 85344880fe45907bd9777dfdeb29dce1f07db3fb | Modelo conceptual de entidades y ownership; sin entidades EF nuevas |
| C01-02.M02.02 | 126802a61e74816785242c0fc815e36a70398139 | API/DTO propuestos; sin endpoints desplegados |
| C01-02.M02.03 | 32fbba1c338acc45d78a8bb66d727c8a05480132 | Persistencia e índices propuestos; sin DDL ni fixtures aplicados |
| C01-02.M02.04 | a4365613ff7ac601c744e967d2c47a4cae655ef5 | Configuración por plan/rubro/tenant y feature flags sin activación |
| C01-02.M02.05 | 32afe1c677de9a121e8f3a9925b3b36292e70418 | Envelope/idempotencia/correlación FUTUROS, sin dispatcher ni worker |

Los seis antecedentes representan **contratos de diseño**, no certificaciones de ejecución .NET, HTTP, JWT, MySQL, Playwright o activación comercial. No convertir documentación en autoridad por sí misma. Si cualquier blob deja de estar vigente, el preflight de M03 debe revalidar su equivalencia semántica y trazabilidad causal.

## 2. Dependencias y frontera de arquitectura

1. **Entrada M02:** M01.06 LISTO y A5 independiente causal, y M02.01–05 LISTO con A5 independiente por otro TASK_ID Javier. Los estados en PLAN, la unicidad del owner, COLA G/O y DAG F/G se comprueban por ID exacto, no por fotografías históricas.
2. **Dependencias exteriores:** conservar los precedentes declarados en PLAN_MAESTRO y DAG_TECNICO, incluida la capa C00 y el núcleo universal C01-01. Ninguna marca VALIDANDO documental permite el cierre M02.06 ni una implementación IAM/JWT/RBAC/tenant.
3. **Frontera Domain:** una capability identifica una acción/feature con ámbito y vigencia, nunca confiere permisos por simple etiqueta; la propiedad del recurso, pertenencia empresarial y rol efectivo se resuelven desde la fuente autoritativa vigente.
4. **Frontera Application:** un eventual evaluador debe calcular intersección segura entre capability habilitada, plan/rubro/tenant y permiso RBAC explícito, con denegación por defecto y auditoría de decisiones sensibles.
5. **Frontera API:** autenticar JWT y autorización server-side por tenant, rol, sucursal y recurso. Ningún claim cliente, DTO, ruta Angular o parámetro empresaId autodeclarado sustituye validación de pertenencia.
6. **Frontera Infrastructure:** cualquier tabla/índice de capabilities, migración EF/MySQL, control de concurrencia, caché o flag editable son trabajos posteriores con contrato técnico, pruebas efímeras reales y rollback separado.
7. **Eventos:** el diseño M02.05 permanece INACTIVO; NO implementar ni activar outbox, webhook, event dispatcher, cola, Tasks adicionales, brokers, pagos ni servicios externos por este documento.
8. **Consumos:** Angular puede mostrar affordances, pero no definir permisos; API y servicios de dominio deben denegar siempre el acceso si la membresía está revocada o el tenant no coincide. La lógica de plan no otorga autorización.
9. **Seguridad transversal:** secreto fuera de Git, JWT/RBAC/multitenancy A/B fail-closed, auditoría sin PII excesiva, rechazo de IDOR/BOLA, no cross-tenant cache y rate limiting proporcional.
10. **Compatibilidad:** no introducir endpoints ni cambiar firmas, esquemas o comportamiento existente de permisos/matriz o planes antes de una microtarea de implementación aprobada con CI dirigida.

## 3. Contrato de SALIDA de M02: evidencia exigida antes de tomar M03

La salida de M02 consiste sólo en un **paquete de contratos documentales revisables**: modelo y ownership; API/DTO y errores; persistencia/fixtures; configuración tipada; eventos futuros sin ejecución; y este documento de dependencias y retorno seguro. Su publicación no libera automáticamente ningún sucesor.

El preflight de cada ID M03 debe volver a confirmar: (a) PLAN H= PENDIENTE e I vacío, (b) COLA G=SI y O=SI, (c) DAG F=VALIDADO y G=SI, (d) todos los predecesores exigibles LISTO con A5 independiente causal y sin A5_FAIL vigente, (e) Scope Lock del HEAD exacto aprobado, (f) permisos de la Task y exclusión de escritores, (g) impacto en Domain/Application/Infrastructure/API, (h) pruebas específicas en GitHub-hosted .NET 10, MySQL efímero y seguridad tenant/JWT/RBAC y (i) rollback viable.

**Si falta cualquiera de esas pruebas, NO_GO para ese ID**, sin transformar un diseño en runtime ni relajar el gate de seguridad. Una microtarea M03 debe producir código, no otro documento que pretenda sustituirla. La revisión A5 independiente de M02.06 deberá realizarla una Task Javier distinta de su autora antes de habilitar cierre dependiente.

## 4. Oráculos de aceptación documental: no pruebas ejecutadas de runtime

**Positivos de inspección documental (P, objetivos FUTUROS_NO_EJECUTADOS):**

- P01: empresa A, usuario con membresía A activa, RBAC de recurso y plan vigente: decisión permitir sólo tras verificación server-side de todos los factores.
- P02: sucursal autorizada y recurso propio con capability visible: la UI únicamente informa y la API valida la pertenencia.
- P03: revocación de plan versionado: próximas evaluaciones vuelven a consultar versión y niegan operaciones retiradas.
- P04: eventual evento duplicado con misma clave tenant+operación: un consumidor futuro debe conservar resultado idempotente y correlación opaca sin reenvío duplicado.

**Negativos de inspección documental (N, objetivos FUTUROS_NO_EJECUTADOS):**

- N01 tenant B usando token de A; N02 usuario sin membresía activa; N03 rol ajeno a tenant; N04 recurso de otra empresa.
- N05 sucursal no asignada; N06 cliente manipula plan en DTO; N07 capability no declarada; N08 feature flag ausente o inválido.
- N09 plan vencido; N10 cache con clave sin tenant; N11 recurso inexistente con filtración de datos; N12 membresía revocada tras emitir JWT.
- N13 replay cruzado de operación/evento; N14 envelope sin versión; N15 provider de capabilities no disponible; N16 falla de concurrencia/rollback sin transacción.
  
Todos los N deben terminar en **denegación segura y auditoría proporcional** cuando se implementen; un documento no demuestra que los tests o endpoints existan. No registrar PASS de integración o pruebas reales a partir de estos oráculos.

## 5. A0–A5, observabilidad, revisión y rollback

- **A0:** verificar diff estrictamente documental, procedencia de seis blobs y ausencia de nuevas entidades/endpoints/tablas, secretos o encabezados vacíos en CHANGELOG.
- **A1:** contrastar por inspección las cuatro hipótesis positivas y los dieciséis rechazos definidos; estado **CONTRATOS_FUTUROS_NO_EJECUTADOS**, sin atribuir HTTP o ejecución real.
- **A2:** revisar límites JWT/RBAC, tenant A/B, sucursal, recurso, membresía, plan y PII; controles del servidor permanecen obligatorios, nunca sustituidos por UI.
- **A3:** consistencia de referencias con M01.06 y M02.01–05, sin declarar integración .NET/MySQL por solo Markdown.
- **A4:** para SHA publicado, fast-forward CAS, Scope Lock y V5 procedencia CHANGELOG completados SUCCESS, diff y blob readback; sincronizar PLAN/COLA y registrar evento BITACORA A:Z idempotente.
- **A5:** PENDIENTE hasta auditoría técnica independiente por otra de las cinco Tasks Javier; A5 no autoriza QA/main/PROD.

**Rollback del changeset documental:** revertir con git revert normal sobre dev revalidando HEAD, sin force-push, conservando eventos históricos y agregando únicamente evento compensatorio si procede. Para M03 futuro: rollback de código/migración aislado con pruebas y transacciones demostradas, no presumido desde este documento.

**Observabilidad y seguridad:** versionar evidencias por SHA, no incluir secretos, tokens, PII ni datos tenant reales en trazas; los estados oficiales provienen de PLAN_MAESTRO y COLA, y BITACORA es append-only. Si CI pertinente sigue queued/in_progress, mantener esa unidad VALIDANDO sin PASS ficticio; otra candidata independiente puede continuar.
