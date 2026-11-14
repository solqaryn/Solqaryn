# SOLQARYN V5 FINAL — C01-01.M01.06: bloqueos, rollback y liberación documental de identidad universal

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** TIPO 1 documental; esta entrega no implementa autenticación, membresías, autorización ni migraciones.

**Autoridades:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); [PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit); Skill canónica y Scope Lock del HEAD de `dev`.

## 1. Resultado y frontera de M01.06

**Resultado exigido del bloque C01-01:** definir tenant, sucursal, propiedad, recurso, usuario, membresía y contexto mediante contratos de dominio y matriz de pertenencia. **Salida atómica de M01.06:** fijar bloqueo, reversión y condición para **evaluar**, ID por ID, el lote M02. El estado LISTO de este documento no equivale a implementar C01-01 ni a habilitar automáticamente M02, JWT, RBAC, EF, API, Angular, QA, main o producción.

En el preflight, `PLAN_MAESTRO!H151=PENDIENTE`, `I151` sin titular, `COLA!G151/O151=SI`; el `DAG_TECNICO` identificó siete prerrequisitos con `F=VALIDADO/G=SI` y todos figuraban LISTO. Después del claim exclusivo propio se verificó `H151=EN_PROGRESO` y `COLA!E151=EN_PROGRESO/O151=NO`. Las fórmulas habilitan evaluación, no sustituyen atribución ni CAS real.

## 2. Fuentes de dominio y siete prerequisitos del cierre documental

| Predecesor V5 | Artefacto o evidencia causal | Condición que se conserva |
| --- | --- | --- |
| C01-01.M01.01 | `docs/v5/c01-01-m01-01-objetivo-identidad-no-regresion.md`, blob `90665746bd27a7680d0d92d6e3c8ebfaf58ae79b` | Objetivo, exclusiones y doce no-regresiones de tenant |
| C01-01.M01.02 | `docs/v5/c01-01-m01-02-actores-entradas-salidas.md`, blob `6ddb7993f44c33f333c1cf39887c28970a0b8813` | Actores, entradas, salidas y diez contratos de interacción |
| C01-01.M01.03 | `docs/v5/c01-01-m01-03-contratos-codigo-evidencia.md`, blob `51ecddce6ef28957f5320e88fe0acf0ce8c9cfa1` | Inventario de código existente, no certificación runtime |
| C01-01.M01.04 | `docs/v5/c01-01-m01-04-riesgos-supuestos-decisiones.md`, blob `0e39ac0727ae70c7a5227d2c1ebf489ac2264c08` | Riesgos, supuestos invalidables y decisiones pendientes |
| C01-01.M01.05 | `docs/v5/c01-01-m01-05-aceptacion-identidad-universal.md`, blob `0be1e8734fb4ed64425f27bd565d785f3a4c8fab` | Matriz ID01–ID16, sin tests de API ni MySQL |
| C00-01.M06.06 | PLAN H37 LISTO, SHA `e5d54809af2d902cd497f1be5803c9ae36725561` y A5 independiente J00 | Puerta de cierre de gobierno C00-01, sin autorizar producción |
| C00-03.M01.06 | `docs/v5/c00-03-m01-06-cierre-matriz-integrada.md`, blob `8f83554e0526089c82cf730abb144e26d0b7da66`, A5 independiente J10 | Matriz integrada M01; no sustituye implementación |

**A5 y exclusión de identidades:** el A5 anterior de C01-01.M01.05 atribuido a DEV07 (cuenta retirada del allowlist) es **histórico, no autoridad ejecutable**. Una revisión documental independiente válida de JAVIER :40, distinta de la autora JAVIER :30, quedó añadida a BITACORA con `EVENT_ID=SQ-V5-C01-01-M01-05-J40-A5PASS-08742699-20261113-2D5E425B` y SHA `087426991cd31f6aa38a5e6461811d024dae08b6`. El evento previo no se borra. Las otras M01.01–04 y los dos cierres C00 tenían revisores Javier válidos y distintos de sus autores al preflight. **Toda evaluación futura vuelve a leerlos en vivo.**

