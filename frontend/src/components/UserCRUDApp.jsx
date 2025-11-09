import React, { useState } from 'react';
import { useQuery, useMutation, useSubscription, gql } from '@apollo/client';

// GraphQL Queries
const GET_USERS = gql`
  query GetUsers {
    users {
      id
      name
      email
      createdAt
      updatedAt
    }
  }
`;

// GraphQL Mutations
const CREATE_USER = gql`
  mutation CreateUser($name: String!, $email: String!) {
    createUser(name: $name, email: $email) {
      id
      name
      email
      createdAt
    }
  }
`;

const UPDATE_USER = gql`
  mutation UpdateUser($id: Int!, $name: String, $email: String) {
    updateUser(id: $id, name: $name, email: $email) {
      id
      name
      email
      updatedAt
    }
  }
`;

const DELETE_USER = gql`
  mutation DeleteUser($id: Int!) {
    deleteUser(id: $id)
  }
`;

// GraphQL Subscriptions
const ON_USER_CREATED = gql`
  subscription OnUserCreated {
    onUserCreated {
      id
      name
      email
      createdAt
    }
  }
`;

const ON_USER_UPDATED = gql`
  subscription OnUserUpdated {
    onUserUpdated {
      id
      name
      email
      updatedAt
    }
  }
`;

const ON_USER_DELETED = gql`
  subscription OnUserDeleted {
    onUserDeleted {
      id
      name
      email
    }
  }
`;

