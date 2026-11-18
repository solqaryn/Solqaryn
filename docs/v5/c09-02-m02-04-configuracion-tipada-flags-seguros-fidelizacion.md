# SOLQARYN V17 — C09-02.M02.04: configuración tipada y flags seguros de fidelización y reputación

**Microtarea:** `C09-02.M02.04`, PLAN_MAESTRO fila 1343. **Autor:** PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, R0 documental; futuras implementaciones en comercio, finanzas, privacidad o tenant son R2. **No se crea una clave de configuración real, ni se activan campañas, beneficios, puntos, moderación, notificaciones o servicios.**

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO, COLA, DAG_TECNICO, Skill canónica dev, V15 A0-A5, V17 candado. Resultado exigido: **definir configuración tipada, ámbitos y feature flags seguros**, no implementar API ni persistencia.

## 1. Fuentes existentes, dominios y frontera

- `docs/v5/c09-02-m01-03-inventario-codigo-brechas-fidelizacion.md` (blob `b5bbb9d2c8e85814390bd12c85e5f4d6bd3bfed7`): los tipos `Cliente`, `Venta`, `CreditoCliente`, `UsuarioEmpresa` y `RegistroAuditoria` son sustratos, no motor de puntos o flags activos.
- `docs/v5/c09-02-m01-06-gates-rollback-liberacion-fidelizacion.md` (blob `0bfaffb03f54411a17557924b64fcb67451bc0ee`): G01–G16 con rollback, A5 independiente y pruebas R2 futuras.
- `docs/v5/c09-02-m02-01-modelo-entidades-ownership-fidelizacion-reputacion.md` (blob `0f55f250f7c12badcd49717dc3e7a43579e6496a`): entidades E01–E16 e invariantes INV01–INV20 como propuesta documental, no certificación de ledger.
- `docs/v5/c09-02-m02-02-contratos-api-dto-errores-fidelizacion.md` (blob `1f27a16eb0b51609fee99df884a9d7efa592e7ed`): ocho operaciones, ocho DTO, doce errores y dieciséis validaciones conceptuales, no rutas disponibles.
- Configuración real futura: valores tipados, origen confiable, estado y versión auditables. No confiar en `EmpresaId` recibido desde frontend. Un `EmpresaId=NULL` legado no concede tenant ni saldos. Preservar `Domain <- Application <- Infrastructure` y autorización server-side, además de cachés, privacidad, observabilidad y rollback.

## 2. Seis ámbitos de configuración propuestos

| Ámbito | Puede restringir | No puede autorizar |
| --- | --- | --- |
| GlobalSeguridad | techo de rate, verificación de firma, desactivación global | permisos de usuario o lectura cross-tenant |
| Empresa | programa permitido, ventanas y límites menores al global | activar funciones deshabilitadas globalmente |
| Sucursal | límites y canales más restrictivos del tenant | cambiar `EmpresaId` de operación |
| Programa | reglas, versión y expiración dentro de tenant | crear dinero o crédito sin transacción |
| Canal | consentimiento, propósito, cuotas y opt-out | reenviar tras revocación o ignorar finalidad |
| UsuarioSesion | personalización no autoritativa y preferencia visible | JWT/RBAC, saldo, canje, moderación o firma |

**Precedencia:** deny/kill global > denegación por empresa > denegación por sucursal/programa > consentimiento revocado > preferencia usuario. La resolución combina restricciones mediante intersección; una opción local nunca ensancha un máximo superior.

## 3. Dieciséis opciones tipadas futuras, todas inertes

