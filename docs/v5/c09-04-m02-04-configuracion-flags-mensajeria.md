# SOLQARYN V19 — C09-04.M02.04: configuración tipada, ámbitos y flags seguros

**ID institucional:** C09-04.M02.04, PLAN_MAESTRO fila 1415 · **Autor:** PRIMARY DEV05, `6adc2e55f47c81919f6374e561687076` · **Ámbito exclusivo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

**Riesgo del dominio:** R2 por secretos/tenant/consentimiento, **tipo de este cambio:** R0 documental. Esta microtarea no crea ni modifica variables de entorno, `appsettings`, `IOptions`, base de datos, SMTP, WhatsApp, configuración activa, workers, eventos, webhooks, feature flags runtime, credenciales ni servicios de pago. Los nombres de configuración propuestos abajo **no existen como propiedades verificadas y NO se implementan aquí**.

## A0. Fuentes y límites reales en dev

- `backend/src/Domain/Entities/ConfiguracionWhatsAppEmpresa.cs` blob `e861f83314287f59b00290f579cae0247d95a780`: tiene `EmpresaId`, `NumeroTelefonoE164`, `TokenSecretoReferencia` y `WebhookSecretoReferencia` opacas, `Activa`, `Version`. `Reconfigurar` incrementa versión, `Desactivar` es idempotente. Referencia de secreto no es el secreto; no modificarla ni inferir almacén real.
- `backend/src/API/Controllers/WhatsAppController.cs` blob `efc1e79625e62925efe7d1e47040eae91fb12dc6`: `POST whatsapp/webhook` anónimo revisa `WA_WEBHOOK_VERIFY_TOKEN` y cabecera `X-WhatsApp-Verify-Token` (comparación fija), **no acredita HMAC ni replay guard**; `POST whatsapp/iniciar-whatsapp` con autenticación/permisos puede devolver `CONFIGURADA_SIN_SESION`, QR nulo, sin crear sesión real. Si falta verify token el webhook rechaza la operación. Los requisitos de firma futura no se atribuyen al código actual.
- `backend/src/Domain/Entities/EmailEmpresarial.cs` blob `718421df52d525712ee9512d0c164ffebfcf80a5`: intención SMTP tenant-bound, estado e idempotencia, no autorización de campañas.
- `backend/src/Application/Services/MensajeOutboxService.cs` blob `a00d353c474d7d7f785a58f443c66c5f12a542f7`: valida scope antes de crear intención; no prueba dispatcher concurrente.
- Predecesoras M01.01–06 y M02.01/M02.02 contienen invariantes de tenant, consentimiento, idempotencia y compatibilidad. Sus artefactos no constituyen ejecución de nuevas capacidades.

## CFG01–CFG12: contrato conceptual de configuración futura

| ID | Campo lógico propuesto, **NO_IMPLEMENTADO** | Ámbito/valor seguro y rechazo |
| --- | --- | --- |
| CFG01 | `ComunicacionHabilitada` | Default false; jamás true por ausencia o error de configuración |
| CFG02 | `CanalesPermitidos` | Allowlist por tenant con validación server-side; sin fallback automático |
| CFG03 | `FinalidadesPermitidas` | Canal+finalidad; marketing requiere consentimiento vigente separado de avisos transaccionales |
| CFG04 | `ProveedorPorCanal` | Identificador allowlisted sin URL arbitraria/SSRF, credenciales no expuestas |
| CFG05 | `SecretoTransporteRef` | Referencia opaca por tenant; no token, contraseña ni DSN en documento/API/log |
| CFG06 | `WebhookFirmaRequerida` | Futuro control fail-closed criptográfico; **no confundir con verify token actual** |
| CFG07 | `WebhookVentanaReplay` | Ventana acotada/nonce único y tenant/proveedor validados; no habilitar sin almacenamiento y prueba |
| CFG08 | `ReintentosMaximos` | Entero positivo limitado y dead-letter; valores inválidos bloquean envío |
| CFG09 | `TimeoutProveedor` | Duración positiva finita y cancelación segura; sin reintentos infinitos |
| CFG10 | `CuotaEnvios` | Límite por tenant/canal/período, sin bypass por concurrencia |
| CFG11 | `PlantillasHabilitadas` | Versiones allowlisted por empresa/finalidad, variables minificadas y sin PII ajena |
| CFG12 | `ModoSoloLectura` | Default true para diagnósticos; jamás habilita mutación, envíos o impersonación |

Estas propiedades son exclusivamente **hipótesis contractuales**, no nombres registrados en `appsettings`, `IOptions`, Environment o esquema. Versionar contrato y no declarar valores de producción. Las decisiones de cuota, timing, retención, proveedor y consentimiento comercial quedan abiertas al scope autorizado posterior.

## Ámbitos y precedencia propuesta

