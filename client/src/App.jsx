import { Routes, Route, Navigate } from 'react-router-dom'
import Login from './pages/Login'
import Register from './pages/Register'
import UserManagement from './pages/UserManagement'
import VerifyEmail from './pages/VerifyEmail'

function App() {
    return (
        <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/users" element={<UserManagement />} />
            <Route path="/verify/:token" element={<VerifyEmail />} />
            <Route path="/" element={<Navigate to="/login" replace />} />
        </Routes>
    )
}

export default App
