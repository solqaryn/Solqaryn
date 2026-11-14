# SOLQARYN V5 — C10-03.M01.02: actores, entradas, salidas y fronteras del inventario y almacenamiento futuro

**Microtarea:** C10-03.M01.02 · PLAN_MAESTRO fila 1515 · **Autor:** PRIMARY DEV 01 / TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, 2026-11-14. **Preflight:** DAG_TECNICO fila 1476 F=VALIDADO/G=SI, COLA G/O SI, PLAN PENDIENTE libre con CAS exclusivo.

## Alcance real y estado de datos

La unidad identifica actores, contratos **candidatos** y límites de confianza para verificar inventario histórico real antes de cualquier futura migración, y prepara rutas de archivos futuros. Su regla central es: **INVENTARIO_DESCONOCIDO no significa INVENTARIO_CERO**. El resultado de `C10-03.M01.01` mantiene `NO_VERIFICADO` el inventario real de medios; no hay evidencia de que sea cero, ni de que sea positivo. La ruta cero sólo aplica tras lectura autorizada y conciliada de todas las fuentes relevantes. Si se certifica cero, se omitirán exportación, copia y checksum de objetos históricos, pero se permitirán fixtures nuevos en entorno efímero sujeto a autorización.

Esta M01.02 es exclusivamente **documental**. No conecta a Cloudinary, Cloudflare R2, MySQL productivo, DNS ni un bucket; no extrae datos, cuenta archivos reales, instala proveedor, ejecuta cargas, registra secretos, cambia permisos o crea nuevos endpoints. Los actores y DTO siguientes son candidatos de diseño, **NO_IMPLEMENTADOS**.

## Actores y responsabilidades A01–A14

| ID | Actor, sistema o rol | Frontera de autoridad y obligación |
| --- | --- | --- |
| A01 | Usuario autenticado de tenant A | Solo acceso a medios A bajo JWT y RBAC vigentes |
| A02 | Usuario de tenant B | No puede deducir existencia ni metadatos de A |
| A03 | Administrador del tenant | Consulta estado de su empresa; no privilegio global ni proveedor |
| A04 | Lector de archivos | Lee objetos permitidos; no exporta, elimina ni reconfigura |
| A05 | Gestor autorizado de archivos | Puede solicitar upload propio si API lo valida, sin decidir owner |
| A06 | Auditor de empresa | Accede únicamente a evidencias saneadas y propias |
| A07 | Operador global autorizado | Solo puede inventariar tras orden precisa, alcance y auditoría |
| A08 | Browser/Angular 22.2.1 | Muestra UX, nunca decide tenant, bucket ni permiso real |
| A09 | API .NET 10 | Autentica JWT, valida RBAC, tenant, cuotas, retención y errores |
| A10 | Servicio Application | Orquesta inventario/manifiesto y casos de uso sin SDK acoplado |
| A11 | Adaptador Infrastructure | Implementa contratos específicos del proveedor permitido |
| A12 | Metadatos MySQL/EF | Posible fuente de referencias, no prueba única de objetos existentes |
| A13 | Cloudinary actual documentado | Configuración real por ambiente requiere verificación autorizada |
| A14 | R2/almacenamiento futuro | Objetivo sujeto a autorización, no destino activado |

Los A07, A12–A14 no significan que exista acceso a un panel, inventario completo o proveedor habilitado. No se otorga permiso administrativo por redactar este contrato.

## Entradas I01–I14 y salidas O01–O14 candidatas

| Par | Entrada futura | Salida y aceptación futura |
| --- | --- | --- |
| I01/O01 | Principal JWT y contexto tenant validado | Identidad server-side o denegación genérica |
| I02/O02 | Alcance solicitado de inventario por ambiente | Alcance autorizado verificable o NO_GO |
| I03/O03 | Referencias de media en persistencia | Conteo de metadatos con procedencia y fecha, no conteo final automático |
| I04/O04 | Listado de objetos de proveedor autorizado | Conteo/keys opacas limitados, sin URL firmada ni secretos en reportes |
| I05/O05 | Diferencia entre referencias y objetos | Estado INCONSISTENTE con evidencia, nunca cero ficticio |
| I06/O06 | Cero confirmado en todas las fuentes | Estado CERO_VERIFICADO, rama sin exportación, copia o hash histórico |
| I07/O07 | Uno o más objetos conciliados | Estado POSITIVO_VERIFICADO y manifiesto sujeto a nueva autorización |
| I08/O08 | Owner, tenant, tipo y retención | Clasificación sin cross-tenant ni borrado bajo hold |
| I09/O09 | Upload nuevo con bytes/size/MIME reales | Validación de contenido y cuota, identificador opaco |
| I10/O10 | Firma de acceso temporal solicitada | Objeto/verbo/TTL acotados; política replay explícita |
| I11/O11 | Error, timeout o retry de proveedor | Error tipado y compensación, no objeto parcial ni fuga de token |
| I12/O12 | Correlación, tiempo UTC y actor autorizado | Auditoría saneada y trazable, sin PII/URL secreta |
| I13/O13 | Política de retención/lifecycle | Hold protegido y decisión reversible |
| I14/O14 | Solicitud de migración o cutover | Bloqueo por defecto sin permisos separados y rollback probado |