1. Configuración de **plataforma** define únicamente límites máximos no el permiso efectivo para enviar desde una empresa.
2. Configuración de **empresa** restringe canales/finalidades y credenciales opacas; nunca incrementa privilegios concedidos por JWT/RBAC server-side.
3. Configuración de **actor y operación** exige membresía actual, permiso del canal, finalidad y consentimiento del destinatario; el navegador no es fuente de autoridad.
4. Precedencia por **intersección de permisos y restricciones**; ningún nivel puede forzar true si una capa superior o consentimiento lo deniega.
5. Flag desconocido, estado sin fuente, version mismatch, timeout, falta de secreto, tenant legado ambiguo o política corrupta → **fail-closed**; registrar sólo código y correlación, sin secreto.
6. Revocación o desactivación cambia la decisión **antes de efectos externos futuros**, sin borrar mensajes/bitácoras históricas; requiere prueba de carrera e idempotencia al implementarse.

## Invariantes de no regresión CF01–CF16

- **CF01:** flags son booleans/enum/valores limitados, no JSON dinámico ejecutable ni SQL libre.
- **CF02:** cada clave de configuración tenant-scoped exige `EmpresaId` resuelta, >0 y validada por servidor.
- **CF03:** feature flag jamás sustituye autorización de actor/rol ni opt-in por finalidad.
- **CF04:** config de A no puede alterar canal/secretos/estado de B.
- **CF05:** un contacto legacy sin tenant verificado no recibe envíos por default.
- **CF06:** referencias de secretos opacas no se resuelven en logs/DTO/respuestas o mensajes de error.
- **CF07:** desactivar un canal impide nuevos efectos, preservando evidencia legítima y dead-letter.
- **CF08:** envío transaccional no convierte opt-in en marketing.
- **CF09:** no hay fallback WhatsApp→SMTP implícito ni cross-channel sin consentimiento.
- **CF10:** token actual de verificación webhook no certifica HMAC/nonce y no puede marcar `WebhookFirmaRequerida` como satisfecho.
- **CF11:** valores inválidos, faltantes o fuera de rango bloquean, no relajan límites.
- **CF12:** toda reconfiguración futura requiere versión/concurrencia/CAS y audit trail separado.
- **CF13:** retries/cuotas finitos; prueba de dos writers requerida antes de garantía de exclusión.
- **CF14:** proveedor no disponible no modifica transacción comercial ni envía por otro canal.
- **CF15:** no hay configuración privilegiada desde rutas públicas ni frontend.
- **CF16:** activar proveedor, secrets, DB migration o QA/main/PROD requiere autorización concreta separada; esta entrega R0 no la concede.

## A1–A3. Oráculos futuros explícitamente NO EJECUTADOS

**Positivos P01–P08 FUTUROS_NO_EJECUTADOS:** flag en false bloquea; flag true+actor+tenant A+consentimiento válido podría habilitar caso autorizado posterior; revocación efectiva; cuota acotada; referencia de credencial opaca; versión de configuración monotónica; rollback desactiva sin borrar historial; auditoría sin PII. **Negativos N01–N12 FUTUROS_NO_EJECUTADOS:** A modifica B; miembro revocado; opt-out; secreto literal en config; flag mal tipado; fallback no consentido; proveedor sin token; nonce replay; HMAC incorrecto; cuota excedida; dos escrituras concurrentes; tenant NULL. Estas pruebas aún deberán ejecutarse con xUnit, HTTP/JWT/RBAC y MySQL efímero o proveedor autorizado según impacto real; ninguna aprobación runtime se infiere de un Markdown.

## A4, A5, reversibilidad y DAG

**A0:** diff exclusivamente este archivo `docs/v5/` y una sección autocontenida `CHANGELOG_AI.md`. **A1:** consistencia de fuentes, CFG01–12/CF01–16 y casos P/N; no tests runtime falsos. **A2:** JWT, RBAC, consent, tenant A/B y secretos protegidos por gates declarativos futuros, no nuevos permisos. **A3:** MySQL/HTTP/Angular/SMTP/WhatsApp/webhook reales **NO_APLICA al changeset documental**. **A4:** commit fast-forward exclusivo en dev, Scope Lock y control V5 completados SUCCESS en SHA exacto, readback PLAN/COLA/DAG/BITACORA y blobs; no atribuir PASS a SKIPPED/QUEUED/FAIL. **A5:** revisor Javier de otra Task ID, distinta del autor, puede aprobar/rechazar con evidencia causal; LISTO no equivale a A5_PASS.

**Rollback:** `git revert` ordinario únicamente del cambio documental tras verificar HEAD y otros escritores, sin force-push ni borrar BITACORA. **Gate:** seguir fórmulas institucionales, no forzar G/O=SI. No crear eventos, nuevos schedulers, providers, secretos, QA/main/PROD. Definición de configuración definitiva corresponde a microfase técnica posterior, con su propia CI, autoridad y revisión.
