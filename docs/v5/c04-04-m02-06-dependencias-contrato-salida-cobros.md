# SOLQARYN V5 — C04-04.M02.06 | Dependencias técnicas y contrato de salida de M02 (cobros)

**Responsable:** SOLQARYN PRIMARY DEV 03 — JAVIER, Task `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Rama:** `solqaryn/Solqaryn@dev`. **Control:** `PROJECT_SCOPE_LOCK=STRICT`. **Corte de diseño:** 2026-11-19.

**Clasificación:** entrega documental M02 con consecuencias financieras potenciales (tratar decisiones de implementación como R2). No implementa ni activa pagos, webhooks, cargos, refund, ledger, migración EF, MySQL, JWT, proveedor, secrets ni QA/main/PROD. Ninguna decisión comercial se presume resuelta por un documento.

## 1. Fuentes y dependencias exactas

El plan institucional exige «Fijar dependencias técnicas y el contrato de salida del lote». La fila `C04-04.M02.06` se habilitó sólo después de verificar PLAN H=PENDIENTE, I vacío; COLA G/O=SI; DAG F=VALIDADO/G=SI. Su DAG exige **C04-04.M01.01–06+A5** y **C04-04.M02.01–05+A5**; la revisión independiente correctiva de M02.03 aprobó únicamente el diseño documental sobre SHA `414eb48e9b1d041e6f125f8353fa0f39d5c0b1b6`, preservando el A5_FAIL previo. Esta condición habilita M02.06, **no** M03 ni operaciones reales.

| Fuente versionada en dev | Función para el contrato de salida | Control obligatorio |
| --- | --- | --- |
| `docs/v5/c04-04-m01-06-gates-rollback-liberacion-cobros.md` | Límites G01–G10, D01–D10 y condiciones de rollback | No convertir un gate documental en permiso para producción |
| `docs/v5/c04-04-m02-01-entidades-ownership-invariantes-cobros.md` | Entidades, ownership y invariantes A/B | Cada factura, transacción y reembolso pertenece a una empresa comprobada |
| `docs/v5/c04-04-m02-02-contratos-api-dto-errores-cobros.md` | API/DTO, errores saneados y compatibilidad | Sólo contratos futuros; no afirmar endpoints ejecutables |
| `docs/v5/c04-04-m02-03-persistencia-indices-fixtures-cobros.md` | Esquema conceptual, unicidad durable y rollback | Identidad `(EmpresaId,ProveedorId,Operacion,IdempotencyKey)` separada de `PayloadDigestCanonico`; conflicto 409 sin segundo cobro |
| `docs/v5/c04-04-m02-04-configuracion-tipadas-flags-cobros.md` | Configuración por ámbito, overrides y feature flags | Fail-closed y defaults sin precio/moneda/PSP implícitos |
| `docs/v5/c04-04-m02-05-eventos-idempotencia-correlacion-limites-cobros.md` | Envelope, correlación, antifraude, retries y límites | Verificación HMAC y deduplicación transaccional futuras, sin usar colas reales aquí |

El autor del cambio de M02.03 es DEV06, el reparador DEV01 y su A5 correctiva DEV03; esas tres atribuciones son distintas y siguen auditadas. El cierre **de M02.06** necesitará su propia A0–A4 y una **nueva A5 por otra Task Javier**, distinta de DEV03.

## 2. Contrato de salida de M02 — comprobaciones, no hechos productivos

Antes de declarar completo el lote: (E01) M02.01–06 deben figurar LISTO y A5_PASS independiente aplicable a la versión causal; (E02) las seis fuentes se mantienen rastreables y no contradicen el código real sin inventariarlo; (E03) D01–D10 sobre comercial, tarifas, proveedor, moneda, impuestos, caducidad, reversión y responsabilidad están registradas como PENDIENTES cuando proceda; (E04) todo cambio R2 queda restringido a dev con Scope Lock, seguridad y CI por SHA; (E05) sólo una escritura por microtarea con V17, owner preservado y BITACORA append-only.

El estado `VALIDANDO`, `A5_PENDING` o `A5_FAIL` de cualquier predecesora impide certificar su puerta. No editar manualmente fórmulas `DAG_TECNICO!G` ni `COLA!G/O`; recalcular desde PLAN, evidencias y A5. `LISTO_DEV` significa alcance dev demostrado y **no** aprobación de QA, `main`, PROD, migraciones reales ni integración PSP.

## 3. Handoff técnico condicionado para M03 (NO AUTORIZA ACTIVACIÓN)

| ID futuro | Artefacto mínimo a producir | Prueba adversarial y rollback requeridos |
| --- | --- | --- |
| M03.01 Reglas de dominio server-side | Validaciones de moneda, monto minor-unit, invoice ownership, factura pagable e idempotencia | Negativos A/B, factura ajena, importe cambiado y revocación; xUnit con límites/error |
| M03.02 Servicio/caso de uso | Interfaz provider-neutral, decisiones de estado y límites de efectos | Timeout ambiguo sin segundo cargo, reintento/replay; no asumir exactly-once distribuido |
| M03.03 Autorización/tenancy | JWT/RBAC, permiso financiero y empresa/factura verificados en cada acción | Token inválido, rol lector, A->B, cabecera falsificada y revocación; 403/404 sin fuga |
| M03.04 Persistencia/adaptador | EF/MySQL 8.4 efímero, índice único estable, digest separado, migración reversible | Dos conexiones compiten misma clave; distinto digest 409; rollback, índices y 0 FAIL de integración |
| M03.05 Worker/proveedor/cola | Adaptador aislado **sólo si** el alcance y permisos efectivos lo autorizan | HMAC/timestamp/replay, reintento acotado, orden y dedupe; NO conectar proveedor de pagos real sin permiso comercial específico |
| M03.06 Auditoría/log/error | Eventos trazables y saneados; errores tipados sin contexto ajeno | Fallos no exponen JWT, HMAC, PAN/CVV, firma ni información de empresa B; verificar CI y rollback |

Cada M03 requiere su propia lectura de PLAN/COLA/DAG, R0–R3, límites del scope y candado V17 **cuando realmente sea elegible**. En especial, un cronograma de diseño no es permiso para instalar migraciones sobre Aiven/PROD, cobrar, enviar correos o conectar cuentas de pago. Si el cambio rebasa dev/R3, bloquear sin escalamiento implícito.

## 4. Matriz P/N documental de aceptación posterior — NO EJECUTADA

| ID | Positivo verificable FUTURO | Negativo que debe impedir avance |
| --- | --- | --- |
| G-M02-01 | Seis entregas M02 con A5 de otras Tasks, SHA/owner trazables | Predecesora VALIDANDO o A5_FAIL libera M03 |
| G-M02-02 | Tenant A repite clave K con payload igual y obtiene mismo intento | Misma K con factura/importe B o distinto produce otro cargo en lugar de 409 |
| G-M02-03 | Dos workers compiten con índice UNIQUE persistente y un único intento | Lock en memoria o SELECT+INSERT sin unicidad permite doble efecto |
| G-M02-04 | Webhook firmado se valida antes de conciliación | Callback falso, replay o navegación browser marca SETTLED |
| G-M02-05 | Fallo PSP ambiguo conserva AMBIGUOUS y requiere conciliación real | Timeout genera reintento ciego o refund sin compensación |
| G-M02-06 | Auditoría conserva correlación saneada y permisos por rol/tenant | Logs con tokens, PAN/CVV, HMAC o acceso cruzado A/B |
| G-M02-07 | CI dirigida evidencia artefactos y SHA exacto, con readback PLAN/BITACORA | SKIPPED, CANCELLED o IN_PROGRESS se equipara a SUCCESS |
| G-M02-08 | Rollback de código mediante revert aditivo probado en sandbox | Reescritura de historial, migración live sin backup o alteración de QA/PROD |

Estos casos son **criterios futuros**, no resultados xUnit, HTTP, MySQL, navegador ni PSP de esta entrega. Ante ausencia de fuente real, evidencia = NO_MEDIDO, no PASS.

## 5. A0–A5, seguridad y recuperación

**A0:** comparar el alcance M02.06 y las seis fuentes predecesoras contra PLAN, revisar diff sólo este Markdown y sección autocontenida en `CHANGELOG_AI.md`, sin secretos. **A1:** revisar consistencia de E01–E05, seis handoffs M03 y ocho pares P/N de diseño; los ejemplos no son tests ejecutados. **A2:** mantener exigencia tenant A/B, JWT/RBAC, revocación, HMAC, límites, índice durable y redacción de datos; **no certificar seguridad runtime**. **A3:** lectura de documentos y DAG; .NET/MySQL/PSP/Chromium = **NO_APLICA a este diff documental**. **A4:** commit fast-forward/CAS de `dev`, `verify-solqaryn-project-scope` y V5 exact-SHA `completed/success`, BITACORA/PLAN/COLA/DAG readback. **A5:** revisión genuina de otra Task Javier distinta de la autora DEV03, incluyendo discrepancias entre fuentes y cobertura; puede rechazar.

Reversión de la entrega: `git revert` normal del commit documental tras reevaluar HEAD, conservando cambios de otros y toda BITACORA; no reset, no force-push, no borrar eventos A5_FAIL. Nunca tocar QA/main/PROD, claves, cobranza ni proveedores.