| Opción contractual | Tipo propuesto | Valor seguro inicial | Control y rechazo obligatorio |
| --- | --- | --- | --- |
| O01 ProgramEnabled | Boolean | false | solo administrador autorizado y prerequisitos |
| O02 EarnPointsEnabled | Boolean | false | sin venta confirmada, nunca acreditar |
| O03 RedeemPointsEnabled | Boolean | false | sin saldo/transacción/permiso, nunca canjear |
| O04 PromotionEnabled | Boolean | false | impedir promos no allowlisted o de otro tenant |
| O05 ReviewSubmissionEnabled | Boolean | false | sin identidad/consentimiento/saneamiento no publicar |
| O06 ReviewModerationEnabled | Boolean | false | sin rol y motivo no moderar |
| O07 AfterSalesEnabled | Boolean | false | sin ownership de caso/venta no consultar |
| O08 MarketingSendEnabled | Boolean | false | sin consentimiento vigente y revocación no enviar |
| O09 MaxEarnPerSale | NonNegativeInteger | 0 | techo global, antirreplay, nunca puntos monetarios |
| O10 MaxRedeemPerTxn | NonNegativeInteger | 0 | transacción/versión y saldo suficiente |
| O11 PromotionMaxUses | NonNegativeInteger | 0 | cupo por tenant y vigencia UTC |
| O12 MaxReviewLength | NonNegativeInteger | 0 | longitud máxima sólo tras revisión y sanitización |
| O13 PromotionStartUtc | NullableUtcInstant | null | nulo=inhabilitado, no asumir tiempo local |
| O14 PromotionEndUtc | NullableUtcInstant | null | end > start, sin caducidad aceptada por defecto |
| O15 ConsentPurposeAllowlist | BoundedStringSet | empty | lista explícita por tenant/canal, deny default |
| O16 RetryPolicy | BoundedRetrySpec | attempts=0 | revalidar permiso/opt-out/idempotencia al retry |

**Invariante correctiva R0 (A5_FAIL P2):** O09, O10 y O12 usan `NonNegativeInteger` porque su valor inicial seguro es `0`. El cero es un centinela de **operación deshabilitada**, incluso si otro flag fuese `true`; sólo un entero positivo dentro de un límite superior validado y junto con autorizaciones, consentimiento, tenant y dependencias reales podrá habilitar el uso futuro. Rechazar negativos, fracciones, cadenas numéricas, nulos y desbordamientos antes de calcular cupos; no aceptar conversiones permisivas ni inferir permisos del tipo. Este contrato sólo documenta futuras validaciones, no activa lógica productiva.

Todas O01–O16 son **nombres de contrato**, no claves de `appsettings`, flags existentes ni permisos para configurar los servicios. `false`, cero, null y listas vacías niegan acciones de negocio hasta implementación y validaciones reales independientes.

## 4. Doce reglas fail-closed de interpretación

| Regla | Resultado que debe poder demostrarse en fase de implementación |
| --- | --- |
| R01 | Política más restrictiva prevalece; tenant/sucursal no sobrescribe kill global |
| R02 | Ausencia o error de lectura de opción no cambia false a true |
| R03 | Validación estricta de bool/número/UTC: 0 entero no negativo es denegación segura, valores operativos mayores que 0; negativos, fracciones, strings, NaN e infinitos se rechazan sin coerción |
| R04 | Cambios requieren autor autenticado, rol, membresía vigente y auditoría |
| R05 | Configuración no es fuente de autorización a recursos: backend valida cada petición |
| R06 | Un tenant nunca lee o modifica el flag o cupo del tenant B |
| R07 | Caché por tenant/versión y revocación inmediata al inhabilitar |
| R08 | La lectura de `EmpresaId=NULL` sin conciliación real es NO_GO |
| R09 | Una promoción caducada, fuera de cupo o sin rango UTC válido no activa beneficio |
| R10 | Idempotencia, transacción y ledger no se deshabilitan mediante flags |
| R11 | Sin consentimiento vigente no hay envío ni retries después de opt-out |
| R12 | Rollback vuelve a estado más restrictivo sin borrar historial ni ventas |

**Observabilidad futura:** documentar actor, tenant, versión, origen, resultado deny y timestamp UTC sin copiar secretos, PII, keys o tokens. Alertar configuración inválida sin publicar detalles sensibles ni generar autorizaciones ficticias.

