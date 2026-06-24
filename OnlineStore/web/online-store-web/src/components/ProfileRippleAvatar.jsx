import React from 'react';
import './ProfileRippleAvatar.css';

const UserIcon = () => (
    <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
        <circle cx="12" cy="7" r="4"></circle>
    </svg>
);

const ProfileRippleAvatar = ({
    src,
    size = 150,
    glowColor = '#FFD700'
}) => {
    return (
        <div
            className="premium-avatar-container"
            style={{
                '--size': `${size}px`,
                '--glow-color': glowColor
            }}
        >
            {/* Multi-layered dynamic rings */}
            <div className="orbital-ring ring-1"></div>
            <div className="orbital-ring ring-2"></div>
            <div className="orbital-ring ring-3"></div>

            <div className="avatar-main-wrapper">
                <div className="avatar-inner-glow"></div>
                <div className="avatar-frame">
                    {src ? (
                        <img src={src} alt="Profile" className="avatar-image" />
                    ) : (
                        <div className="avatar-placeholder">
                            <UserIcon />
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default ProfileRippleAvatar;
