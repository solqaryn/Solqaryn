# C11-03.M05.05 — negativos tenant y abuso Outbox (V22)

- PLAN D: prueba negativa de seguridad, aislamiento y abuso para la fase M05. Resultado de fase en E es acumulativo y NO queda certificado completo por esta microtarea.
- R2 test-only: servicio de aplicación real, repositorio Oracle EF y MySQL 8.4 efímero. Se verifica denegación de empresa B desde membresía A, misma clave con payload distinto (Conflict), tenant inválido, inyección CR/LF en clave, UNIQUE físico mediante contexto independiente y dos empresas con clave igual pero eventos distintos.
- La membresía de IUsuarioScopeService es fixture para acotar el caso; no representa RBAC/UsuarioEmpresa productivo ni JWT firmado, y esta prueba NO ejerce HTTP, worker, SMTP o PSP. Limitación de fase expresamente conservada.
- CI dirigida SHA exacto, Scope Lock, MySQL 8.4 físico, .NET10 Release y TRX 1/1 sin fallos ni omitidas. Hasta SUCCESS real permanecer en VALIDANDO con A5 pendiente por Task distinta.
- No cambia runtime, esquema ni datos productivos. Reversión mediante commit revert aditivo en dev, sin force-push ni QA/main/PROD.