La existencia de I03 e I04 no autoriza ejecutar consultas. Para obtener un conteo real se requiere fuente, credencial propia, ámbito, permiso, técnica y reconciliación demostrables.

## Fronteras de confianza F01–F09

| Frontera | Riesgo | Requisito futuro |
| --- | --- | --- |
| F01 Browser→API | Header/slug controla tenant | Autorización server-side JWT/RBAC y pertenencia |
| F02 API→Application | Solicitud arbitraria de enumeración | Alcance, propósito, owner y denegación explícita |
| F03 Application→metadatos DB | Confundir referencias con objetos | Reconciliación DB/proveedor con evidencia |
| F04 Application→adaptador storage | Llamada con key libre | Identificadores opacos, scope y mínimo privilegio |
| F05 Adaptador→Cloudinary | Secretos/URLs en logs | Secrets externos, ACL y auditoría saneada |
| F06 Adaptador→R2 futuro | Confundir diseño con activación | No usar sin autorización, seguridad y pruebas efímeras |
| F07 Caché/CDN→cliente | Fuga entre tenants o URLs caducas | Partición A/B, expiración e invalidación |
| F08 Lifecycle→retención | Borrado de histórico bajo hold | Protección y recuperación antes de cualquier borrado |
| F09 Inventario→informes | Filtrar PII, object key y firmas | Conteos agregados sólo con permiso y redacción |

## Pruebas futuras propuestas: P01–P08 y N01–N12 (NO_EJECUTADAS)

**Positivas P01–P08:** P01 inventario autenticado descubre cero en todas las fuentes; P02 descubre objetos positivos conciliados; P03 almacena fixture nuevo A con MIME/bytes válidos; P04 B sólo ve B; P05 comprueba límites y checksum de fixture; P06 TTL válido/expirado con reloj de prueba; P07 fallo efímero se compensa sin parcial; P08 retención impide borrar y permite rollback sin datos reales.

**Negativas N01–N12:** N01 confundir NO_VERIFICADO con cero; N02 considerar solo MySQL e ignorar proveedor; N03 A enumera B; N04 cliente manipula EmpresaId o key; N05 rol lector realiza delete/export; N06 fixture nuevo se clasifica como histórico; N07 MIME por header no coincide con bytes; N08 stream supera quota y deja parcial; N09 URL firmada permite método fuera de alcance o replay no modelado; N10 logs contienen secretos/PII; N11 lifecycle elimina objeto bajo hold; N12 activar R2/DNS/cutover sin nueva orden y rollback.

**Ninguno de estos escenarios fue ejecutado en esta unidad.** Se requiere test causal .NET/Angular/MySQL/servicio efímero únicamente cuando la implementación afecte dichas capas y GitHub Actions sobre SHA exacto. No contabilizar SKIPPED/CANCELLED/IN_PROGRESS como PASS requerido.

## Impacto, auditorías y reversión

**Impacto exacto:** sólo este nuevo Markdown en docs/v5 y sección autocontenida de `CHANGELOG_AI.md`. No se modifican backend Domain/Application/Infrastructure/API, DTO/HTTP, frontend, MySQL, secretos, Cloudinary/R2, DNS, infraestructura ni QA/main/PROD.

- **A0:** Skill/Scope Lock, identidad, SHA, DAG, claim CAS y exclusión de archivos.
- **A1:** cotejo de actores A01–A14, contratos I/O01–14, fronteras F01–F09 y P/N futuros con C10-03.M01.01 y C10-02.M01.01–05; no se inventan conteos.
- **A2:** seguridad JWT/RBAC/tenant A/B, URLs bearer/TTL/replay, MIME, secretos, retención y default-deny revisados como contratos; no PASS funcional.
- **A3:** integración **documental** con arquitectura actual (.NET 10/Angular 22.2.1/MySQL/Cloudinary). CI runtime `NO_APLICA` para cambios sólo Markdown.
- **A4:** commit fast-forward CAS en dev, Scope Lock y verificador V5 CHANGELOG exact-SHA completed/success, PLAN/COLA/BITACORA readback.
- **A5:** revisión independiente posterior por otra Task Javier, nunca autoaprobación.

**Rollback:** `git revert` aditivo, sin force-push. M01.06, el inventario real, implementación M03 y promoción necesitan nuevos controles y autorización según el scope real.
