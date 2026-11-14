# P0-011.A — Frontera de la versión comercial inicial (DEV)

**Corte del acta:** 2026-10-25; **base de inventario:** `dev@6a6d6e8870b0a53db9b3dc19f197be409082b6e6`.  
**Microtarea:** P0-011.A · **Task:** `6adbbc9ad9e88191bea8eb69104afb25` · **actor GitHub:** `morales35alex`.  
**Fuente normativa ÚNICA:** Google Doc `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), V3, apartados «2. Frontera estricta», «7. Prioridades» y «9. Plan operativo», ficha P0-011. Ningún plan maestro histórico concede trabajo.

## Regla de clasificación

- **INICIAL:** forma parte del producto vendible de la versión comercial inicial; la clasificación NO acredita implementación, pruebas, despliegue ni estado LISTO. Cada microfase conserva sus dependencias, CI, pruebas de negocio, aislamiento por tenant y permisos server-side.
- **CONDICIONAL:** se prepara y documenta sin activación automática; requiere evidencia externa y/o autorización posterior explícita del propietario. No convierte gates de QA, PROD o DNS/TLS en tareas DEV ejecutables ahora.
- **FUTURO:** visión pospuesta; no se inicia implementación por menciones a un documento maestro, por un ejemplo o por estar presente un módulo antiguo. Las funciones del ERP que YA existan deben preservarse y auditarse, sin reimplementarlas.
- **NO_CERTIFICADO:** clasificación de alcance, no conclusión técnica; una mención a infraestructura, migración, seguridad o UX no sustituye su readback causal.

## Matriz ejecutable de alcance (una clase por requisito)

| ID | Requisito | Clase | Justificación y límite verificable | Fuente |
| --- | --- | --- | --- | --- |
| C01 | SaaS shared MySQL y binding por tenant | INICIAL | El ERP comercial utiliza contexto empresarial; aislar datos en backend y persistencia es exigible, pero esta acta no certifica el esquema físico DEV. | §2, §3 |
| C02 | ERP actual con módulos ya presentes | INICIAL | Preservar y auditar las funciones existentes, reutilizando evidencias exactas antes de crear código; ninguna pantalla implica funciones completas. | §2 |
| C03 | Roles plataforma, empresa y operador | INICIAL | Permisos se validan server-side por empresa y capability; superadmin no lee otras empresas automáticamente sin contexto auditado. | §3 |
| C04 | Tiendas públicas, catálogo y compra habilitada | INICIAL | El producto público A/B necesita aislamiento, publicación y pedido/checkout comprobados; catálogo no se presume por identidad visual. | §2 |
| C05 | Ruta corta pública mediante /{slug} | INICIAL | Es la alternativa inicial sin DNS externo; un slug no sustituye EmpresaId ni concede lectura privada. | §2, §7 |
| C06 | Administrativo responsive y menú jerárquico | INICIAL | La navegación, modales, alertas, teclado y responsive son parte del lanzamiento; requieren pruebas visuales por componente. | §2, §7 |
| C07 | Contacto, demo y contratación manual | INICIAL | Oferta comercial inicial sin suscripción automática; no introducir cobros recurrentes ni facturación SaaS avanzada. | §2 |
| C08 | JWT, RBAC, auditoría y denegación multiempresa | INICIAL | Son controles transversales ineludibles y no dependen de la interfaz; deben superar regresión positiva y negativa propia. | §2, §3 |
| C09 | Backup, restore y reportes actuales | INICIAL | Exigir evidencia de restauración aislada y reportes disponibles antes de certificarlos; no ejecutar migraciones de datos desde este inventario. | §2, §7 |
| C10 | Sistema de componentes y validación visual | INICIAL | Se reutiliza frontend actual y se verifica UX/a11y proporcionalmente; un inventario estático nunca acredita E2E o accesibilidad. | §2, §7 |
| C11 | Subdominios empresariales | CONDICIONAL | Pueden prepararse sin activar DNS/TLS ni alterar dominios; comprobar ownership y aislamiento antes de cualquier despliegue. | §2, §7 |
| C12 | Dominios propios, certificados y DNS externos | CONDICIONAL | Se activan únicamente con DNS/TLS reales, decisión explícita y controles de seguridad; no bloquear ruta corta comercial. | §2, §7 |
| C13 | Promoción a QA y producción | CONDICIONAL | Requiere gates y autorización propia posterior; una PR fusionada en dev o CI verde no concede despliegue a otros entornos. | §7 |
| C14 | Dedicated/shared placement dinámico y cells | FUTURO | El cambio a bases dedicadas por tenant y migración automatizada pertenece a la visión pospuesta, no al shared actual. | §2 |
| C15 | Marketplace logístico, app repartidores y fulfillment avanzado | FUTURO | No forma parte de la entrega comercial inicial de este ERP; no agregar servicios logísticos por asociación temática. | §2 |
| C16 | Experience Studio, builders y personalización arbitraria | FUTURO | El editor dinámico completo es evolución posterior; ni el Maestro ni una referencia nominal autoriza implementarlo ahora. | §2 |
| C17 | Suscripción self-service, metering y billing SaaS | FUTURO | La contratación inicial es manual; no habilitar upgrades automáticos, cobros ni cuotas de pago por defecto. | §2 |
| C18 | SCIM/SAML completos, agentes IA y API Economy/SDK | FUTURO | Las integraciones enterprise e IA se difieren; no introducir SDKs, flujos de identidad nuevos ni servicios comprados. | §2 |
| C19 | Industry packs, DMS/GRC/EHS/ESG, PPM/PLM y fabricación | FUTURO | Expansión vertical enterprise excluida del primer lanzamiento; módulos heredados, si existen, solo se preservan. | §2 |
| C20 | Compras nuevas y upgrades pagados automáticos | FUTURO | La versión inicial tiene presupuesto de cero compras; ninguna microtarea puede contratar o aumentar planes. | §2 |

## Decisiones y regresiones negativas

1. **No confundir clase con estado:** INICIAL identifica producto objetivo; cada unidad permanece según PLAN_MAESTRO/COLA. No se cambian estados ajenos ni se proclaman P0/P1=0.
2. **No hacer trabajo del plan histórico:** Experience Studio, IA, plataforma enterprise y logística avanzada quedan FUERA de esta microtarea. Ninguna entrada FUTURO origina un claim automático.
3. **No forzar ambientes externos:** QA, main, PROD, DNS, Vercel, Render, MySQL ni secretos cambian en este PR. Los dominios propios siguen CONDICIONAL.
4. **No inferir certificado desde CI:** una prueba de texto/Node verifica la consistencia de esta matriz, no ventas, E2E, restore físico ni despliegue. Para paridad Render/Vercel ver P0-001.B/P0-003.B, sin asumir READY.
5. **No sustituir autorización:** los guards de Angular son experiencia; el backend autentica y autoriza. `/{slug}` jamás habilita acceso cross-tenant.

## Pruebas reproducibles y alcance técnico

`node scripts/verify-p0-011-a-initial-scope.mjs --self-test` lee esta tabla, valida 20 IDs únicos, clases esperadas y justificaciones completas, y ejecuta fixtures negativos de duplicado, cambio FUTURO→INICIAL, activación de dominio, fila faltante y razón vacía. También acepta `--check` para verificar el documento sin mutaciones.

**Impacto:** documentación y validador Node sin dependencias externas. **No hay** cambios en controladores, servicios, DTOs, modelos MySQL, migraciones, JWT, CORS, RBAC, rutas frontend, secretos, jobs operativos ni proveedores. **Consumidor:** P0-011.B deberá revisar independientemente la negativa y cualquier brecha de clasificación; no se libera hasta que P0-011.A sea LISTO. **Rollback:** revert protegido del acta/validador/workflow si el criterio cambiara; sin tocar datos ni entornos.

**Cierre:** requiere checkout + prueba nativa, CI exacto 3/3, revisión distinta del autor, merge protegido a `dev`, y readback de `PLAN_MAESTRO!H22:M22`, `COLA!A22:N22` y `BITACORA`. Hasta entonces `VALIDANDO` o `EN_PROGRESO`, nunca LISTO anticipado.
