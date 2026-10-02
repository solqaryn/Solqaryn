module.exports = function handler(_req, res) {
  const keys = [
    'VERCEL',
    'VERCEL_ENV',
    'VERCEL_TARGET_ENV',
    'VERCEL_PROJECT_ID',
    'VERCEL_PROJECT_PRODUCTION_URL',
    'VERCEL_URL',
    'VERCEL_BRANCH_URL',
    'VERCEL_GIT_COMMIT_REF',
    'SOLQARYN_ENV',
    'API_UPSTREAM',
    'PUBLIC_ORIGIN',
    'SEO_INDEXING_ENABLED'
  ];

  const safe = {};
  for (const key of keys) {
    const value = process.env[key];
    safe[key] = {
      present: typeof value === 'string' && value.length > 0,
      value: ['VERCEL_PROJECT_ID','VERCEL_ENV','VERCEL_TARGET_ENV','VERCEL_PROJECT_PRODUCTION_URL','VERCEL_URL','VERCEL_BRANCH_URL','VERCEL_GIT_COMMIT_REF','SOLQARYN_ENV','API_UPSTREAM','PUBLIC_ORIGIN','SEO_INDEXING_ENABLED'].includes(key)
        ? (value || null)
        : null
    };
  }

  res.statusCode = 200;
  res.setHeader('Content-Type', 'application/json; charset=utf-8');
  res.setHeader('Cache-Control', 'no-store');
  res.end(JSON.stringify({ success: true, safe }));
};
