# SOLQARYN V5 — C04-02.M01.06: barreras, rollback y condición de liberación de contratos SaaS

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER (`6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-15. **Rama:** `solqaryn/Solqaryn@dev`. **Control:** `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** contrato de preparación documental M01.06. **No constituye motor contractual implementado, certificación de cobros ni aprobación comercial.**

## 1. Autoridad y cadena causal para M01.06

La única autoridad ejecutable es el Google Doc `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). `PLAN_MAESTRO!A619` identifica `C04-02.M01.06`, «Definir bloqueo, rollback y condición de liberación del lote». Resultado contractual deseado de la etapa: alta, renovación, pausa, cancelación, morosidad y cierre contractual con transiciones inválidas rechazadas; aquí sólo se especifican puertas y pruebas **futuras**.

**Preflight de la reclamación:** `PLAN H619=PENDIENTE`, `I619` vacío, `COLA G619=SI/O619=SI` y `DAG_TECNICO F580=VALIDADO/G580=SI`; después el claim exclusivo institucional cambió H a `EN_PROGRESO`, conservando autor/owner. Las puertas requeridas son los cinco documentos M01.01–M01.05 en LISTO con A5 de Task Javier distinta de su autora y `C04-01.M01.06` LISTO/A5 independiente. No se supone ninguna otra puerta por texto; las fórmulas del Sheet gobiernan la elegibilidad.

| Evidencia previa | Alcance acreditado | Frontera no acreditada |
| --- | --- | --- |
| `C04-02.M01.01` y `.02` | Objetivo/no regresión, actores y superficies; revisiones A5 independientes J40 | Políticas económicas y autorizaciones HTTP reales |
| `C04-02.M01.03` | Doce blobs de código/DTO/API/EF/xUnit/Angular revisados y A5 independiente J40 | Ejecución de SQL, JWT, HTTP o Playwright |
| `C04-02.M01.04` | R01–R12, S01–S08 y D01–D08 documentados; A5 independiente J40 | Decisiones D01–D08 **siguen sin aprobación** |
| `C04-02.M01.05` | AC01–AC14 y N01–N14 definidos; A5 independiente J40 | Oráculos futuros **no ejecutados** |
| `C04-01.M01.06` | Contrato de dependencias preliminares LISTO/A5 por Javier distinto | Autorización de cobros, producción o nuevos proveedores |

El código previo de suscripciones SaaS, entre otros, se identifica en `SuscripcionesSaaS.cs` (blob `d9b5dbe19fb728293b00b748f10ac10336debd2d`), `SuscripcionesSaaSService.cs` (`bb501285f11cd4b32709a58a2c51efa19c3664ea`), `SuscripcionesSaaSController.cs` (`cab95db5a33c6a15448e2437481eaaac25ffc4fe`), `SuscripcionesSaaSConfiguration.cs` (`16631a0155a449ab71e7479c92e8ba8804303249`) y `SuscripcionesSaaSServiceTests.cs` (`090bf81e7a8f08e86dcdc7ddf5b0c20625ae2839`). Estos blobs describen **suscripciones técnicas**, no contratos legales ni facturación ejecutada. Una nueva fase debe releer los blobs vigentes y demostrar equivalencia o revalidar.

## 2. Barreras G01–G16, todas exigibles antes del efecto correspondiente

| Gate | Condición objetiva y responsable de acreditarla |
| --- | --- |
| G01 | Identidad de empresa y miembro validada por backend antes de cualquier consulta/escritura; casos A/B y miembro revocado negativos |
| G02 | JWT auténtico y RBAC de acción contractual independiente del plan/entitlement; rol y alcance **aprobados**, no inventados |
| G03 | D01 autoridad de aceptación y representación legal/comercial documentada y autorizada |
| G04 | D02 contrato versionado, términos y fecha efectiva inmutables con trazabilidad de aceptación |
| G05 | D03 moneda, impuestos, tarifa, periodicidad y jurisdicción aprobadas; nunca valores por defecto ficticios |
| G06 | D04 reglas de renovación, aviso, vencimiento y no retroactividad autorizadas |
| G07 | D05 definición de mora, días de gracia y suspensión basada en conciliación; timeout no demuestra impago |
| G08 | D06 proveedor/método de pago y consentimiento explícito: nueva autorización antes de cualquier cargo real |
| G09 | D07 cancelación, prorrateo, reembolso y preservación legal/histórica definidos y aprobados |
| G10 | D08 evento, auditoría, retención y restauración autorizados; no habilitar dispatcher, webhook ni worker por documentarlos |
| G11 | Máquina de estados Domain ensayada: Activa/Suspendida/Cancelada/Expirada y transiciones inválidas; no reactivar cerradas |
| G12 | Idempotencia transaccional por tenant/acción/clave, conflictos payload y dos solicitudes concurrentes acreditados con MySQL efímero |
| G13 | EF Core/MySQL 8.4 con índices, migraciones aditivas, pruebas Category=Integration y reversa causal; **no Aiven productivo** |
| G14 | API 401/403/404/409, errores sin datos tenant B y controles de autorización probados sobre el SHA de la implementación |
| G15 | Angular accesible: foco/teclado/estados de error; UI nunca concede privilegios ni convierte suscripción técnica en pago |
| G16 | A0–A4 pertinentes, GitHub Actions completed/success por SHA exacto o equivalencia auditada, A5 por otra Task Javier, Scope Lock STRICT y autorización diferenciada antes de QA/main/PROD |

**Consecuencia:** una barrera sin evidencia es `NO_GO` **sólo para su transición de efecto**. Este contrato M01.06 documental puede cerrarse con A0–A4 documental sin convertir G01–G16 de runtime en PASS. Las decisiones D01–D08 permanecen explícitamente abiertas; su mera enumeración no las aprueba.

## 3. Tabla de transiciones futuras con denegación por defecto

| Estado o acción solicitada | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| Alta | Empresa autorizada, versión y consentimiento aprobados, una alta única | Empresa B, actor revocado, término no aprobado, replay distinto y doble alta |
| Activa → Suspendida | Motivo autorizado, actor y UTC conservados | Estado no Activa, permiso ausente o mora inferida de timeout |
| Suspendida → Activa | Revalidación membresía/política, expediente económico conciliado | Cancelada o Expirada, contrato de B, mora no resuelta |
| Activa → Cancelada | Inmutabilidad de historial, motivo, referencias y fecha | Borrado físico de vínculos, refund implícito o fecha anterior al inicio |
| Renovación | Nueva versión y periodo posteriores con precio autorizado | Renovación retroactiva, doble cargo, moneda/tarifa no aprobadas |
| Morosidad | Ledger confiable reconciliado y política D05 aprobada | Callback tardío/no firmado, proveedor indisponible o deuda de otro tenant |
| Cierre | Estado terminal auditable con retención y reversas pertinentes | Eliminación de eventos previos, pérdida de referencias o acceso UI privilegiado |

Los positivos y negativos son **especificación de tests**, no resultados de xUnit, HTTP, MySQL, Playwright ni pruebas de aceptación ejecutadas durante esta M01.06.

## 4. Ocho disparadores ROLLBACK01–08 y respuesta permitida

| Disparador verificable | Acción segura prevista |
| --- | --- |
| ROLLBACK01 — denegación A/B falla | Detener nueva transición, preservar evidencia/redacción y revertir changeset aditivo |
| ROLLBACK02 — cambio de RBAC concede más permisos | Denegar por defecto, restaurar política anterior, no migrar grants |
| ROLLBACK03 — repetición duplica contrato/cargo | Suspender operación, conciliar ledger; no reintentar cobro ciegamente |
| ROLLBACK04 — migración/índice MySQL falla | Transacción o plan de retorno versionado previamente probado con DB efímera, nunca DROP destructivo improvisado |
| ROLLBACK05 — callback de proveedor no autenticado | Rechazar/retenener sin efectos ni exposición de secretos |
| ROLLBACK06 — estado o importe histórico mutado | Detener nuevos cambios y reconciliar sólo con evidencia inmutable; nunca borrar histórico |
| ROLLBACK07 — API/Angular confunde autoridad | Ocultar affordance y rechazar en servidor; restaurar mensaje seguro sin autorizar por UI |
| ROLLBACK08 — CI Scope Lock, seguridad, integración o A5 FAIL | Conservar estado real VALIDANDO/BLOQUEADO, investigar logs y reparar; no liberar dependencias falsas |

Para **este documento** el rollback es `git revert` ordinario del changeset Markdown+CHANGELOG en `dev`, respetando HEAD concurrente y sin reescritura de BITACORA. No ejecutar ninguna reversa SQL o financiera por haber escrito esta tabla.

## 5. Criterios de aceptación de la microtarea documental (separados del producto)

- **A0 estática:** fuente comercial, identidad, alcance único M01.06, dependencias/COLA/DAG, doce referencias de código y límites comprobados; diff planeado de dos Markdown.
- **A1 funcional proporcional:** coherencia interna verificable de 16 gates, siete líneas de transición y sus positivos/negativos *futuros*; no registrar xUnit como PASS.
- **A2 seguridad:** documentación explícita fail-closed para JWT/RBAC/tenant A/B, consentimiento, PII, idempotencia y denegación; sin alterar aplicación.
- **A3 integración proporcional:** referencias a Domain/Application/API/Infrastructure/Angular y a M01.01–.05, sin operar MySQL/EF, proveedor, billing o eventos; runtime `NO_APLICA` porque sólo cambia Markdown.
- **A4 publicación:** dos archivos Markdown (este documento y sección autocontenida CHANGELOG), commit fast-forward/CAS, `verify-solqaryn-project-scope` y check de procedencia V5 en `completed/success` del SHA publicado, PLAN/COLA/BITACORA con readback. `queued/in_progress/skipped/cancelled/failed` no son PASS para los controles requeridos.
- **A5_PENDIENTE:** otra de las cinco Tasks Javier distinta del autor debe revisar SHA/artefacto y decidir con evidencia. Ni A0–A4 ni A5 por sí solos autorizan QA, main o PROD.

**Salida disciplinada:** `LISTO_DEV` de M01.06, sólo si A0–A4 documentales y readback son reales, **no** certifica el resultado total del bloque C04-02 ni libera M02 automáticamente: reconsultar DAG, PLAN, COLA y A5 independientes. Cero nuevas APIs, permisos, cargos, migraciones, workers, eventos o proveedores activados.
