import { useKeycloak } from '@react-keycloak/web';
import { useQuery, gql } from '@apollo/client';

const GET_USERS = gql`
  query {
    users {
      id
      name
      email
      createdAt
    }
  }
`;

function App() {
  const { keycloak, initialized } = useKeycloak();
  const { data, loading, error } = useQuery(GET_USERS, {
    skip: !keycloak.authenticated, // Only run query if authenticated
  });

    // Add this debug logging
  if (keycloak.authenticated) {
    console.log('Token Issuer:', keycloak.tokenParsed?.iss);
    console.log('Token Audience:', keycloak.tokenParsed?.aud);
    console.log('Full Token:', keycloak.token);
  }

  if (!initialized) {
    return <div>Loading Keycloak...</div>;
  }

  if (!keycloak.authenticated) {
    return (
      <div style={{ padding: '50px', textAlign: 'center' }}>
        <h1>Welcome to the App</h1>
        <p>Please log in to continue</p>
        <button 
          onClick={() => keycloak.login()}
          style={{
            padding: '10px 20px',
            fontSize: '16px',
            cursor: 'pointer',
            backgroundColor: '#4CAF50',
            color: 'white',
            border: 'none',
            borderRadius: '4px'
          }}
        >
          Login
        </button>
      </div>
    );
  }

  return (
    <div style={{ padding: '20px' }}>
      <div style={{ marginBottom: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h2>Welcome, {keycloak.tokenParsed?.preferred_username}!</h2>
          <p>Email: {keycloak.tokenParsed?.email}</p>
        </div>
        <button 
          onClick={() => keycloak.logout()}
          style={{
            padding: '10px 20px',
            cursor: 'pointer',
            backgroundColor: '#f44336',
            color: 'white',
            border: 'none',
            borderRadius: '4px'
          }}
        >
          Logout
        </button>
      </div>

      <h3>Users List</h3>
      
      {loading && <p>Loading users...</p>}
      {error && <p style={{ color: 'red' }}>Error: {error.message}</p>}
      
      {data && (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ backgroundColor: '#f2f2f2' }}>
              <th style={{ border: '1px solid #ddd', padding: '8px' }}>ID</th>
              <th style={{ border: '1px solid #ddd', padding: '8px' }}>Name</th>
              <th style={{ border: '1px solid #ddd', padding: '8px' }}>Email</th>
              <th style={{ border: '1px solid #ddd', padding: '8px' }}>Created At</th>
            </tr>
          </thead>
          <tbody>
            {data.users.map((user) => (
              <tr key={user.id}>
                <td style={{ border: '1px solid #ddd', padding: '8px' }}>{user.id}</td>
                <td style={{ border: '1px solid #ddd', padding: '8px' }}>{user.name}</td>
                <td style={{ border: '1px solid #ddd', padding: '8px' }}>{user.email}</td>
                <td style={{ border: '1px solid #ddd', padding: '8px' }}>
                  {new Date(user.createdAt).toLocaleString()}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default App;