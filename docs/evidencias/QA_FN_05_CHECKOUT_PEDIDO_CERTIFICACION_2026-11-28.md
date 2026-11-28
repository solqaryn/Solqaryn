# QA-FN-05 — Certificación del checkout y pedido durable
Fecha: 2026-11-28. Estado: CERTIFICADO para el alcance de checkout comercial en QA (cinco controles). No equivale a certificación de toda QA-FN-05 ni de cobros.

## Identidad y alcance
- Repositorio solqaryn/Solqaryn, rama qa, QA_MANUAL_OWNER, PROJECT_SCOPE_LOCK=STRICT. Cuerpo de Skill instalada y canónica QA idéntico; Scope Lock aprobado.
- Angular 22.2.1, Node 24.x, ASP.NET Core 10, EF Core 10.0.12, Oracle MySql.EntityFrameworkCore 10.0.9; MySQL QA 8.4.11.
- HEAD de evidencia 8c68033e962b09487cfe6ccdb188ccde99784b58. Render LIVE 88dc7c095fd63474dfbbe5e7b5ad9d269b8ba5b4, deploy dep-dc5ik0ajnfac73dbhl9g. Vercel READY 8c68033e962b09487cfe6ccdb188ccde99784b58, deploy dpl_FAGmsjFhCdizhaKr2k2r5Ts3r9JM, alias solqarynqa-nu.vercel.app.
- Comparación GitHub entre ambos SHA: únicamente script y workflow de prueba del pedido y CHANGELOG_AI.md; backend, frontend, proxy, migraciones, configuración e identidad sin cambios.
- 19:40:22 UTC: GET https://solqarynqa-nu.vercel.app/api/health/ready HTTP 200, status=ready, database=connected, X-Solqaryn-Environment=QA, X-Render-Origin-Server=Kestrel, no-store, Vercel cache MISS.

## Cinco controles cerrados
| Control | Evidencia |
| --- | --- |
| 1. Ensayo correctivo del respaldo | [46178800298](https://github.com/solqaryn/Solqaryn/actions/runs/46178800298) SUCCESS |
| 2. Aplicación segura de migración existente | [46179506984](https://github.com/solqaryn/Solqaryn/actions/runs/46179506984) SUCCESS; [46179506929](https://github.com/solqaryn/Solqaryn/actions/runs/46179506929) SUCCESS |
| 3. Sincronización Render/Vercel/Aiven | Deploys y readiness anteriores; equivalencia funcional |
| 4. Pedido real desplegado y limpieza | [46181529540](https://github.com/solqaryn/Solqaryn/actions/runs/46181529540) etapas funcionales PASS; [46182209196](https://github.com/solqaryn/Solqaryn/actions/runs/46182209196) SUCCESS |
| 5. Certificación final del alcance | Reconciliación causal, runtime y limpieza documentada aquí |

## Datos y respaldo
Migración existente 20261128183000_QaCheckoutPedidoV6 aplicada una vez. Historial EF 114 -> 115; 138 tablas restauradas, conteos exactos del snapshot; datos históricos, IAM y restricciones preservados. QA_CHECKOUT_LIVE_MIGRATION=PASS. Respaldo AES-256 verificado: [artefacto 15277260845](https://github.com/solqaryn/Solqaryn/actions/runs/46179506984/artifacts/15277260845), retención limitada. Mantenimiento retirado mediante Maintenance__Enabled=false; readiness verificado después del redeploy. No repetir DDL.

## Evidencia funcional y de seguridad
Run 46181529540/job 141091953351: QA_ORDER_QUOTE, SERVER_PRICES, FOREIGN_PRODUCT_DENIED, TAMPER_REJECTED, CREATE, REPLAY, KEY_CONFLICT, RECEIPT, NO_KEY_DENIED, WRONG_KEY_DENIED, PERSISTENCE, ERP_LOGIN_A, ERP_LOGIN_B, ERP_READ_A, ERP_B_DENIED, ERP_TENANT_SPOOF_DENIED, ERP_ANONYMOUS_DENIED y DEPLOYED_FUNCTIONAL=PASS.
Pedido sintético ID 3, Borrador, dos unidades a 100, subtotal 200, envío 37.25, total 237.25 en moneda QA. Un detalle, un pedido y un cliente; persistencia física y snapshot comprobados. Inventario intacto; ausencia de efectos enlazados según catálogo FK. Lectura ERP autenticada, rol de solo lectura y aislamiento A/B. Pedido y cliente retirados: QA_ORDER_ERP_IAM_CLEANUP=PASS y QA_ORDER_DRAFT_CLEANUP=PASS.

## Recuperación
La primera ejecución terminó failure por MySQL 1451 / SQLSTATE 23000 exclusivamente al limpiar catálogo sintético; la sentencia agrupada no identificó una FK única. Intento 46182161053 falló al compilar el script antes de acceder a Aiven. Run 46182209196/job 141094072906 verificó identidad del conjunto anterior, FK físicas incluidas compuestas, eliminación protegida y ausencia de residuos. QA_ORDER_CATALOG_CLEANUP=PASS inactive_audit_company=0 y QA_ORDER_FAILED_RUN_CLEANUP_RECOVERY=PASS. Empresa sintética eliminada; no se borraron auditorías ni se deshabilitaron FK. No se recreó pedido.

## Regresión causal
- [46176430491](https://github.com/solqaryn/Solqaryn/actions/runs/46176430491): build y pruebas backend/frontend, scope, 77 pruebas de navegador aprobadas; regresión local/controlada, no una compra real desde UI.
- [46176430512](https://github.com/solqaryn/Solqaryn/actions/runs/46176430512): certificación viva de disponibilidad, aislamiento, proxy y storefront.
- [46176353884](https://github.com/solqaryn/Solqaryn/actions/runs/46176353884): persistencia MySQL dirigida, 1 aprobada, 0 fallos.
- [46176353875](https://github.com/solqaryn/Solqaryn/actions/runs/46176353875): propiedad comercial MySQL A/B.
- [46176353844](https://github.com/solqaryn/Solqaryn/actions/runs/46176353844): esquema Oracle EF.
- [46176353859](https://github.com/solqaryn/Solqaryn/actions/runs/46176353859): barrido de rutas/tema en cuatro grupos de resoluciones.
- Gate 46179506984 verificó equivalencia y siete gates causales; gate de recuperación verificó equivalencia funcional respecto del SHA aplicado.

## Límites
Se cierra checkout -> pedido durable en borrador -> recibo -> lectura ERP mediante prueba de API real y regresión frontend. No se certifica una transacción de tarjeta, liquidación bancaria, factura, cobro real ERP, reserva o confirmación, entrega externa WhatsApp ni un recorrido nuevo completo de navegador contra la API. Tarjeta conserva su contrato fail-closed. Descuentos/impuestos y resto de QA-FN-05 no se declaran cerrados por inferencia. Advertencia CS8604 sobre moneda nullable observada en gate MySQL aprobado. Sin cambios de código funcional, datos, infraestructura, DEV o MAIN en este cierre. No repetir gates ni despliegues sin cambio causal.
