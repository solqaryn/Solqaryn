# SOLQARYN V5 FINAL — C05-02.M02.04: configuración tipada, ámbitos y feature flags de branding seguro

**Autor:** PRIMARY DEV 06, JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Repositorio:** solqaryn/Solqaryn, exclusivamente dev. PROJECT_SCOPE_LOCK=STRICT. Fecha 2026-11-15.

**Contrato:** Standard V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN/COLA/DAG/BITACORA, skill canónica, documentos C05-02.M01.01–06 y M02.01/M02.03. Fila M02.04 habilitada por DAG VALIDADO/SI y COLA G/O=SI. Esta entrega DOCUMENTAL no activa flags, migra tablas, altera estilos, da permisos ni ejecuta servicios externos.

## 1. Estado real verificado y jerarquía de autoridad

- TemaVisual es entidad GLOBAL con Id=1, quince campos de color y administración existente; carece de EmpresaId. No usar ese tema como prueba de marca particular por empresa.
- ConfigEmpresa es configuración OPERATIVA de EmpresaId con Version monotónica; no implica colores por tenant ni selección de presets visuales.
- TemaVisualService y ThemeApplierService usan endpoint tema-visual y whitelist de hexadecimal. El aplicador limpia tokens anteriores y descarta respuestas obsoletas; no otorga autorización ni asegura tenancy por sí solo.
- EmpresaIdentidadService usa fallback de plataforma y versión de respuesta para no reintroducir identidad de A después de seleccionar B. Su configuración pública no puede transformarse en grant.
- Todo override empresarial futuro debe comprobar EmpresaId, membresía y permiso del servidor. Un flag local, query, host, CSS token o claim no eleva rol ni cambia el tenant validado.

## 2. Especificación tipada de 14 claves K01–K14 (FUTURAS)

| Clave | Tipo y valor válido propuesto | Ámbito y valor seguro |
| --- | --- | --- |
| K01 temaPublicable | boolean | empresa verificada, default false |
| K02 presetsHabilitados | boolean | tenant con grant, default false |
| K03 logoPersonalizadoHabilitado | boolean | tenant con permiso, default false |
| K04 vistaPreviaHabilitada | boolean | preview privada, default false |
| K05 versionTema | entero positivo | EmpresaId+version CAS, sin confiar en cliente |
| K06 colorPrimario | HEX #RGB/#RRGGBB | tokens permitidos; fallback de plataforma |
| K07 colorSecundario | HEX #RGB/#RRGGBB | contraste verificado y fallback seguro |
| K08 colorTexto | HEX #RGB/#RRGGBB | garantizar contraste WCAG según controles UI |
| K09 colorFondo | HEX #RGB/#RRGGBB | sin CSS URL ni var arbitraria |
| K10 logoAssetId | identificador opaco opcional | asset de la misma Empresa y permiso |
| K11 presetId | identificador versionado opcional | catálogo autorizado sin heredar permisos |
| K12 revisionPublicada | entero monotónico | servidor, no se actualiza por localStorage |
| K13 politicaRollback | enum cerrado: solo-version-verificada/bloqueado | bloquear por defecto, conservar auditoría |
| K14 auditoriaCambio | boolean de política efectiva | obligatorio en backend, nunca desactivable por cliente |

Las claves K01–K14 son nombres conceptuales, **NO** claves de configuración agregadas a archivos, tablas, servicios ni frontend. Los defaults fail-closed sirven a un posible diseño y no declaran que hoy exista ese conjunto de flags.

## 3. Precedencia y segregación por ámbito

| Prioridad | Fuente | Regla |
| --- | --- | --- |
| 1 | Autorización backend y estado/membresía verificada | Siempre precede a cualquier preferencia visual, denegar ante duda |
| 2 | Política global del operador plataforma | Puede prohibir una capacidad para todos; tenant no la reabre |
| 3 | Config tenant persistida y validada por servidor | Solo dentro de EmpresaId actual, con versión y autorización |
| 4 | Preset permitido con versión publicada | Nunca hereda secrets, permisos o logo de otra empresa |
| 5 | Preferencia visual temporal/preview | No modifica fuente de verdad ni versión publicada |
| 6 | Fallback de plataforma | Estado neutral cuando falla carga o contexto no verificado |

**Cambio A→B:** limpiar identidad y tokens de A ANTES de esperar B; si B no está validada mostrar únicamente fallback neutral, no último preset de A. Un error HTTP, token expirado, cuenta revocada o desconexión no conserva marca sensible privada.

## 4. Reglas fail-closed y no-regresión R01–R14

