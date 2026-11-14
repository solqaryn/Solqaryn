# N7.6 — Integración API de WhatsApp Business — Certificación canónica

## Autoridad y alcance

Autoridad operativa única: `docs/VAEP_AUTHORITY.md`.

Esta certificación documenta el cierre técnico de ERP-N7.6 en `Desarrollo`. `N7.6.H` es `DOC_CERT`: no autoriza cambios funcionales nuevos ni amplía el alcance a `main`, Producción, deploys, secretos o PR #2.

La dependencia inmediata es `N7.6.G`, cerrada como `LISTO_REAL` mediante `vaep/evidence/receipts/N7.6.G_LISTO_REAL_20260914T155920Z_SUP48.json`.

## Candidate funcional congelado

Candidate funcional final: `abb4a3bfdbe0d2896abcf33e5c9547e1dfc1016b`.

Los commits posteriores de REVIEW_FIRST, receipts y reconciliación documental deben conservar equivalencia funcional con ese candidate. N7.6.H no agrega lógica de producto ni schema.

## Contrato certificado

La cadena N7.6 certifica, dentro del alcance realmente implementado y probado:

- configuración WhatsApp tenant-scoped mediante `ConfiguracionWhatsAppEmpresa`;
- número de teléfono en formato E.164 y referencias opacas para material sensible, sin campos de credencial en claro;
- persistencia EF/migración de configuración con constraints compatibles con los providers de prueba certificados;
- boundary HTTP de WhatsApp con comportamiento fail-closed y autorización tenant/RBAC donde corresponde;
- endpoint de webhook revisado explícitamente como superficie anónima acotada;
- flujo de inicio/configuración que no inventa una sesión ni un QR cuando el proveedor no ha entregado uno;
- frontend de configuración que usa tenant verificado, mantiene `Configuracion:Editar` reactivo y no presenta estado de proveedor como listo sin respuesta verificada;
- render de QR limitado a payload `data:image`;
- controles de RBAC, tenant boundary, validación, auditoría/observabilidad y no exposición de secretos;
- regresión de backend, frontend, seguridad, persistencia y contratos aplicables sobre el candidate final.

Esta certificación no afirma capacidades no demostradas por la implementación o los gates (por ejemplo, entrega de mensajes de proveedor fuera del contrato observado).

## Modelo y persistencia

`N7.6.B` materializó el modelo `backend/src/Domain/Entities/ConfiguracionWhatsAppEmpresa.cs` y sus pruebas de dominio. El receipt canónico es `vaep/evidence/receipts/N7.6.B_LISTO_REAL_20260914T112812Z_SUP12.json`.

`N7.6.C` materializó la migración y configuración de persistencia sobre candidate `1492e1cd6fb915ceebf9c360134c84db8d29405a`. REVIEW_FIRST terminó P0=0/P1=0 y los gates de backend, MySQL y recovery parcial finalizaron en `SUCCESS`; receipt `vaep/evidence/receipts/N7.6.C_LISTO_REAL_20260914T122316Z_SUP12.json`.

No existe schema delta posterior atribuido a N7.6.D-G.

## Application/API

`N7.6.D` certificó el boundary Application/API sobre candidate `987e07e2f2d328ad21aa1558f1674e67048970b6`. La recuperación same-run incorporó el webhook anónimo exacto a la allowlist revisada de autorización y los gates causales de backend/Docker terminaron `SUCCESS`. Receipt: `vaep/evidence/receipts/N7.6.D_LISTO_REAL_20260914T142400Z_SUP00.json`.

La superficie verificada conserva fail-closed ante configuración/token inválidos y evita inventar sesión/QR. Los endpoints autenticados de configuración respetan tenant y permiso `Configuracion/Editar` conforme a las pruebas certificadas.

## Frontend/UX y seguridad

`N7.6.E` certificó `frontend/src/app/features/configuracion/whatsapp-business-card.component.ts` y sus pruebas dirigidas: tenant obtenido de contexto verificado, permiso reactivo, estado truthful y render de QR acotado. Receipt: `vaep/evidence/receipts/N7.6.E_LISTO_REAL_20260914T153315Z_SUP12.json`.

`N7.6.F` certificó RBAC, tenant boundary, auditoría/observabilidad, validación, hardening y ausencia de exposición de secretos sobre implementación ya existente, sin delta de producto o schema en esa fase. Receipt: `vaep/evidence/receipts/N7.6.F_LISTO_REAL_20260914T154700Z_SUP24.json`.

## QA, gates y equivalencia

`N7.6.G` congeló el candidate `abb4a3bfdbe0d2896abcf33e5c9547e1dfc1016b` con REVIEW_FIRST P0=0/P1=0 y certificó los gates causales:

- `34864310838/104044575850` — static contracts/tests/production build: `SUCCESS`;
- `34864310860/104044621837` — API/RBAC/audit/MySQL/Angular E2E: `SUCCESS`;
- `34864311032/104044562880` — MySQL/backend/Angular/Playwright regression: `SUCCESS`;
- `34864310684/104044559652` — frontend unit: `SUCCESS`;
- `34864310645/104044379586` — security/configuration isolation: `SUCCESS`;
- migración N7.6.C reutilizada causalmente desde run `34841294854`, sin schema delta posterior.

Los checks legacy explícitamente clasificados como no causales en el receipt de G no se usan como PASS.

N7.6.H no debe rerunear gates funcionales costosos mientras su delta permanezca estrictamente documental/evidencia y la equivalencia funcional con `abb4a3bfdbe0d2896abcf33e5c9547e1dfc1016b` quede demostrada. Sus gates causales propios son la integridad documental, la preservación byte-prefix de históricos, REVIEW_FIRST final P0=0/P1=0 y readback del receipt.

## Operación y rollback

Operación: conservar tenant/RBAC antes de operaciones autenticadas, mantener fail-closed ante configuración incompleta o verificación inválida y no presentar disponibilidad de proveedor que no haya sido confirmada por respuesta verificada.

Rollback seguro: revertir únicamente cambios funcionales atribuibles a N7.6 cuando una regresión causal lo exija. N7.6.H no autoriza deploy, modificación de secretos, cambios de Producción o `main`, ni borrado/reescritura de evidencia histórica.

## DoD de N7.6.H

Para `N7.6.H=LISTO_REAL` deben cumplirse conjuntamente:

1. dependencia N7.6.G releída y válida;
2. SHEET_SCHEMA_GUARD sobre la fila existente de N7.6.H;
3. esta certificación persistida y releída;
4. `TASKS.md` y `CHANGELOG_AI.md` reconciliados de forma estrictamente aditiva/history-preserving;
5. prefijo byte-exacto de cada histórico demostrado y tamaño posterior mayor;
6. si Git muestra una sustitución lógica de la última línea por ausencia previa de LF, esa representación no invalida el gate cuando `cmp`/hash demuestran cero mutación de bytes históricos;
7. REVIEW_FIRST fresco con P0=0/P1=0;
8. equivalencia funcional demostrada contra `abb4a3bfdbe0d2896abcf33e5c9547e1dfc1016b`;
9. receipt H persistido y releído antes de promover `N7.7.A`.

Esta certificación no declara por sí sola `LISTO_REAL`.
