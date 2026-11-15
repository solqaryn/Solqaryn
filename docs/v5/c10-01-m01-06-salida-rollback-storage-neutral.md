# SOLQARYN V5 — C10-01.M01.06: bloqueos, reversión y salida documental de storage provider-neutral

**Microtarea:** `C10-01.M01.06` / PLAN_MAESTRO fila 1447, M01 terminal de `C10-01 — Storage abstraction`. **Responsable:** PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Destino exclusivo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## Naturaleza de este cierre y criterio de no-regresión

Este entregable **cierra exclusivamente el contrato documental de preparación M01**: condiciones de bloqueo, evidencias exigidas, escenarios de reversión y gate de entrada a diseño M02. No implementa la interfaz neutral ni certifica un proveedor activo, objetos, backups, restore, SDK, checksum de bytes, integración HTTP/MySQL, JWT/RBAC runtime o aislamiento tenant A/B end-to-end. No habilita por sí solo M02: cada sucesora tiene DAG propio, estado, owner y revisión A5. **No autoriza R2, Hostinger, QA, main, PROD, migraciones, DNS, secretos ni compras.**

La arquitectura vigente conserva ASP.NET Core 10 en capas Domain/Application/Infrastructure/API, Angular 22.2.1, EF Core/MySQL Oracle, Cloudinary de transición, QuestPDF y SMTP. El diseño futuro provider-neutral debe separar autorización server-side del proveedor de almacenamiento, con respuestas opacas y sin usar identidad de tenant suministrada por el cliente.

## Evidencias previas y procedencia

| Unidad | Artefacto de entrada | Blob Git comprobado | Estado institucional |
| --- | --- | --- | --- |
| M01.01 | `c10-01-m01-01-objetivo-no-alcance-storage-neutral.md` | `622884a0a307e01c41ff1bec6797a1f36e67e21e` | LISTO + A5_PASS independiente J00 |
| M01.02 | `c10-01-m01-02-actores-superficies-storage-neutral.md` | `6d18023f7bb42685cce150b6bbe29f1fa20c30c8` | LISTO + A5_PASS independiente |
| M01.03 | `c10-01-m01-03-inventario-codigo-evidencia-storage-neutral.md` | `1d807b2b9cba1f8b87a0e4e4e571f8e832ff0203` | LISTO + A5_PASS independiente J00 |
| M01.04 | `c10-01-m01-04-riesgos-supuestos-decisiones-storage-neutral.md` | `683db7bf7886a6af8ad0675c45ec632b67bc80d7` | LISTO + A5_PASS independiente |
| M01.05 | `c10-01-m01-05-criterios-aceptacion-storage-neutral.md` | `ad285ba9c6742fab1eff3138ab6b35ff1234261e` | LISTO + A5_PASS independiente |

Estos blobs describen objetivo NR01–NR12; actores y fronteras I01–I12; inventario del código Cloudinary, interfaces y tests existentes; riesgos/supuestos/decisiones y criterios AC01–AC16/N01–N16. **Sus positivos y negativos son oráculos FUTUROS NO_EJECUTADOS**. Los blobs documentales no sustituyen la comprobación de su versión en un despliegue real. `DAG_TECNICO!A1408:G1408` devolvió `VALIDADO/SI` y `COLA!G1447/O1447=SI` antes del claim; esa instantánea no es permiso de fases posteriores.

## Bloqueos BL01–BL14; si falla uno, NO_GO solo a su operación

