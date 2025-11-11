// Access runtime config or fallback to build-time env vars
declare global {
  interface Window {
    APP_CONFIG?: {
      GRAPHQL_HTTP_URL: string;
      GRAPHQL_WS_URL: string;
      KEYCLOAK_URL: string;
      KEYCLOAK_REALM: string;
      KEYCLOAK_CLIENT_ID: string;
    };
  }
}

export const config = {
  graphqlHttpUrl: window.APP_CONFIG?.GRAPHQL_HTTP_URL || import.meta.env.VITE_GRAPHQL_HTTP_URL,
  graphqlWsUrl: window.APP_CONFIG?.GRAPHQL_WS_URL || import.meta.env.VITE_GRAPHQL_WS_URL,
  keycloakUrl: window.APP_CONFIG?.KEYCLOAK_URL || import.meta.env.VITE_KEYCLOAK_URL,
  keycloakRealm: window.APP_CONFIG?.KEYCLOAK_REALM || import.meta.env.VITE_KEYCLOAK_REALM,
  keycloakClientId: window.APP_CONFIG?.KEYCLOAK_CLIENT_ID || import.meta.env.VITE_KEYCLOAK_CLIENT_ID,
};

console.log('🔧 App Configuration:', config);