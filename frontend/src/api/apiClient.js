/* ==========================================================================
   API CLIENT / SERVICE LAYER - NEXUS SYSTEM
   Gọi ASP.NET Core Web API (.NET 10) tại backend/src/Nexus.Api.

   Quy ước phản hồi của backend:
     thành công: { success: true,  data: ..., message: "OK" }
     lỗi:       { success: false, error: { code, message } }

   Token JWT lưu ở localStorage và gửi qua header Authorization.
   Đổi VITE_API_BASE_URL nếu backend chạy cổng khác.
   ========================================================================== */

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';
const TOKEN_KEY = 'nexus.accessToken';

class ApiError extends Error {
  constructor(code, message, status) {
    super(message);
    this.name = 'ApiError';
    this.code = code;
    this.status = status;
  }
}

class ApiClient {
  constructor() {
    this.baseUrl = API_BASE_URL.replace(/\/$/, '');
  }

  get token() {
    return localStorage.getItem(TOKEN_KEY);
  }

  setToken(token) {
    if (token) {
      localStorage.setItem(TOKEN_KEY, token);
    } else {
      localStorage.removeItem(TOKEN_KEY);
    }
  }

  /**
   * Gọi API và bóc lớp vỏ { success, data }.
   * Lỗi có mã nghiệp vụ được ném thành ApiError để UI hiển thị đúng thông báo.
   */
  async request(path, { method = 'GET', body, query, anonymous = false } = {}) {
    // baseUrl có thể là '/api' (gọi cùng origin qua Vite proxy) hoặc URL tuyệt đối.
    const origin = typeof window === 'undefined' ? 'http://localhost' : window.location.origin;
    const url = new URL(this.baseUrl + path, origin);
    if (query) {
      Object.entries(query).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          url.searchParams.set(key, value);
        }
      });
    }

    const headers = { Accept: 'application/json' };
    if (body !== undefined) {
      headers['Content-Type'] = 'application/json';
    }
    if (!anonymous && this.token) {
      headers.Authorization = `Bearer ${this.token}`;
    }

    let response;
    try {
      response = await fetch(url, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body)
      });
    } catch {
      throw new ApiError('NETWORK_ERROR', 'Không kết nối được tới máy chủ .NET. Kiểm tra backend đã chạy chưa.', 0);
    }

    const payload = await response.json().catch(() => null);

    if (!response.ok || payload?.success === false) {
      const error = payload?.error;
      throw new ApiError(error?.code || 'HTTP_ERROR', error?.message || `HTTP ${response.status}`, response.status);
    }

    return payload?.data;
  }

  // ------------------------------ Xác thực ------------------------------
  async login(username, password) {
    const result = await this.request('/auth/login', {
      method: 'POST',
      body: { username, password },
      anonymous: true
    });
    this.setToken(result.accessToken);
    return result;
  }

  async logout() {
    try {
      await this.request('/auth/logout', { method: 'POST' });
    } finally {
      this.setToken(null);
    }
  }

  changePassword(oldPassword, newPassword) {
    return this.request('/auth/change-password', { method: 'POST', body: { oldPassword, newPassword } });
  }

  // --------------------------- Danh mục công khai ---------------------------
  getPlans(type) {
    return this.request('/public/plans', { query: { type }, anonymous: true });
  }

  getCities() {
    return this.request('/public/cities', { anonymous: true });
  }

  getShops(cityId) {
    return this.request('/public/shops', { query: { cityId }, anonymous: true });
  }

  getDiscount(quantity) {
    return this.request('/public/discount', { query: { qty: quantity }, anonymous: true });
  }

  trackOrder(orderId) {
    return this.request('/public/track-order', { query: { orderId }, anonymous: true });
  }

  trackAccount(accountId) {
    return this.request('/public/track-account', { query: { accountId }, anonymous: true });
  }

  // ------------------------------ Khách hàng ------------------------------
  searchCustomers(q, page = 1, size = 20) {
    return this.request('/customers', { query: { q, page, size } });
  }

  createCustomer(customer) {
    return this.request('/customers', { method: 'POST', body: customer });
  }

  updateCustomer(id, customer) {
    return this.request(`/customers/${id}`, { method: 'PUT', body: customer });
  }

  issueCustomerAccount(id, username) {
    return this.request(`/customers/${id}/issue-account`, { method: 'POST', body: { username } });
  }

  // -------------------------------- Đơn --------------------------------
  createOrder(order) {
    return this.request('/orders', { method: 'POST', body: order });
  }

  getOrders(filters = {}) {
    return this.request('/orders', { query: filters });
  }

  getOrder(orderId) {
    return this.request(`/orders/${orderId}`);
  }

  // ------------------------------ Kỹ thuật ------------------------------
  startFeasibilityCheck(orderId) {
    return this.request('/tech/start-check', { method: 'POST', body: { orderId } });
  }

  recordFeasibility(payload) {
    return this.request('/tech/feasibility', { method: 'POST', body: payload });
  }

  createConnection(payload) {
    return this.request('/tech/connections', { method: 'POST', body: payload });
  }

  getConnections(filters = {}) {
    return this.request('/tech/connections', { query: filters });
  }

  getConnection(accountId) {
    return this.request(`/tech/connections/${accountId}`);
  }

  changeConnectionStatus(accountId, status, reason) {
    return this.request('/tech/connections/status', { method: 'POST', body: { accountId, status, reason } });
  }

  replaceEquipment(payload) {
    return this.request('/tech/equipment/replace', { method: 'POST', body: payload });
  }

  // ------------------------------- Hóa đơn -------------------------------
  previewBill(payload) {
    return this.request('/billing/preview', { method: 'POST', body: payload });
  }

  generateBill(payload) {
    return this.request('/billing/generate', { method: 'POST', body: payload });
  }

  updateBill(billId, payload) {
    return this.request(`/billing/${billId}`, { method: 'PUT', body: payload });
  }

  getBills(filters = {}) {
    return this.request('/billing', { query: filters });
  }

  getBill(billId) {
    return this.request(`/billing/${billId}`);
  }

  getOverdueBills(days) {
    return this.request('/billing/overdue', { query: { days } });
  }

  requestSuspend(accountId, reason) {
    return this.request(`/billing/${accountId}/request-suspend`, { method: 'POST', body: { reason } });
  }

  // ------------------------------ Thanh toán ------------------------------
  recordPayment(payload) {
    return this.request('/payments', { method: 'POST', body: payload });
  }

  getPayments(filters = {}) {
    return this.request('/payments', { query: filters });
  }

  getReceipt(paymentId) {
    return this.request(`/payments/${paymentId}/receipt`);
  }

  refundDeposit(accountId) {
    return this.request(`/payments/refund-deposit/${accountId}`, { method: 'POST' });
  }

  // --------------------------- Cổng khách hàng ---------------------------
  getMyProfile() {
    return this.request('/my/profile');
  }

  updateMyProfile(payload) {
    return this.request('/my/profile', { method: 'PUT', body: payload });
  }

  getMyOrders() {
    return this.request('/my/orders');
  }

  getMyConnections() {
    return this.request('/my/connections');
  }

  getMyBills() {
    return this.request('/my/bills');
  }

  getMyOrderRequests() {
    return this.request('/my/order-requests');
  }

  createOrderRequest(payload) {
    return this.request('/my/order-requests', { method: 'POST', body: payload });
  }

  sendFeedback(payload) {
    return this.request('/my/feedback', { method: 'POST', body: payload });
  }

  // ------------------------------ Quản trị ------------------------------
  getEmployees() {
    return this.request('/admin/employees');
  }

  createEmployee(payload) {
    return this.request('/admin/employees', { method: 'POST', body: payload });
  }

  updateEmployee(id, payload) {
    return this.request(`/admin/employees/${id}`, { method: 'PUT', body: payload });
  }

  deactivateEmployee(id) {
    return this.request(`/admin/employees/${id}/deactivate`, { method: 'POST' });
  }

  getAdminShops() {
    return this.request('/admin/shops');
  }

  createShop(payload) {
    return this.request('/admin/shops', { method: 'POST', body: payload });
  }

  updateShop(id, payload) {
    return this.request(`/admin/shops/${id}`, { method: 'PUT', body: payload });
  }

  getAdminCities() {
    return this.request('/admin/cities');
  }

  createCity(payload) {
    return this.request('/admin/cities', { method: 'POST', body: payload });
  }

  updateCity(id, payload) {
    return this.request(`/admin/cities/${id}`, { method: 'PUT', body: payload });
  }

  getAdminPlans(type) {
    return this.request('/admin/plans', { query: { type } });
  }

  createPlan(payload) {
    return this.request('/admin/plans', { method: 'POST', body: payload });
  }

  updatePlan(id, payload) {
    return this.request(`/admin/plans/${id}`, { method: 'PUT', body: payload });
  }

  getVendors() {
    return this.request('/admin/vendors');
  }

  createVendor(payload) {
    return this.request('/admin/vendors', { method: 'POST', body: payload });
  }

  getProducts() {
    return this.request('/admin/products');
  }

  createProduct(payload) {
    return this.request('/admin/products', { method: 'POST', body: payload });
  }

  adjustStock(productId, delta, note) {
    return this.request(`/admin/products/${productId}/stock`, { method: 'POST', body: { delta, note } });
  }

  getFeedback() {
    return this.request('/admin/feedback');
  }
}

export const api = new ApiClient();
export { ApiError };