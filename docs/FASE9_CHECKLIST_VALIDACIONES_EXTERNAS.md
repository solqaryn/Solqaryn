# Fase 9 — Checklist de validaciones externas del propietario

Fecha de preparación: 2026-07-29  
Entorno permitido: `SOLQARYN_desarrollo`  
Producción: prohibida para estas pruebas

## Instrucciones generales

Para cada prueba:

1. ejecutar únicamente en Desarrollo;
2. no copiar secretos en GitHub, chat, capturas ni documentos;
3. registrar fecha, responsable y resultado;
4. adjuntar captura o evidencia sin credenciales;
5. anotar cualquier error con su referencia segura;
6. no corregir directamente Producción;
7. detenerse si existe riesgo de pérdida de datos.

## 1. Correo real

### Preparación

- [ ] Configurar secretos SMTP directamente en Render Desarrollo.
- [ ] Confirmar que el servicio es `solqaryn-api-desarrollo`.
- [ ] Confirmar que Producción no comparte estas credenciales.
- [ ] Usar un buzón controlado por Javier Mejía.

### Ejecución

- [ ] Crear o seleccionar una factura de prueba.
- [ ] Enviar la factura al buzón controlado.
- [ ] Confirmar recepción.
- [ ] Revisar bandeja principal.
- [ ] Revisar spam/promociones.
- [ ] Confirmar remitente.
- [ ] Confirmar `Reply-To`.
- [ ] Confirmar asunto.
- [ ] Confirmar versión HTML.
- [ ] Confirmar texto plano.
- [ ] Abrir PDF A4 adjunto.
- [ ] Confirmar importes, cliente y número de factura.
- [ ] Ejecutar doble clic o doble solicitud.
- [ ] Confirmar que solo se recibe un correo.
- [ ] Revisar historial de correo.
- [ ] Revisar logs de Render sin secretos.

Resultado esperado:

```text
Correo recibido una sola vez, contenido correcto, PDF válido y logs seguros.
```

## 2. Render Desarrollo

- [ ] Confirmar rama desplegada: `Desarrollo`.
- [ ] Confirmar nombre lógico: `SOLQARYN_desarrollo`.
- [ ] Confirmar health `/health`.
- [ ] Confirmar readiness `/health/ready`.
- [ ] Confirmar que el servicio inicia sin errores.
- [ ] Confirmar migraciones de Desarrollo según la estrategia aprobada.
- [ ] Confirmar ausencia de stack traces visibles.
- [ ] Confirmar ausencia de secretos en logs.
- [ ] Confirmar reinicio controlado.
- [ ] Confirmar recuperación después del reinicio.

## 3. Vercel Desarrollo

- [ ] Confirmar que el frontend corresponde a `Desarrollo`.
- [ ] Confirmar proxy `/api` hacia Render Desarrollo.
- [ ] Iniciar sesión.
- [ ] Recorrer menú completo permitido.
- [ ] Crear un producto de prueba.
- [ ] Editar el producto.
- [ ] Crear una venta de prueba.
- [ ] Abrir una factura.
- [ ] Confirmar recarga directa de rutas.
- [ ] Confirmar ausencia de errores 404 del SPA.
- [ ] Revisar consola del navegador.

## 4. Aiven Desarrollo

- [ ] Confirmar base de datos de Desarrollo.
- [ ] Confirmar usuario y permisos mínimos esperados.
- [ ] Generar respaldo previo.
- [ ] Verificar que el respaldo sea descargable o restaurable.
- [ ] Aplicar únicamente la estrategia de migración aprobada.
- [ ] Confirmar tabla de historial de migraciones.
- [ ] Confirmar tablas de variantes.
- [ ] Confirmar tablas de cargas masivas.
- [ ] Confirmar índices únicos.
- [ ] Ejecutar consultas de lectura de control.
- [ ] Confirmar que Producción no fue consultada ni modificada.

## 5. Cloudinary Desarrollo