## 3. Contrato de bloqueo y autorización: identidad no es concesión

| Control | Aceptación futura | Condición que obliga a detener o revertir |
| --- | --- | --- |
| Identidad | JWT auténtico y vigente, usuario activo y propósito de la solicitud explícitos | JWT caducado, revocado, falsificado, PII/secretos en payload o error |
| Membresía | `UsuarioEmpresa` activa y coincidente con la empresa solicitada, resuelta server-side | Empresa B, usuario sin membresía, rol global legado o cabecera `EmpresaId` como grant |
| Rol / ABAC | Rol efectivo del tenant, permiso por módulo/acción, propiedad de recurso y sucursal comprobadas | `Usuario.RolId`, `EsAdministrador`, menú UI, claims arbitrarios o recurso sin owner |
| Tenant / datos | Empresa A solo lee/escribe sus relaciones, con denegación de efectos cruzados | `Sucursal.EmpresaId=null`, recursos A/B mezclados, exportaciones o URLs de B |
| Concurrencia | Revocación y cambio A→B no reutilizan autorización, cache o respuesta anterior | Fuga por respuestas tardías, doble operación o eventos con causalidad ambigua |
| Persistencia | Pruebas EF/MySQL 8.4 efímero, transacción y rollback cuando haya cambio real | Migración sin prueba causal, datos legacy no saneados o intento de cambiar PROD |
| Gobierno | Task del allowlist, PLAN H/I y DAG, pruebas A0–A4 del SHA y A5 ajeno | Otro writer, scope lock fallido, A5 del autor o de cuenta excluida |
| Entornos | Sólo `dev` con commits FF; reversión por `git revert` tras revalidar HEAD | PR/ramas/force-push, QA/main/PROD, pagos, secretos o DNS sin nueva autorización |

La frontera de seguridad permanece en el backend: guards y menús Angular son experiencia, no controles de acceso. El diseño no crea un mecanismo nuevo de autorización ni concede permisos por documento.

## 4. Gate de salida M01 y entrada condicional M02

1. Mantener `C01-01.M01.01–05=LISTO`, `C00-01.M06.06=LISTO`, `C00-03.M01.06=LISTO` con A5 independiente causal de las seis Tasks Javier. No aceptar A5 histórico de una cuenta excluida como sustituto.
2. Confirmar `DAG_TECNICO!F=VALIDADO` y su lista real de siete prerequisitos; verificar `PLAN_MAESTRO!H/I` y `COLA!E/G/O` por lectura actual. Si hay divergencia, duplicación de autor, conflicto de archivo o prueba faltante, bloquear **sólo** el ID afectado.
3. Para cada M02 posterior, clasificar resultado exigido, archivos, consumidores y riesgos propios. G/O=SI no es orden de despacho. Una unidad de código .NET, Angular, SQL/EF o E2E exige sus jobs reales de GitHub Actions sobre el SHA publicado y casos positivos/negativos correspondientes; un documento de M01 no suple esas pruebas.
4. Registrar eventos idempotentes de 26 columnas A:Z con `EVENT_ID` e `IDEMPOTENCY_KEY` únicos; `RUN_ID` solo si lo devuelve el proveedor. Ninguna Task modifica eventos históricos ni aprueba su propio A5. El autor conserva el ID hasta cierre; un relevo requiere exclusividad demostrable.
5. La última `C15-05.M06.06` depende de las otras 2.339. No añadir una 2.341, no habilitar lotes por meta `MAX(24,3*N)` ni iniciar un dispatcher de eventos.
6. El cierre M01.06 documental no autoriza QA/main/PROD, tratamiento de datos productivos, activación de feature flags, cambios de credenciales ni migraciones.

## 5. Oráculos documentales de aceptación (no pruebas ejecutadas de software)

