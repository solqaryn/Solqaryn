# SOLQARYN V5 — C06-04.M01.06: bloqueos, rollback y contrato de salida del portal de cliente

**Unidad:** C06-04.M01.06, fila institucional PLAN_MAESTRO 979. **Responsable:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Repositorio:** `solqaryn/Solqaryn`, exclusivamente `dev`; PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

## Frontera exacta del cierre M01

El resultado de esta microtarea es **un contrato documental de salida del lote**, no un portal nuevo. SOLQARYN ya contiene una experiencia de cuenta de tienda y servicios de pedidos, favoritos, direcciones y notificaciones. Este M01 no certifica la seguridad integral de esa cuenta ni la existencia de recuperación, reservas, membresías, documentos y preferencias con todas las reglas aquí propuestas. Tampoco modifica Angular, API .NET, MySQL, JWT/RBAC, integraciones, roles, sesiones, workers, dispatchers, DNS, almacenamiento ni producción.

La fuente actual del DAG exige `C06-04.M01.01–05+A5` y dependencias documentales de C01/C02/C05. Se cotejaron las puertas reactivas de `PLAN_MAESTRO`, `COLA` y `DAG_TECNICO` por ID exacto antes de reclamar esta unidad, con G/O=SI y F=VALIDADO/G=SI. La preparación permite **solamente continuar al diseño M02 cuando sus propias fórmulas tengan SI tras LISTO + A5 independiente de M01.06**. No autoriza tecnología, persistencia o disponibilidad operacional.

## Trazabilidad de las cinco entradas y brechas sin resolver

| Predecesora M01 | Blob Git de evidencia documental | Alcance contrastado, nunca prueba runtime |
| --- | --- | --- |
| C06-04.M01.01 | `afa54d6939df46719cfe89a9f3b657b2bce38f03` | NR01–NR12 y límites de no-regresión del portal |
| C06-04.M01.02 | `d0fc169736e28f93128b10b7c15f32406e1eb0a1` | A01–A12, I01–I12, B01–B08; identidades, interfaces, fronteras |
| C06-04.M01.03 | `163402f99029b7371c5ed898562467267e6c1a7a` | S01–S16 fuentes y G01–G12 brechas futuras |
| C06-04.M01.04 | `19508bcb6b20eaad6043d100d872c6e105775903` | R01–R12 potenciales; S01–S08 supuestos; D01–D08 decisiones PENDIENTES |
| C06-04.M01.05 | `f5ba1fe80dbfdf2e559d737ad86e14504988123e` | 14 positivos/14 negativos de aceptación FUTUROS NO_EJECUTADOS |

Se comprobó que `StorefrontCuentaService`, `StorefrontCuentaComponent` y `StorefrontPedidoService` son piezas existentes, no autorización global para leer pedidos ajenos. El cliente puede presentar `EmpresaId`, ruta o token, pero la autoridad de tenant, propietario, cliente y permiso proviene de backend. Este documento no registra incidentes reales ni atribuye vulnerabilidades a esos componentes.

## B01–B16: bloqueos de implementación y condiciones de liberación futura

| ID | Bloqueo comprobable que debe preservar M02/M03 |
| --- | --- |
| B01 | Identidad del cliente distinta de principal ERP; JWT de cliente nunca aporta privilegios administrativos de empresa |
| B02 | Contexto tenant resuelto y verificado en servidor; URL, slug, cookie, Host o encabezado `X-Empresa-Id` no elevan autoridad |
| B03 | Propiedad de pedidos, direcciones, favoritos, reservas y documentos comprobada en persistencia por tenant y cliente |
| B04 | Todas las lecturas llevan filtros de empresa y usuario; ausencia de contexto autenticado deniega, nunca retorna consulta sin filtrar |
| B05 | Enumeración negativa: ID ajeno de tenant B debe devolver respuesta uniforme, sin existencia ni PII de A |
| B06 | Intra-tenant: cliente A1 no obtiene objetos de A2 por pertenecer a la misma empresa |
| B07 | Recuperación y cambio de credenciales con token efímero, caducidad, revocación, límite de intentos y sin exposición de cuentas |
| B08 | Registro y login separan sesiones de storefront y ERP; rotación/logout no reutiliza credenciales administrativas |
| B09 | Descargas de documentos requieren propiedad y enlace autorizado temporal, sin URL permanente de objeto privado |
| B10 | Reservas y membresías requieren estados y capacidades verificadas, sin inferir pago, cupo, beneficio ni cumplimiento |
| B11 | Notificaciones y SMTP no filtran token, PII o datos de otro cliente; auditar sólo metadatos mínimos |
| B12 | Preferencias y consentimientos deben ser revocables y vinculados al titular; negativa no activa marketing |
| B13 | Escrituras idempotentes, versionadas y transaccionales; conflictos CAS no duplican pedidos ni alteran inventario |
| B14 | Caché/sesión/frontend no mezclan tenants ni usuarios al cambiar cuenta; fallback neutro y expiración segura |
| B15 | UI accesible/responsive (teclado, foco, aria-live, 320 px y reduced motion) sin reemplazar enforcement backend |
| B16 | QA/main/PROD, bases reales, dominios, servicios de pago, secretos y proveedores requieren autorización específica ajena al DAG |

Los dieciséis bloqueos son **requisitos abiertos para implementación y pruebas posteriores**. Una especificación M01 o M02 no implica que B01–B16 estén aprobados. Cualquier dato en almacenamiento local es contexto no confiable para autorización.

## H01–H08: contrato de entrada M02 y handoff verificable

