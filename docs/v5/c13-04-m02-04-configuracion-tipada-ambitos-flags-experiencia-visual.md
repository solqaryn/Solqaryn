# SOLQARYN V5 — C13-04.M02.04: configuración tipada, ámbitos y flags seguros de experiencia visual

**Fuente ejecutable:** STANDARD V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO institucional (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`), Skill canónica y `docs/PROJECT_SCOPE_LOCK.md`. **Autor:** PRIMARY DEV 02, Task `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Rama:** `solqaryn/Solqaryn@dev` exclusivamente.

**Frontera:** contrato M02 de diseño de auditoría visual. No modifica Angular, SCSS, API, MySQL, tablas, migraciones, permisos JWT/RBAC, tenant, proveedores, webhooks ni entornos QA/main/PROD. Los casos descritos son `FUTUROS_NO_EJECUTADOS`; no acreditan WCAG, integración, aislamiento A/B ni rendimiento medido. Requiere A5 independiente de una Task Javier distinta de la autora.

## 1. Política de configuración propuesta

La auditoría de experiencia visual debe respetar los tokens compartidos existentes en `frontend/src/styles.scss`, la escala de `frontend/src/styles/_enterprise-design-system.scss`, los contratos de navegación y feedback y las preferencias del usuario. La presencia de estos archivos no acredita consistencia visual ni accesibilidad de todas las rutas. Este documento **define tipos contractuales futuros**, no agrega campos reales a `environment.ts`, servicios Angular o configuración del servidor.

| Clave conceptual | Tipo / valores permitidos | Ámbito | Fallo seguro y límite |
| --- | --- | --- | --- |
| `auditEnabled` | boolean | CI/dev interno | False por defecto; jamás acceso a datos fuera de scope |
| `themeVariant` | enum `system,light,dark` | sesión autorizada | Valor inválido vuelve a `system`, no CSS arbitrario |
| `density` | enum `comfortable,compact` | preferencia de usuario | Modo compacto no oculta controles críticos |
| `locale` | locale permitido por catálogo | usuario/tenant | Idioma no reconocido preserva mensaje legible |
| `viewportProfile` | enum `320,390,768,1280` para fixture CI | solo prueba | Nunca confiar en viewport como autorización |
| `showReducedMotion` | boolean derivado de preferencia de OS | cliente | Respetar `prefers-reduced-motion`, no imponer animación |
| `feedbackAnnouncements` | enum `polite,assertive,off` según tipo de mensaje | componente | Errores importantes nunca invisibles al lector |
| `tenantBrandTokenSet` | referencia a catálogo autorizado | tenant validado | No aceptar hojas CSS remotas o tokens de otra empresa |
| `accessibilityAuditMode` | boolean sólo CI | runner de pruebas | Nunca habilita permisos ni endpoint privilegiado |
| `snapshotRedaction` | enum `strict` | runner/soporte | No desactivar saneamiento de PII/JWT |
| `navigationContractVersion` | identificador inmutable | release SHA | Rechaza contrato que no coincide con router |
| `maxClientFeedbackItems` | entero positivo acotado por diseño | componente | Sin umbral arbitrario antes de medir; fail-closed si inválido |

**Precedencia conceptual:** invariantes de backend (tenant/RBAC) > contrato de seguridad > catálogo de marca del tenant validado > preferencias de accesibilidad del dispositivo > preferencias visuales del usuario > defaults seguros. El cambio de tenant debe invalidar el tema, preferencias y resultados cacheados del contexto anterior cuando sean dependientes de esa empresa.

## 2. Validaciones V01–V12 que requieren prueba futura

| ID | Entrada / decisión | Rechazo obligado |
| --- | --- | --- |
| V01 | Flag no autorizado en producción | Habilitar un modo de auditoría en PROD |
| V02 | Identificador tenant | Aceptar un alias sin prueba backend de membresía |
| V03 | Catálogo de tema | Inyectar CSS arbitrario o activo remoto no autorizado |
| V04 | Cambio A/B | Mantener marca o datos visuales de empresa previa |
| V05 | Permiso de ruta | Considerar guard Angular sustituto de autorización API |
| V06 | Mensaje de error | Entregar traza interna, token, PII o URL sensible |
| V07 | Contrato navegación | Enlace privado sin permiso real o ruta obsoleta |
| V08 | Densidad | Colapsar labels, CTA, focus ring o teclado |
| V09 | Motion | Ignorar `prefers-reduced-motion` |
| V10 | Locale | Mostrar cadena sin texto o traducción ausente sin fallback |
| V11 | Payload de configuración | Permitir claves desconocidas o tipos incorrectos |
| V12 | CI/evidencia | Presentar screenshot/test listado como PASS sin ejecución SHA |

## 3. Compatibilidad y ámbito técnico

- Angular 22.2.1 standalone/Signals, Angular Material/CDK y Zone.js 0.16.3 continúan en el estado actual. No introducir otro framework, store global ni zoneless.
- `app.routes.ts` y `permiso.guard.ts` siguen siendo controles de experiencia; JWT/RBAC y pertenencia al tenant se validan del lado servidor.
- Sin nuevas variables de ambiente, secretos, endpoints, preferencias persistidas, feature flags activadas, cambios CSS o métricas emitidas.
- Valores propuestos para futuros contratos tipados requieren validación antes de escritura, owner por tenant y matriz rol/tenant/viewport/estado.

## 4. Pruebas posteriores y cierre

Oráculos futuros: sesión permitida A conserva foco y su tema; B nunca ve marca o datos de A; 320px mantiene CTA; error ofrece texto y acción; teclado/restauración del foco se conserva; lector oye anuncio una vez; reduced motion no elimina información; tema remoto no autorizado se deniega. Los negativos correspondientes deben verificarse en Playwright/CI en un SHA concreto. **No se ejecutaron en esta M02.04.**

A0: diffs/catálogos existentes y compatibilidad conceptual. A1: tabla de tipos y valores válidos/negativos revisada documentalmente. A2: separación de permiso servidor, tenant A/B y sanitización como condiciones, no pruebas runtime. A3: API/DB/DI no aplican. A4: Scope Lock, CHANGELOG y commit exact-SHA requieren `completed/success`. Reversión: revert Git normal y ninguna migración. A5 independiente y recálculo de DAG por fuentes vivas.
