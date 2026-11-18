# SOLQARYN V19 — C09-02.M02.06: dependencias técnicas y contrato de salida M02 de fidelización y reputación

**Microtarea:** \`C09-02.M02.06\`; **PLAN_MAESTRO:** fila 1345; **ejecutor:** PRIMARY DEV 01, TASK_ID \`6adbb9fff1548191a1bd5b30d9e9984a\`; **rama:** \`solqaryn/Solqaryn@dev\`; **PROJECT_SCOPE_LOCK:** \`STRICT\`; **riesgo V15 de esta entrega:** R0 exclusivamente documental. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`, PLAN_MAESTRO/COLA/DAG_TECNICO, Skill canónica de \`dev\`, controles V15–V19.

**Resultado exigido:** fijar dependencias técnicas y contrato de salida de M02 para puntos, promociones, reseñas, respuestas y posventa, con antifraude, moderación y trazabilidad. Este documento **no** implementa entidades C#, endpoints, EF/migraciones, MySQL, Angular, emisión de eventos, canje, saldos, proveedores, datos reales ni despliegues. Las pruebas de runtime aquí descritas son futuras y **NO EJECUTADAS**.

## 1. Precondiciones institucionales verificadas y alcance

La selección de la unidad requirió ID exacto \`C09-02.M02.06\`, PLAN H1345=PENDIENTE/I1345 vacío, COLA G1345/O1345=SI, DAG_TECNICO F1306=VALIDADO/G1306=SI, ausencia de EN_PROGRESO propia, seis Tasks Javier V17 habilitadas, Skill canónica coincidente y Scope Lock SUCCESS en el HEAD de \`dev\` observado. Se adquirió el candado V17 \`SQV17_C09_02_M02_06\`, namedRangeId \`2000001345\`, H:I de fila 1345, y CAS de H con \`occurrencesChanged=1\` seguido de owner I y readback. Ninguna de esas señales es autorización productiva.

La fuente DAG exige \`C09-02.M01.01–06+A5\`, \`C09-02.M02.01–05+A5\`, \`C07-02.M01.06+A5\` y \`C09-01.M01.06+A5\`. Las dependencias de fase C07.02/C09.01 y de bloque C01/C06/C08 siguen siendo referencias preparatorias: jamás deben interpretarse como aceptación de M03, M06, integración, JWT, tenant A/B o QA/PROD. Cualquier sucesora técnica exige **nueva** lectura de PLAN/COLA/DAG, LISTO y A5 donde se requiera.

## 2. Fuentes de diseño (blobs comprobados en dev)

| Autoridad técnica publicada | Blob Git | Parte que fija |
| --- | --- | --- |
| \`docs/v5/c09-02-m01-06-gates-rollback-liberacion-fidelizacion.md\` | \`0bfaffb03f54411a17557924b64fcb67451bc0ee\` | G01–G16 preparatorias, rollback y liberación M01 |
| \`docs/v5/c09-02-m02-01-modelo-entidades-ownership-fidelizacion-reputacion.md\` | \`0f55f250f7c12badcd49717dc3e7a43579e6496a\` | E01–E16, INV01–INV20 y ownership |
| \`docs/v5/c09-02-m02-02-contratos-api-dto-errores-fidelizacion.md\` | \`1f27a16eb0b51609fee99df884a9d7efa592e7ed\` | O01–O08, D01–D08, X01–X12, validación |
| \`docs/v5/c09-02-m02-03-persistencia-indices-migraciones-fixtures-fidelizacion-reputacion.md\` | \`bfcd2f372598ed84452137b99f170d0465112a02\` | I01–I12, migraciones reversibles futuras y fixtures |
| \`docs/v5/c09-02-m02-04-configuracion-tipada-flags-seguros-fidelizacion.md\` | \`ca697da0e7135b5c83040716b81a3cf98768aea8\` | ámbitos, O01–O16 tipadas, deny-default y corrección P2 |
| \`docs/v5/c09-02-m02-05-eventos-idempotencia-correlacion-limites-fidelizacion-reputacion.md\` | \`4b6e8a99581892bc3fbc7a4a4e57b0d9ffbe08c8\` | EV01–EV12, ID01–ID12, reintentos y correlación |
| \`docs/v5/c07-02-m01-06-gates-rollback-liberacion-storefront-checkout.md\` | \`122674cd481553a8bf2da3d420f379604211d9f1\` | frontera storefront/checkout sin pedido, pago ni saldo implícito |
| \`docs/v5/c09-01-m01-06-gates-rollback-liberacion-crm-universal.md\` | \`ffc5e73423a8be7e0f1064c8df5b1fb68bbc2769\` | identidad y CRM; ninguna afiliación de tenant inferida |

Estas fuentes se usan como contratos de alcance. El documento terminal M02 **no sustituye ni altera** sus especificaciones ni borra A5_FAIL históricos o revisiones correctivas.

## 3. Matriz de entrega y dependencias para M03 (inertes)

| Grupo | Contrato que deberá conservar la implementación | Evidencia obligatoria posterior |
| --- | --- | --- |
| Dominio | ProgramaFidelidad, CuentaPuntos, MovimientoPuntos, CanjePuntos, BeneficioPromocion, Resena, DecisionModeracion, CasoPosventa, ConsentimientoCanal y auditoría; \`EmpresaId\` explícita e inmutable | pruebas xUnit de invariantes, venta confirmada y compensación sin borrar historia |
| API/DTO | operaciones lógicas O01–O08 y DTO D01–D08; backend deriva tenant e identidad de JWT y membresía, nunca de payload/host | HTTP firmado 401/403/404/409 con recursos A/B, roles y revocación |
| Persistencia | índices I01–I12 candidatos, FK compuestas por tenant, CAS, transacción e idempotencia | EF Core 10 + proveedor Oracle y MySQL 8.4 **efímero**; fixtures A/B, migraciones up/down, rollback y TRX |
| Configuración | opciones O01–O16 son contractuales, no claves instaladas; flags false, límites 0, null y allowlist vacía niegan acceso | configuración realmente tipada, pruebas de precedencia global/empresa/sucursal, valores negativos y revocación |
| Eventos | EV01–EV12 y ID01–ID12 no implican broker/outbox en ejecución; repetir/reordenar jamás dobla puntos o envíos | transacciones y concurrencia reales, retries acotados, idempotency digest, cuarentena y observabilidad |
| Storefront/CRM | venta, cliente y consentimiento requieren ownership real; \`EmpresaId=NULL\` legado permanece NO_RESUELTO | reconciliación expresamente autorizada, aislamiento de compra/CRM, pruebas negativas sin inferir empresas |
| Angular | UI informa estados, errores y accesibilidad; **no autoriza** el backend | lint, build, Playwright/Chromium con rutas y accesibilidad cuando exista UI real |

No incorporar datos productivos Aiven, contraseñas, claves, despliegues, proveedores de pago ni mensajes reales en una unidad posterior sin autorización vigente. Las pruebas de integración deberán ejecutarse en GitHub-hosted con servicios aislados cuando correspondan.

## 4. Dieciséis puertas terminales contractuales SAL01–SAL16

Estas puertas son requisitos propuestos para futura implementación; **NO son valores ya satisfechos, ni reescriben las G01–G16 de M01 ni fórmulas G/O**.

| Puerta | Rechazo o aceptación que deberá ser demostrable |
| --- | --- |
| SAL01 | Denegar operación sin JWT firmado, sesión vigente y membresía tenant verificable |
| SAL02 | Denegar recurso Empresa B desde A; \`EmpresaId=NULL\` no prueba pertenencia |
| SAL03 | Verificar rol/RBAC en backend, con revocación en el momento del efecto |
| SAL04 | Separar puntos, dinero, crédito, facturas y stock; sin conversión implícita |
| SAL05 | Acreditación solo por venta confirmada, idempotente y del mismo tenant |
| SAL06 | Canje con saldo suficiente, cupo y CAS transaccional; sin gasto parcial |
| SAL07 | Reversión compensatoria append-only, sin DELETE de historia |
| SAL08 | Promociones acotadas por empresa, periodo UTC, uso y versión |
| SAL09 | Revisión/moderación con autor y revisor distintos cuando aplique, sin autoaprobación |
| SAL10 | Reseñas saneadas, PII mínima, sin XSS, IDOR ni publicación prematura |
| SAL11 | Posventa y respuestas restringidas a cliente, venta y tenant reales |
| SAL12 | Consentimiento explícito por canal y finalidad; opt-out prevalece sobre retry |
| SAL13 | Configuración fail-closed: false/0/null/empty deshabilitan, y límites no negativos bien tipados |
| SAL14 | Eventos con misma clave/digest = un efecto; digest distinto o versión stale = rechazo seguro |
| SAL15 | Outbox/correlación y logs sin secretos, PII indebida ni referencias cross-tenant |
| SAL16 | A0–A4 causales exact-SHA, CI pertinente SUCCESS, rollback y A5 de otra Task antes de sucesoras |

Una puerta no comprobada sigue pendiente aunque M02.06 documental pase a LISTO_DEV. No alterar COLA/DAG para forzar etapa.

## 5. Doce pares positivo/negativo FUTUROS — no ejecutados

| ID | Positivo a implementar después | Negativo/frontera/error que debe fallar sin efectos |
| --- | --- | --- |
| P01/N01 | Tenant A y cliente propios leen cuenta | cliente B, tenant NULL o sesión ausente no leen |
| P02/N02 | venta A confirmada acredita una sola vez | venta anulada, replay o actor B no acredita |
| P03/N03 | compensación legítima reconcilia saldo | UPDATE/DELETE del ledger o compensación doble se rechaza |
| P04/N04 | canje con CAS válido confirma | carrera, saldo insuficiente y versión vieja no gastan |
| P05/N05 | promoción vigente A respeta cupo UTC | caducada, B o sin cupo no se aplica |
| P06/N06 | mismas key y digest retornan resultado autorizado | key igual/payload diferente produce conflicto sin efecto |
| P07/N07 | moderador A distinto del autor decide con historia | autoaprobación, rol revocado y moderador B no alteran |
| P08/N08 | reseña permitida y saneada se modera | XSS, PII, recurso ajeno y ausencia de consentimiento no publican |
| P09/N09 | caso A se consulta por miembro válido | IDOR/venta B/rol revocado no filtra contenido |
| P10/N10 | envío consentido por propósito se permite | opt-out y retries tardíos no producen envíos |
| P11/N11 | opción NonNegativeInteger válida y positiva aplica dentro de techo | cero deshabilita; negativo, fracción, string y overflow fallan |
| P12/N12 | timeout transitorio reintenta de forma acotada | poison-event, reordenamiento, error DB y rechazo JWT no duplican ni revelan |

Se declaran **12/12 pares especificados, 0/12 pares ejecutados**. No se atribuye PASS de HTTP, xUnit, EF/MySQL ni Playwright por el hecho de redactar expectativas.

## 6. Criterios de aceptación, seguridad y reversión de ESTA unidad R0

- **A0:** documento único M02.06 y sección autocontenida CHANGELOG; requisitos de fila 1345, fuentes Git exactas, DAG y alcance confrontados. No se modifican API, DTO, entidades, datos, permisos, configuración ni esquema.
- **A1:** revisión estática de SAL01–SAL16 y P01/N01–P12/N12, con casos positivos, negativos, fronteras y errores **futuros**. No declarar ejecución de regresión.
- **A2:** contrato explícito tenant A/B, legacy NULL, roles, revocación, privacidad, correlación, consentimientos y antifraude; implementación de controles **NO_APLICA R0** por no haber código.
- **A3:** xUnit .NET, MySQL 8.4, Angular y Chromium **NO_APLICA R0** exclusivamente para este diff Markdown. Serán obligatorios en las microtareas R2 correspondientes.
- **A4:** publicar en \`dev\` por avance fast-forward con HEAD esperado, comprobar Scope Lock y V5 provenance SUCCESS exact SHA, confirmar PLAN/COLA/DAG y BITACORA idempotente/readback. Un check pendiente, fallido, cancelado o SKIPPED no se reporta como PASS obligatorio.
- **A5:** otra de las cinco Tasks Javier, distinta del autor DEV01, revisa este documento y el SHA real; LISTO_DEV A0–A4 no equivale a A5_PASS ni licencia R3.

**Rollback:** \`git revert <SHA_de_esta_entrega>\` sólo si se requiere y tras comprobar HEAD y otros escritores; no reset, force-push, alteración de historia ni borrado de BITACORA. No hay migración o estado operativo que revertir. Se conserva \`PROJECT_SCOPE_LOCK=STRICT\`, rama \`dev\` y separación estricta QA/main/PROD.

**Handoff:** acreditar CI Scope Lock/V5, cerrar R0 bajo V17 y readback, liberar candado sólo después de evento institucional, dejar A5 independiente pendiente, recalcular DAG/COLA de M03.01–06 y no reclamar M03 mientras \`C07-02.M06.06+A5\`, \`C09-01.M06.06+A5\` u otras puertas sigan en NO.
