import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../apiClient';
import './AiDashboardTab.css';

export default function AiDashboardTab() {
  const { t } = useTranslation();
  const [summary, setSummary] = useState(null);
  const [suggestions, setSuggestions] = useState([]);
  const [knowledgeGaps, setKnowledgeGaps] = useState([]);
  const [latestAnalysis, setLatestAnalysis] = useState(null);
  const [loading, setLoading] = useState(true);
  const [activeSubTab, setActiveSubTab] = useState('ratings');
  const [notesText, setNotesText] = useState({});
  const [resolutionText, setResolutionText] = useState({});

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    setLoading(true);
    try {
      const [sumRes, sugRes, gapRes, anaRes] = await Promise.all([
        api.get('/api/feedback/summary'),
        api.get('/api/learning/suggestions?status=pending'),
        api.get('/api/learning/knowledge-gaps'),
        api.get('/api/learning/analysis/latest')
      ]);

      setSummary(sumRes.data);
      setSuggestions(sugRes.data);
      setKnowledgeGaps(gapRes.data);
      setLatestAnalysis(anaRes.data);
    } catch (err) {
      console.error('Failed to load AI dashboard data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleReviewSuggestion = async (id, approved) => {
    const notes = notesText[id] || '';
    try {
      await api.post(`/api/learning/suggestions/${id}/review`, {
        approved,
        notes: notes.trim() || null
      });
      // Filter out reviewed suggestion
      setSuggestions(prev => prev.filter(s => s.id !== id));
      // Refresh summary/analysis
      const sumRes = await api.get('/api/feedback/summary');
      setSummary(sumRes.data);
    } catch (err) {
      alert(t('Failed to submit review'));
      console.error(err);
    }
  };

  const handleResolveGap = async (id) => {
    const resolution = resolutionText[id] || '';
    if (!resolution.trim()) {
      alert(t('Please enter a resolution note.'));
      return;
    }

    try {
      await api.patch(`/api/learning/knowledge-gaps/${id}`, {
        status: 'resolved',
        resolution: resolution.trim()
      });
      // Refresh knowledge gaps
      const gapRes = await api.get('/api/learning/knowledge-gaps');
      setKnowledgeGaps(gapRes.data);
    } catch (err) {
      alert(t('Failed to resolve knowledge gap'));
      console.error(err);
    }
  };

  const runManualAnalysis = async () => {
    if (!confirm(t('Run daily learning analysis now? This compiles all conversation telemetry.'))) return;
    try {
      await api.post('/api/learning/run-analysis');
      alert(t('Learning analysis executed successfully!'));
      fetchDashboardData();
    } catch (err) {
      alert(t('Failed to run analysis'));
      console.error(err);
    }
  };

  if (loading) {
    return (
      <div className="ai-dashboard-loading">
        <div className="spinner"></div>
        <p>{t('Loading AI Agent Analytics...')}</p>
      </div>
    );
  }

  return (
    <div className="ai-dashboard-container">
      <div className="ai-dashboard-header">
        <div>
          <h3 className="dashboard-title">{t('AI Performance & Learning Dashboard')}</h3>
          <p className="dashboard-subtitle">{t('Monitor agent ratings, review prompt optimization suggestions, and resolve identified knowledge gaps.')}</p>
        </div>
        <button className="primary manual-analysis-btn" onClick={runManualAnalysis}>
          ⚙ {t('Run Analytics Engine')}
        </button>
      </div>

      {/* Sub tabs */}
      <div className="dashboard-tabs">
        <button 
          className={`d-tab-btn ${activeSubTab === 'ratings' ? 'active' : ''}`}
          onClick={() => setActiveSubTab('ratings')}
        >
          ⭐ {t('Agent Ratings & Stats')}
        </button>
        <button 
          className={`d-tab-btn ${activeSubTab === 'suggestions' ? 'active' : ''}`}
          onClick={() => setActiveSubTab('suggestions')}
        >
          💡 {t('Improvement Suggestions ({{count}})', { count: suggestions.length })}
        </button>
        <button 
          className={`d-tab-btn ${activeSubTab === 'learning' ? 'active' : ''}`}
          onClick={() => setActiveSubTab('learning')}
        >
          🧠 {t('What Agents Learned ({{count}})', { count: knowledgeGaps.filter(g => g.status === 'detected').length })}
        </button>
      </div>

      {/* Ratings Tab */}
      {activeSubTab === 'ratings' && (
        <div className="ratings-grid">
          {/* Summary metrics */}
          <div className="metrics-row">
            <div className="metric-card">
              <span className="m-label">{t('Avg Agent Rating')}</span>
              <span className="m-val">{summary?.averageRating ? `${summary.averageRating} / 5` : 'N/A'}</span>
              <div className="stars-indicator">
                {Array.from({ length: Math.round(summary?.averageRating || 0) }).map((_, i) => (
                  <span key={i}>★</span>
                ))}
              </div>
            </div>
            <div className="metric-card">
              <span className="m-label">{t('Total Feedbacks Received')}</span>
              <span className="m-val">{summary?.totalFeedback || 0}</span>
            </div>
            <div className="metric-card">
              <span className="m-label">{t('Telemetry Conversations')}</span>
              <span className="m-val">{latestAnalysis?.totalConversations || 0}</span>
            </div>
          </div>

          <div className="dashboard-panels-row">
            {/* Agent Satisfactions */}
            <div className="dashboard-panel">
              <h4>{t('Agent Ratings Breakdown')}</h4>
              <div className="agent-ratings-list">
                {summary?.agentSatisfactionScores && Object.keys(summary.agentSatisfactionScores).length > 0 ? (
                  Object.entries(summary.agentSatisfactionScores).map(([agent, score]) => (
                    <div key={agent} className="agent-rating-item">
                      <span className="agent-name-badge">{agent}</span>
                      <div className="rating-progress-bar">
                        <div className="rating-fill" style={{ width: `${(score / 5) * 100}%` }}></div>
                      </div>
                      <span className="agent-score">{score} ★</span>
                    </div>
                  ))
                ) : (
                  <p className="no-data-msg">{t('No ratings received for individual agents yet.')}</p>
                )}
              </div>
            </div>

            {/* Common Feedback Categories */}
            <div className="dashboard-panel">
              <h4>{t('Feedback Type Breakdown')}</h4>
              <div className="type-breakdown-list">
                {summary?.feedbackTypeBreakdown && Object.keys(summary.feedbackTypeBreakdown).length > 0 ? (
                  Object.entries(summary.feedbackTypeBreakdown).map(([type, count]) => (
                    <div key={type} className="type-breakdown-item">
                      <span className="type-name">{t(type)}</span>
                      <span className="type-count-badge">{t('{{count}} times', { count })}</span>
                    </div>
                  ))
                ) : (
                  <p className="no-data-msg">{t('No specific feedback types logged yet.')}</p>
                )}
              </div>
            </div>
          </div>

          {/* Top complaints / comments */}
          <div className="dashboard-panel full-width">
            <h4>{t('Recent Complaints & Low Ratings')}</h4>
            {summary?.topComplaints && summary.topComplaints.length > 0 ? (
              <ul className="complaints-list">
                {summary.topComplaints.map((comp, idx) => (
                  <li key={idx} className="complaint-item">
                    <span className="complaint-bullet">⚠</span>
                    <p className="complaint-text">"{comp}"</p>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="no-data-msg">{t('No negative feedback comments received recently. Keep it up!')}</p>
            )}
          </div>
        </div>
      )}

      {/* Suggestions Tab */}
      {activeSubTab === 'suggestions' && (
        <div className="suggestions-tab-container">
          <div className="suggestions-header">
            <h4>{t('Pending Prompts & Rules Optimization Suggestions')}</h4>
            <p>{t('The learning engine automatically flags agents with high negative feedback rates or low tool success rates.')}</p>
          </div>

          <div className="suggestions-list">
            {suggestions.map((sug) => (
              <div key={sug.id} className="suggestion-card">
                <div className="suggestion-card-header">
                  <div>
                    <span className={`priority-badge p-${sug.priority}`}>
                      {t('Priority: {{level}}', { level: t(sug.priority === 1 ? 'High' : sug.priority === 2 ? 'Medium' : 'Low') })}
                    </span>
                    <span className="agent-name-badge">{sug.agentName}</span>
                  </div>
                  <span className="sug-category">{t('{{category}} suggestion', { category: sug.category })}</span>
                </div>

                <div className="suggestion-body">
                  <h5>{sug.title}</h5>
                  <p className="sug-desc">{sug.description}</p>
                  
                  <div className="sug-change-box">
                    <strong>{t('Suggested Action:')}</strong>
                    <pre className="sug-change-pre">{sug.suggestedChange}</pre>
                  </div>
                </div>

                <div className="suggestion-actions">
                  <input
                    type="text"
                    placeholder={t('Add review notes (optional)...')}
                    value={notesText[sug.id] || ''}
                    onChange={(e) => setNotesText(prev => ({ ...prev, [sug.id]: e.target.value }))}
                    className="review-notes-input"
                  />
                  <div className="review-buttons">
                    <button 
                      className="approve-btn"
                      onClick={() => handleReviewSuggestion(sug.id, true)}
                    >
                      ✓ {t('Approve Change')}
                    </button>
                    <button 
                      className="reject-btn"
                      onClick={() => handleReviewSuggestion(sug.id, false)}
                    >
                      ✕ {t('Reject')}
                    </button>
                  </div>
                </div>
              </div>
            ))}

            {suggestions.length === 0 && (
              <div className="empty-state-suggestions">
                <span className="empty-icon">✓</span>
                <h5>{t('All Agent Configs Clear')}</h5>
                <p>{t('There are no pending optimization suggestions. Satisfaction rates are healthy!')}</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Learning Tab */}
      {activeSubTab === 'learning' && (
        <div className="learning-tab-container">
          <div className="learning-header">
            <h4>{t('Knowledge Gaps Identified by Telemetry')}</h4>
            <p>{t('These gaps represent areas where agents were unable to resolve queries (e.g. empty tool results, assistant uncertainty).')}</p>
          </div>

          <div className="gaps-list">
            {knowledgeGaps.map((gap) => (
              <div key={gap.id} className={`gap-card ${gap.status}`}>
                <div className="gap-card-header">
                  <span className="gap-topic">{t('Topic:')} <code>{gap.topic}</code></span>
                  <span className={`status-pill ${gap.status}`}>{t(gap.status)}</span>
                </div>

                <div className="gap-body">
                  <p className="gap-desc">{gap.description}</p>
                  {gap.exampleQuestions && (
                    <div className="gap-examples">
                      <strong>{t('Sample User Queries:')}</strong>
                      <p className="example-text">"{gap.exampleQuestions}"</p>
                    </div>
                  )}
                  <div className="gap-stats">
                    <span>{t('Occurrences:')} <strong>{gap.occurrenceCount}</strong></span>
                    {gap.resolvedAtUtc && (
                      <span>{t('Resolved At: {{date}}', { date: new Date(gap.resolvedAtUtc).toLocaleDateString() })}</span>
                    )}
                  </div>

                  {gap.resolution && (
                    <div className="gap-resolution-display">
                      <strong>{t('Resolution details:')}</strong>
                      <p>{gap.resolution}</p>
                    </div>
                  )}
                </div>

                {gap.status === 'detected' && (
                  <div className="gap-resolve-area">
                    <input
                      type="text"
                      placeholder={t('Explain how this gap was fixed (e.g. adding info to product description)...')}
                      value={resolutionText[gap.id] || ''}
                      onChange={(e) => setResolutionText(prev => ({ ...prev, [gap.id]: e.target.value }))}
                      className="gap-resolution-input"
                    />
                    <button 
                      className="resolve-gap-btn"
                      onClick={() => handleResolveGap(gap.id)}
                    >
                      {t('Mark Resolved')}
                    </button>
                  </div>
                )}
              </div>
            ))}

            {knowledgeGaps.length === 0 && (
              <p className="no-data-msg">{t('No knowledge gaps detected from conversation telemetry yet.')}</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
