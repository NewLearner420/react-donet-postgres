import { useState, useEffect } from 'react';
import { useQuery, useMutation, useSubscription, gql } from '@apollo/client';
import './App.css';

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
  mutation CreateUser($name: String!, $email:String!) {
    createUser(name:$name, email: $email) {
      id
      name
      email
      createdAt
    }
  }
`;

const UPDATE_USER = gql`
  mutation UpdateUser($id: Int!, $name: String!, $email:String!) {
    updateUser(id: $id, name:$name, email: $email) {
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

// GraphQL Subscription
const USER_CHANGED = gql`
  subscription OnUserChanged {
    userChanged {
      id
      name
      email
      createdAt
      updatedAt
    }
  }
`;

function App() {
  const [formData, setFormData] = useState({ name: '', email: '' });
  const [editingUser, setEditingUser] = useState(null);

  const { loading, error, data, refetch } = useQuery(GET_USERS);
  const [createUser] = useMutation(CREATE_USER, {
    refetchQueries: [{ query: GET_USERS }],
  });
  const [updateUser] = useMutation(UPDATE_USER, {
    refetchQueries: [{ query: GET_USERS }],
  });
  const [deleteUser] = useMutation(DELETE_USER, {
    refetchQueries: [{ query: GET_USERS }],
  });

  // Subscribe to user changes
  useSubscription(USER_CHANGED, {
    onData: ({ data }) => {
      console.log('User changed:', data.data.userChanged);
      refetch();
    },
  });

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!formData.name || !formData.email) {
      alert('Please fill in all fields');
      return;
    }

    try {
      if (editingUser) {
        await updateUser({
          variables: {
        
              id: editingUser.id,
              name: formData.name,
              email: formData.email,
            
          },
        });
        setEditingUser(null);
      } else {
        await createUser({
          variables: {
            
              name: formData.name,
              email: formData.email,
            
          },
        });
      }
      setFormData({ name: '', email: '' });
    } catch (err) {
      console.error('Error saving user:', err);
      alert('Error saving user: ' + err.message);
    }
  };

  const handleEdit = (user) => {
    setEditingUser(user);
    setFormData({ name: user.name, email: user.email });
  };

  const handleDelete = async (id) => {
    if (window.confirm('Are you sure you want to delete this user?')) {
      try {
        await deleteUser({ variables: { id } });
      } catch (err) {
        console.error('Error deleting user:', err);
        alert('Error deleting user: ' + err.message);
      }
    }
  };

  const handleCancel = () => {
    setEditingUser(null);
    setFormData({ name: '', email: '' });
  };

  if (loading) return <div className="loading">Loading users...</div>;
  if (error) return <div className="error">Error: {error.message}</div>;

  return (
    <div className="container">
      <h1>User Management</h1>

      <div className="form-container">
        <h2>{editingUser ? 'Edit User' : 'Add New User'}</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="name">Name:</label>
            <input
              id="name"
              type="text"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="Enter name"
            />
          </div>
          <div className="form-group">
            <label htmlFor="email">Email:</label>
            <input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              placeholder="Enter email"
            />
          </div>
          <div className="form-actions">
            <button type="submit" className="btn btn-primary">
              {editingUser ? 'Update User' : 'Create User'}
            </button>
            {editingUser && (
              <button type="button" className="btn btn-secondary" onClick={handleCancel}>
                Cancel
              </button>
            )}
          </div>
        </form>
      </div>

      <div className="users-container">
        <h2>Users List ({data?.users?.length || 0})</h2>
        {data?.users?.length === 0 ? (
          <p className="empty-message">No users found. Create one above!</p>
        ) : (
          <div className="users-grid">
            {data?.users?.map((user) => (
              <div key={user.id} className="user-card">
                <div className="user-info">
                  <h3>{user.name}</h3>
                  <p className="email">{user.email}</p>
                  <p className="meta">
                    Created: {new Date(user.createdAt).toLocaleDateString()}
                  </p>
                  {user.updatedAt && (
                    <p className="meta">
                      Updated: {new Date(user.updatedAt).toLocaleDateString()}
                    </p>
                  )}
                </div>
                <div className="user-actions">
                  <button
                    className="btn btn-edit"
                    onClick={() => handleEdit(user)}
                  >
                    Edit
                  </button>
                  <button
                    className="btn btn-delete"
                    onClick={() => handleDelete(user.id)}
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default App;