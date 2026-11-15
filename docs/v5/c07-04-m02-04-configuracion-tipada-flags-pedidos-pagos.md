# SOLQARYN V5 — C07-04.M02.04 configuración tipada y flags seguros de pedidos/pagos

**Autor:** PRIMARY DEV 06, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Repositorio:** solqaryn/Solqaryn@dev; **PROJECT_SCOPE_LOCK=STRICT**; 2026-11-14 America/Tegucigalpa.

## Autoridad, preflight y límite de alcance

El STANDARD V5 FINAL (Doc Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA) y PLAN_MAESTRO/COLA/DAG_TECNICO determinan ejecución. C07-04.M02.04 fila 1127 estaba PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI al claim; antecedentes M01.01–06 estaban LISTO+A5 independiente. Las otras M02 de diseño son scopes independientes y no se presentan como requisito cumplido si continúan VALIDANDO.

Contrato **documental y futuro** de configuración: no modifica valores en repositorio, .NET Options, env vars, secretos, RBAC, MySQL, PSP, fiscalidad o pagos. Sin migraciones, servidor de eventos, webhooks, QA/main/PROD, ni activación real de flags.

## Ámbitos y regla restrictiva de precedencia CFG01–CFG12

| ID | Parámetro lógico | Ámbito y restricción |
| --- | --- | --- |
| CFG01 | PedidosHabilitados | Plataforma, plan, tenant/sucursal; default false ante duda |
| CFG02 | CotizacionesHabilitadas | Plan autorizado y empresa verificable; no sustituye permiso usuario |
| CFG03 | ConversionCotizacionPedido | Requiere fases previas y control idempotente; default false |
| CFG04 | FacturacionHabilitada | Tenant, país, emisor fiscal y autorización explícita; default false |
| CFG05 | AplicacionPagosHabilitada | PSP aprobado, caja activa y permiso; default false |
| CFG06 | ProveedorPagoId | Referencia permitida en allowlist; nunca token o credencial en Sheet |
| CFG07 | MonedasPermitidas | País/plan/tenant; sin inferir conversión ni tasas vigentes |
| CFG08 | PrecisionImporte | Tipo decimal/restricción fiscal validada; no float |
| CFG09 | LimiteCobro | Tenant/sucursal/operador; mínimo de límites aplicables |
| CFG10 | IdempotencyTtl | Duración acotada por política y retención; no elimina historia fiscal |
| CFG11 | ExportacionDocumentos | Verificación de propietario + TTL + acceso por objeto |
| CFG12 | CallbackExternos | Deshabilitado hasta autorización externa y firma probada |

**Resolución:** autoridad institucional segura > prohibición plataforma > permisos producto/plan > bloqueos tenant > bloqueos sucursal/caja > permisos usuario > configuración operacional. Un flag true nunca eleva RBAC, nunca autoriza tenant distinto y no puede prevalecer sobre scope lock, A5, CI o pagos no contratados. En conflicto: **deny wins**. Si cualquier nivel carece de valor verificado, usar estado seguro (sin ejecutar efecto económico) y registrar código de diagnóstico redactado.

## Modelo de configuración efectiva CFG13–CFG18

- CFG13 SourceRevision: versión/timestamp auditables de catálogo autorizado, nunca datos client-side.
- CFG14 TenantVerified: sólo procedente del servidor tras JWT/membresía, no encabezado.
- CFG15 FeatureDecision: ENABLED, DISABLED, UNKNOWN; UNKNOWN equivale a deny para efectos.
- CFG16 SourceProvenance: origen de política (plan, empresa, sucursal) sin claves o PII.
- CFG17 EffectiveRestriction: mínimo seguro de límites y prohibiciones aplicables.
- CFG18 ChangeAudit: actor y motivo autorizados, antes/después, timestamp UTC y correlación sin secretos.

Estos nombres son especificaciones, **no DTO .NET existentes**, y no implican nuevo motor de política.

## Matriz de validaciones VAL01–VAL14

