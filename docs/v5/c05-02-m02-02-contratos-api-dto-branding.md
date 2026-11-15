# SOLQARYN V5 FINAL — C05-02.M02.02: contratos API/DTO, errores y compatibilidad de branding empresarial

**Fuente:** STANDARD V5 FINAL, PLAN_MAESTRO fila 765, DAG validado para C05-02.M02.02. **Autor:** PRIMARY DEV 03 JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Rama:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. **Clase:** diseño documental M02. No crea endpoint, API ejecutable, proveedor, archivo, migración, feature flag activo ni cambio de CSS runtime.

## 1. Interfaces ya existentes, comprobadas contra Git

`EmpresaConfiguracionController` expone la ruta `empresa-configuracion` con GET `publica`, GET/PUT protegidos por permiso de Configuración, POST `logo`, GET/PUT `tenant/{empresaId}`, POST `tenant/{empresaId}/logo` y PUT `tenant/{empresaId}/plantillas/{tipoPlantilla}`. Las rutas autenticadas requieren permisos `Configuracion.Ver/Editar` y autoridad tenant efectiva; no convierten un parámetro de ruta en permiso para otra empresa. Los servicios `EmpresaConfiguracionService`, `TemaVisualService` y `ThemeApplierService` existen en el frontend; `ThemeApplierService` limpia colores anteriores y valida literales hexadecimales para evitar estilos libres.

Las superficies actuales no constituyen un flujo certificado de **preview–publicación–rollback versionado**, ni ofrecen por inferencia presets universales entre tenants. Ninguna capacidad propuesta en este contrato debe describirse como implementada.

## 2. DTO futuros propuestos, NO registrados ni desplegados

| Tipo lógico | Campos propuestos | Restricciones |
| --- | --- | --- |
| `BrandingDraftV1` | `empresaId` derivada, `revisionBase`, `nombreComercial`, `logoAssetId`, `tema`, `presetId`, `sector` | La empresa se resuelve y coteja en backend; logo sólo asset autorizado, no URL libre |
| `BrandingPreviewV1` | `previewId`, `revisionBase`, `temaNormalizado`, `avisosAccesibilidad`, `expiraUtc` | Sólo simulación efímera y privada: nunca escribe configuración publicada |
| `BrandingPublishV1` | `previewId`, `expectedRevision`, `idempotencyKey`, `motivoCodigo` | Revalidar tenant, permiso, CAS e integridad de recursos al confirmar |
| `BrandingRevisionV1` | `revisionId`, `empresaId`, `autorId`, `timestamp`, `hash`, `estado` | Inmutable/registrada, sin credenciales o archivos de terceros |
| `BrandingRollbackV1` | `targetRevision`, `expectedCurrentRevision`, `idempotencyKey` | Volver a contenido validado, nunca restaurar permisos o credenciales |
| `BrandingErrorV1` | `code`, `message`, `correlationId`, `retryable` | Mensaje público redactado, jamás stack, DSN, JWT, ruta interna o PII |

Los campos y rutas siguientes son **FUTUROS, NO EXISTENTES**: `POST /empresa-configuracion/tenant/{id}/branding/previews`, `POST /empresa-configuracion/tenant/{id}/branding/publicaciones` y `POST /empresa-configuracion/tenant/{id}/branding/reversiones`. Las rutas `GET/PUT tenant/{id}` existentes no se modifican aquí. Cualquier implementación requiere revisión de API y autorización independiente.

## 3. Invariantes de contrato I01–I14

| ID | Regla fail-closed y criterio futuro |
| --- | --- |
| I01 | `empresaId` efectiva siempre proviene de contexto server-side autorizado; tenant A no lee ni publica branding de B |
| I02 | Revisión `expectedRevision` es positiva y vigente; un CAS obsoleto no publica |
| I03 | `previewId` caducado o de otro actor/tenant no publica |
| I04 | Mismo `idempotencyKey` con payload igual reproduce resultado durable; payload diferente devuelve conflicto |
| I05 | Colores sólo hex seguro, sin CSS libre, `url()`, scripts o referencias externas arbitrarias |
| I06 | Texto visible se escapa en Angular; nombre comercial no acepta HTML ejecutable |
| I07 | Logo pertenece a empresa y media verificada; rechazar URL absoluta/host externo o asset B |
| I08 | Preset por rubro es una plantilla, nunca un permiso para cambiar marca de otro tenant |
| I09 | El contraste WCAG mínimo de texto/controles requiere ensayo real antes de publicar |
| I10 | Atributos públicos se exponen sólo bajo reglas de tienda pública; no filtrar config privada en bootstrap |
| I11 | Aprobación/publish no cambia JWT/RBAC, sesiones, credenciales ni privilegios |
| I12 | Publicación debe invalidar caché por versión de la empresa correcta; no reenviar tema A a B |
| I13 | Rollback restaura sólo revisión de marca permitida, sin sobrescribir cambios ajenos ni borrar historial |
| I14 | Errores, logs y señales son redactados; sin DSN, secretos, PII, bearer ni eventos externos reales |

