# C15-03.M05.05 — reparación de Playwright Sucursales en tenant único

Fecha: 2026-11-20. Entorno exclusivo: SOLQARYN/dev. Riesgo R2 de autorización/tenancy. Autor de la reparación: DEV01; owner histórico C15-03.M05.05: DEV03.

## Defecto causal
El job GitHub-hosted 136156658402 de `ERP-N1.1 - Certificación Sucursales`, SHA b5824596e6d22213ff0e958a8816c9d72a3acbe0, acreditó 5 pruebas HTTP/API, pero falló `UI expone mantenimiento protegido y no desborda en móvil` con `TimeoutError: locator.waitFor: Timeout 5000ms exceeded` para `ID de empresa`. Con única membresía verificada la UI puede entrar al dashboard sin presentar el selector. El fallo no prueba por sí solo que el backend niegue al tenant ni autoriza relajar esa frontera.

## Prueba reparada y criterios adversariales
En `frontend/e2e/n1-1-sucursales.spec.ts` la UI debe alcanzar selector real (si es multitenant) o dashboard (membresía única) dentro de 20 segundos. Si aparece selector, exige elegir tenant con controles reales. En las dos rutas comprueba navegación a dashboard, JWT real de tres segmentos en localStorage y autorización HTTP 200 en `GET /tenant-context/{empresaId}` con ese JWT, incluida coincidencia de `empresaId`. Un login estancado, JWT ausente/malformado, denegación 401/403 o un tenant diferente hacen fallar el caso. No hay mocks de autenticación ni cambios de código productivo.

Los otros escenarios verifican 401 anónimo, duplicados, normalización, auditoría, idempotencia, filtros, edición, responsive y baja lógica en MySQL 8.4 efímero.

## A0-A5, limitaciones y rollback
A0: diff limitado a E2E + este documento + changelog preservado íntegro. A1/A2/A3: ejecutar GitHub Actions `ERP-N1.1 - Certificación Sucursales` sobre nuevo SHA exacto y leer logs de Chromium/.NET/MySQL reales; no inventar PASS antes de CI. A4: Scope Lock STRICT/V5/seguridad y checks obligatorios por SHA, readback institucional. A5: otra Task Javier distinta del autor y reparador, tras A0-A4 completos.

No se ha implementado ni validado todo el aislamiento granular de sucursales/archivos/jobs, ni todos los P0/P1 de C15-03.M05.05. Conservar H=VALIDANDO, I=DEV03 y fallos A5 previos. No QA/main/PROD, secretos, grants ni datos reales. Rollback: revertir únicamente el commit de reparación con CAS de HEAD y registros históricos intactos.
