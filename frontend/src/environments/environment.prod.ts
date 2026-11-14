export const environment = {
  production: true,
  // En Vercel se usa un proxy same-origin para evitar exponer el navegador
  // a restricciones CORS entre los dominios Preview y Render.
  apiUrl: '/api'
};