| ID | Regla | Oráculo de rechazo futuro |
| --- | --- | --- |
| R01 | Flags por defecto false hasta comprobación de autorización | Un flag local true no habilita publicación |
| R02 | TemaVisual global no representa override de Empresa | No escribir Id=1 desde ruta tenant |
| R03 | Endpoint debe validar membresía de EmpresaId | Admin A no publica tema B |
| R04 | Roles/claims no se calculan desde tema o preset | UI no crea permiso JWT/RBAC |
| R05 | Lista de tokens/color es cerrada y HEX-only | Rechazar CSS, JS, URL o propiedad no reconocida |
| R06 | Contraste de texto y foco debe mantenerse | Tema ilegible se rechaza o corrige con fallback |
| R07 | Version CAS antes de publicar | Versión stale => conflicto, no sobrescribir |
| R08 | Preview no altera configuración publicada | Cerrar preview deja estado previo intacto |
| R09 | Logo sólo con asset dueño igual a tenant | URL de B no se muestra en A |
| R10 | Preset no incluye PII, token ni secreto | Redactar/denegar payload y logs |
| R11 | Respuesta antigua A nunca reemplaza B | Cambios de contexto invalidan generaciones anteriores |
| R12 | Reversión usa snapshot de misma empresa | Reset global no pretende restauración tenant |
| R13 | Flags no autorizan QA/main/PROD ni CI bypass | Gate requiere autorización y SHA exacto |
| R14 | A5 exige Task distinta del autor | Ninguna autoaprobación o reserva de sucesora |

## 5. Casos de prueba conceptuales P01–P04 / N01–N10 (NO EJECUTADOS)

| Caso | Tipo | Estímulo futuro | Resultado requerido |
| --- | --- | --- | --- |
| P01 | positivo | Empresa A autorizada consulta configuración con versión | Sólo flags A validados y valor conservador |
| P02 | positivo | A previsualiza tema sin publicar | Tokens accesibles de preview sin escritura persistida |
| P03 | positivo | A publica con expectedVersion correcta | CAS ganador con auditoría y versión nueva |
| P04 | positivo | A cambia a B autorizada | Marca A limpiada y marca B tras readback seguro |
| N01 | negativo | A envía EmpresaId de B | Denegar, sin revelar preset B |
| N02 | negativo | Cliente activa K01=true sin grant | No habilita endpoint ni guard servidor |
| N03 | negativo | CSS inseguro en K06 | Rechazar y conservar fallback |
| N04 | negativo | AssetId perteneciente a B | Rechazo y no carga archivo privado |
| N05 | negativo | Dos publicaciones versión n | Una acepta, otra conflicto CAS |
| N06 | negativo | Payload contiene JWT, DSN, contraseña | Redactar y bloquear |
| N07 | negativo | Logout durante carga de tema | No reaparece marca A ni permiso |
| N08 | negativo | Revert intenta snapshot de B | Denegar, no mutar versiones |
| N09 | negativo | Prueba causal CI FAILED/SKIPPED/QUEUED | VALIDANDO, nunca PASS |
| N10 | negativo | Documentar un flag se interpreta como activarlo | Prohibido publicar API, SQL, QA/main/PROD |

Todos los casos son FUTUROS_NO_EJECUTADOS: esta microtarea no ejecutó Angular, MySQL, API, pruebas de navegador ni operación de branding tenant.

## 6. Impacto, verificación A0–A5 y rollback

**A0:** diff estrictamente dos Markdown, CHANGELOG con encabezados/viñetas contiguos, sin secretos. **A1:** enumerar K01–K14, ámbitos, prioridad y R01–R14, P/N y distinguir contratos de flags operativos. **A2:** evaluar estáticamente aislamiento A/B, JWT/RBAC, tokens seguros, accesibilidad, CAS, revocación y fallbacks. **A3:** cotejar TemaVisual, ConfigEmpresa, ThemeApplierService, EmpresaIdentidadService y modelo C05-02.M02.01 con estado real de sus filas, revisar DAG/COLA dinámico. **A4:** commit fast-forward CAS dev, checks Scope Lock y procedencia V5 completed/success en SHA exacto, readback PLAN/COLA/BITACORA. Tests de compilación no causales NO_APLICA, nunca PASS ficticio. **A5:** revisión independiente por otra de cinco Tasks Javier, antes de liberar dependientes.

**No alcance:** no crear flags reales, cambiar Angular, Domain, API, migraciones/DB, tema global Id=1, roles, datos live, Storage, secrets, APIs de proveedores, QA/main/PROD o nuevos Tasks. **Rollback:** revert Git normal con nuevo SHA y controles, BITACORA histórica intacta. Las fases de implementación posteriores requieren CI de código y seguridad efectiva.
