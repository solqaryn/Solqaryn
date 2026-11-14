# SOLQARYN V5 — C10-01.M01.01: objetivo, no-alcance y no-regresión de archivos provider-neutral

**Unidad:** `C10-01.M01.01`, preparación documental de `C10-01 — Storage abstraction`. **Autor:** PRIMARY DEV 04 / JAVIER_CHATGPT / TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Autoridad ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO fila 1442, `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK=STRICT. Fecha:** 2026-11-14.

## Objetivo concreto y estado actual

Preparar el contrato futuro de una interfaz **neutral respecto al proveedor** para imágenes, documentos, videos y exportaciones. La fase deberá hacer explícitos propietario/tenant, operación, tipo MIME permitido, tamaño máximo, huella de integridad (checksum), localizador opaco, lectura/escritura, autorización, auditoría y reversión. La evidencia final exigirá un contrato verificable de lectura/escritura, controles MIME/tamaño/checksum y pruebas positivas/negativas independientes.

**Esta microtarea M01.01 es únicamente especificación de objetivo, exclusiones y límites.** No crea interfaces C#, código, buckets, claves, credenciales, URLs, trabajos, endpoints, almacenamiento, migraciones ni archivos de usuario. No demuestra la existencia de datos históricos por migrar, ni de objetos en R2. El plan V5 define Cloudflare R2/Hostinger como **objetivos de migración posterior**, no proveedores activos habilitados por este documento. Cloudinary forma parte de la arquitectura de transición actual y su comportamiento real debe inventariarse por separado antes de implementar adaptadores.

## Fuentes y arquitectura respetada

- La Skill canónica de `dev` y `references/architecture.md` delimitan Angular 22.2.1, ASP.NET Core 10, capas Domain/Application/Infrastructure/API, MySQL y proveedor Oracle EF Core; seguridad JWT/RBAC/tenant se impone en backend. Cloudinary, QuestPDF y SMTP son integraciones actuales declaradas, no autorización para cambiarlas.
- El STANDARD V5 fija para C10-01 **interfaz provider-neutral** para imágenes, documentos, videos y exportaciones; evidencia de contrato de lectura/escritura, MIME, tamaño y checksum. La topología Hostinger/R2 es futura y exige gates de migración y rollback.
- El DAG individual `C10-01.M01.01` exige predecesores documentales `C01-03.M01.01–05` y `C02/C03 M01.01–05`; son antecedentes de preparación, **no** prueba de autorización para usar storage o cambiar tenant, IAM, API, DB o producción.
- Todo inventario de objetos existentes, cardinalidad, proveedor real por ambiente y permisos de credenciales requiere lectura y pruebas posteriores con evidencia; aquí no se presupone ningún número de ficheros.

## Dentro del lote y exclusiones expresas

**Dentro del lote futuro:** especificar identidad opaca de objeto, operación `read/write/delete/list` con permisos distintos, límites de tamaño configurables, tipos MIME con inspección de contenido, huella verificable, stream seguro, segregación por tenant, expiración de links, auditoría y migración reversible **solo si autorizada**, sin exponer nombres de bucket, claves ni rutas internas en DTO públicos.

**Fuera de C10-01.M01.01:** implementación de `IFileStorage` u otra interfaz nueva, adopción de SDK/cloud R2, activación de buckets, URLs firmadas reales, copy/delete de objetos, mover o reescribir imágenes existentes, workers de transferencia, exponer documentos privados, autorización por `EmpresaId` enviado del navegador, modificación de QuestPDF/Cloudinary, secretos, DNS, VPS, nuevas Tasks, webhooks, QA/main/PROD y compras. Ningún gate documental autoriza operaciones productivas.

## Invariantes NR01–NR12 para etapas de implementación posteriores

| ID | Límite esperado | Condición explícita de NO_GO |
| --- | --- | --- |
| NR01 | Selección de proveedor tras abstracción; consumidores no conocen SDK/bucket | API pública acoplada a una credencial o bucket específico |
| NR02 | Propietario y tenant se derivan del contexto autorizado server-side | Objeto B accesible con tenant/slug/header enviado por A |
| NR03 | Lectura y escritura se autorizan de forma independiente | Permiso de consulta concede upload, export o eliminación |
| NR04 | MIME se permite por política y contenido verificado | Confiar únicamente en extensión o cabecera del cliente |
| NR05 | Tamaño y cuota se validan antes/durante streaming | Upload ilimitado o memoria descontrolada |
| NR06 | Checksums cubren bytes reales de entrada/salida | Declarar integridad sin comparar hash y contenido |
| NR07 | Identificadores públicos son opacos y no filtran rutas internas | DTO revela URL permanente privada, path físico o secreto |
| NR08 | Fallos producen resultado seguro sin escritura parcial visible | Objeto truncado queda publicado o marcado íntegro |
| NR09 | Logs/auditoría no exponen archivo privado, token ni contenido | Persistir URL firmada, PII o credencial en trazas |
| NR10 | Links temporales tienen TTL y alcance mínimo definidos | URL de objeto privado no expira o sirve a otro tenant |
| NR11 | La migración conserva hashes y rollback comprobado | Copia irreversible, sin inventario o sin restauración ensayada |
| NR12 | Valores y permisos reales no se activan por documentar el plan | M01.01 interpretada como autorización de R2, workers o PROD |

## Casos positivos especificados para el futuro — NO_EJECUTADOS

**P01:** subir un objeto del tipo/tamaño permitidos en una integración efímera y verificar hash exacto contra bytes almacenados. **P02:** descargar el mismo objeto con propietario autorizado y comparar checksum. **P03:** exportar un documento permitido preservando tipo MIME y nombre seguro. **P04:** operar con dos tenants de prueba A/B y comprobar aislamiento de identificadores/cachés. **P05:** fallar un proveedor efímero y recuperar el estado sin objeto parcial ni fuga de detalle interno.

## Casos negativos especificados para el futuro — NO_EJECUTADOS

**N01:** leer objeto B desde sesión A; **N02:** subir sin JWT cuando se requiera; **N03:** rol de solo lectura intenta escribir; **N04:** cliente manipula `EmpresaId`/ruta/slug para elevar permisos; **N05:** extensión permitida con firma MIME prohibida; **N06:** archivo supera límite/cuota; **N07:** checksum incorrecto; **N08:** nombre con traversal `../` o control characters; **N09:** URL de descarga caducada o usada por otro contexto; **N10:** fallo de red deja objeto truncado visible; **N11:** log incluye token/PII/URL firmada; **N12:** petición de migración R2 sin autorización, respaldo ni rollback. El resultado esperado es rechazo o compensación segura, **nunca PASS inferido**.

P01–P05 y N01–N12 son criterios que aún deben traducirse a pruebas reales .NET/MySQL/API/Playwright según el alcance de cada unidad, con servicio efímero y SHA exacto cuando proceda. Su enumeración **no** demuestra que se hayan ejecutado pruebas contra Cloudinary/R2, ni que existan objetos productivos.

## Auditorías y reversibilidad de esta unidad

**A0:** confirmar fuente V5, identidad, archivo documental único, ámbito y ausencia de secretos. **A1:** revisar que NR01–NR12, P01–P05 y N01–N12 son verificables y no se presenten como ejecutados. **A2:** constatar límites de JWT/RBAC, tenant A/B, MIME, tamaño, checksum, secretos y paths, sin tocar seguridad activa. **A3:** cotejar arquitectura y DAG M01 documental, sin inferir proveedor live por el plan objetivo. **A4:** publicación serializada fast-forward en `dev`, diff Markdown limitado, Scope Lock y procedencia V5 `completed/success` en SHA exacto, readback PLAN/COLA/BITACORA. CI de runtime ajena al diff es `NO_APLICA` causal, no PASS.

**A5:** otra de las cinco Tasks Javier autorizadas debe verificar de forma independiente publicación, evidencia y límites; nunca autoaprobación. El cierre de M01.01 no habilita M01.06/M06.06, permisos, storage ni despliegue. **Rollback:** `git revert` regular del commit documental, conservando historia; no force-push, no borrado o copia de datos.
