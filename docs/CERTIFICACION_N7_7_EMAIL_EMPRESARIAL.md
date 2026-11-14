# N7.7 — Email empresarial — Certificación canónica

## Autoridad y alcance

Autoridad operativa única: `docs/VAEP_AUTHORITY.md`.

Esta certificación documenta el cierre técnico de ERP-N7.7 en `Desarrollo`. `N7.7.H` es `DOC_CERT`: no autoriza cambios funcionales nuevos ni amplía el alcance a `main`, Producción, deploys, secretos o PR #2.

La dependencia inmediata es `N7.7.G`, cerrada como `LISTO_REAL` mediante `vaep/evidence/receipts/N7.7.G_LISTO_REAL_20260914T181734Z_SUP12.json`.

## Candidate funcional congelado

Candidate funcional final: `7a0765aa37533b8df2e52807f0a63800872c009d`.

Los commits posteriores de REVIEW_FIRST, receipts y reconciliación documental deben conservar equivalencia funcional con ese candidate. N7.7.H no agrega lógica de producto ni schema.

## Contrato certificado

La cadena N7.7 certifica, dentro del alcance realmente implementado y probado:

- servicio de correo empresarial tenant-scoped;
- idempotencia durable: replay de la misma solicitud no duplica persistencia y el reuso conflictivo de la misma clave falla cerrado;
- rechazo cross-tenant antes de persistir;
- autorización relacional y boundary de tenant en API;
- `Idempotency-Key` obligatorio en el envío;
- consultas acotadas y respuestas de error controladas mediante `ProblemDetails`;
- auditoría sin persistir contenido sensible del correo;
- regresión MySQL y migraciones actuales en verde;
- E2E de correo real con SMTP, PDF, reintento y prevención de duplicados;
- gates de seguridad, hardening y auditoría de dependencias en verde.

Esta certificación no usa como PASS señales no causales del proveedor de deploy ni fallos de suites ajenas al flujo Email Empresarial.

## Dominio, persistencia y Application/API

Los receipts N7.7.B-C certifican el dominio y la persistencia de Email Empresarial. No existe schema delta atribuido a N7.7.G-H.

`N7.7.D` y las fases posteriores preservan la separación tenant/RBAC en el boundary HTTP. El controller autenticado exige tenant coincidente entre ruta y header, permisos relacionales y `Idempotency-Key` para las operaciones mutantes cubiertas.

## Seguridad, auditoría y observabilidad

`N7.7.F` quedó `LISTO_REAL` mediante `vaep/evidence/receipts/N7.7.F_LISTO_REAL_20260914T180430Z_SUP12.json`. El candidate funcional final es `7a0765aa37533b8df2e52807f0a63800872c009d`; REVIEW_FIRST quedó P0=0/P1=0 y los controles de RBAC, tenant boundary, auditoría y seguridad quedaron certificados antes de N7.7.G.

## QA, gates y equivalencia

`N7.7.G` certificó el mismo candidate funcional con REVIEW_FIRST P0=0/P1=0 y los gates causales:

- `34878120842/104090379270` — Backend Release y pruebas: `SUCCESS`;
- `34878120842/104090379470` — Frontend producción: `SUCCESS`;
- `34878120842/104090379657` — higiene de repositorio: `SUCCESS`;
- `34878120842/104090379730` — Docker y aislamiento: `SUCCESS`;
- `34878120842/104090379936` — migraciones EF + pruebas `Category=Integration` en MySQL 8.4: `SUCCESS`;
- `34878120666/104090324122` — autorización/files/secrets/tenant: `SUCCESS`;
- `34878120666/104090324618` — backup/restore MySQL aislado: `SUCCESS`;
- `34878120877/104090398621` — configuración, aislamiento y hardening: `SUCCESS`;
- `34878120877/104090398463` — dependencias .NET vulnerables: `SUCCESS`;
- `34878120877/104090398123` — auditoría npm de producción: `SUCCESS`;
- `34878120656/104090324430` — contratos estáticos/tests/build: `SUCCESS`.

El artifact de aceptación del run `34878120447` demuestra que `fase7-correo.spec.ts` pasó el caso de flujo real SMTP, envío de PDF, reintento y prevención de duplicados; el log SMTP asociado registra operaciones HTTP 200. El fallo global de ese run provino de suites Fase 4/Fase 8 ajenas a Email Empresarial y fue clasificado no causal, no como PASS.

La señal Vercel `build-rate-limit` es externa al alcance TEST_CI de N7.7 y tampoco se usa como PASS.

N7.7.H no debe rerunear gates funcionales costosos mientras su delta permanezca estrictamente documental/evidencia y la equivalencia funcional con `7a0765aa37533b8df2e52807f0a63800872c009d` quede demostrada. Sus gates propios son integridad documental, preservación byte-prefix de históricos, REVIEW_FIRST final y readback del receipt.

## Operación y rollback

Operación: mantener autorización relacional, aislamiento tenant, idempotencia durable y logs/auditoría sin contenido sensible. Mantener consultas paginadas/acotadas y errores controlados.

Rollback seguro: revertir únicamente cambios funcionales atribuibles a N7.7 cuando una regresión causal lo exija. N7.7.H no autoriza deploy, modificación de secretos, cambios de Producción o `main`, ni borrado/reescritura de evidencia histórica.

## DoD de N7.7.H

Para `N7.7.H=LISTO_REAL` deben cumplirse conjuntamente:

1. dependencia N7.7.G releída y válida;
2. SHEET_SCHEMA_GUARD sobre la fila existente de N7.7.H;
3. esta certificación persistida y releída;
4. `TASKS.md` y `CHANGELOG_AI.md` reconciliados de forma estrictamente aditiva/history-preserving porque el punto cambia de estado;
5. prefijo byte-exacto de cada histórico demostrado y tamaño posterior mayor;
6. REVIEW_FIRST fresco con P0=0/P1=0;
7. equivalencia funcional demostrada contra `7a0765aa37533b8df2e52807f0a63800872c009d`;
8. receipt H persistido y releído antes de promover `N7.8.A`.

Esta certificación no declara por sí sola `LISTO_REAL`.
