# SOLQARYN V17 — C13-04.M03.03 autoridad actual y denegación por defecto de auditoría visual

**Alcance:** fuente autorizada: Standard V5 FINAL, PLAN_MAESTRO C13-04.M03.03, `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Autor: PRIMARY DEV 01 (Task ID 6adbb9fff1548191a1bd5b30d9e9984a). Riesgo **R2** por tenant, permisos y revocación. Predecesoras documentales y M03.01/.02 con A5 independiente, DAG F=VALIDADO/G=SI; CLAIM exclusivo con candado V17 determinista `2000001996`.

## A0 — Código, límites y recuperación
- Se introduce `C1304VisualReviewAuthorityAdapter` en Infrastructure .NET 10, conectado exclusivamente al caso Application existente de consistencia visual y sus reglas Domain.
- `IC1304VisualReviewAuthoritySource` es un puerto hacia futura autoridad backend **actual**; su implementación debe comprobar identidad, membresía, rol y revocación en la fuente segura. **No se registra un lector real ni DI, middleware, JWT, endpoint, base de datos, migración, worker, exportador, auditoría durable o proveedor externo**.
- El adapter valida primero alcance de solicitud, compara tenant solicitado con tenant **emitido por fuente verificada**, SHA ordinal exacto, vigencia de autorización de hasta 15 minutos, rol/permiso actual y catálogo estricto de superficies aprobado del lado servidor (1–64 claves seguras). En caso de falla retorna solo código opaco y conteo cero; propaga cancelación.
- Si cumple, llama el servicio Application + Domain, que rechaza tenant B, PII, pantalla no autorizada, evidencia insegura, SHA distinto, lote inválido y navegación sin señales suficientes. La respuesta aceptada exige revisión de navegador independiente; **nunca concede acceso real, JWT/RBAC, producción, persistencia ni certificación WCAG**.
- Impacto en arquitectura: Domain y Application sin cambios; Infrastructure depende de Application y Domain ya referenciado; tests xUnit aislados sin red ni MySQL. Rollback normal: git revert del commit publicado en dev, preservando BITACORA.

## A1–A3 — Validación causal requerida
- **12 pruebas xUnit reales**: positivo no autorizante, tenant A/B y spoofing, backend inexistente e identidad inválida, membresía/permiso/revocación/fuente no confiable, caducidad y vigencia límite, SHA ordinal, allowlist vacía/insegura, evidencia cross-tenant/PII, 65 observaciones, error opaco del proveedor, cancelación, solicitudes inválidas sin lectura de autoridad.
- Backend Release xUnit, Build y Seguridad multiempresa deben finalizar completed/success sobre SHA publicado. Para cambios al control de acceso, se inspecciona Authorization/tenant gate pertinente. Scope Lock y procedencia V5 SUCCESS exact SHA, sin aceptar queued/cancelled/skipped como PASS.
- A3 = integración **real en proceso** entre Infrastructure, Application y Domain con fuente falsa de pruebas xUnit. **No** se acreditan HTTP/JWT reales, MySQL EF 8.4, Playwright, QA/main/PROD, autorizaciones comerciales, ni seguridad de proveedor externo: ninguno está conectado por este diff.

## A4–A5 — Publicación, estados y límites
- Una sola entrega atómica de C13-04.M03.03 con diff de fuente, 12 xUnit, este documento y CHANGELOG_AI.md; transicionar primero EN_PROGRESO → VALIDANDO antes de verificación A1–A4. Si la CI externa está ejecutándose mantener estado, owner y RUN/SHA; realizar REVIEW_FIRST al concluir.
- A5 debe efectuarla otra Task Javier con TASK_ID distinto del autor, leyendo código, pruebas, SHA, errores, tenant A/B y casos negativos. No se presume A5_PASS ni se otorga QA/main/PROD.