## 5. Doce pares de escenarios FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Programa autorizado se habilita tras completar gates | global false prevalece aunque empresa true |
| P02/N02 | Empresa A cambia su cuota permitida | editor de A intenta configurar B |
| P03/N03 | Sucursal reduce un límite de A | sucursal intenta superar techo de empresa |
| P04/N04 | Venta confirmada acumula bajo política vigente | flag true sin ledger/venta acredita puntos |
| P05/N05 | Canje transaccional usa versión correcta | versión vieja, saldo insuficiente o carrera canjea |
| P06/N06 | Promo A en ventana UTC y cupo correcto aplica | caducada, tenant B o rango inválido aplica |
| P07/N07 | Reseña legítima pasa saneamiento y permiso | HTML/PII y autor revocado publican |
| P08/N08 | Moderador A con permiso y motivo actúa | autor/rol lector de A se autoaprueba o B modera |
| P09/N09 | Posventa A verifica Cliente y Venta A | legacy NULL y caso/venta B son visibles |
| P10/N10 | Canal con opt-in vigente permite envío | opt-out y retry envían mensaje |
| P11/N11 | Un límite entero 0 deshabilita y uno positivo acotado sólo opera tras permisos reales | negativos, fracciones, strings (incluido "true"), null, NaN, overflow y 0 interpretado como permiso habilitan |
| P12/N12 | Kill/rollback frena acciones y conserva log | caché vieja sigue acreditando o mandando |

**Casos adicionales FUTUROS_NO_EJECUTADOS para R03/P11/N11:** O09/O10/O12 con 0 → denegación; valor positivo entero y acotado → continúa a verificación de permiso y tenant (no concede autorización por sí solo); -1, 0.5, "0", "1", null, NaN, infinito y overflow → error seguro/deny. No registrar estos escenarios como pruebas ejecutadas.

**Ejecución real en esta unidad documental:** cero tests xUnit/MySQL/HTTP/Playwright ejecutados, cobertura runtime `NO_MEDIDO`. Las filas P/N son oráculos propuestos, no PASS ni funcionalidades certificadas.

## 6. Gates R0, evolución y rollback

- **A0:** validar requisitos, DAG, lock V17, dos Markdown de esta unidad y que ningún archivo de runtime fue alterado.
- **A1:** revisión documental de tipos, precedencia, estados iniciales, UTC y negativos propuestos; casos 0/12 ejecutados.
- **A2:** analizar estáticamente JWT/RBAC, tenant A/B, consentimiento/opt-out, `EmpresaId=NULL`, PII, replay, concurrencia y ausencia de privilegio por flag.
- **A3:** MySQL 8.4 EF Core, APIs reales, Angular y Playwright `NO_APLICA` al diff exclusivamente documental; pasan a obligatorios al implementarse M03/M05.
- **A4:** fast-forward con CAS HEAD, `verify-solqaryn-project-scope` + V5 procedencia `completed/success` para SHA causal; PLAN/COLA/BITACORA readbacks. `IN_PROGRESS`, `SKIPPED`, `CANCELLED` no son PASS de prueba requerida.
- **A5:** revisor de **otra** Task Javier examina diferencial, fuente, SHA, seguridad y límites; LISTO_DEV no implica A5 aprobado.
- **Dependencias:** M02.06 y posteriores permanecen gobernadas por DAG+COLA dinámicos y A5 verdaderos. Esta unidad R0 no habilita M03/QA/main/PROD.
- **Reversión:** `git revert` ordinario sobre HEAD actualizado y evaluación de conflictos; no modificar ni borrar BITACORA histórica. No habilitar flags vivos ni iniciar operaciones de dinero, inventario o privacidad por este documento.

**Conclusión:** contrato de opciones tipadas, seis ámbitos, doce reglas, 12 pares de escenarios futuros. **Sin cambios de configuración ejecutable, código, datos, secreto o servicio.**
