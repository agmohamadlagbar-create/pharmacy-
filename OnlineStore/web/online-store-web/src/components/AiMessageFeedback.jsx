import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../apiClient';
import './AiMessageFeedback.css';

export default function AiMessageFeedback({ conversationId, messageId }) {
  const { t } = useTranslation();
  const [rating, setRating] = useState(0);
  const [hoverRating, setHoverRating] = useState(0);
  const [comment, setComment] = useState('');
  const [feedbackType, setFeedbackType] = useState('Helpful');
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [showForm, setShowForm] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (rating === 0) return;

    setSubmitting(true);
    try {
      await api.post('/api/feedback/message', {
        conversationId,
        messageId,
        rating,
        feedbackType,
        comment: comment.trim() || null
      });
      setSubmitted(true);
    } catch (err) {
      console.error('Failed to submit AI feedback:', err);
    } finally {
      setSubmitting(false);
    }
  };

  if (submitted) {
    return (
      <div className="ai-feedback-submitted">
        <span className="feedback-check">✓</span> {t('Thank you for your feedback!')}
      </div>
    );
  }

  const tags = rating <= 3 
    ? ['Incorrect Info', 'Unhelpful', 'Nonsense', 'Too Slow', 'Other']
    : ['Accurate', 'Helpful', 'Excellent Answer', 'Fast Response', 'Other'];

  return (
    <div className="ai-message-feedback-container">
      {!showForm ? (
        <div className="feedback-trigger-row">
          <span className="feedback-prompt">{t('Rate answer:')}</span>
          <div className="feedback-stars">
            {[1, 2, 3, 4, 5].map((star) => (
              <button
                key={star}
                type="button"
                className={`star-btn ${star <= (hoverRating || rating) ? 'active' : ''}`}
                onMouseEnter={() => setHoverRating(star)}
                onMouseLeave={() => setHoverRating(0)}
                onClick={() => {
                  setRating(star);
                  setFeedbackType(star <= 3 ? 'Incorrect Info' : 'Helpful');
                  setShowForm(true);
                }}
              >
                ★
              </button>
            ))}
          </div>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="feedback-form">
          <div className="feedback-form-header">
            <span>{t('Rate:')} {rating} ★</span>
            <button 
              type="button" 
              className="feedback-cancel"
              onClick={() => {
                setShowForm(false);
                setRating(0);
                setComment('');
              }}
            >
              {t('Cancel')}
            </button>
          </div>

          <div className="feedback-tags-row">
            {tags.map((tag) => (
              <button
                key={tag}
                type="button"
                className={`feedback-tag-btn ${feedbackType === tag ? 'selected' : ''}`}
                onClick={() => setFeedbackType(tag)}
              >
                {t(tag)}
              </button>
            ))}
          </div>

          <textarea
            placeholder={t('Add comments or suggestions (optional)...')}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            className="feedback-comment-input"
            maxLength={500}
            rows={2}
          />

          <button 
            type="submit" 
            disabled={submitting || rating === 0} 
            className="feedback-submit-btn"
          >
            {submitting ? t('Submitting...') : t('Submit Feedback')}
          </button>
        </form>
      )}
    </div>
  );
}