| ID | Bloqueo técnico que M02 debe conservar hasta prueba causal |
| --- | --- |
| BL01 | No definir propietario/tenant efectivo a partir de identidad confiable del backend |
| BL02 | No separar RBAC de leer, escribir, listar, borrar y exportar; nunca delegar control al navegador |
| BL03 | No documentar límites por tipo, firma MIME real, tamaño, cuotas y streaming seguro |
| BL04 | No verificar checksum sobre bytes reales al ingresar y recuperarlos |
| BL05 | No aislar IDs, prefijos, cache y búsqueda de recursos entre tenants A y B |
| BL06 | No definir expiración, revocación y alcance de URLs temporales para objetos privados |
| BL07 | No prever archivos truncados, fallos a mitad de carga y compensación idempotente |
| BL08 | No eliminar la exposición de tokens, URLs firmadas, PII y rutas internas en logs y DTO |
| BL09 | No inventariar consumidores reales de imagen, documento, video y exportación ni contratos legacy |
| BL10 | No verificar compatibilidad de adaptadores Cloudinary existentes antes de sustituirlos |
| BL11 | No demostrar manifiesto, inventario, propietario, conteos y hashes antes de migración futura |
| BL12 | No demostrar backup/restauración sandbox y rollback repetible antes de una operación irreversible |
| BL13 | No contar con CI causal por SHA, incluyendo pruebas de autorización/tenant A/B, MIME, límites e integridad |
| BL14 | No acreditar A0–A4, A5 independiente, DAG individual y autorización diferenciada antes de QA/PROD o proveedores |

Cada BL es requisito de diseño y verificación posterior, no una vulnerabilidad explotada ni resultado de un pentest. La ausencia de evidencia se trata como NO_ACREDITADO, nunca se transforma en PASS.

## Reversión y recuperación RB01–RB10

| ID | Escenario futuro de fallo y estrategia de rollback a validar |
| --- | --- |
| RB01 | Fallo parcial de subida: transacción de metadatos no publica objeto incompleto; limpieza controlada |
| RB02 | Error de checksum: denegar entrega, retener evidencia mínima y recuperar desde fuente verificada |
| RB03 | MIME/tamaño alterado: rechazo antes de persistencia o cuarentena sin exposición pública |
| RB04 | Acceso cruzado A/B: denegar operación y revertir permiso/caché afectada; auditar sin filtrar PII |
| RB05 | URL temporal filtrada o expirada: revocar/invalidar y comprobar que no permita otro tenant |
| RB06 | Adaptador no disponible: respuesta segura, circuit breaker y reintento idempotente sujeto a cuota |
| RB07 | Diferencia de metadatos/bytes entre proveedores: detener cutover y conservar origen íntegro |
| RB08 | Restauración sandbox fallida: mantener proveedor anterior, impedir DNS/routing/DB irreversibles |
| RB09 | Cambio de contratos rompe consumidores: revertir commit compatible y ejecutar regresión de API/cliente |
| RB10 | Check SHA/Scope Lock/seguridad fallido: mantener scope VALIDANDO, corregir causa, no promover ni repetir PASS |

**Rollback real de esta microtarea documental:** `git revert` normal del commit que añade el Markdown y su sección autocontenida del CHANGELOG; conservar BITACORA histórica, sin force-push ni objetos de usuario manipulados. Los RB01–RB10 son planes futuros; aquí no se provocaron ni probaron fallos de proveedores.

## Gate de salida y comprobaciones

1. Evidencia y precedencia: cinco unidades M01.01–05 LISTO + A5 independiente y contrato anterior `C01-03.M01.06` LISTO+A5; `DAG_TECNICO` de este ID `VALIDADO/SI`, `COLA` y PLAN concordantes antes de reclamar.
2. A0: dos Markdown, autor/owner verificado, sin secretos/archivos de datos ni cambios API, EF, DI, UI o infraestructura; sección CHANGELOG completa.
3. A1: BL01–BL14 y RB01–RB10 presentes, con escenarios y decisión fail-closed; pruebas de comportamiento FUTURAS, no PASS de runtime.
4. A2: tenant A/B, JWT/RBAC, MIME/tamaño/checksum, PII y secretos tratados como barreras que requieren validación de servidor.
5. A3: coherencia con los cinco documentos previos, skill y arquitectura de `dev`, sin presentar M01 como implementación M02.
6. A4: publicación fast-forward/CAS en `dev`; diff sólo este archivo y CHANGELOG; checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success en **SHA exacto**; readback individual PLAN/COLA/DAG/BITACORA. Compilación .NET/Angular/MySQL/Playwright sin cambios ejecutables = **NO_APLICA causal**, no PASS.
7. A5: revisión independiente posterior por otra Task Javier distinta de la autora; mantener `A5_PENDIENTE` hasta evidencia individual. **LISTO documental no libera automáticamente desarrollo técnico ni QA/main/PROD.**
