import React, { useEffect, useState } from 'react';
import Lottie from 'lottie-react';
import './AnimationOverlay.css';

const AnimationOverlay = ({ animationData, onComplete, duration = 2000 }) => {
    const [isVisible, setIsVisible] = useState(false);

    useEffect(() => {
        setIsVisible(true);
        const timer = setTimeout(() => {
            setIsVisible(false);
            if (onComplete) {
                setTimeout(onComplete, 300); // Wait for fade-out
            }
        }, duration);

        return () => clearTimeout(timer);
    }, [duration, onComplete]);

    if (!animationData) return null;

    return (
        <div className={`animation-overlay ${isVisible ? 'fade-in' : 'fade-out'}`}>
            <div className="animation-container">
                <Lottie 
                    animationData={animationData} 
                    loop={false} 
                    style={{ width: 300, height: 300 }}
                />
            </div>
        </div>
    );
};

export default AnimationOverlay;
