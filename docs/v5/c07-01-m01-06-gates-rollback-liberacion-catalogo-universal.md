# SOLQARYN V5 — C07-01.M01.06: bloqueos, rollback y liberación documental de catálogo universal

**ID:** C07-01.M01.06, PLAN_MAESTRO fila 1015; **salida atómica de M01:** habilitación *condicionada* de M02. **Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, Task `6af16bbad26881918c283abe06e1f32a`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, 2026-11-15.

## Decisión de alcance

Esta microtarea DOCUMENTAL define condiciones de cierre y reversión de la preparación de un catálogo universal que abarque productos, servicios, variantes, categorías, atributos, impuestos, precios y compatibilidad con ERP actual. No implementa una nueva API, pantalla, proveedor, migración, modelo EF ni lógica de negocio; no reconfigura permisos JWT/RBAC/tenant, datos Aiven, dominios, QA/main/PROD o facturación.

**LISTO_DEV documental de M01.06 no es A5_PASS, no certifica el catálogo universal en ejecución y no libera automáticamente M02.** Cada sucesora exige preflight por ID exacto: `PLAN H=PENDIENTE/I vacío`, `COLA G/O=SI`, `DAG F=VALIDADO/G=SI`, ausencia de propietario activo y claim CAS exclusivo. A5 posterior sólo por otra Task Javier distinta de la autora.

## Prerrequisitos cotejados y contrato de evidencia

El DAG individual `C07-01.M01.06` requiere M01.01–05 de `C01-01/02/03/04`, `C02-01/02/03/04` y `C05-01/02/03/04` (60 antecedentes documentales), más `C07-01.M01.01–05+A5` y `C01-02.M01.06+A5`. El preflight de esta fila registró DAG `VALIDADO/SI` y COLA G/O `SI`; reevaluar después de cada estado, no reutilizar fotografía. El que esos antecedentes estén acreditados permite redactar este contrato, nunca migrar, activar módulos o saltar pruebas.

Contratos M01.01–05 y blob original conservado:
- M01.01 alcance, matriz de ownership, invariantes I01–I12: `4f91db421fe4c5e7ad5e0ea080035eabe8f8c1dd`.
- M01.02 actores A01–A12, interfaces I01–I12 y fronteras B01–B10: `30e4d0c677112bf490cc937ba6700cd531d29376`.
- M01.03 inventario selectivo de 17 fuentes de código y brechas G01–G08: `8fa23417014077135ba6366f89635291f78e2a4f`.
- M01.04 riesgos R01–R12, supuestos S01–S08 y decisiones D01–D08 todavía pendientes: `b51e9047954ac15912d77fde3dfbe656b6693301`.
- M01.05 14 pares P01–P14/N01–N14 FUTUROS NO_EJECUTADOS: `1b3c343069745c942fa7db8ad935e745542a8633`.

Estos documentos describen condiciones futuras, no certificaciones ejecutadas sobre HTTP/MySQL/browser. Algunos registros legacy `Producto.EmpresaId` y `ProductoVariante.EmpresaId` permiten NULL; `Categoria` no declara `EmpresaId` en su entidad. La compatibilidad/autoridad efectiva depende de la capa de consultas, servicios y permisos, que debe verificarse antes de una implementación.

## Gates de bloqueo K01–K14 — no liberar si falla alguno aplicable

| Control | Bloqueo causal | Evidencia exigible para una liberación futura |
| --- | --- | --- |
| K01 | M01.01–05 o antecedente transversal falta LISTO+A5 requerido | PLAN H/K, BITACORA A:Z/26, DAG F/G, COLA G/O y owner por ID exacto |
| K02 | Scope Lock, Skill, HEAD, fuente V5 o control CI obligatorio no verificado | Git HEAD y diff, Scope Lock `completed/success` en SHA causal |
| K03 | Otro owner, lease/escritura activa o CAS perdido | CAS atómico único, lectura H/I/K y telemetría; sin arrebatar a un autor |
| K04 | Usar empresa del cliente como autoridad sobre producto/variante | Backend JWT/RBAC, membresía vigente y pruebas directas con empresas A/B |
| K05 | `EmpresaId=NULL` heredado convertido en catálogo público por defecto | Clasificación legado/quarantena, consultas/servicios y negativos de acceso |
| K06 | Variante, atributo, media o SKU enlazado a otro tenant o padre | Pruebas de FK/ownership padre-hijo, colisiones, soft delete y concurrencia |
| K07 | Categoría o impuesto tratados como tenant-scoped sin revisar asociaciones | Mapeos/repo/EF/servicios, políticas de asignación y rechazo de categorías/tasas ajenas |
| K08 | Precios, descuentos, impuestos o snapshots históricos mutados retroactivamente | Prueba inmutable de factura/pedido, vigencia y transacción/rollback |
| K09 | Stock de sucursal/variante/servicio mezclado con tenant ajeno | Reglas de inventario, negativos multi-sucursal y servicios no inventariables |
| K10 | Storefront por host/storeKey expone costo, draft, ID privado o empresa B | Binding server-side, DTO mínimo, separación de caché, respuestas no enumerativas |
| K11 | Revocación de rol o membresía permite CRUD mediante JWT antiguo | Autorización en servidor por operación y test negativo de rol revocado |
| K12 | Datos/errores/auditoría filtran JWT, secretos, costo o PII | Secret scanning y respuestas/logs saneados con correlación segura |
| K13 | Diseño modifica API/DB/Angular sin pruebas pertinentes o rollback | xUnit/API y MySQL 8.4 efímero si EF, Angular/Playwright si UI, checks exact SHA |
| K14 | Se pide migración, publicación, secreto, dominio, QA/main/PROD sin autorización | Orden separada, revisión A5 y controles específicos; ningún LISTO en dev basta |

