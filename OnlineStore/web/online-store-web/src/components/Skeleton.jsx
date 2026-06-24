import React from 'react';
import './Skeleton.css';

export default function Skeleton({ width, height, borderRadius = '8px', className = '', style = {} }) {
    const combinedStyle = {
        width: width || '100%',
        height: height || '100%',
        borderRadius,
        ...style
    };

    return (
        <div className={`skeleton-loader ${className}`} style={combinedStyle}></div>
    );
}
