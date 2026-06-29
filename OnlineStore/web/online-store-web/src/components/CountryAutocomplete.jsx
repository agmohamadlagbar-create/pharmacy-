import { useState, useRef, useEffect, useMemo, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import countries from '../data/countries.js'

function getLocalName(c, lng) {
  if (lng === 'ar' && c.nameAr) return c.nameAr
  if (lng === 'tr' && c.nameTr) return c.nameTr
  return c.name
}

export default function CountryAutocomplete({ value, onChange, placeholder, required }) {
  const { i18n } = useTranslation()
  const lng = i18n.language
  const [inputValue, setInputValue] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const [highlightedIndex, setHighlightedIndex] = useState(-1)
  const [selectedCode, setSelectedCode] = useState('')
  const [error, setError] = useState('')
  const inputRef = useRef(null)
  const wrapperRef = useRef(null)

  const filtered = useMemo(() => {
    const q = inputValue.trim().toLowerCase()
    if (!q) return countries
    return countries.filter(c =>
      c.name.toLowerCase().includes(q) ||
      c.code.toLowerCase().includes(q) ||
      (c.nameAr && c.nameAr.toLowerCase().includes(q)) ||
      (c.nameTr && c.nameTr.toLowerCase().includes(q))
    )
  }, [inputValue])

  const syncFromValue = useCallback(() => {
    if (value) {
      const match = countries.find(c => c.name === value)
      if (match) {
        setInputValue(getLocalName(match, lng))
        setSelectedCode(match.code)
      } else {
        setInputValue(value)
        setSelectedCode('')
      }
    } else {
      setInputValue('')
      setSelectedCode('')
    }
  }, [value, lng])

  useEffect(() => { syncFromValue() }, [syncFromValue])

  useEffect(() => {
    function handleClickOutside(e) {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  function handleInputChange(e) {
    const val = e.target.value
    setInputValue(val)
    setIsOpen(true)
    setHighlightedIndex(0)
    if (selectedCode) {
      setSelectedCode('')
      onChange({ name: val, code: '' })
    }
    setError('')
  }

  function selectCountry(c) {
    setInputValue(getLocalName(c, lng))
    setSelectedCode(c.code)
    onChange({ name: c.name, code: c.code })
    setIsOpen(false)
    setHighlightedIndex(-1)
    setError('')
  }

  function handleFocus() {
    if (!inputValue.trim()) {
      setHighlightedIndex(0)
      setIsOpen(true)
    }
  }

  function handleBlur() {
    setTimeout(() => {
      if (!selectedCode && inputValue.trim()) {
        const q = inputValue.trim().toLowerCase()
        const match = countries.find(c =>
          c.name.toLowerCase() === q ||
          (c.nameAr && c.nameAr.toLowerCase() === q) ||
          (c.nameTr && c.nameTr.toLowerCase() === q)
        )
        if (!match) {
          setError('Please select a valid country from the list.')
        }
      }
      setIsOpen(false)
    }, 200)
  }

  function handleKeyDown(e) {
    if (!isOpen) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        setIsOpen(true)
        setHighlightedIndex(0)
        e.preventDefault()
        return
      }
      return
    }

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault()
        setHighlightedIndex(prev =>
          prev < filtered.length - 1 ? prev + 1 : 0
        )
        break
      case 'ArrowUp':
        e.preventDefault()
        setHighlightedIndex(prev =>
          prev > 0 ? prev - 1 : filtered.length - 1
        )
        break
      case 'Enter':
        e.preventDefault()
        if (highlightedIndex >= 0 && highlightedIndex < filtered.length) {
          selectCountry(filtered[highlightedIndex])
        }
        break
      case 'Escape':
        e.preventDefault()
        setIsOpen(false)
        setHighlightedIndex(-1)
        break
    }
  }

  return (
    <div ref={wrapperRef} style={{ position: 'relative' }}>
      <input
        ref={inputRef}
        type="text"
        value={inputValue}
        onChange={handleInputChange}
        onFocus={handleFocus}
        onBlur={handleBlur}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        required={required}
        autoComplete="off"
        style={{
          width: '100%',
          padding: '12px 14px',
          borderRadius: 'var(--radius-m)',
          border: error ? '1px solid #ef4444' : '0.5px solid var(--border)',
          background: 'var(--bg)',
          color: 'var(--text-primary)',
          fontFamily: "'DM Sans', sans-serif",
          fontSize: '13px',
          outline: 'none',
          transition: 'border-color 0.2s',
          boxSizing: 'border-box',
        }}
      />
      {error && (
        <div style={{
          fontSize: '11px',
          color: '#ef4444',
          marginTop: '4px',
          paddingLeft: '2px',
        }}>
          {error}
        </div>
      )}
      {isOpen && (
        <div style={{
          position: 'absolute',
          top: '100%',
          left: 0,
          right: 0,
          zIndex: 50,
          background: 'var(--card)',
          border: '1px solid var(--border)',
          borderRadius: '8px',
          marginTop: '4px',
          maxHeight: '220px',
          overflowY: 'auto',
          boxShadow: '0 8px 24px rgba(0,0,0,0.3)',
          backdropFilter: 'blur(16px)',
        }}>
          {filtered.length === 0 ? (
            <div style={{
              padding: '10px 14px',
              color: 'var(--muted)',
              fontSize: '13px',
            }}>
              No countries found
            </div>
          ) : (
            filtered.map((c, i) => (
              <div
                key={c.code}
                onMouseDown={() => selectCountry(c)}
                onMouseEnter={() => setHighlightedIndex(i)}
                style={{
                  padding: '10px 14px',
                  cursor: 'pointer',
                  borderBottom: i < filtered.length - 1 ? '1px solid var(--border)' : 'none',
                  background: i === highlightedIndex ? 'var(--bg)' : 'transparent',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '10px',
                  transition: 'background 0.15s',
                }}
              >
                <span style={{ fontSize: '18px', lineHeight: 1 }}>{c.flag}</span>
                <span style={{ fontWeight: 500, color: 'var(--text)', fontSize: '14px' }}>
                  {getLocalName(c, lng)}
                </span>
                <span style={{
                  marginLeft: 'auto',
                  fontSize: '11px',
                  color: 'var(--muted)',
                  fontFamily: 'monospace',
                }}>
                  {c.code}
                </span>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  )
}