function UserCRUDApp({ keycloak }) {
  const [formData, setFormData] = useState({ name: '', email: '' });
  const [editingUser, setEditingUser] = useState(null);
  const [notifications, setNotifications] = useState([]);

  // Query
  const { data, loading, error, refetch } = useQuery(GET_USERS, {
    skip: !keycloak.authenticated,
  });

  // Mutations
  const [createUser, { loading: creating }] = useMutation(CREATE_USER, {
    refetchQueries: [{ query: GET_USERS }],
    onCompleted: () => {
      setFormData({ name: '', email: '' });
      addNotification('User created successfully!', 'success');
    },
    onError: (err) => addNotification(`Error: ${err.message}`, 'error')
  });

  const [updateUser, { loading: updating }] = useMutation(UPDATE_USER, {
    refetchQueries: [{ query: GET_USERS }],
    onCompleted: () => {
      setEditingUser(null);
      setFormData({ name: '', email: '' });
      addNotification('User updated successfully!', 'success');
    },
    onError: (err) => addNotification(`Error: ${err.message}`, 'error')
  });

  const [deleteUser, { loading: deleting }] = useMutation(DELETE_USER, {
    refetchQueries: [{ query: GET_USERS }],
    onCompleted: () => addNotification('User deleted successfully!', 'success'),
    onError: (err) => addNotification(`Error: ${err.message}`, 'error')
  });

  // Subscriptions
  useSubscription(ON_USER_CREATED, {
    skip: !keycloak.authenticated,
    onData: ({ data }) => {
      if (data?.data?.onUserCreated) {
        addNotification(`🔔 New user created: ${data.data.onUserCreated.name}`, 'info');
        refetch();
      }
    }
  });

  useSubscription(ON_USER_UPDATED, {
    skip: !keycloak.authenticated,
    onData: ({ data }) => {
      if (data?.data?.onUserUpdated) {
        addNotification(`🔔 User updated: ${data.data.onUserUpdated.name}`, 'info');
        refetch();
      }
    }
  });

  useSubscription(ON_USER_DELETED, {
    skip: !keycloak.authenticated,
    onData: ({ data }) => {
      if (data?.data?.onUserDeleted) {
        addNotification(`🔔 User deleted: ${data.data.onUserDeleted.name}`, 'info');
        refetch();
      }
    }
  });

  // Notification system
  const addNotification = (message, type) => {
    const id = Date.now();
    setNotifications(prev => [...prev, { id, message, type }]);
    setTimeout(() => {
      setNotifications(prev => prev.filter(n => n.id !== id));
    }, 5000);
  };

  // Handlers
  const handleSubmit = (e) => {
    e.preventDefault();
    if (!formData.name || !formData.email) {
      addNotification('Please fill in all fields', 'error');
      return;
    }

    if (editingUser) {
      updateUser({
        variables: {
          id: editingUser.id,
          name: formData.name,
          email: formData.email
        }
      });
    } else {
      createUser({
        variables: {
          name: formData.name,
          email: formData.email
        }
      });
    }
  };

  const handleEdit = (user) => {
    setEditingUser(user);
    setFormData({ name: user.name, email: user.email });
  };

  const handleCancelEdit = () => {
    setEditingUser(null);
    setFormData({ name: '', email: '' });
  };

  const handleDelete = (id, name) => {
    if (window.confirm(`Are you sure you want to delete ${name}?`)) {
      deleteUser({ variables: { id } });
    }
  };

  if (!keycloak.authenticated) {
    return (
      <div style={{ padding: '50px', textAlign: 'center', fontFamily: 'Arial, sans-serif' }}>
        <h1 style={{ color: '#333', marginBottom: '20px' }}>🔐 User Management System</h1>
        <p style={{ color: '#666', marginBottom: '30px' }}>Please log in to manage users</p>
        <button 
          onClick={() => keycloak.login()}
          style={{
            padding: '12px 24px',
            fontSize: '16px',
            cursor: 'pointer',
            backgroundColor: '#4CAF50',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            boxShadow: '0 2px 4px rgba(0,0,0,0.2)',
            transition: 'background-color 0.3s'
          }}
          onMouseOver={(e) => e.target.style.backgroundColor = '#45a049'}
          onMouseOut={(e) => e.target.style.backgroundColor = '#4CAF50'}
        >
          Login with Keycloak
        </button>
      </div>
    );
  }

  return (
    <div style={{ 
      padding: '20px', 
      maxWidth: '1200px', 
      margin: '0 auto',
      fontFamily: 'Arial, sans-serif'
    }}>
      {/* Notifications */}
      <div style={{ position: 'fixed', top: '20px', right: '20px', zIndex: 1000 }}>
        {notifications.map(notif => (
          <div 
            key={notif.id}
            style={{
              padding: '12px 20px',
              marginBottom: '10px',
              borderRadius: '6px',
              backgroundColor: notif.type === 'success' ? '#4CAF50' : 
                             notif.type === 'error' ? '#f44336' : '#2196F3',
              color: 'white',
              boxShadow: '0 2px 8px rgba(0,0,0,0.2)',
              minWidth: '250px',
              animation: 'slideIn 0.3s ease-out'
            }}
          >
            {notif.message}
          </div>
        ))}
      </div>

      {/* Header */}
      <div style={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center',
        marginBottom: '30px',
        padding: '20px',
        backgroundColor: '#f5f5f5',
        borderRadius: '8px'
      }}>
        <div>
          <h1 style={{ margin: '0 0 5px 0', color: '#333' }}>👥 User Management</h1>
          <p style={{ margin: 0, color: '#666' }}>
            Welcome, <strong>{keycloak.tokenParsed?.preferred_username}</strong> ({keycloak.tokenParsed?.email})
          </p>
        </div>
        <button 
          onClick={() => keycloak.logout()}
          style={{
            padding: '10px 20px',
            cursor: 'pointer',
            backgroundColor: '#f44336',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            fontSize: '14px',
            transition: 'background-color 0.3s'
          }}
          onMouseOver={(e) => e.target.style.backgroundColor = '#da190b'}
          onMouseOut={(e) => e.target.style.backgroundColor = '#f44336'}
        >
          🚪 Logout
        </button>
      </div>

      {/* Form */}
      <div style={{
        backgroundColor: 'white',
        padding: '30px',
        borderRadius: '8px',
        boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
        marginBottom: '30px'
      }}>
        <h2 style={{ marginTop: 0, color: '#333' }}>
          {editingUser ? '✏️ Edit User' : '➕ Add New User'}
        </h2>
        <div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            <div>
              <label style={{ display: 'block', marginBottom: '8px', color: '#555', fontWeight: 'bold' }}>
                Name *
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="Enter name"
                style={{
                  width: '100%',
                  padding: '10px',
                  border: '2px solid #ddd',
                  borderRadius: '4px',
                  fontSize: '14px',
                  boxSizing: 'border-box'
                }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: '8px', color: '#555', fontWeight: 'bold' }}>
                Email *
              </label>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                placeholder="Enter email"
                style={{
                  width: '100%',
                  padding: '10px',
                  border: '2px solid #ddd',
                  borderRadius: '4px',
                  fontSize: '14px',
                  boxSizing: 'border-box'
                }}
              />
            </div>
          </div>
          <div style={{ marginTop: '20px', display: 'flex', gap: '10px' }}>
            <button
              onClick={handleSubmit}
              disabled={creating || updating}
              style={{
                padding: '12px 24px',
                backgroundColor: editingUser ? '#FF9800' : '#4CAF50',
                color: 'white',
                border: 'none',
                borderRadius: '6px',
                cursor: creating || updating ? 'not-allowed' : 'pointer',
                fontSize: '14px',
                fontWeight: 'bold',
                opacity: creating || updating ? 0.6 : 1
              }}
            >
              {creating || updating ? '⏳ Processing...' : editingUser ? '💾 Update User' : '➕ Create User'}
            </button>
            {editingUser && (
              <button
                onClick={handleCancelEdit}
                style={{
                  padding: '12px 24px',
                  backgroundColor: '#757575',
                  color: 'white',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  fontSize: '14px'
                }}
              >
                ❌ Cancel
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Users List */}
      <div style={{
        backgroundColor: 'white',
        padding: '30px',
        borderRadius: '8px',
        boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
      }}>
        <h2 style={{ marginTop: 0, color: '#333' }}>📋 Users List</h2>
        
        {loading && <p>⏳ Loading users...</p>}
        {error && <p style={{ color: '#f44336' }}>❌ Error: {error.message}</p>}
        
        {data?.users && data.users.length === 0 && (
          <p style={{ color: '#666', textAlign: 'center', padding: '40px' }}>
            No users found. Create your first user above! 👆
          </p>
        )}

        {data?.users && data.users.length > 0 && (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ 
              width: '100%', 
              borderCollapse: 'collapse',
              marginTop: '20px'
            }}>
              <thead>
                <tr style={{ backgroundColor: '#f5f5f5' }}>
                  <th style={{ border: '1px solid #ddd', padding: '12px', textAlign: 'left' }}>ID</th>
                  <th style={{ border: '1px solid #ddd', padding: '12px', textAlign: 'left' }}>Name</th>
                  <th style={{ border: '1px solid #ddd', padding: '12px', textAlign: 'left' }}>Email</th>
                  <th style={{ border: '1px solid #ddd', padding: '12px', textAlign: 'left' }}>Created</th>
                  <th style={{ border: '1px solid #ddd', padding: '12px', textAlign: 'left' }}>Updated</th>
                  <th style={{ border: '1px solid #ddd', padding: '12px', textAlign: 'center' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.users.map((user) => (
                  <tr key={user.id} style={{ 
                    backgroundColor: editingUser?.id === user.id ? '#fff3e0' : 'white'
                  }}>
                    <td style={{ border: '1px solid #ddd', padding: '12px' }}>{user.id}</td>
                    <td style={{ border: '1px solid #ddd', padding: '12px', fontWeight: 'bold' }}>
                      {user.name}
                    </td>
                    <td style={{ border: '1px solid #ddd', padding: '12px' }}>{user.email}</td>
                    <td style={{ border: '1px solid #ddd', padding: '12px', fontSize: '12px', color: '#666' }}>
                      {new Date(user.createdAt).toLocaleString()}
                    </td>
                    <td style={{ border: '1px solid #ddd', padding: '12px', fontSize: '12px', color: '#666' }}>
                      {user.updatedAt ? new Date(user.updatedAt).toLocaleString() : '-'}
                    </td>
                    <td style={{ border: '1px solid #ddd', padding: '12px', textAlign: 'center' }}>
                      <button
                        onClick={() => handleEdit(user)}
                        disabled={deleting}
                        style={{
                          padding: '6px 12px',
                          backgroundColor: '#2196F3',
                          color: 'white',
                          border: 'none',
                          borderRadius: '4px',
                          cursor: deleting ? 'not-allowed' : 'pointer',
                          marginRight: '8px',
                          fontSize: '12px'
                        }}
                      >
                        ✏️ Edit
                      </button>
                      <button
                        onClick={() => handleDelete(user.id, user.name)}
                        disabled={deleting}
                        style={{
                          padding: '6px 12px',
                          backgroundColor: '#f44336',
                          color: 'white',
                          border: 'none',
                          borderRadius: '4px',
                          cursor: deleting ? 'not-allowed' : 'pointer',
                          fontSize: '12px'
                        }}
                      >
                        🗑️ Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Footer Stats */}
      <div style={{
        marginTop: '30px',
        padding: '20px',
        backgroundColor: '#f5f5f5',
        borderRadius: '8px',
        textAlign: 'center',
        color: '#666'
      }}>
        <p style={{ margin: 0 }}>
          📊 Total Users: <strong>{data?.users?.length || 0}</strong> | 
          🔄 Real-time updates enabled via WebSocket subscriptions
        </p>
      </div>

      <style>{`
        @keyframes slideIn {
          from {
            transform: translateX(100%);
            opacity: 0;
          }
          to {
            transform: translateX(0);
            opacity: 1;
          }
        }
      `}</style>
    </div>
  );
}

export default UserCRUDApp;