- [ ] Confirmar prefijo/carpeta de Desarrollo.
- [ ] Cargar imagen de producto de prueba.
- [ ] Confirmar visualización en listado y detalle.
- [ ] Cambiar imagen principal.
- [ ] Confirmar fallback ante URL inválida controlada.
- [ ] Eliminar lógicamente el producto o imagen según el flujo autorizado.
- [ ] Confirmar que ningún activo productivo fue afectado.
- [ ] Revisar transformaciones y cuotas.

## 6. WhatsApp

- [ ] Abrir factura desde un teléfono real.
- [ ] Compartir por WhatsApp.
- [ ] Confirmar texto generado.
- [ ] Confirmar enlace público.
- [ ] Abrir enlace desde otro dispositivo.
- [ ] Confirmar vencimiento o límite de acceso.
- [ ] Confirmar que no expone datos internos.

## 7. Impresión física

### Oficina

- [ ] A4.
- [ ] Carta.
- [ ] Legal.
- [ ] Oficio.
- [ ] A5.
- [ ] Márgenes correctos.
- [ ] Logo legible.
- [ ] Importes completos.
- [ ] Sin contenido cortado.

### Térmica

- [ ] POS 58 mm.
- [ ] POS 80 mm.
- [ ] USB.
- [ ] Red, si aplica.
- [ ] Bluetooth, si aplica.
- [ ] Densidad legible.
- [ ] Corte correcto.
- [ ] Avance correcto.
- [ ] Sin columnas cortadas.

## 8. Dispositivos reales

### Android

- [ ] Inicio de sesión.
- [ ] Menú.
- [ ] Producto con variantes.
- [ ] Compra.
- [ ] Venta.
- [ ] Factura.
- [ ] Carga masiva.
- [ ] Tema claro y oscuro.

### iPhone

- [ ] Inicio de sesión.
- [ ] Menú.
- [ ] Formularios.
- [ ] Tablas con scroll interno.
- [ ] Factura y PDF.
- [ ] Compartir.

### Tablet

- [ ] Orientación vertical.
- [ ] Orientación horizontal.
- [ ] Tablas y diálogos.
- [ ] Formularios largos.

## 9. Red y sesión

- [ ] Red lenta real.
- [ ] Pérdida temporal de conexión.
- [ ] Reintento controlado.
- [ ] Mensajes de error sin detalles internos.
- [ ] Sesión con actividad durante 30 minutos.
- [ ] Cierre después de inactividad continua.
- [ ] Varias pestañas.
- [ ] Renovación de token sin perder formulario.

## 10. Aceptación funcional

- [ ] El flujo de productos coincide con la operación real.
- [ ] Las cantidades por color son comprensibles.
- [ ] Compras y ventas reflejan el color correcto.
- [ ] Facturas muestran importes esperados.
- [ ] Costos de envío corresponden a tarifas reales.
- [ ] Descuentos corresponden a políticas reales.
- [ ] Reportes son útiles para administración.
- [ ] Auditoría contiene información suficiente.
- [ ] Cargas masivas usan plantillas aceptables.
- [ ] Diseño final aprobado por Javier Mejía.

## 11. Registro de resultados

| Área | Fecha | Responsable | Resultado | Evidencia | Observaciones |
|---|---|---|---|---|---|
| Correo real |  |  | Pendiente |  |  |
| Render Desarrollo |  |  | Pendiente |  |  |
| Vercel Desarrollo |  |  | Pendiente |  |  |
| Aiven Desarrollo |  |  | Pendiente |  |  |
| Cloudinary Desarrollo |  |  | Pendiente |  |  |
| WhatsApp |  |  | Pendiente |  |  |
| Impresión física |  |  | Pendiente |  |  |
| Android |  |  | Pendiente |  |  |
| iPhone |  |  | Pendiente |  |  |
| Tablet |  |  | Pendiente |  |  |
| Red y sesión |  |  | Pendiente |  |  |
| Aceptación final |  |  | Pendiente |  |  |

## 12. Aceptación de excepción

Cuando una validación no pueda ejecutarse, Javier Mejía debe registrar expresamente:

```text
Validación omitida:
Motivo:
Riesgo conocido:
Medida compensatoria:
Vigencia de la excepción:
Responsable:
Aprobado por Javier Mejía:
Fecha:
```

La ausencia de una prueba no equivale a aprobación automática.