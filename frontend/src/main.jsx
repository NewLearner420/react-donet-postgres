import React from 'react';
import ReactDOM from 'react-dom/client';
import { ApolloProvider } from '@apollo/client';
import { ReactKeycloakProvider } from '@react-keycloak/web';
import client from './apollo-client';
import keycloak from './keycloak';
import App from './App';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <ReactKeycloakProvider authClient={keycloak}>
      <ApolloProvider client={client}>
        <App />
      </ApolloProvider>
    </ReactKeycloakProvider>
  </React.StrictMode>
);