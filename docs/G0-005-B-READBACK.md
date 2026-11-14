# G0-005.B | Readback de entornos y senales operativas

Fuente: Plan Comercial Inicial V3, fila 359. G0-005.F, G0-005.G y G0-005.H estan LISTO en PLAN_MAESTRO/COLA; el cierre institucional de esta microtarea sigue subordinado a evidencias autenticadas y pruebas del candidato.

Alcance: verificar matriz DEV/QA/PROD en un contrato de evidencia estructural, enlaces SHA, proyectos, origen de API, categorias HTTP y referencias NO secretas de base, cache y credenciales. Test positivo y 33 negativos de colisiones, SSO 503, API upstream 503, Vercel READY de proyecto ajeno, SHA no coincidente, dependencias faltantes y fugas de datos. Resultado positivo unico: STRUCTURAL_CONSISTENCY_NOT_LIVE_ATTESTATION. No afirma estado real de proveedores, ausencia de secretos compartidos en entornos, acceso QA/PROD ni autentificacion. Para autenticar entornos se requieren readbacks independientes en sus cuentas autorizadas. No se deben registrar ni leer valores de secretos.

Cambio aditivo aislado en scripts/quality/g0-005-b-readback-policy.mjs y scripts/tests/g0-005-b-readback-policy.test.mjs, sin API, Angular, MySQL, JWT/RBAC ni proveedores. No dependencias nuevas ni servicios de pago. Rollback: revert aditivo de este commit directo en dev conservando trabajo ajeno. Solo dev; A5 independiente antes de cualquier promocion.
