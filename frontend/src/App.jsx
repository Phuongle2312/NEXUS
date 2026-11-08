import React, { useState } from 'react';
import Navbar from './components/Navbar.jsx';
import Footer from './components/Footer.jsx';

import PublicView from './views/PublicView.jsx';
import CustomerRegistrationView from './views/CustomerRegistrationView.jsx';
import CustomerPortalView from './views/CustomerPortalView.jsx';

import { INITIAL_ORDERS, INITIAL_CONNECTIONS, INITIAL_BILLS } from './data/mockData.js';

export default function App() {
  const [activeSection, setActiveSection] = useState('home'); // home | plans | register | tracking | feedback | portal
  const [orders, setOrders] = useState(INITIAL_ORDERS);
  const [connections, setConnections] = useState(INITIAL_CONNECTIONS);
  const [bills, setBills] = useState(INITIAL_BILLS);
  const [selectedPlanForRegistration, setSelectedPlanForRegistration] = useState(null);

  const handleRegisterFromPlan = (plan) => {
    setSelectedPlanForRegistration(plan);
    setActiveSection('register');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleOrderSubmitted = (newOrder) => {
    setOrders([newOrder, ...orders]);
  };

  const handleNavigation = (section) => {
    setActiveSection(section);
    if (section === 'plans') {
      setActiveSection('home');
      setTimeout(() => {
        const el = document.getElementById('plans-section');
        if (el) el.scrollIntoView({ behavior: 'smooth' });
      }, 100);
      return;
    }
    if (section === 'tracking') {
      setActiveSection('home');
      setTimeout(() => {
        const el = document.getElementById('tracking-section');
        if (el) el.scrollIntoView({ behavior: 'smooth' });
      }, 100);
      return;
    }
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Navbar activeSection={activeSection} onNavigate={handleNavigation} />

      <main className="app-container">
        {activeSection === 'home' && (
          <PublicView
            orders={orders}
            connections={connections}
            onRegisterClick={handleRegisterFromPlan}
          />
        )}

        {activeSection === 'register' && (
          <CustomerRegistrationView
            onOrderSubmitted={handleOrderSubmitted}
            preselectedPlan={selectedPlanForRegistration}
          />
        )}

        {activeSection === 'portal' && (
          <CustomerPortalView
            connections={connections}
            bills={bills}
            orders={orders}
          />
        )}

        {activeSection === 'feedback' && (
          <div style={{ maxWidth: '800px', margin: '0 auto' }}>
            <div className="nexus-card">
              <div className="nexus-card-header">
                <h2 className="nexus-card-title">💬 Trung Tâm Tiếp Nhận Ý Kiến & Khiếu Nại Khách Hàng</h2>
              </div>
              <p className="form-hint" style={{ marginBottom: '1.25rem' }}>
                Nexus cam kết lắng nghe mọi đóng góp của quý khách để nâng cao chất lượng dịch vụ viễn thông.
              </p>
              <form onSubmit={(e) => {
                e.preventDefault();
                alert('Cảm ơn quý khách! Ý kiến đóng góp đã được gửi tới Ban Giám đốc Nexus.');
                e.target.reset();
              }}>
                <div className="grid-2">
                  <div className="form-group">
                    <label className="form-label">Họ và tên <span className="req">*</span></label>
                    <input type="text" className="form-control" placeholder="Nguyễn Văn A" required />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Số điện thoại liên hệ <span className="req">*</span></label>
                    <input type="tel" className="form-control" placeholder="0987654321" required />
                  </div>
                </div>
                <div className="form-group">
                  <label className="form-label">Mã Account ID / Order ID (nếu có)</label>
                  <input type="text" className="form-control" placeholder="B001000000000001 hoặc B0000000001" />
                </div>
                <div className="form-group">
                  <label className="form-label">Nội dung góp ý hoặc khiếu nại <span className="req">*</span></label>
                  <textarea className="form-control" rows="4" placeholder="Vui lòng cung cấp chi tiết thông tin phản hồi của bạn..." required></textarea>
                </div>
                <button type="submit" className="btn btn-primary btn-lg">
                  Gửi Phản Hồi Cho Nexus
                </button>
              </form>
            </div>
          </div>
        )}
      </main>

      <Footer />
    </div>
  );
}
