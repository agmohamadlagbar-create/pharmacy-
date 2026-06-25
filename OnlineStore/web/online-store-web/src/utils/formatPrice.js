/**
 * Formats a numeric price into a locale-aware currency string.
 * Uses browser/system locale for standard number formatting rules
 * (e.g., thousands separators, decimal points) and the specified currency symbol.
 *
 * @param {number|string} amount The price amount to format
 * @param {string} currencyCode The ISO 4217 currency code (e.g., 'USD', 'TRY', 'EUR', 'IQD')
 * @returns {string} The formatted currency string
 */
export function formatPrice(amount, currencyCode = 'USD') {
  try {
    const numericAmount = typeof amount === 'number' ? amount : Number(amount || 0);
    // If the currency code is null/undefined or empty, default to USD
    const code = (currencyCode && currencyCode.trim()) ? currencyCode.toUpperCase() : 'USD';
    
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: code,
    }).format(numericAmount);
  } catch (error) {
    const fallbackVal = typeof amount === 'number' ? amount : Number(amount || 0);
    return `$${fallbackVal.toFixed(2)}`;
  }
}