## 4. Errores/compatibilidad futura

| Situación | Comportamiento propuesto |
| --- | --- |
| Sin autenticación | 401, cero escritura |
| Usuario con token válido sin `Configuracion.Editar` | 403, cero escritura |
| Empresa de ruta distinta de tenant autorizado | 403/404 según política de ocultación; sin enumerar B |
| Asset logo ajeno o no verificado | 400/422, nunca emitir URL ajena |
| Nombre, idioma, tipografía o color inválido | 400/422 con validación de DTO, no sanitizar CSS arbitrario a éxito |
| Preview inexistente/caducado | 404/410; no publicar |
| Revisión obsoleta | 409/412 según contrato definitivo; no sobrescribir |
| Reutilización conflictiva de idempotencyKey | 409, no duplicar publicación |
| Contraste insuficiente | 422/bloqueo hasta resolver o excepción de accesibilidad aprobada |
| Storage/servicio temporalmente indisponible | 503 o fallo recuperable con estado consistente, sin éxito falso |

No se cambian contratos existentes de `empresa-configuracion`, `TemaVisualService`, `ThemeApplierService`, tienda pública o rutas de plataforma. DTO antiguos siguen interpretándose según el servidor vigente; ningún cliente nuevo puede presumir soporte para `preview` o rollback hasta versionado y negociación definidos. La aplicación de colores en UI debe conservar limpieza al salir de empresa A antes de cargar B; respuestas HTTP tardías de A no reactivan su tema.

## 5. Ocho pares de pruebas DOCUMENTALES futuras, no ejecutadas

- P01 publicación A con sesión/permiso verificados / N01 usuario A publica marca de B: denegar.
- P02 preview propio sin persistencia / N02 preview vencido o ajeno no modifica estado.
- P03 color hexadecimal permitido / N03 CSS malicioso y URL externa rechazados.
- P04 logos validados vinculados al tenant / N04 logo de otra empresa denegado.
- P05 CAS con revisión vigente / N05 publicación basada en revisión obsoleta bloqueada.
- P06 retry idéntico recupera resultado / N06 payload diferente con misma clave no escribe.
- P07 rollback a revisión autorizada / N07 no restaurar secretos/roles ni pisar revisión nueva.
- P08 transición A→B elimina tokens de tema anterior / N08 respuesta tardía A no altera B.

Los 16 casos son oráculos escritos; no equivalen a Vitest, Playwright, pruebas de API, EF/MySQL o seguridad runtime. La prueba real de accesibilidad y cross-tenant requiere M03–M05 con CI dirigida y datos efímeros.

## 6. Gates A0–A5 y rollback

**A0:** diff sólo doc + CHANGELOG, ningún runtime. **A1:** validar seis tipos, 14 invariantes, 10 casos de errores y 8 P/N; pruebas documentales positivas/negativas, no ejecutadas. **A2:** JWT/RBAC, separación empresa A/B, assets, CSS/HTML, permisos y contrastes conservadores. **A3:** comprobar rutas reales y contratos existentes sin alterarlos, predecesores M01 + A5, DAG/COLA. **A4:** Git dev fast-forward/CAS, SHA exacto, Scope Lock y procedencia V5 `completed/success`, readback PLAN/COLA/BITACORA antes de LISTO_DEV; CI Angular/MySQL no causal = NO_APLICA, no PASS. **A5:** sólo Task Javier diferente al autor, jamás habilitar QA/main/PROD automáticamente.

Rollback de este artefacto: `git revert` normal sin force. Rollback de branding real queda FUTURO, requiere revisar permisos, caches, versionado y auditoría de cambios, sin despliegue productivo.
