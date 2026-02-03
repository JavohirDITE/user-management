import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'react-toastify'
import { usersApi } from '../services/api'

function getUniqIdValue(prefix = 'id') {
    return `${prefix}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
}

function UserManagement() {
    const navigate = useNavigate()
    const [users, setUsers] = useState([])
    const [selectedIds, setSelectedIds] = useState([])
    const [loading, setLoading] = useState(true)
    const [actionLoading, setActionLoading] = useState(false)
    const [filterText, setFilterText] = useState('')
    const currentUser = JSON.parse(localStorage.getItem('user') || '{}')
    const componentId = getUniqIdValue('user-mgmt')

    useEffect(() => {
        const token = localStorage.getItem('token')
        if (!token) {
            navigate('/login')
            return
        }
        fetchUsers()
    }, [navigate])

    const fetchUsers = async () => {
        try {
            const response = await usersApi.getAll()
            setUsers(response.data)
            setSelectedIds([])
        } catch (error) {
            toast.error('Failed to load users')
        } finally {
            setLoading(false)
        }
    }

    const filteredUsers = users.filter(user => {
        if (!filterText) return true
        const searchLower = filterText.toLowerCase()
        return (
            user.name.toLowerCase().includes(searchLower) ||
            user.email.toLowerCase().includes(searchLower) ||
            user.status.toLowerCase().includes(searchLower)
        )
    })

    const handleSelectAll = (e) => {
        if (e.target.checked) {
            setSelectedIds(filteredUsers.map(u => u.id))
        } else {
            setSelectedIds([])
        }
    }

    const handleSelectOne = (id) => {
        if (selectedIds.includes(id)) {
            setSelectedIds(selectedIds.filter(i => i !== id))
        } else {
            setSelectedIds([...selectedIds, id])
        }
    }

    const handleBlock = async () => {
        if (selectedIds.length === 0) {
            toast.warning('Please select at least one user')
            return
        }
        setActionLoading(true)
        try {
            const response = await usersApi.block(selectedIds)
            toast.success(response.data.message)

            if (response.data.selfBlocked) {
                localStorage.removeItem('token')
                localStorage.removeItem('user')
                toast.info('You have been blocked')
                navigate('/login')
                return
            }

            fetchUsers()
        } catch (error) {
            toast.error('Failed to block users')
        } finally {
            setActionLoading(false)
        }
    }

    const handleUnblock = async () => {
        if (selectedIds.length === 0) {
            toast.warning('Please select at least one user')
            return
        }
        setActionLoading(true)
        try {
            const response = await usersApi.unblock(selectedIds)
            toast.success(response.data.message)
            fetchUsers()
        } catch (error) {
            toast.error('Failed to unblock users')
        } finally {
            setActionLoading(false)
        }
    }

    const handleDelete = async () => {
        if (selectedIds.length === 0) {
            toast.warning('Please select at least one user')
            return
        }
        setActionLoading(true)
        try {
            const response = await usersApi.delete(selectedIds)
            toast.success(response.data.message)

            if (response.data.selfDeleted) {
                localStorage.removeItem('token')
                localStorage.removeItem('user')
                toast.info('Your account has been deleted')
                navigate('/login')
                return
            }

            fetchUsers()
        } catch (error) {
            toast.error('Failed to delete users')
        } finally {
            setActionLoading(false)
        }
    }

    const handleDeleteUnverified = async () => {
        setActionLoading(true)
        try {
            const response = await usersApi.deleteUnverified()
            toast.success(response.data.message)

            if (response.data.selfDeleted) {
                localStorage.removeItem('token')
                localStorage.removeItem('user')
                toast.info('Your unverified account has been deleted')
                navigate('/login')
                return
            }

            fetchUsers()
        } catch (error) {
            toast.error('Failed to delete unverified users')
        } finally {
            setActionLoading(false)
        }
    }

    const handleLogout = () => {
        localStorage.removeItem('token')
        localStorage.removeItem('user')
        navigate('/login')
    }

    const formatDate = (dateStr) => {
        if (!dateStr) return 'Never'
        return new Date(dateStr).toLocaleString()
    }

    const getStatusBadge = (status) => {
        const classes = {
            unverified: 'status-badge status-unverified',
            active: 'status-badge status-active',
            blocked: 'status-badge status-blocked'
        }
        return <span className={classes[status] || classes.unverified}>{status}</span>
    }

    if (loading) {
        return (
            <div className="dashboard-container">
                <div className="loading-container">
                    <div className="spinner-border text-primary" role="status">
                        <span className="visually-hidden">Loading...</span>
                    </div>
                </div>
            </div>
        )
    }

    return (
        <div className="dashboard-container" id={componentId}>
            <div className="dashboard-header">
                <h1 className="dashboard-title">
                    <i className="bi bi-people-fill me-2"></i>
                    User Management
                </h1>
                <div className="user-info">
                    <span className="user-email">{currentUser.email}</span>
                    <button
                        className="btn btn-outline-secondary btn-sm"
                        onClick={handleLogout}
                        title="Sign out"
                    >
                        <i className="bi bi-box-arrow-right"></i> Logout
                    </button>
                </div>
            </div>

            <div className="dashboard-content">
                <div className="toolbar">
                    <button
                        className="btn btn-warning"
                        onClick={handleBlock}
                        disabled={actionLoading || selectedIds.length === 0}
                        title="Block selected users"
                    >
                        <i className="bi bi-lock-fill me-1"></i>
                        Block
                    </button>

                    <button
                        className="btn btn-success"
                        onClick={handleUnblock}
                        disabled={actionLoading || selectedIds.length === 0}
                        title="Unblock selected users"
                    >
                        <i className="bi bi-unlock-fill"></i>
                    </button>

                    <button
                        className="btn btn-danger"
                        onClick={handleDelete}
                        disabled={actionLoading || selectedIds.length === 0}
                        title="Delete selected users"
                    >
                        <i className="bi bi-trash-fill"></i>
                    </button>

                    <button
                        className="btn btn-outline-secondary"
                        onClick={handleDeleteUnverified}
                        disabled={actionLoading}
                        title="Delete all unverified users"
                    >
                        <i className="bi bi-person-x-fill"></i>
                    </button>

                    <div className="toolbar-right">
                        <input
                            type="text"
                            className="form-control filter-input"
                            placeholder="Filter"
                            value={filterText}
                            onChange={(e) => setFilterText(e.target.value)}
                        />
                    </div>
                </div>

                {selectedIds.length > 0 && (
                    <div className="selection-bar">
                        {selectedIds.length} selected
                    </div>
                )}

                <div className="table-container">
                    {filteredUsers.length === 0 ? (
                        <div className="empty-state">
                            <i className="bi bi-inbox"></i>
                            <p>No users found</p>
                        </div>
                    ) : (
                        <table className="table user-table table-hover">
                            <thead>
                                <tr>
                                    <th style={{ width: '50px' }}>
                                        <input
                                            type="checkbox"
                                            className="form-check-input"
                                            checked={selectedIds.length === filteredUsers.length && filteredUsers.length > 0}
                                            onChange={handleSelectAll}
                                            title="Select all / Deselect all"
                                        />
                                    </th>
                                    <th>Name</th>
                                    <th>Email</th>
                                    <th>Status</th>
                                    <th>Last seen</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredUsers.map(user => (
                                    <tr
                                        key={user.id}
                                        className={selectedIds.includes(user.id) ? 'table-active' : ''}
                                    >
                                        <td>
                                            <input
                                                type="checkbox"
                                                className="form-check-input"
                                                checked={selectedIds.includes(user.id)}
                                                onChange={() => handleSelectOne(user.id)}
                                            />
                                        </td>
                                        <td>{user.name}</td>
                                        <td>{user.email}</td>
                                        <td>{getStatusBadge(user.status)}</td>
                                        <td>{formatDate(user.lastLogin)}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>
            </div>
        </div>
    )
}

export default UserManagement
