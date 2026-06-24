import React from 'react';
import './RippleAvatar.css';

const UserIcon = () => (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
        <circle cx="12" cy="7" r="4"></circle>
    </svg>
);

const RippleAvatar = ({
    src,
    onClick,
    title,
    size = 80,
    glowColor = '#FFD700',
    glowThickness = 2,
    speed = '2s'
}) => {
    return (
        <div
            className="pulse-avatar-wrapper"
            onClick={onClick}
            title={title}
            style={{
                '--size': `${size}px`,
                '--glow-color': glowColor,
                '--thickness': `${glowThickness}px`,
                '--speed': speed
            }}
        >
            <div className="pulse-ring"></div>
            <div className="avatar-circle">
                {src ? (
                    <img src={src} alt="Avatar" className="avatar-img" />
                ) : (
                    <div className="avatar-placeholder-content">
                        <UserIcon />
                    </div>
                )}
            </div>
        </div>
    );
};

export default RippleAvatar;
