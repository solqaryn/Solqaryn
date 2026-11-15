# SOLQARYN V17 — C01-03.M03.03: autorización actual, tenant y denegación por defecto para configuración

**ID:** C01-03.M03.03 (PLAN fila 232). **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID \`6af16bbad26881918c283abe06e1f32a\`. Rama \`dev\`, PROJECT_SCOPE_LOCK=STRICT. **Riesgo V15:** R2 (JWT/RBAC, tenant, configuración y concurrencia).

## A0 — Implementación y dependencias técnicas

Se agrega un gate Application \`C0103DynamicConfigurationAuthorizationGate\` y sus puertos \`IC0103TrustedConfigurationActorSource\` y \`IC0103TenantConfigurationCatalogSource\`. El contrato exige en cada revisión un actor **actual** de backend, JWT verificado, membresía y grant RBAC relacional, entitlement vigente y no revocación; el catálogo se lee por tenant autenticado y clave exacta, con propiedad persistida, versión y SHA. La clave canónica se compara ordinalmente, conservando la reparación P2 de C01-03.M03.01 y su regresión. Compone \`V5DynamicConfigurationApplicationPreflight\` (M03.02) y \`V5DynamicConfigurationDomainRules\` (M03.01) sin invertir dependencias.

**Límites:** este código .NET 10 ejecuta revisión de autoridad y política, pero solo con fuentes que una implementación posterior suministre. No crea DI/API, lector JWT vivo, guard de ruta, tabla, migración, transacción durable CAS, secreto, feature flag de plataforma, worker, proveedor ni permiso real de escritura. **La respuesta positiva siempre exige adaptador autenticado duradero y A5.** El cliente nunca puede seleccionar tenant de persistencia o declarar grant. Rollback: \`git revert\` convencional tras reconciliar HEAD, no reset/force-push.

## A1 — Pruebas funcionales reproducibles

\`C0103DynamicConfigurationAuthorizationGateTests\` contiene doce xUnit Fact con rutas positivas y negativas: nominal no autorizante, entrada nula/clave/SHA/versión inválidos, intento plataforma y bypass cliente, identidad/revocación/RBAC/entitlement y membresía actual, tenant B, catálogo sin owner/versión/SHA, regresión \`HashSet(StringComparer.OrdinalIgnoreCase)\` que no debe ampliar claves canónicas, versión stale, tipo/valor inseguro, clave JWT reservada/definición secreta, error opaco de fuente, cancelación y dependencias nulas. La corrida real Release y seguridad del SHA publicado determina PASS/FAIL; no se certifica desde el texto.

## A2 — Seguridad y aislamiento

Para toda fuente ausente o insegura: DENY. JWT, membership, RBAC, entitlement y ownership tienen que venir del backend; la API externa futura deberá revalidarlos antes de cualquier mutación. Un catálogo de tenant B o una clave con comparación laxa se rechazan; el control de plataforma es otra autorización separada. Las salidas de decisión no exponen valores de config, PII ni secretos, ni conceden permisos.

## A3 — Integración aplicable

La compilación/pruebas xUnit de Application→Domain son reales y obligatorias, junto con seguridad multiempresa y autorización en GitHub Actions del SHA causal. Esta entrega no modifica EF, MySQL, controlador HTTP, Angular, Playwright ni proveedor: sus integraciones LIVE son NO_APLICA **solo a este changeset aislado**, nunca equivalen a certificación de configuración end-to-end.

## A4/A5 — Handoff y límites de despliegue

La publicación se hace mediante fast-forward/CAS sobre HEAD dev, 4 archivos autocontenidos incluyendo CHANGELOG. Después pasar PLAN a VALIDANDO antes de contar A1–A3 CI; revisar Scope Lock exact-SHA, procedencia V5, backend Release, build, seguridad y autorización. Registrar PLAN/COLA/DAG/BITACORA A:Z con EVENT_ID idempotente y readback, liberar candado V17. Solo A0–A4 verdaderos permiten LISTO_DEV; A5 revisora debe ser otra de las cinco Tasks Javier y no DEV06. No QA/main/PROD ni configuración física de Tasks, secretos o base de datos.
