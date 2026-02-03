import { useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { toast } from 'react-toastify'
import { authApi } from '../services/api'

function VerifyEmail() {
    const { token } = useParams()
    const navigate = useNavigate()

    useEffect(() => {
        const verifyEmail = async () => {
            try {
                await authApi.verify(token)
                toast.success('Email verified successfully! You can now access all features.')
                navigate('/users')
            } catch (error) {
                toast.error('Invalid or expired verification link')
                navigate('/login')
            }
        }

        if (token) {
            verifyEmail()
        }
    }, [token, navigate])

    return (
        <div className="auth-container">
            <div className="auth-card text-center">
                <div className="spinner-border text-primary mb-3" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
                <p>Verifying your email...</p>
            </div>
        </div>
    )
}

export default VerifyEmail
