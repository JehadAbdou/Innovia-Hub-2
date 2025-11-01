import React, { useEffect, useState } from "react";
import { getAllUsers } from "../api/api";
import type { User } from "../Interfaces/types";
import UserCard from "./UserCard";

interface Props {
  token: string;
}

const UsersTab: React.FC<Props> = ({ token }) => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState("");
  const [page, setPage] = useState(1);

  const filteredUsers =
    filter.trim() === ""
      ? users
      : users.filter((user) =>
          user.name.toLowerCase().startsWith(filter.toLowerCase())
        );
  const usersPerPage = 10;
  const startIndex = (page - 1) * usersPerPage;
  const paginatedUsers = filteredUsers.slice(
    startIndex,
    startIndex + usersPerPage
  );

  const loadUsers = async () => {
    if (loading) return; // Prevent multiple simultaneous loads
    try {
      setLoading(true);
      const data = await getAllUsers(token);
      setUsers(data);
      setError("");
    } catch (err) {
      console.error('Error loading users:', err);
      setError("Kunde inte ladda användare");
    } finally {
      setLoading(false);
    }
  };

  // Load users when component mounts or token changes
  useEffect(() => {
    loadUsers();
  }, [token]); // eslint-disable-line react-hooks/exhaustive-deps

  if (loading) return <p>Laddar användare...</p>;
  if (error) return <p className="error">{error}</p>;
  if (users.length === 0) return <p>Inga användare hittades</p>;

  const handleDeleteUser = (id: string) => {
    setUsers(users.filter((u) => u.id !== id));
  };

  return (
    <div className="users">
      <input
        type="text"
        placeholder="Sök användare..."
        value={filter}
        onChange={(e) => setFilter(e.target.value)}
        className="user-filter-input"
      />
      {paginatedUsers.map((u) => (
        <UserCard key={u.id} user={u} onDelete={handleDeleteUser} />
      ))}
      <div className="pagination">
        {Array.from(
          { length: Math.ceil(filteredUsers.length / usersPerPage) },
          (_, i) => {
            const pageNumber = i + 1;
            return (
              <button
                key={i}
                onClick={() => setPage(pageNumber)}
                className={page === pageNumber ? "activePage" : ""}
              >
                {i + 1}
              </button>
            );
          }
        )}
      </div>
    </div>
  );
};

export default UsersTab;
