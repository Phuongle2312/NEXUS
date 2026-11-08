import React from 'react';
import { Phone, User, Search, FileText, Send, Sparkles } from 'lucide-react';

export default function Navbar({ activeSection, onNavigate }) {
  return (
    <header className="main-navbar">
      <div className="navbar-container">
        <div className="brand-wrapper" onClick={() => onNavigate('home')} style={{ cursor: 'pointer' }}>
          <img
            src="https://coderent.vn/storage/settings/YfEwVsPubmDolpSAhDLDc5DKuDdRCZZccY3M9yfO.png"
            alt="Nexus Telecom - Service Marketing System"
            className="brand-logo-img"
          />
        </div>

        <nav>
          <ul className="nav-links">
            <li>
              <button
                className={`nav-link-btn ${activeSection === 'home' ? 'active' : ''}`}
                onClick={() => onNavigate('home')}
              >
                Trang Chủ
              </button>
            </li>
            <li>
              <button
                className={`nav-link-btn ${activeSection === 'plans' ? 'active' : ''}`}
                onClick={() => onNavigate('plans')}
              >
                Gói Cước & Dịch Vụ
              </button>
            </li>
            <li>
              <button
                className={`nav-link-btn ${activeSection === 'register' ? 'active' : ''}`}
                onClick={() => onNavigate('register')}
              >
                <Sparkles size={15} color="var(--primary)" />
                Đăng Ký Dịch Vụ
              </button>
            </li>
            <li>
              <button
                className={`nav-link-btn ${activeSection === 'tracking' ? 'active' : ''}`}
                onClick={() => onNavigate('tracking')}
              >
                <Search size={15} />
                Tra Cứu Đơn & Cước
              </button>
            </li>
            <li>
              <button
                className={`nav-link-btn ${activeSection === 'feedback' ? 'active' : ''}`}
                onClick={() => onNavigate('feedback')}
              >
                <Send size={15} />
                Góp Ý & Khiếu Nại
              </button>
            </li>
          </ul>
        </nav>

        <div className="header-actions">
          <a href="tel:19008888" className="btn btn-secondary btn-sm" style={{ textDecoration: 'none' }}>
            <Phone size={15} />
            1900 8888
          </a>
          <button
            className={`btn ${activeSection === 'portal' ? 'btn-primary' : 'btn-outline'} btn-sm`}
            onClick={() => onNavigate('portal')}
          >
            <User size={15} />
            Cổng Khách Hàng
          </button>
        </div>
      </div>
    </header>
  );
}
