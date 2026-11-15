# SOLQARYN V5 — C10-01.M02.04: configuración tipada, ámbitos y feature flags de storage neutral

**Autor:** PRIMARY DEV 03 JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Origen:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

## Alcance, evidencia y compatibilidad

Esta es una **especificación documental M02**, no una configuración aplicada. Define tipado, ámbitos de decisión, defaults seguros y banderas hipotéticas para un futuro almacenamiento provider-neutral de imágenes, documentos, vídeos y exportaciones. M01.01–M01.06 cuentan con LISTO+A5 independiente; `DAG_TECNICO` y `COLA` declararon este ID `VALIDADO/SI` y `G/O=SI` al claim individual. Esta preparación no permite por sí sola iniciar M02.06 ni alterar M03.

Fuentes reales de compatibilidad: `backend/src/Application/Interfaces/IImageStorageService.cs`, `ICompraDocumentoStorageService.cs`, `StorageTenantContext.cs`, `Infrastructure/Services/CloudinaryImageStorageService.cs`, `CloudinaryCompraDocumentoStorageService.cs`, `ImagenUploadSecurity.cs`, y las pruebas xUnit listadas en `C10-01.M01.03`. Cloudinary es integración actual; R2/Hostinger permanecen **no configurados ni autorizados** por este artefacto. No se inspeccionaron valores secretos, buckets, endpoints live, recursos de usuarios ni topología productiva.

## Esquema tipado conceptual (sin clases compiladas)

| Contrato propuesto | Tipo lógico | Ámbito y validación fail-closed |
| --- | --- | --- |
| `StorageMode` | enum `LegacyOnly / CandidateShadow / CutoverRequested` | `LegacyOnly` por defecto. Los otros modos son diseños, no estados activos; `CutoverRequested` necesita gate humano nuevo |
| `AllowedResourceKinds` | conjunto enum | `Image / PrivateDocument / Video / Export`; catálogo allowlist servidor, sin inventar compatibilidad de proveedores |
| `MaxBytesByKind` | mapa por tipo a entero positivo | Límite explícito por tipo, máximo efectivo más restrictivo entre seguridad global y política aprobada |
| `RequireVerifiedMime` | booleano | Siempre verdadero en el contrato futuro, never aceptar sólo extensión / header cliente |
| `RequireServerChecksum` | booleano | Siempre verdadero antes de confirmar almacenamiento y lectura; SHA-256 de bytes del servidor |
| `RequireServerTenantScope` | booleano | Siempre verdadero, empresa desde JWT validado, membresía y relación de objeto en backend |
| `AllowProviderFallback` | booleano | Falso por defecto y no habilitable por simple petición del cliente |
| `TemporaryLinkTtl` | duración positiva con cota | TTL máximo institucional futuro; no enlaces sin caducidad |
| `EnableProviderNeutralCandidate` | booleano | Falso por defecto; su habilitación en dev sólo tras autorización y pruebas dirigidas |
| `EnableWriteShadow` | booleano | Falso por defecto; prohibir doble escritura real sin reconciliación, idempotencia y rollback |
| `EnableAutomaticCutover` | booleano | Siempre falso, sin ruta automática por mera banderas; requerirá autorización independiente |
| `ApprovedProviderKey` | identificador opaco | Catálogo de adaptadores permitidos por servidor; nunca URL/credencial entregada por browser |

Nombres y tipos son **propuestas de diseño**. No se declaran opciones .NET, variables de entorno, flags persistidos, cambios de DI ni nuevos endpoints en este changeset.

## Autoridad y resolución de ámbitos

