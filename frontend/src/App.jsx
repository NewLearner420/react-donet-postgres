import { useKeycloak } from '@react-keycloak/web';
import UserCRUDApp from './components/UserCRUDApp';

function App() {
  const { keycloak, initialized } = useKeycloak();

  if (!initialized) {
    return <div style={{ padding: '50px', textAlign: 'center' }}>Loading Keycloak...</div>;
  }

  return <UserCRUDApp keycloak={keycloak} />;
}

export default App;