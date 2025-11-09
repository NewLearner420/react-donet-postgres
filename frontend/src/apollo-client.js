import { ApolloClient, InMemoryCache, HttpLink, split } from '@apollo/client';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { getMainDefinition } from '@apollo/client/utilities';
import { createClient } from 'graphql-ws';
import { setContext } from '@apollo/client/link/context';
import keycloak from './keycloak';

// HTTP connection to the API
const httpLink = new HttpLink({
  uri: import.meta.env.VITE_GRAPHQL_HTTP_URL || 'http://localhost:5000/graphql',
  credentials: 'include',
});

// Auth link to add JWT token to requests
const authLink = setContext(async (_, { headers }) => {
  // Wait for Keycloak to be ready and update token if needed
  try {
    await keycloak.updateToken(5); // Refresh if expiring in 5 seconds
  } catch (error) {
    console.error('Failed to refresh token:', error);
  }

  const token = keycloak.token;
  
  console.log('🔑 Auth Link - Token exists:', !!token);
  console.log('🔑 Auth Link - Token preview:', token ? token.substring(0, 50) + '...' : 'NO TOKEN');
  
  return {
    headers: {
      ...headers,
      authorization: token ? `Bearer ${token}` : '',
    }
  };
});

// Combine auth and http links
const httpLinkWithAuth = authLink.concat(httpLink);

// WebSocket connection for subscriptions
const wsLink = new GraphQLWsLink(
  createClient({
    url: import.meta.env.VITE_GRAPHQL_WS_URL || 'ws://localhost:5000/graphql',
    connectionParams: async () => {
      try {
        await keycloak.updateToken(5);
      } catch (error) {
        console.error('Failed to refresh token for WebSocket:', error);
      }
      
      const token = keycloak.token;
      return {
        authorization: token ? `Bearer ${token}` : '',
      };
    },
    shouldRetry: () => true,
    retryAttempts: 5,
    on: {
      connected: () => console.log('✅ WebSocket connected'),
      closed: () => console.log('❌ WebSocket closed'),
      error: (error) => console.error('❌ WebSocket error:', error),
    },
  })
);

// Split links based on operation type
const splitLink = split(
  ({ query }) => {
    const definition = getMainDefinition(query);
    return (
      definition.kind === 'OperationDefinition' &&
      definition.operation === 'subscription'
    );
  },
  wsLink,
  httpLinkWithAuth
);

const client = new ApolloClient({
  link: splitLink,
  cache: new InMemoryCache(),
});

export default client;