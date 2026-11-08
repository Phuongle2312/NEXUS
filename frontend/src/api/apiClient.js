/* ==========================================================================
   API CLIENT / SERVICE LAYER - NEXUS SYSTEM
   Quản lý gọi API tới Backend (Tomcat 10.1 / Java Servlets)
   Hỗ trợ chuyển đổi mượt mà giữa chế độ Mock Data và API thật
   ========================================================================== */

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8081/nexus/api';

class ApiClient {
  constructor() {
    this.baseUrl = API_BASE_URL;
  }

  async request(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      ...options.headers
    };

    try {
      const response = await fetch(url, {
        ...options,
        headers,
        credentials: 'include' // Giữ session cookie của Tomcat
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error?.message || `HTTP Error ${response.status}`);
      }

      return await response.json();
    } catch (err) {
      console.warn(`[ApiClient] Call to ${endpoint} failed or Backend is offline:`, err.message);
      throw err;
    }
  }

  // Auth APIs
  login(username, password) {
    return this.request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password })
    });
  }

  logout() {
    return this.request('/auth/logout', { method: 'POST' });
  }

  // Orders APIs
  getOrders() {
    return this.request('/orders');
  }

  createOrder(orderData) {
    return this.request('/orders/create', {
      method: 'POST',
      body: JSON.stringify(orderData)
    });
  }

  // Connections APIs
  getConnections() {
    return this.request('/connections');
  }

  updateConnectionStatus(accountId, status) {
    return this.request('/connections/status', {
      method: 'POST',
      body: JSON.stringify({ accountId, status })
    });
  }

  // Bills & Payments APIs
  getBills() {
    return this.request('/billing/list');
  }

  generateBill(billData) {
    return this.request('/billing/generate', {
      method: 'POST',
      body: JSON.stringify(billData)
    });
  }

  recordPayment(paymentData) {
    return this.request('/payment/record', {
      method: 'POST',
      body: JSON.stringify(paymentData)
    });
  }
}

export const api = new ApiClient();

