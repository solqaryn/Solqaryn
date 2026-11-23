# C02-04.M06.06 — cierre DEV condicionado y mapa de dependencias verificables (V24)

**Ámbito:** SOLQARYN, repositorio `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`.  
**Microtarea:** `C02-04.M06.06`; criterio atómico de `PLAN_MAESTRO!D433`: «Cerrar estado, liberar el siguiente lote y actualizar el mapa de dependencias».  
**Clase de este incremento:** R0, evidencia documental de reconciliación y decisión de gate. No modifica UI, HTTP, auth, base de datos, reglas de negocio, fórmulas, tareas físicas ni infraestructura.  
**Base de lectura:** `dev@17efe41d96f1d3bc6f18d86c657e890fead54f87`; datos vivos de la hoja institucional contrastados durante la corrida de 2026-11-22 (America/Tegucigalpa). El SHA indicado es la base de lectura, **no** un PASS del commit de este documento.

## 1. Autoridad y alcance exacto

La autoridad es [el STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), la fila 433 de [PLAN_MAESTRO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, la skill canónica y la arquitectura de `dev`.

`PLAN_MAESTRO!E433` contiene la meta **acumulativa** de fase («separar plataforma y ERP con MFA, reautenticación, motivo y acceso excepcional temporal») y **no** autoriza certificar esas prestaciones por el mero cierre documental de un ID. El presente incremento registra qué predecesoras constan terminadas y qué requisitos siguen sin acreditación. No declara aprobados MFA, reautenticación privilegiada, alta global de empresas, pagos, seguridad transversal ni despliegues.

## 2. Predecesoras: estado observado, no certificación extensiva

`DAG_TECNICO` para el ID exige `C02-04.M05.01-06+A5 ; C02-04.M06.01-05+A5`. Las once filas correspondientes `PLAN_MAESTRO!422:432` tenían `H=LISTO`, con evidencia `A5_PASS` independiente en `K`/BITACORA al leerlas. La comprobación de cierre debe repetirse en vivo antes de cambiar estados.

| ID | Evidencia de cierre | Limitación que se conserva |
| --- | --- | --- |
| M05.01 | LISTO + revisión histórica A5 | Navegación de navegador/sesión sintética no es JWT y MySQL real |
| M05.02 | LISTO + A5 independiente | Contrato y prueba acotada: no convertir en garantía de toda la API |
| M05.03 | LISTO + A5 independiente | JWT/HTTP y MySQL 8.4 fueron probados de forma dirigida, no todo SaaS |
| M05.04 | LISTO + A5 independiente | Chromium rutas: sesión sintética y alcance definido |
| M05.05 | LISTO + A5 independiente | Negativos JWT/tenant/IDOR acotados, no todos los endpoints |
| M05.06 | LISTO + A5 correctiva posterior, FAIL histórico intacto | 8 pruebas Chromium con sesión sintética y 9 de JWT/MySQL real en laboratorio separado; **no** viaje navegador→API firmada integral |
| M06.01 | LISTO + A5 DEV01 | Revisión de correspondencia de evidencia, no generación de nuevas pruebas |
| M06.02 | LISTO + A5 DEV06 | Registro del SHA/diff/artefactos, no certificación general |
| M06.03 | LISTO + A5 DEV06 | Riesgos, límites y decisiones explícitos |
| M06.04 | LISTO + A5 DEV06 | Reversión Git de pruebas, no rollback físico de migración productiva |
| M06.05 | LISTO + A5 DEV01 | Solicitud/revisión documental genuina, **no** PASS de toda M06 ni reparación UX UI-08 |

Huellas concretas recientes de A5: `M06.01` evento BITACORA 11741 / [SHA](https://github.com/solqaryn/Solqaryn/commit/0a4c4760baab304c3199d4ae32de13f7c69a042d); `M06.02` 11737 / [SHA](https://github.com/solqaryn/Solqaryn/commit/9f185ebc2b1732c2738e8596071fc859461f4950); `M06.03` 11731 / [SHA](https://github.com/solqaryn/Solqaryn/commit/362a2895d8a16fe886bd9beac0259ab1aaa72fc9); `M06.04` 11735 / [SHA](https://github.com/solqaryn/Solqaryn/commit/662e3e1a5cd68efd386d5e37ebc3e43751322da5); `M06.05` 11742 / [SHA](https://github.com/solqaryn/Solqaryn/commit/4c47f19fef938b3654342e7f62b229609150363c). El historial de FAIL no se elimina ni se reescribe.

## 3. Estado del mapa DAG y desbloqueo legítimo

En el censo de `DAG_TECNICO!A:G`, `C02-04.M06.06` figuró `F=VALIDADO` y `G=SI`; `COLA!G433=SI,O433=SI` antes de su CLAIM. El barrido textual de prerrequisitos devuelve **72 referencias descendientes adicionales**, incluidas las puertas de `C03-01.M03.01` y `C03-03.M03.01`. Esas sucesoras se mantenían en `G=NO` porque exigen **`C02-04.M06.06` LISTO + A5 independiente**, además de sus propios predecesores.

**Regla de salida, estricta:** esta entrega documental ni un estado `VALIDANDO` habilitan descendientes. Ninguna Task debe escribir manualmente `DAG_TECNICO!G`, `COLA!G/O`, cambiar fórmulas o simular `A5_PASS`. Solo al reunir A0–A4 causales, cerrar `H433` mediante candado V17/CAS, obtener A5 genuina de otra Task distinta de la autora y sus reparadoras, y releer PLAN/COLA/DAG/BITACORA, podrá afirmarse que alguna sucesora pasó a elegible **por sus fórmulas**. Incluso entonces, `G=SI` no autoriza producción ni pruebas no realizadas.

## 4. Riesgos abiertos y NO_GO expresos

- **UI-08, P1 conocido:** en `AUDIT_UI_BRECHAS` la creación de empresas de consola global sigue inhabilitada por controles de backend. `C02-04.M06.05` se limita a **SOLO_VERIFICAR**; no habilitarla por estética, no atribuirle reparación.
- No hay certificación transversal de MFA, reautenticación privilegiada, acceso excepcional temporal ni toda la separación plataforma/ERP por estos once cierres. Registrar `NO_MEDIDO` donde falte prueba completa.
- Las sesiones Chromium sintéticas y los tests HTTP/JWT/MySQL efímeros son pruebas reales de **alcances distintos**, no un único flujo navegador→API firmada de extremo a extremo.
- No hay permiso para `qa`, `main`, `PROD`, dinero real, secretos, migraciones productivas, nueva infraestructura o alteraciones de las seis automations.

## 5. A0–A5 y procedimiento de cierre/handoff

**A0:** scope documental identificado: evidencias de predecesoras, fórmula DAG, amenazas de desbloqueo anticipado, dependientes y rollback. Diff limitado a este expediente; sin arquitectura nueva.  
**A1:** comprobar en readback que cada una de las once predecesoras está LISTO con A5 independiente real; negativos: `VALIDANDO`, `SKIPPED`, `CANCELLED`, FAIL previo sin corrección o A5 del autor **no** satisfacen el gate. Comparar los 72 descendientes y no asegurar desbloqueo por simple coincidencia textual.  
**A2:** `NO_APLICA` a cambios nuevos de JWT/RBAC/tenant/HTTP, por no modificar runtime. **No** significa que la seguridad global haya sido certificada.  
**A3:** `NO_APLICA` a nueva integración MySQL/Chromium por este diff R0; las pruebas previas conservan su alcance real y sus limitaciones.  
**A4:** exigir SHA dev **nuevo** con `verify-solqaryn-project-scope` y checks aplicables `completed/success`, versión V5/procedencia cuando corresponda, correlación del documento y readback de PLAN/COLA/BITACORA. Si hay CI requerida `queued/in_progress`, mantener `VALIDANDO`, no marcar PASS.  
**A5:** revisor de otra Task Javier allowlist, distinto del autor de este expediente, debe inspeccionar críticamente enlaces, criterios negativos y fórmula real; no es una firma automática ni se sustituye por un autorreporte.

**Rollback:** revert ordinario del commit documental en `dev` conservando historial, sin force-push. Reversión de una documentación de gates **no** libera estados ni revierte acciones comerciales.

**Siguiente acción segura:** terminar A4 del commit de este expediente en GitHub-hosted, completar A0–A4 con los readbacks exactos y, si satisface `D433` sin declarar cerrados los P1 globales, cerrar este ID mediante V17/CAS; después exigir A5 independiente. Si el criterio `D433` se interpreta como liberar efectivamente descendientes en este mismo turno, mantener `VALIDANDO` hasta la secuencia real de cierre y A5; no desbloquear por inferencia.