1. **CFG01 — Seguridad global obligatoria.** Reglas de tenant, JWT/RBAC, MIME comprobado, tamaño, checksum y logs sin PII no admiten override por empresa ni cliente.
2. **CFG02 — Entorno verificado.** Una configuración de dev no se eleva a QA/main/PROD ni activa cuentas/cobros. En entorno desconocido, fallar cerrado.
3. **CFG03 — Empresa desde servidor.** Únicamente la membresía activa validada en backend permite resolver políticas por empresa; no confiar en `EmpresaId`, subdominios ni rutas del navegador.
4. **CFG04 — Jerarquía restrictiva.** El límite efectivo es la intersección entre políticas globales aprobadas, ambiente permitido, empresa actual y operación RBAC.
5. **CFG05 — Menor privilegio.** Política ausente, versión obsoleta, enum desconocido o flag ausente equivale a NO_AUTORIZADO, no a habilitación por default.
6. **CFG06 — Separación de permisos.** Leer/escribir/borrar/exportar requieren decisiones explícitas distintas; un booleano de feature no sustituye autorización.
7. **CFG07 — Tenant A/B.** Caché, resolución de políticas, locators, URLs temporales y cuotas no se reutilizan entre empresas.
8. **CFG08 — Integridad.** La bandera no puede apagar el cálculo/verificación de checksum real ni downgrade de MIME seguro.
9. **CFG09 — Secretos.** Credenciales y firmas permanecen fuera del repositorio; respuesta API y observabilidad no exponen valores o rutas privadas.
10. **CFG10 — Fallback controlado.** Ausencia o error del proveedor aprobado devuelve error seguro; no cambiar automáticamente a un proveedor ajeno o a otro ambiente.
11. **CFG11 — Versionado observable.** Snapshot de configuración futura deberá registrar revisión, alcance, fuente autorizada y decisión redactada, sin PII ni material secreto.
12. **CFG12 — Rollback ordenado.** Cualquier cambio futuro requiere opción explícita de volver al adaptador actual, verificación de datos/bytes y CI exact-SHA.
13. **CFG13 — Consistencia por operación.** Una operación usa snapshot verificado consistente de configuración y tenant desde el inicio hasta la confirmación.
14. **CFG14 — Antibypass.** Flags del frontend sólo controlan visibilidad de UI; no crean acceso servidor, permisos institucionales o eventos.
15. **CFG15 — Cuota e idempotencia.** Reintentos y shadow no pueden eludir límites ni duplicar recursos, ni compartir claves entre tenants.
16. **CFG16 — Gates separados.** M02 documental y A5 no prueban migración, HTTP/MySQL real, restore, Playwright ni permiten QA/main/PROD.

## Tabla de fallos de configuración y respuesta esperada futura

| Caso positivo hipotético | Contracaso negativo hipotético |
| --- | --- |
| P01 Política global estricta con un subconjunto aprobado de tipos | N01 Cliente solicita habilitar tipo no permitido |
| P02 Tenant A obtiene la política de A desde sesión backend | N02 Tenant B intenta usar política/locator de A |
| P03 Flag ausente deja interfaz nueva apagada | N03 Valor desconocido habilita cutover automático |
| P04 Política más estricta prevalece sobre cota de empresa | N04 Empresa eleva cuota/tamaño por encima de límite global |
| P05 Modo `LegacyOnly` conserva integraciones existentes | N05 Error del proveedor cambia implícitamente a R2 |
| P06 TTL temporal válido tiene duración limitada | N06 TTL nulo, negativo o ilimitado |
| P07 Snapshot versionado se registra sin secretos | N07 Logs contienen URL firmada, token o clave |
| P08 Checksum real y MIME validado impiden publicación insegura | N08 Bandera del navegador desactiva validación MIME/checksum |

**Los ocho pares P/N son criterios futuros NO_EJECUTADOS**, no pruebas automatizadas ni validación HTTP/MySQL. No se afirma que las características propuestas existan en runtime.

## Revisión A0–A5, dependencias y rollback

**A0:** diff intencional limitado a este documento y una sección de CHANGELOG autocontenida, sin secretos, migraciones o código. **A1:** comprobar tabla de 12 contratos, 16 invariantes y ocho pares positivos/negativos; detectar contradicciones entre defaults y catálogo, sin atribuir pruebas runtime. **A2:** verificar en el propio contrato la denegación por defecto JWT/RBAC, tenant A/B, cuotas, bytes, MIME, checksum, flags y secretos. **A3:** enlazar contratos actuales del inventario M01.03, M01.06 LISTO+A5 y DAG individual; compilación .NET/Angular, pruebas MySQL, browser y proveedor = NO_APLICA causal al cambio Markdown. **A4:** Git commit fast-forward/CAS `dev`, controles Scope Lock y procedencia V5 completados `success` en SHA pertinente, readbacks PLAN/COLA/DAG/BITACORA. **A5:** única revisión posterior por Task Javier distinta de la autora, con diff/blobs/checks individuales.

**Rollback:** `git revert` normal del changeset de documentación. Ninguna activación de R2, cambio de Cloudinary, secretos, servicios, datos, DNS, QA/main/PROD, ni runtime de eventos/webhooks.