1. VAL01 Rechazar TenantId/EmpresaId del cliente como autoridad aunque el flag cliente sea true.
2. VAL02 Rechazar tenant sin membresía vigente y casos EmpresaId NULL legado.
3. VAL03 Rechazar pago con proveedor no permitido/no autorizado.
4. VAL04 Rechazar callback sin firma aun si CallbackExternos=true.
5. VAL05 No habilitar pago por permiso de cotización o por plan únicamente.
6. VAL06 La denegación plataforma y el estado UNKNOWN prevalecen sobre override de sucursal.
7. VAL07 Moneda y precisión no permiten desbordamiento o redondeo no fiscal.
8. VAL08 Límite menor prevalece; cero o negativo no constituye cupo.
9. VAL09 Cambios de rol/plan invalidan sesión de autorización operacional.
10. VAL10 No incluir credenciales, PAN/CVV, JWT ni API keys en configuración, logs o DTO.
11. VAL11 Exportar sólo documento propio, con URL efímera y revocación.
12. VAL12 IdempotencyTtl no permite duplicar efectos previos al expirar la key.
13. VAL13 El cambio de flags no reescribe pedidos/facturas previos ni historial fiscal.
14. VAL14 La ausencia de CI exact-SHA requerida mantiene estado VALIDANDO sin efectos económicos.

## Pruebas futuras positivas/negativas T01–T12 — NO_EJECUTADAS

| ID | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| T01 | Plan/tenant/sucursal permiten cotización A | Tenant B con plan distinto no hereda flags A |
| T02 | Operador autorizado convierte cotización A | Rol revocado no puede convertir |
| T03 | Facturación habilitada con emisor válido | Emisor sin RTN verificado no factura |
| T04 | Pago permitido con PSP contratado | PSP faltante/secret inválido no llama proveedor |
| T05 | Callback firmado idempotente concilia una vez | Callback repetido o manipulado no cobra dos veces |
| T06 | Restricción de límite aplica el mínimo | Override local más permisivo no eleva límite |
| T07 | Documento A se exporta con TTL | Documento B o enlace expirado no expone bytes |
| T08 | Moneda, precisión y total válidos se aceptan | Monto negativo/overflow falla sin escritura |
| T09 | Flag deshabilitado deniega creación | Frontend envía true y backend igual deniega |
| T10 | Cambio de plan autorizado queda auditado | Cambio no autorizado no habilita flags |
| T11 | Estado UNKNOWN sigue fail-closed | Falta configuración no habilita PSP por default |
| T12 | Cambio de configuración no muta documentos históricos | Retroactividad indebida se rechaza |

Son **oráculos a ejecutar** mediante xUnit, integración ASP.NET Core y MySQL 8.4 efímero cuando las fases de implementación lo autoricen. Ningún test HTTP/DB real se ejecutó por esta documentación.

## Dependencias, validación A0–A5 y rollback

M02.05 describe eventos conceptualizados, no los emite. M02.06 exige M02.01–05 LISTO+A5 para cierre contractual, y M03 requiere código real y CI por causa. No se altera el DAG ni se fuerza G/O=SI.

**A0:** validar los 18 CFG, 14 VAL, fuente, predecesores y dos Markdown del diff. **A1:** revisar consistencia de 12 pares futuros, no atribuir PASS runtime. **A2:** documentar JWT/RBAC/tenant A/B, secretos, fallback deny y PSP no aprobado. **A3:** contraste conceptual con arquitectura vigente y contratos M01, sin afirmar test .NET/Angular/MySQL. **A4:** commit fast-forward/HEAD CAS sólo dev, SHA, blobs, Scope Lock y V5 completed/success exact SHA; readback PLAN/COLA/BITACORA. **A5:** otra Task Javier distinta de la autora.

**Rollback:** git revert normal del documento+CHANGELOG, evento de compensación en BITACORA, sin reset/force-push ni cambios QA/main/PROD. No se crea API, webhook, dispatcher, migración ni pago.
