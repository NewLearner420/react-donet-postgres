import Keycloak from 'keycloak-js';

const { VITE_KEYCLOAK_URL, VITE_KEYCLOAK_REALM, VITE_KEYCLOAK_CLIENT_ID } = import.meta.env;

if (!VITE_KEYCLOAK_URL || !VITE_KEYCLOAK_REALM || !VITE_KEYCLOAK_CLIENT_ID) {
  throw new Error('Missing required Keycloak environment variables');
}

const keycloak = new Keycloak({
  url: VITE_KEYCLOAK_URL,
  realm: VITE_KEYCLOAK_REALM,
  clientId: VITE_KEYCLOAK_CLIENT_ID,
});

export default keycloak;