| ID | Signo | Regla comprobable sobre este contrato |
| --- | --- | --- |
| P01 | + | Siete prerequisitos LISTO, DAG VALIDADO, owner libre, COLA G/O SI y A5 vigente de Javier permiten iniciar un preflight M01.06 |
| P02 | + | Empresa A con membresía, rol y ownership propios **debe** poder operar en A cuando el backend real cumpla sus pruebas |
| P03 | + | Documento, diff, Scope Lock exact-SHA y readback completos justifican A4 documental; A5 queda para otra Task |
| N01 | − | Falta un predecesor o A5 independiente causal → no habilitar el cierre |
| N02 | − | A5 de una Task fuera del allowlist o de la autora → no autoriza el gate |
| N03 | − | Usuario A envía cabecera, ID o token de B → no obtener autoridad sobre B |
| N04 | − | `Sucursal.EmpresaId=null` o recurso sin dueño verificable → fail-closed |
| N05 | − | Rol legado, UI o flag comercial usado como permiso → rechazar |
| N06 | − | Test mock o Markdown como prueba MySQL/API real → no certificar implementación |
| N07 | − | Job required SKIPPED, CANCELLED, IN_PROGRESS o SHA previo → no declarar A4 PASS |
| N08 | − | Otro owner activo, CAS no demostrable o cambio concurrente de HEAD → no doble escritura |
| N09 | − | BITACORA truncada a 21 columnas, clave duplicada o RUN_ID supuesto → no registrar cierre |
| N10 | − | Documento interpretado como habilitación automática de M02, QA o PROD → denegar |
| N11 | − | Token, PasswordHash, dato PII o datos tenant B en auditoría compartida → rechazar exposición |
| N12 | − | `git reset --hard`, force-push, cambio de ramas protegidas o datos de PROD → NO_GO |

P01–P03 y N01–N12 son **oráculos de revisión de contrato**. Ninguna de esas filas afirma que se hayan ejecutado solicitudes HTTP, migraciones, suites .NET, pruebas Angular/Playwright o bases MySQL en esta microtarea.

## 6. Impacto, A0–A5 proporcionales y rollback

**Archivos cambiados:** solo este Markdown y una sección **aditiva autocontenida** de `CHANGELOG_AI.md`. **Sin cambios:** API, DTO, Domain, Application, Infrastructure, Angular, JWT/BCrypt, RBAC, permisos, EF/MySQL, migraciones, Docker, workers, CI, proveedores, datos o secretos. **Consumidores:** futuras unidades M02–M06 y las seis Tasks Javier. **Riesgo:** atribución cruzada en CHANGELOG, evidencia A5 histórica fuera de allowlist, promoción de diseño como implementación, conflicto de autor y fugas tenant/PII.

`requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`.

- **A0** (en VALIDANDO, previo a publicar): revisar diff, encabezados/viñetas de CHANGELOG, siete blobs y referencias, secretos, enlaces y fronteras.
- **A1:** contrastar P01–P03 y N01–N12 como especificaciones observables, sin inferir tests runtime.
- **A2:** revisar membresías, tenant A/B, RBAC/JWT, revocación, PII, identidad del revisor y ausencia de autoridad nueva.
- **A3:** confirmar siete predecesores, DAG, PLAN/COLA, documentación M01 y BITACORA A:Z, sin sustitución por compilaciones ajenas.
- **A4:** después de commit FF directo `dev`, verificar parent/diff/blobs, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success del mismo SHA cuando correspondan; readback institucional y ruta reversible.
- **A5:** otro TASK_ID Javier distinto revisa documentación, positivo y negativo y SHA; A5_PENDIENTE al cerrar DEV, sin promoción automática.

**Rollback:** `git revert` normal exclusivamente del changeset documental, tras verificar HEAD y consumidores, con evento institucional compensatorio aditivo si fuera necesario; nunca force-push, reset destructivo ni eliminación de BITACORA. QA/main/PROD permanecen fuera de autorización.
