# SOLQARYN V5 FINAL — C14-04.M01.02: actores, entradas, salidas y fronteras de puesta en marcha

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 03 JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Rama/ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** M01.02, contrato documental de interacción; **no** implementación, soporte habilitado, monitoreo desplegado, restore ejecutado ni release.

**Fuentes autorizadas:** [STANDARD DE IMPLEMENTACIÓN V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `references/architecture.md`, y [M01.01](./c14-04-m01-01-alcance-puesta-en-marcha-soporte.md). PLAN fila 2127, COLA G/O=SI y DAG VALIDADO/SI validaron **sólo esta preparación**.

## 1. Propósito y modelo de interacción

Precisar actores, insumos, entregables y límites de confianza que necesitaría un proceso futuro de salida controlada de landing, dominios, almacenamiento/medios, soporte, alertas, backup y continuidad. El criterio de aceptación es que cada interfaz tenga autoridad, dueño de dato, evidencia requerida, riesgo de aislamiento, fallo esperado y vía de reversión identificables **antes** de construirla o activarla. Esta unidad no declara disponibilidad del sitio, SLO, observabilidad real, prueba de recuperación ni derecho a desplegar.

Los contratos se separan en **actuales documentados**, que requieren verificación causal por entorno, y **futuros propuestos**, que permanecen `NO_IMPLEMENTADO`. Render/Vercel/Aiven/Cloudinary figuran como proveedores documentados actuales; Cloudflare/Hostinger/R2 como topología objetivo no contratada ni activada por este cambio. El navegador nunca determina tenant, empresa ni permiso; las autorizaciones finales residen en API ASP.NET Core net10.0, JWT/RBAC y persistencia MySQL/EF.

## 2. Actores y responsabilidades A01–A14

| ID | Actor/rol | Responsabilidad propuesta y límite |
| --- | --- | --- |
| A01 | Propietario del sistema | Autoriza por separado cambios fuera de dev; aprueba ventana, riesgo y rollback |
| A02 | Task autora en dev | Entrega cambio y evidencia A0–A4; no autoaprueba A5 |
| A03 | Task revisora Javier distinta | Comprueba A5 exact SHA sin heredar facultad de desplegar |
| A04 | Responsable de release futuro | Compara SHA/digest/ambiente y ejecuta sólo después de autorización específica |
| A05 | Operador de infraestructura futuro | Gestiona configuración no sensible y rotaciones aprobadas fuera de Git |
| A06 | Responsable de soporte | Clasifica ticket e identifica el recurso/tenant sin divulgar información ajena |
| A07 | Responsable de incidentes | Coordina severidad, stop/rollback y comunicación auditada |
| A08 | Responsable de privacidad/seguridad | Exige JWT/RBAC/tenant A/B y trazas sin PII/secretos |
| A09 | Usuario público anónimo | Navega landing pública sin acceder a recursos privados |
| A10 | Usuario autenticado de tenant A | Actúa sólo según rol y pertenencia de empresa/sucursal A |
| A11 | Usuario de tenant B | Debe permanecer aislado de A en API, caché, archivos y soporte |
| A12 | Cliente de API/front Angular | Presenta UX/estados pero no concede autoridad por route guard |
| A13 | Proveedores operativos actuales | Render/Vercel/Aiven/Cloudinary exponen señales sólo según permisos reales |
| A14 | Proveedor futuro de DNS/storage | Hostinger/Cloudflare/R2 sujetos a nueva decisión y contrato de transición |

El rol de un actor **no acredita** que exista una cuenta técnica, una integración o una autorización física. Un ticket y un dashboard son contextos de trabajo, no autorizaciones para consultar datos ajenos.

## 3. Entradas verificables IN01–IN12

| ID | Entrada necesaria en la fase futura | Validación mínima |
| --- | --- | --- |
| IN01 | `SOURCE_SHA` y árbol publicado | Igual al commit que originó pruebas y artefacto |
| IN02 | `ARTIFACT_DIGEST` del frontend/backend | Se empareja con build y destino |
| IN03 | Target environment y autorización humana | Permiso específico de QA/main/PROD, jamás inferido de dev |
| IN04 | Matriz no sensible de variables/configuración | No incluye JWT secrets, passwords ni llaves |
| IN05 | Lista de dominios/hosts permitidos | Host y tenant resueltos por regla del servidor |
| IN06 | Matriz de RBAC y pertenencia de usuario | API comprueba rol, tenant y sucursal del actor |
| IN07 | Inventario de archivos y ACL/owner | Sólo rutas/objetos existentes realmente verificados |
| IN08 | Plan de backup con RPO/RTO objetivos | No confundir objetivos con restauración ensayada |
| IN09 | Mapa de health/alertas y severidades | Cada señal indica sistema, entorno, dueño y acción |
| IN10 | Pruebas smoke/negativas causalmente ejecutadas | `head_sha/status/conclusion` y logs por caso |
| IN11 | Requisitos de soporte/guardia e incidentes | Canal, responsables y escalación formalmente definidos |
| IN12 | Plan de reversión y criterio de aborto | Versiones compatibles y preservación de datos transaccionales |

## 4. Salidas auditables OUT01–OUT12

| ID | Salida a producir en etapa futura | Evidencia requerida |
| --- | --- | --- |
| OUT01 | Acta de autorización por entorno | Aprobador y alcance explícito |
| OUT02 | Manifiesto de SHA/digest | Comparación origen/destino sin mutaciones |
| OUT03 | Matriz de validaciones de CI | Check runs concluidos y pertinentes al SHA |
| OUT04 | Resultado smoke de landing/API | Traces/capturas, rutas y respuesta real |
| OUT05 | Resultado negativo JWT/RBAC/tenant A/B | Rechazo cross-tenant sin fuga de recursos |
| OUT06 | Verificación de host/CORS/TLS | Política correcta, sin host spoofing |
| OUT07 | Verificación de archivo y ACL | Acceso propio positivo y ajeno negativo |
| OUT08 | Acta de restore aislado | Integridad y rollback con datos sintéticos |
| OUT09 | Dashboard y alertas operativos | Correlación de incidentes por release |
| OUT10 | Registro de ticket e incidente | Sin datos secretos, con severidad/escalación |
| OUT11 | Bitácora de decisiones de stop/rollback | Acción y readback firmados por responsable |
| OUT12 | Cierre de ventana de observación | SLO observado, riesgos abiertos y decisión futura |

Ninguna salida OUT01–OUT12 ha sido producida operativamente por este Markdown. El diseño de una matriz no equivale a su ejecución ni a la certificación de proveedores.

## 5. Límites de confianza B01–B10

| ID | Frontera | Negativo que deberá fallar cerrado |
| --- | --- | --- |
| B01 | Navegador → API | Header, query o body falsos cambian tenant |
| B02 | JWT → autorización backend | Token expirado/revocado obtiene operación |
| B03 | Roles → recurso/empresa/sucursal | Rol de A modifica recurso de B |
| B04 | Host/landing → resolución de tenant | Host no permitido revela configuración privada |
| B05 | API → MySQL/EF | Query omite pertenencia o pierde transacción |
| B06 | API → archivos/Cloudinary/R2 eventual | Objeto de B accesible desde A |
| B07 | API → proveedor SMTP/externo | Error del proveedor expone PII/secreto |
| B08 | Backup/restore → datos de empresa | Restore cruza tenants o sobreescribe producción |
| B09 | Telemetría → soporte/alertas | Log público contiene JWT, PII o ID privado |
| B10 | DEV → QA/main/PROD | A5 o check de dev se toma como permiso para promoción |

Estos límites requieren prueba positiva y negativa **real** en su etapa; no se compensan con guards Angular, Markdown ni capturas simuladas. La política futura debe controlar también idempotencia, auditoría y caché tenant-aware sin implementar eventos ahora.

## 6. Pares de aceptación futuros PN01–PN10 (NO EJECUTADOS)

| Par | Positivo a demostrar | Negativo bloqueante |
| --- | --- | --- |
| PN01 | Release muestra SHA/digest comprobado | Artefacto diferente del probado |
| PN02 | Dominio autorizado publica landing de tenant correcto | Host manipulado suplanta tenant |
| PN03 | Actor autorizado de A ve recurso de A | Actor A lee recurso B |
| PN04 | Soporte autentica identidad y limita datos | Ticket divulga PII de otra empresa |
| PN05 | Archivo público/privado respeta ACL | URL caducada/ajena permite acceso |
| PN06 | Backup aislado preserva vínculos y auditoría | Restore destruye documentos o inventario |
| PN07 | Alerta detecta incidente y escalación | Errores críticos quedan sin dueño |
| PN08 | UX mantiene responsive, teclado y error visible | Pantalla inaccesible se declara aceptada |
| PN09 | CI required termina success sobre SHA | queued/skipped/cancelled/failed cuenta PASS |
| PN10 | Autorización QA/PROD separada y trazable | LISTO_DEV/A5 se usa como orden de desplegar |

Todos los PN01–PN10 son `FUTURO_NO_EJECUTADO`. No se inventan pruebas HTTP/MySQL/Playwright, incidentes observados, SLO ni resultados de producción.

## 7. Impacto, A0–A5 y reversión

**Impacto acotado:** se agrega este Markdown y una sección aditiva de `CHANGELOG_AI.md`; no cambian Domain/Application/Infrastructure/API, DTO, frontend Angular, rutas, configuraciones, DB, JWT, RBAC, DNS, backups, alertas ni proveedores. Consumidores futuros: M01.03–05, fases M02–M06 y checklists de operación sujetos a otros gates. Riesgo principal: confundir interfaz conceptual con disponibilidad física; mitigación: `NO_IMPLEMENTADO` y evidencia causal exigida.

- **A0:** presencia de 14 actores, 12 entradas, 12 salidas, 10 fronteras y 10 pares PN; encabezados y atribución CHANGELOG íntegros.
- **A1:** casos futuros positivos/negativos observables y límites de responsabilidad; ningún resultado operativo supuesto.
- **A2:** preservación conceptual de JWT/RBAC, tenant A/B, PII, host, integridad MySQL y secretos.
- **A3:** cotejo con M01.01 y arquitectura; builds .NET, Angular, Playwright, MySQL `NO_APLICA` a dos Markdown, nunca `PASS`.
- **A4:** commit fast-forward/CAS a dev, Scope Lock y procedencia V5 `completed/success` exact SHA, readback institucional y rollback registrado.
- **A5:** revisión independiente pendiente de otra Task Javier, no autoaprobación. QA/main/PROD necesitan autorizaciones explícitas y separadas.

**Rollback:** revert normal del commit documental después de revisar HEAD concurrente; preservar BITACORA append-only, sin force-push/reset ni operación física sobre infraestructura.
