# C15-04.M05.04 — incremento A109 de cobertura de navegación por teclado

**Estado:** incremento de verificación R2 en la WIP histórica DEV02, no certificación global ni transición LISTO. **Entorno:** exclusivamente dev; CI GitHub-hosted con API .NET 10, MySQL 8.4 efímero, Angular 22.2.1 y Chromium.

**Requisito propio D:** ejecutar caso positivo de aceptación sobre UX global, sin simular las ~160 rutas. La suite existente A108 quedó 143/143 Chromium PASS en SHA 9a261d50bb2e86f5ac89de10de54589f9ac0b008; este incremento añade tres tests por interfaz privada autentica. Se prueban foco por tecla Tab, :focus-visible, dos controles con etiqueta accesible y ausencia de body overflow en anchos 320, 390, 768, 1280 y 1440. JWT firmado y permiso global se comprueban mediante /plataforma/sesion; se excluyen escrituras al backend.

**A0-A4:** A0 ruta y diff de spec + changelog; A1 tres positivos de teclado/foco con fallos visibles si no hay controles accesibles; A2 JWT/RBAC/tenant A/B referenciado a tests existentes (ningun nuevo permiso); A3 navegador/API/DB real en CI pendiente del SHA nuevo; A4 exigir Scope Lock/V5, build Angular/backend/security y check C15 exact-SHA completos SUCCESS. Las pruebas nuevas no afirman zoom 200 real ni auditoria WCAG/comercial total. Un resultado SKIPPED, CANCELLED o IN_PROGRESS no es PASS.

**Gobierno de referencias:** SKREF-03 Impeccable origen pbakaus/impeccable pin 2149fcce39a90bb409df5f16515f316a76dc6199; SKREF-10 Angular Developer origen angular/angular pin 6808966993bf2b2d455d9b6245e960a59a01b9a9, solo lectura. Motion/landing/copy/threat model/.NET perf NO_APLICA: no se cambian esas capas.

**Rollback:** git revert normal del commit específico previa revalidación de HEAD; sin force-push, sin QA/main/PROD ni secretos. El candado propio V17 2000002297 permanece mientras H=EN_PROGRESO, owner histórico preservado. A5 pendiente únicamente tras A0-A4 global verdaderos, por revisor independiente.