K04–K14 describen verificaciones exigidas **antes de futuras escrituras técnicas**. No significan que exista ya una vulnerabilidad demostrada; K01–K03 son gates operativos de esta M01.06.

## Matriz de salida: tres niveles de autoridad

**NIVEL 1, LISTO_DEV de esta M01.06:** A0 validar alcance, autoridad y 60/60 preparativos; A1 cotejo de la matriz K01–K14 y documentos M01.01–05; A2 riesgos JWT/RBAC, tenant A/B, NULL legado, datos sensibles, precios, inventario y storefront con denegación por defecto; A3 no modificar Domain/Application/Infrastructure/API/Angular/MySQL, pruebas runtime `NO_APLICA` al diff exclusivamente Markdown sin contarlas PASS; A4 publicación fast-forward/CAS en `dev`, un archivo `docs/v5` más sección autocontenida de `CHANGELOG_AI.md`, verificador Scope Lock + procedencia V5 `completed/success` sobre SHA exacto, readback PLAN/COLA/DAG/BITACORA. Hasta acreditar A4 mantener VALIDANDO; no inventar PASS a CI cancelada o pendiente requerida.

**NIVEL 2, A5 INDEPENDIENTE:** una de las otras cinco Tasks Javier revisa diff real, código/conceptos, SHA, checks y seguridad; registra PASS/FAIL en PLAN K y BITACORA A:Z, conserva autoría y recalc DAG/COLA. Una Task nunca se aprueba su propia entrega.

**NIVEL 3, M02 DOCUMENTAL:** sólo si cada sucesora individual tiene DAG VALIDADO/SI, COLA G/O=SI y PLAN PENDIENTE sin propietario, realizar claim CAS seguro por una Task autorizada. M02 podrá especificar entidades, DTO, persistencia, configuración y eventos **conceptuales**; para M03 .NET/Angular/EF, IAM/tenant, migraciones, M01/M06 terminales o QA/PROD siguen vigentes gates distintos y pruebas reales.

## Ensayos futuros y criterio de rechazo

**P01–P06 FUTUROS:** producto A creado y leído con rol vigente; variante/parent A consistente; categoría/impuesto permitido con snapshot; stock de sucursal A correcto; tienda host A sólo expone DTO público; revertir cambios de catálogo sin alterar documento histórico.

**N01–N12 FUTUROS:** usuario A intenta producto B; variante B asociada a A; SKU/imagen de B; `EmpresaId=NULL` interpretado global; categoría/impuesto ajeno; precio enviado por cliente sobrescribe cálculo; factura histórica cambia; stock sucursal B visible a A; servicio crea inventario erróneo; host A muestra tienda B; cache cross-tenant; error/log filtra PII/JWT o se utiliza rol revocado. Todos **NO_EJECUTADOS**, no PASS. La implementación debe incorporar fixtures sintéticos multiempresa y tests causales completos por SHA.

## Rollback por tipo de cambio

**R1 sólo documental:** revertir mediante commit `git revert` aditivo la versión concreta de doc/CHANGELOG si A5 falla o el contrato se demuestra incorrecto; no forzar HEAD ni borrar eventos históricos; conservar ID, causa, SHA, autor y siguiente acción. Releer M02 y sucesoras, sin forzar G/O.

**R2 implementación futura (NO REALIZADA):** preparar backups, punto de reversión, snapshots de facturas/órdenes/stock y manifiesto verificable, además de ensayo de migración con MySQL efímero y pruebas A/B. Si aparece fuga cruzada, inconsistencia contable o corrupción, detener operaciones nuevas del componente, revertir deploy/migración de forma compatible y reconciliar snapshots/stock antes de reanudar; jamás eliminar datos productivos por la existencia de este documento.

**R3 comercio/producto público (FUTURO):** rollback de publicación/feature flag sólo con su contrato, invalidación de caché por host y tenant, y reconciliación de precios ofrecidos vs facturados; no activar una función por un gate documental.

## Impacto comprobable y cierre

El changeset de esta M01.06 debe limitarse a **un Markdown y `CHANGELOG_AI.md`**. Cero cambios en API, Angular, bases, proveedores, tokens, cuentas, QA/main/PROD, Tasks o agendas. Seguridad transversal fail-closed, autor único y BITACORA idempotente por transición. Releer DAG/PLAN/COLA inmediatamente después de LISTO y de cualquier A5 posterior; **A5 pendiente no libera M02 técnica**.