| ID | Entrega preparatoria permitida al entrar M02 |
| --- | --- |
| H01 | Modelo tipado de `Tenant`, `Customer`, sesión, propiedad y recurso; no mezclar consumidor con empleado/administrador |
| H02 | DTO/contratos de API con errores genéricos y semántica 401/403/404 consistente sin revelar existencia ajena |
| H03 | Modelo de persistencia e índices por tenant+cliente, sin migración real hasta gate CI de servicio MySQL efímero |
| H04 | Configuración allowlisted y flags fail-closed para registrar, recuperar, reservar y compartir documentos |
| H05 | Eventos y notificaciones **solo como contrato conceptual**: no instalar colas, webhooks ni dispatchers |
| H06 | Regla de idempotencia, versión y concurrencia en pedidos/direcciones; definir rollback sin borrar historial |
| H07 | Plan de pruebas positivas y negativas por tenant A/B, clientes A1/A2, permisos, sesión y objeto |
| H08 | Observabilidad sin PII/JWT, evidencias SHA, ownership, auditoría A0–A5 y aprobación independiente |

El handoff no fuerza fórmulas de COLA ni DAG. M02 debe revalidar G/O y F/G actuales para **cada** microtarea y cualquier prerequisito adicional. Un `VALIDANDO` documental provisional no habilita terminal M01.06, autorización de runtime ni migración.

## RB01–RB08: recuperación sin destrucción ni elevación

| ID | Riesgo futuro y reversión exigible |
| --- | --- |
| RB01 | Sesión incorrecta: revocar token, cerrar acceso y conservar auditoría sin activar principal ERP |
| RB02 | Cruce tenant/cliente: bloquear inmediatamente lectura, invalidar caché y verificar aislamiento A/B y A1/A2 antes de reabrir |
| RB03 | Recuperación comprometida: invalidar tokens de restablecimiento, limitar intentos y preservar trazas mínimas |
| RB04 | Descarga ajena: revocar enlace y token, comprobar propiedad actual, sin exposición de URL secreta en logs |
| RB05 | Pedido/reserva duplicado: revertir operación autorizada por clave idempotente y transacción, sin borrar ventas contables |
| RB06 | Índice/migración fallida: rollback reversible probado en MySQL efímero, backup y restore antes de dato real |
| RB07 | Falla SMTP/archivo/proveedor: estado de error neutro, no habilitar acceso cruzado, no reenviar información sensible |
| RB08 | Despliegue no autorizado: no ejecutarlo; para futuro QA/PROD autorizado requerir smoke, aprobación y restauración documentada |

**Rollback de este changeset documental:** revert normal Git con un nuevo commit en `dev`; nunca force-push, reset destructivo ni mutación retrospectiva de BITACORA.

## Casos de aceptación futuros, sin ficción de pruebas

**Positivos P01–P06 FUTUROS NO_EJECUTADOS:** P01 cliente de A accede sólo a pedidos propios; P02 recuperación propia completa con token temporal; P03 el cambio de tenant reinicia estado público; P04 reservas y beneficios muestran sólo propiedad acreditada; P05 enlaces privados caducan y no se indexan; P06 rollback restaura versión sin pérdida de historial.

**Negativos N01–N14 FUTUROS NO_EJECUTADOS:** N01 identidad de B en A; N02 otro cliente en mismo A; N03 `X-Empresa-Id` arbitrario; N04 recurso ID enumerado; N05 recuperación con token vencido; N06 sesión ERP desde cliente; N07 descarga privada ajena; N08 reserva sin permiso/cupo; N09 membresía no vigente; N10 petición duplicada; N11 borrado/desactivación sin versión; N12 PII en error/log; N13 caché cruzada al cambiar sesión; N14 mail/proveedor fallido sin fallback. Estos resultados no han sido observados ni ejecutados por esta microtarea.

## Auditorías A0–A5, límites y condición de salida

- **A0 documental:** correlación con el plan V5, las cinco microtareas y sus blobs, 16 bloqueos y ocho reglas de handoff sin copiar funciones inexistentes.
- **A1 revisión de aceptación:** cotejo lógico de P01–P06/N01–N14 con estado conocido, no atribución de xUnit, Playwright, HTTP ni pruebas de seguridad en vivo.
- **A2 seguridad documental:** revisión de aislamiento tenant A/B y clientes A1/A2, RBAC, JWT, ownership de objetos y límites de confianza; riesgos siguen abiertos.
- **A3 integración documental:** coherencia de H01–H08 y RB01–RB08 con arquitectura Angular 22.2.1/.NET 10/EF MySQL y fronteras existentes, sin activar implementaciones.
- **A4 publicación:** diff solo de este Markdown y sección autocontenida en `CHANGELOG_AI.md`, fast-forward/CAS sobre HEAD, Scope Lock y procedencia V5 en `completed/success` del SHA exacto, readbacks PLAN/COLA/DAG/BITACORA. Builds runtime de cambios no afectados no son evidencia funcional y se marcan `NO_APLICA` cuando corresponda.
- **A5 independiente:** debe realizarla otro TASK_ID de las seis Javier diferente del autor, comprobando SHA, blobs, evidencia y gating. Esta Task no puede aprobarse sola.

**Resultado honesto de M01.06:** contrato documental concluible con A0–A4; el portal integral no está certificado, los controles B01–B16 no fueron implementados en este changeset y todas las pruebas de runtime aquí enumeradas siguen pendientes. Ninguna promoción QA/main/PROD, consulta de secretos, mutación de BD real, servicio de pago ni operación de infraestructura está autorizada.
